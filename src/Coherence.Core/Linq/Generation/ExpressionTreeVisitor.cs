using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;
using Tangosol.Util;
using Tangosol.Util.Extractor;
using Tangosol.Util.Filter;

namespace Tangosol.Linq.Generation
{
    public class ExpressionTreeVisitor : ThrowingExpressionVisitor
    {
        public static IFilter GetFilter(Expression linqExpression)
        {
            var visitor = new ExpressionTreeVisitor();
            visitor.Visit(linqExpression);
            return visitor.GetFilter();
        }

        public static IValueExtractor GetExtractor(Expression linqExpression)
        {
            var visitor = new ExpressionTreeVisitor();
            visitor.Visit(linqExpression);
            return visitor.GetExtractor();
        }

        public static object GetDefaultValue(Expression linqExpression)
        {
            if (linqExpression == null)
            {
                return null;
            }

            var visitor = new ExpressionTreeVisitor();
            visitor.Visit(linqExpression);
            return visitor.GetDefaultValue();
        }

        private readonly Stack stack = new Stack();
        private readonly Stack<IValueExtractor> member = new Stack<IValueExtractor>();

        IFilter GetFilter()
        {
            IFilter filter = (IFilter) stack.Pop();
            return filter;
        }

        IValueExtractor GetExtractor()
        {
            IValueExtractor extractor = (stack.Count > 0) ? stack.Pop() as IValueExtractor : null;
            return extractor;
        }

        object GetDefaultValue()
        {
            // TODO: add check to allow only constants as a default value
            return stack.Pop();
        }

        protected override Expression VisitUnary(UnaryExpression expression)
        {
            if (expression.NodeType == ExpressionType.Convert)
            {
                Visit(expression.Operand);
            }
            else
            {
                throw new NotSupportedException("Not supported unary expression " + expression.NodeType);
            }

            return expression;
        }

        protected override Expression VisitBinary(BinaryExpression expression)
        {
            Visit(expression.Left);
            var left = stack.Pop();
            Visit(expression.Right);
            var right = stack.Pop();

            // in production code handle this via lookup tables
            switch (expression.NodeType)
            {
                case ExpressionType.Equal:
                {
                    if (left is IValueExtractor extractor)
                    {
                        stack.Push(new EqualsFilter(extractor, right));
                    }
                    else if (right is IValueExtractor extractorRight)
                    {
                        stack.Push(new EqualsFilter(extractorRight, left));
                    }
                    else
                    {
                        base.VisitBinary(expression);
                    }
                }
                    break;

                case ExpressionType.AndAlso:
                case ExpressionType.And:
                {
                    if (left is IFilter leftFilter && right is IFilter rightFilter)
                    {
                        stack.Push(new AndFilter(leftFilter, rightFilter));
                    }
                    else
                    {
                        base.VisitBinary(expression);
                    }
                }
                    break;

                case ExpressionType.OrElse:
                case ExpressionType.Or:
                    // TODO: replace with  AnyFilter and remove unnecessary nesting
                {
                    if (left is IFilter leftFilter && right is IFilter rightFilter)
                    {
                        stack.Push(new OrFilter(leftFilter, rightFilter));
                    }
                    else
                    {
                        base.VisitBinary(expression);
                    }
                }
                    break;

                case ExpressionType.NotEqual:
                {
                    if (left is IValueExtractor extractor)
                    {
                        stack.Push(new NotEqualsFilter(extractor, right));
                    }
                    else if (right is IValueExtractor extractorRight)
                    {
                        stack.Push(new NotEqualsFilter(extractorRight, left));
                    }
                    else
                    {
                        base.VisitBinary(expression);
                    }
                }

                    break;

                case ExpressionType.GreaterThanOrEqual:
                {
                    if (left is IValueExtractor extractor && right is IComparable comparable)
                    {
                        stack.Push(new GreaterEqualsFilter(extractor, comparable));
                    }
                    else if (left is IComparable comparable2 && right is IValueExtractor extractor2)
                    {
                        stack.Push(new LessEqualsFilter(extractor2, comparable2));
                    }
                    else
                    {
                        base.VisitBinary(expression);
                    }
                }
                    break;
                case ExpressionType.GreaterThan:
                {
                    if (left is IValueExtractor extractor && right is IComparable comparable)
                    {
                        stack.Push(new GreaterFilter(extractor, comparable));
                    }
                    else if (left is IComparable comparable2 && right is IValueExtractor extractor2)
                    {
                        stack.Push(new LessFilter(extractor2, comparable2));
                    }
                    else
                    {
                        base.VisitBinary(expression);
                    }
                }
                    break;

                case ExpressionType.LessThanOrEqual:
                {
                    if (left is IValueExtractor extractor && right is IComparable comparable)
                    {
                        stack.Push(new LessEqualsFilter(extractor, comparable));
                    }
                    else if (left is IComparable comparable2 && right is IValueExtractor extractor2)
                    {
                        stack.Push(new GreaterEqualsFilter(extractor2, comparable2));
                    }
                    else
                    {
                        base.VisitBinary(expression);
                    }
                }
                    break;
                case ExpressionType.LessThan:
                {
                    if (left is IValueExtractor extractor && right is IComparable comparable)
                    {
                        stack.Push(new LessFilter(extractor, comparable));
                    }
                    else if (right is IValueExtractor extractor2 && left is IComparable comparable2)
                    {
                        stack.Push(new GreaterFilter(extractor2, comparable2));
                    }
                    else
                    {
                        base.VisitBinary(expression);
                    }
                }
                    break;

                default:
                    base.VisitBinary(expression);
                    break;
            }

            return expression;
        }

        protected override Expression VisitQuerySourceReference(QuerySourceReferenceExpression expression)
        {
            if (member.Count > 0)
            {
                IValueExtractor[] extractors = member.ToArray();
                member.Clear();
                var extractor = new ChainedExtractor(extractors);
                stack.Push(extractor);
            }

            return expression;
        }

        protected override Expression VisitMember(MemberExpression expression)
        {
            var extractor = new ReflectionExtractor("get" + expression.Member.Name);
            member.Push(extractor);
            Visit(expression.Expression);

            return expression;
        }

        protected override Expression VisitConstant(ConstantExpression expression)
        {
            stack.Push(expression.Value);
            return expression;
        }

        // -------------------------------------------------------------------------------------

        protected override Exception CreateUnhandledItemException<T>(T unhandledItem, string visitMethod)
        {
            string itemText = FormatUnhandledItem(unhandledItem);
            var message = string.Format("The expression '{0}' (type: {1}) is not supported by this LINQ provider.",
                itemText, typeof(T));
            return new NotSupportedException(message);
        }

        private string FormatUnhandledItem<T>(T unhandledItem)
        {
            var itemAsExpression = unhandledItem as Expression;
            return itemAsExpression != null
                ? itemAsExpression.ToString()
                : unhandledItem.ToString();
        }
    }
}
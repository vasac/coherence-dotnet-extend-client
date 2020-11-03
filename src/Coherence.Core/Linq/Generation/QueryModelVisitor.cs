using System;
using System.Linq.Expressions;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ResultOperators;
using Tangosol.Util;

namespace Tangosol.Linq.Generation
{
    public class QueryModelVisitor : QueryModelVisitorBase
    {
        private readonly CohQuery query = new CohQuery();

        public static CohQuery GenerateQuery(QueryModel queryModel)
        {
            var visitor = new QueryModelVisitor();
            visitor.VisitQueryModel(queryModel);
            return visitor.query;
        }

        public override void VisitSelectClause(SelectClause selectClause, QueryModel queryModel)
        {
            IValueExtractor extractor = GetExtractor(selectClause.Selector);
            query.Select = extractor;
            base.VisitSelectClause(selectClause, queryModel);
        }

        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
        {
            base.VisitWhereClause(whereClause, queryModel, index);
            IFilter whereFilter = GetExpression(whereClause.Predicate);
            query.AddFilter(whereFilter);
        }

        public override void VisitAdditionalFromClause(AdditionalFromClause fromClause, QueryModel queryModel,
            int index)
        {
            throw new NotSupportedException("Additional 'from' clauses are not supported.");
        }

        public override void VisitResultOperator(ResultOperatorBase resultOperator, QueryModel queryModel, int index)
        {
            if (typeof(SumResultOperator).IsAssignableFrom(resultOperator.GetType()))
            {
                query.Aggregate = CohQuery.Aggregation.Sum;
            }
            else if (typeof(CountResultOperator).IsAssignableFrom(resultOperator.GetType()))
            {
                query.Aggregate = CohQuery.Aggregation.Count;
            }
            else if (typeof(MinResultOperator).IsAssignableFrom(resultOperator.GetType()))
            {
                query.Aggregate = CohQuery.Aggregation.Min;
            }
            else if (typeof(MaxResultOperator).IsAssignableFrom(resultOperator.GetType()))
            {
                query.Aggregate = CohQuery.Aggregation.Max;
            }
            else if (typeof(AverageResultOperator).IsAssignableFrom(resultOperator.GetType()))
            {
                query.Aggregate = CohQuery.Aggregation.Average;
            }
            else if (typeof(DistinctResultOperator).IsAssignableFrom(resultOperator.GetType()))
            {
                query.Aggregate = CohQuery.Aggregation.Distinct;
            }
            else if (typeof(DefaultIfEmptyResultOperator).IsAssignableFrom(resultOperator.GetType()))
            {
                DefaultIfEmptyResultOperator op = (DefaultIfEmptyResultOperator) resultOperator;
                query.DefaultValue = GetDefaultValue(op.OptionalDefaultValue);
            }
            else
            {
                var message = string.Format("Result operator '{0}' is not supported by this LINQ provider.",
                    resultOperator);
                throw new NotSupportedException(message);
            }

            base.VisitResultOperator(resultOperator, queryModel, index);
        }

        private IFilter GetExpression(Expression expression)
        {
            return ExpressionTreeVisitor.GetFilter(expression);
        }

        private IValueExtractor GetExtractor(Expression expression)
        {
            return ExpressionTreeVisitor.GetExtractor(expression);
        }

        private object GetDefaultValue(Expression expression)
        {
            return ExpressionTreeVisitor.GetDefaultValue(expression);
        }
    }
}
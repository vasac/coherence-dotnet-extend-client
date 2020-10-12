using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Remotion.Linq;
using Tangosol.Linq.Generation;
using Tangosol.Net;
using Tangosol.Net.Cache;
using Tangosol.Util;
using Tangosol.Util.Aggregator;
using Tangosol.Util.Filter;
using Tangosol.Util.Processor;

namespace Tangosol.Linq
{
    public class CohQueryExecutor : IQueryExecutor
    {
        private readonly INamedCache m_cache;

        public CohQueryExecutor(INamedCache cache)
        {
            m_cache = cache;
        }

        public T ExecuteScalar<T>(QueryModel queryModel)
        {
            IEnumerable<T> result = ExecuteCollection<T>(queryModel);
            return result.Single();
        }

        public T ExecuteSingle<T>(QueryModel queryModel, bool returnDefaultWhenEmpty)
        {
            var result = ExecuteCollection<T>(queryModel);
            return returnDefaultWhenEmpty
                ? result.SingleOrDefault()
                : result.Single();
        }

        public IEnumerable<T> ExecuteCollection<T>(QueryModel queryModel)
        {
            CohQuery query = QueryModelVisitor.GenerateQuery(queryModel);
            IFilter filter = query.Filters.Count > 0
                ? (IFilter) new AllFilter(query.Filters.ToArray())
                : new AlwaysFilter();
            IValueExtractor extractor = query.Select;
            IEntryProcessor processor = extractor != null
                ? new ExtractorProcessor(extractor)
                : null;
            CohQuery.Aggregation? resultOperator = query.Aggregate;

            INamedCache cache = m_cache;
            {
                if (resultOperator == null)
                {
                    if (processor == null)
                    {
                        return cache.GetValues(filter).Cast<T>();
                    }

                    var results = cache.InvokeAll(filter, processor);
                    return results.Values.Cast<T>();
                }

                IEntryAggregator aggregator = GetAggregator<T>(resultOperator, extractor);
                var aggregationResult = cache.Aggregate(filter, aggregator);
                if (aggregationResult == null)
                {
                    if (query.DefaultIfEmpty)
                    {
                        if (query.DefaultValue == null)
                        {
                            return new List<T> {default};
                        }

                        return new List<T> {(T) Convert.ChangeType(query.DefaultValue, typeof(T))};
                    }

                    return new List<T>();
                }

                if (aggregationResult.GetType().IsArray)
                {
                    return ((ICollection) aggregationResult).Cast<T>();
                }

                if (aggregationResult is ICollection)
                {
                    return ((ICollection) aggregationResult).Cast<T>();
                }

                var result = new List<T>();
                result.Add((T) aggregationResult);
                return result;
            }
        }

        private IEntryAggregator GetAggregator<T>(CohQuery.Aggregation? aggregation, IValueExtractor extractor)
        {
            if (aggregation == null)
            {
                return null;
            }

            Type type = typeof(T);
            if (CohQuery.Aggregation.Count == aggregation)
            {
                return new Count();
            }

            if (CohQuery.Aggregation.Sum == aggregation)
            {
                switch (type)
                {
                    case Type _ when type == typeof(byte):
                        return extractor == null ? new LongSum() : new LongSum(extractor);
                    case Type _ when type == typeof(short):
                        return extractor == null ? new LongSum() : new LongSum(extractor);
                    case Type _ when type == typeof(int):
                        return extractor == null ? new LongSum() : new LongSum(extractor);
                    case Type _ when type == typeof(long):
                        return extractor == null ? new LongSum() : new LongSum(extractor);
                    case Type _ when type == typeof(float):
                        return extractor == null ? new DoubleSum() : new DoubleSum(extractor);
                    case Type _ when type == typeof(double):
                        return extractor == null ? new DoubleSum() : new DoubleSum(extractor);
                    case Type _ when type == typeof(decimal):
                        return extractor == null ? new DecimalSum() : new DecimalSum(extractor);
                }
            }
            else if (CohQuery.Aggregation.Min == aggregation)
            {
                switch (type)
                {
                    case Type _ when type == typeof(byte): return new LongMin(extractor);
                    case Type _ when type == typeof(short): return new LongMin(extractor);
                    case Type _ when type == typeof(int): return new LongMin(extractor);
                    case Type _ when type == typeof(long): return new LongMin(extractor);
                    case Type _ when type == typeof(float): return new DoubleMin(extractor);
                    case Type _ when type == typeof(double): return new DoubleMin(extractor);
                    case Type _ when type == typeof(decimal): return new DecimalMin(extractor);
                    case Type _ when type == typeof(IComparable): return new ComparableMin(extractor);
                }
            }
            else if (CohQuery.Aggregation.Max == aggregation)
            {
                switch (type)
                {
                    case Type _ when type == typeof(byte): return new LongMax(extractor);
                    case Type _ when type == typeof(short): return new LongMax(extractor);
                    case Type _ when type == typeof(int): return new LongMax(extractor);
                    case Type _ when type == typeof(long): return new LongMax(extractor);
                    case Type _ when type == typeof(float): return new DoubleMax(extractor);
                    case Type _ when type == typeof(double): return new DoubleMax(extractor);
                    case Type _ when type == typeof(decimal): return new DecimalMax(extractor);
                    case Type _ when type == typeof(IComparable): return new ComparableMax(extractor);
                }
            }
            else if (CohQuery.Aggregation.Average == aggregation)
            {
                switch (type)
                {
                    case Type _ when type == typeof(byte): return new DoubleAverage(extractor);
                    case Type _ when type == typeof(short): return new DoubleAverage(extractor);
                    case Type _ when type == typeof(int): return new DoubleAverage(extractor);
                    case Type _ when type == typeof(long): return new DoubleAverage(extractor);
                    case Type _ when type == typeof(float): return new DoubleAverage(extractor);
                    case Type _ when type == typeof(double): return new DoubleAverage(extractor);
                    case Type _ when type == typeof(decimal): return new DecimalAverage(extractor);
                }
            }
            else if (CohQuery.Aggregation.Distinct == aggregation)
            {
                return new DistinctValues(extractor);
            }

            throw new NotSupportedException("Not supported aggregation type: " + aggregation + " for " + type);
        }
    }
}
using System.Collections.Generic;
using Tangosol.Util;

namespace Tangosol.Linq.Generation
{
    public class CohQuery
    {
        public enum Aggregation
        {
            Count,
            Sum,
            Min,
            Max,
            Average,
            Distinct
        }

        #region Properties

        public IValueExtractor Select { get; set; }

        public Aggregation? Aggregate { get; set; }

        public IList<IFilter> Filters { get; } = new List<IFilter>();

        public bool DefaultIfEmpty { get; private set; }

        public object DefaultValue
        {
            get => m_defaultValue;
            set
            {
                m_defaultValue = value;
                DefaultIfEmpty = true;
            }
        }

        #endregion

        public void AddFilter(IFilter filter)
        {
            Filters.Add(filter);
        }

        #region Data members

        private object m_defaultValue;

        #endregion
    }
}
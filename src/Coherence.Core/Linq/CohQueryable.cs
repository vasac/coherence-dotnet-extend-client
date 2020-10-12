using System.Linq;
using System.Linq.Expressions;
using Remotion.Linq;
using Remotion.Linq.Parsing.Structure;
using Tangosol.Net;

namespace Tangosol.Linq
{
    public class CohQueryable<T> : QueryableBase<T>
    {
        private static IQueryExecutor CreateExecutor(INamedCache cache)
        {
            return new CohQueryExecutor(cache);
        }

        public CohQueryable(INamedCache cache)
            : base(QueryParser.CreateDefault(), CreateExecutor(cache))
        {
        }

        // This is an infrastructure constructor
        public CohQueryable(IQueryProvider provider, Expression expression) :
            base(provider, expression)
        {
        }
    }
}
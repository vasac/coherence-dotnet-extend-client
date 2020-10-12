using Tangosol.Net;

namespace Tangosol.Linq
{
    public static class LinqExtensions
    {
        public static CohQueryable<T> AsQueriable<T>(this INamedCache cache)
        {
            return new CohQueryable<T>(cache);
        }
    }
}
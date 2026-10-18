using Common.Stock2;
using System.Reflection;

namespace Service.Service
{
    /// <summary>In-memory sorting and paging. Sorting is limited to real public properties of the row type (no injection risk).</summary>
    public static class StockPaging2
    {
        public static PagedResult2<T> ToPage<T>(IEnumerable<T> source, StockQueryFilter2 filter, string defaultSortBy, bool defaultDescending = false)
        {
            var list = source.ToList();

            string? sortBy = filter.SortBy;
            bool descending = filter.SortDescending;

            PropertyInfo? prop = FindProperty<T>(sortBy);
            if (prop == null)
            {
                prop = FindProperty<T>(defaultSortBy);
                descending = defaultDescending;
            }

            if (prop != null)
            {
                var p = prop;
                list = descending
                    ? list.OrderByDescending(x => p.GetValue(x), Comparer<object?>.Default).ToList()
                    : list.OrderBy(x => p.GetValue(x), Comparer<object?>.Default).ToList();
            }

            int pageSize = filter.PageSize <= 0 ? 20 : filter.PageSize;
            int page = filter.Page <= 0 ? 1 : filter.Page;
            long skip = (long)(page - 1) * pageSize;

            return new PagedResult2<T>
            {
                TotalCount = list.Count,
                Page = page,
                PageSize = pageSize,
                Items = skip >= list.Count
                    ? new List<T>()
                    : list.Skip((int)skip).Take(pageSize).ToList()
            };
        }

        private static PropertyInfo? FindProperty<T>(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return typeof(T).GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        }
    }
}

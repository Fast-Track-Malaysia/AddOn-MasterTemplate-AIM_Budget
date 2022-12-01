using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    static class DataTableExtensions
    {
        public static void SecureQuery(this SAPbouiCOM.DataTable dt, string query)
        {
            SQLQuery.SecureQuery(query, () => dt.ExecuteQuery(query));
        }

        private static List<IDictionary<string, object>> GetEnumerable(this SAPbouiCOM.DataTable dt)
        {
            List<IDictionary<string, object>> enumerable = new List<IDictionary<string, object>>();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var dict = new Dictionary<string, object>();

                foreach (var col in dt.Columns.OfType<SAPbouiCOM.DataColumn>())
                {
                    dict.Add(col.Name, col.Cells.Item(i).Value);
                }

                enumerable.Add(dict);
            }

            return enumerable;
        }

        public static IEnumerable<T> Select<T>(this SAPbouiCOM.DataTable dt, Func<IDictionary<string, object>, T> selector) => dt.GetEnumerable().Select(each => selector(each));
        public static IEnumerable<IDictionary<string, object>> Where(this SAPbouiCOM.DataTable dt, Func<IDictionary<string, object>, bool> predicament) => dt.GetEnumerable().Where(each => predicament(each));
        public static IDictionary<string, object> SingleOrDefault<T>(this SAPbouiCOM.DataTable dt, Func<IDictionary<string, object>, bool> selector) => dt.GetEnumerable().SingleOrDefault(each => selector(each));
        public static void ForEach(this SAPbouiCOM.DataTable dt, Action<IDictionary<string, object>> action) => dt.GetEnumerable().ForEach(each => action(each));
    }
}

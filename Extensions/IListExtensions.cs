using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    public static class IListExtensions
    {
        /// <summary>
        /// Remove only first element that returns true. Use RemoveWhere to remove any element that returns true
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="predicament"></param>
        /// <returns></returns>
        public static IList<T> RemoveIf<T>(this IList<T> list, Func<T, bool> predicament)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (!predicament(list[i])) continue;

                list.RemoveAt(i);
                break;
            }

            return list;
        }

        /// <summary>
        /// Remove any element that returns true. Use RemoveIf to remove first element that returns true
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="predicament">remove when true</param>
        /// <returns></returns>
        public static IList<T> RemoveWhere<T>(this IList<T> list, Func<T, bool> predicament)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (!predicament(list[i])) continue;

                list.RemoveAt(i);
                --i;
            }

            return list;
        }
    }
}

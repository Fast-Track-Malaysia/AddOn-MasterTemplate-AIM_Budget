using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace FT_ADDON
{
    static class IEnumberableExtensions
    {
        public static bool Contains<TSource>(this IEnumerable sources, Func<TSource, object> selector, object value) => Contains(sources as IEnumerable<TSource>, selector, value);
        public static bool Contains<TSource>(this IEnumerable<TSource> sources, Func<TSource, object> selector, object value) => sources.Select(selector).Where(each => each == value).Any();
        public static bool Contains<TSource>(this IEnumerable sources, Func<TSource, string> selector, string value) => Contains(sources as IEnumerable<TSource>, selector, value);
        public static bool Contains<TSource>(this IEnumerable<TSource> sources, Func<TSource, string> selector, string value) => sources.Select(selector).Where(each => each == value).Any();
        public static bool Contains<TSource>(this IEnumerable sources, Func<TSource, int> selector, int value) => Contains(sources as IEnumerable<TSource>, selector, value);
        public static bool Contains<TSource>(this IEnumerable<TSource> sources, Func<TSource, int> selector, int value) => sources.Select(selector).Where(each => each == value).Any();
        public static bool Contains<TSource>(this IEnumerable sources, Func<TSource, bool> selector, bool value) => Contains(sources as IEnumerable<TSource>, selector, value);
        public static bool Contains<TSource>(this IEnumerable<TSource> sources, Func<TSource, bool> selector, bool value) => sources.Select(selector).Where(each => each == value).Any();
        public static bool Contains<TSource>(this IEnumerable sources, Func<TSource, double> selector, double value) => Contains(sources as IEnumerable<TSource>, selector, value);
        public static bool Contains<TSource>(this IEnumerable<TSource> sources, Func<TSource, double> selector, double value) => sources.Select(selector).Where(each => each == value).Any();
        public static bool Contains<TSource>(this IEnumerable sources, Func<TSource, float> selector, float value) => Contains(sources as IEnumerable<TSource>, selector, value);
        public static bool Contains<TSource>(this IEnumerable<TSource> sources, Func<TSource, float> selector, float value) => sources.Select(selector).Where(each => each == value).Any();
        public static bool IfAny<TSource>(this IEnumerable sources, Func<TSource, bool> predicament) => IfAny(sources as IEnumerable<TSource>, predicament);
        public static bool IfAny<TSource>(this IEnumerable<TSource> sources, Func<TSource, bool> predicament)
        {
            foreach (TSource each in sources)
            {
                if (predicament(each)) return true;
            }

            return false;
        }
        public static bool IfAll<TSource>(this IEnumerable sources, Func<TSource, bool> predicament) => IfAll(sources as IEnumerable<TSource>, predicament);
        public static bool IfAll<TSource>(this IEnumerable<TSource> sources, Func<TSource, bool> predicament)
        {
            if (sources.Count() == 0) return false;

            foreach (TSource each in sources)
            {
                if (!predicament(each)) return false;
            }

            return true;
        }
    }
}

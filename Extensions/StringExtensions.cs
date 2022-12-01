using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FT_ADDON
{
    static class StringExtensions
    {
        const string space = " ";

        // Add space between uppercase and lowercase
        public static string NaturalSpacing(this string str)
        {
            StringBuilder sb = new StringBuilder();
            return str.Select(c => sb.Append(char.IsLower(c) || sb.Length == 0 ? c.ToString() : space + c.ToString())).Last().ToString();
        }

        public static string FirstCharToUpper(this string input)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLower());
        }

        public static string SqlFormat(this string query, object obj)
        {
            if (obj == null) return query;

            var proplist = obj.GetType().GetProperties().ToArray();

            for (int index = 0; index < proplist.Count(); index++)
            {
                Regex rgx = new Regex($"(?<![a-zA-Z])@{ proplist[index].Name }(?=[^a-zA-Z]|$)");
                query = rgx.Replace(query, $"{{{index}}}");
            }

            return query.SqlFmt(proplist.Select(prop => prop.GetValue(obj)).ToArray());
        }
    }
}

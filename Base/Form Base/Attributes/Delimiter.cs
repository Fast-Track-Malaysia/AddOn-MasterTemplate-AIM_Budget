using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    class DelimiterAttribute : Attribute
    {
        string delimiter;

        protected DelimiterAttribute(string _delimiter)
        {
            delimiter = _delimiter;
        }

        public static implicit operator string(DelimiterAttribute delimiter) => delimiter.delimiter;
    }
}

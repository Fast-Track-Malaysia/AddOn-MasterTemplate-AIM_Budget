using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    abstract partial class Form_Base
    {
        [AttributeUsage(AttributeTargets.Class)]
        internal protected class FormOrder : Attribute
        {
            public int order { get; set; } = 0;

            public FormOrder(int _order)
            {
                order = _order;
            }

            public static implicit operator int(FormOrder fo) => fo.order;
        }
    }

    static class FormOrderExtensions
    {
        public static int GetFormOrder(this Type type)
        {
            var order = type.GetCustomAttribute<Form_Base.FormOrder>();
            return order == null ? 0 : order;
        }
    }
}

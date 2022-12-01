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
       internal protected class NoForm : Attribute
        {
        }
    }

    static class NoFormExtensions
    {
        public static Form_Base.NoForm GetNoForm(this Type type)
        {
            return type.GetCustomAttribute<Form_Base.NoForm>();
        }
    }
}

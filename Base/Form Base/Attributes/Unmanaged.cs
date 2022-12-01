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
        internal protected class Unmanaged : Attribute
        {
        }
    }

    static class UnmanagedExtensions
    {
        public static Form_Base.Unmanaged GetUnmanaged(this Type type)
        {
            return type.GetCustomAttribute<Form_Base.Unmanaged>();
        }
    }
}

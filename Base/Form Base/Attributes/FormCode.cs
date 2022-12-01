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
        internal protected class FormCode : Attribute
        {
            private string innerCode { get; set; }

            public FormCode(string _code)
            {
                innerCode = _code;
            }

            public static implicit operator string(FormCode fc) => fc.innerCode;
        }
    }

    static class FormCodeExtensions
    {
        public static string GetFormCode(this Type type)
        {
            var code = type.GetCustomAttribute<Form_Base.FormCode>();

            if (code != null) return code;

            // default Get from class name
            return new Form_Base.FormCode(type.Namespace.Substring(type.BaseType.Namespace.Length + 1));
        }
    }
}

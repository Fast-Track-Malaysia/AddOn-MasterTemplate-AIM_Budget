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
        internal protected class FileName : Attribute
        {
            public string file { get; set; }

            public FileName(string _file)
            {
                file = _file;
            }

            public static implicit operator string(FileName fn) => fn.file;
        }
    }

    static class FileNameExtensions
    {
        public static Form_Base.FileName GetFileName(this Type type)
        {
            var file = type.GetCustomAttribute<Form_Base.FileName>();

            if (file != null) return file;

            // default Get from class name
            return new Form_Base.FileName(type.Name + ".xml");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static FT_ADDON.Form_Base;

namespace FT_ADDON
{
    abstract partial class Form_Base
    {
        [AttributeUsage(AttributeTargets.Class)]
        internal protected class MenuName : Attribute
        {
            private string name { get; set; }

            public MenuName(string _name)
            {
                name = _name;
            }

            public static implicit operator string(MenuName mi) => mi.name;
        }
    }

    static class MenuNameExtensions
    {
        public static Form_Base.MenuName GetMenuName(this Type type)
        {
            var id = type.GetCustomAttribute<MenuName>();

            if (id != null) return id;

            // default no menu id
            string str = type.GetFormCode();
            return new Form_Base.MenuName(str.Replace("_", " "));
        }
    }
}

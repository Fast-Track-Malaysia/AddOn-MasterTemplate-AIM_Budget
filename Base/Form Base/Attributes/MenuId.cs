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
        internal protected class MenuId : Attribute
        {
            public string id { get; private set; }
            public int pos { get; private set; }

            /// <summary>
            /// Menu button of this form
            /// </summary>
            /// <param name="_id">Uid of the SAP module</param>
            /// <param name="_pos">position of the menu</param>
            public MenuId(string _id, int _pos = -1)
            {
                id = _id;
                pos = _pos;
            }

            public static implicit operator string(MenuId mi) => mi.id;
        }
    }

    static class MenuIdExtensions
    {
        public static Form_Base.MenuId GetMenuId(this Type type)
        {
            var id = type.GetCustomAttribute<Form_Base.MenuId>();

            if (id != null) return id;

            // default no menu id
            return new Form_Base.MenuId("");
        }

        public static bool HasMenuId(this Type type)
        {
            return type.GetCustomAttribute<Form_Base.MenuId>() != null;
        }
    }
}

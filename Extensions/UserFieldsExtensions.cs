using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    static class UserFieldsExtensions
    {
        public static object GetValue(this SAPbobsCOM.UserFields userFields, object index)
        {
            return userFields.Fields.Item(index).Value;
        }
    }
}

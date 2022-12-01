using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    static class UserTablesExtensions
    {
        public static bool HasUserTable(this SAPbobsCOM.UserTables udts, string name)
        {
            return udts.Contains<SAPbobsCOM.UserTable>(udt => udt.Name, name);
        }

        public static bool TryGetUserTable(this SAPbobsCOM.UserTables udts, string name, out SAPbobsCOM.UserTable udt)
        {
            var list = udts.OfType<SAPbobsCOM.UserTable>().Where(ut => ut.Name == name);

            if (!list.Any())
            {
                udt = null;
                return false;
            }

            udt = list.FirstOrDefault();
            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON.Example
{
    class ExampleAddOnSettings : AddOnSettings
    {
        public override bool Setup()
        {
            //UserTable udt = new UserTable("SQLQuery", "Query Table");

            //if (!udt.createField("Query", "Query", SAPbobsCOM.BoFieldTypes.db_Memo, 254, "", false, SAPbobsCOM.BoFldSubTypes.st_None)) return false;

            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    class FormVariables : AddOnSettings
    {
        public const string TableName = "@FT_VARIABLES";
        const string RegisterTableName = "FT_VARIABLES";

        public override bool Setup()
        {
            UserTable udt = new UserTable(RegisterTableName, "Add-on variables");

            if (!udt.createField("DftValue", "Default Value", SAPbobsCOM.BoFieldTypes.db_Alpha, 254, "", false, SAPbobsCOM.BoFldSubTypes.st_None)) return false;
            if (!udt.createField("Query", "Query", SAPbobsCOM.BoFieldTypes.db_Memo, 254, "", false, SAPbobsCOM.BoFldSubTypes.st_None)) return false;

            return true;
        }
    }
}

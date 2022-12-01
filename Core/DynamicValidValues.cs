using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FT_ADDON
{
    class DynamicValidValues : AddOnSettings
    {
        public const string TableName = "DYNAVALIDVAL";

        public override bool Setup()
        {
            UserTable udt = new UserTable(TableName, "Dynamic Valid Values");

            if (!udt.createField("XML", "XML", fieldtype: SAPbobsCOM.BoFieldTypes.db_Memo)) return false;

            return true;
        }

        public static bool TryAddValidValues(string key, SAPbouiCOM.ValidValues vvlist)
        {
            if (!TryGetValidValues(key, out var xml)) return false;

            vvlist.LoadFromXml(xml);
            return true;
        }

        public static bool TryGetValidValues(string key, out string xml)
        {
            xml = null;

            using (RecordSet rc = new RecordSet())
            {
                rc.DoQuery($"SELECT \"U_XML\" FROM \"@{ TableName }\" WHERE \"Code\"='{ key }'");

                if (rc.RecordCount == 0) return false;

                xml = rc.GetValue(0).ToString();
                return true;
            }
        }
    }
}

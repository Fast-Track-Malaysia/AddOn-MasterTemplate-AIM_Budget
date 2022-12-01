using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FT_ADDON
{
    class ChooseFromListCondition : AddOnSettings
    {
        public const string TableName = "CFLCOND";

        public override bool Setup()
        {
            UserTable udt = new UserTable(TableName, "Choose From List Condition");

            if (!udt.createField("XML", "XML", fieldtype: SAPbobsCOM.BoFieldTypes.db_Memo)) return false;

            return true;
        }

        public static bool TryGetConditions(string key, out SAPbouiCOM.Conditions conditions)
        {
            conditions = null;

            using (RecordSet rc = new RecordSet())
            {
                rc.DoQuery($"SELECT * FROM \"@{ TableName }\" WHERE \"Code\"='{ key }'");

                if (rc.RecordCount == 0) return false;

                conditions = (SAPbouiCOM.Conditions)SAP.SBOApplication.CreateObject(SAPbouiCOM.BoCreatableObjectType.cot_Conditions);
                string xml = rc.GetValue("U_XML").ToString();

                try
                {
                    conditions.LoadFromXML(xml);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
    }
}

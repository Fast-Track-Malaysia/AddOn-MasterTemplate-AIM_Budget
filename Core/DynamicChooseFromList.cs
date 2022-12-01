using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    class DynamicChooseFromList
    {
        public string alias { get; set; }
        public SAPbouiCOM.ChooseFromListCreationParams parameters { get; set; }

        public const string TableName = "DYNACFL";
        const string objtype = "ObjType";
        const string multiselect = "MultSel";
        const string aliasstr = "Alias";

        class DynamicChooseFromListSettings : AddOnSettings
        {
            public override bool Setup()
            {
                UserTable udt = new UserTable(TableName, "Dynamic Choose From List");

                if (!udt.createField(objtype, "Object Type", fieldsize: 50)) return false;
                if (!udt.createField(multiselect, "Multi Selection", fieldsize: 1, defaultvalue: "N", validvalue: "Y:Yes|N:No")) return false;
                if (!udt.createField(aliasstr, "Alias", fieldsize: 50)) return false;

                return true;
            }
        }

        public static bool TryGetChooseFromList(string key, out DynamicChooseFromList dcfl)
        {
            dcfl = null;

            using (RecordSet rc = new RecordSet())
            {
                rc.DoQuery($"SELECT * FROM \"@{ TableName }\" WHERE \"Code\"='{ key }'");

                if (rc.RecordCount == 0) return false;

                dcfl = new DynamicChooseFromList();
                dcfl.alias = rc.GetValue($"U_{ aliasstr }").ToString();
                dcfl.parameters = (SAPbouiCOM.ChooseFromListCreationParams)SAP.SBOApplication.CreateObject(SAPbouiCOM.BoCreatableObjectType.cot_ChooseFromListCreationParams);
                dcfl.parameters.ObjectType = rc.GetValue($"U_{ objtype }").ToString();
                dcfl.parameters.MultiSelection = rc.GetValue($"U_{ multiselect }").ToString() == "Y" ? true : false;
                dcfl.parameters.UniqueID = rc.GetValue("Name").ToString();
                return true;
            }
        }
    }
}

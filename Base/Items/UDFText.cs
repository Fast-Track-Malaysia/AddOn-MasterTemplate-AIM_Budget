using System;

namespace FT_ADDON
{
    class UDFText
    {
        private SAPbouiCOM.EditText txt = null;
        private SAPbouiCOM.ComboBox cb = null;

        bool hasvv;

        public UDFText(string id, SAPbouiCOM.Form oForm)
        {
            var hash = CRC32.Compute(id).ToString();
            string tablename = oForm.DataSources.DBDataSources.Item(0).TableName;
            hasvv = HasValidValue(id.StartsWith("U_") ? id.Substring(2) : id, tablename);

            if (hasvv)
            {
                try
                {
                    cb = oForm.Items.Item(hash).Specific as SAPbouiCOM.ComboBox;
                }
                catch (Exception)
                {
                    cb = oForm.Items.Add(hash, SAPbouiCOM.BoFormItemTypes.it_COMBO_BOX).Specific as SAPbouiCOM.ComboBox;
                    cb.Item.Width = 1;
                    cb.Item.Height = 1;
                    cb.DataBind.SetBound(true, tablename, id);
                }

                return;
            }

            try
            {
                txt = oForm.Items.Item(hash).Specific as SAPbouiCOM.EditText;
            }
            catch (Exception)
            {
                txt = oForm.Items.Add(hash, SAPbouiCOM.BoFormItemTypes.it_EDIT).Specific as SAPbouiCOM.EditText;
                txt.Item.Width = 1;
                txt.Item.Height = 1;
                txt.DataBind.SetBound(true, oForm.DataSources.DBDataSources.Item(0).TableName, id);
            }
        }

        public string value
        {
            get
            {
                return hasvv ? cb.Value : txt.Value;
            }
            set
            {
                if (hasvv)
                {
                    cb.Select(value, SAPbouiCOM.BoSearchKey.psk_ByValue);
                    return;
                }

                txt.Value = value;
            }
        }

        static bool HasValidValue(string id, string table)
        {
            string query = $"SELECT \"B\".\"FldValue\" FROM \"CUFD\" \"A\" INNER JOIN \"UFD1\" \"B\" ON \"A\".\"TableID\"=\"B\".\"TableID\" AND \"A\".\"FieldID\"=\"B\".\"FieldID\" " +
                           $"WHERE \"A\".\"TableID\"='{ table }' AND \"A\".\"AliasID\"='{ id }'";
            SAPbobsCOM.Recordset rc = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            rc.DoQuery(query);
            return rc.RecordCount > 0;
        }
    }
}

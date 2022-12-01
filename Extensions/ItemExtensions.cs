using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    static class ItemExtensions
    {
        public static SAPbouiCOM.Column GetMatrixColumn(this SAPbouiCOM.Item itm, string column)
        {
            return (itm.Specific as SAPbouiCOM.Matrix).Columns.Item(column);
        }
        
        public static SAPbouiCOM.GridColumn GetGridColumn(this SAPbouiCOM.Item itm, string column)
        {
            return (itm.Specific as SAPbouiCOM.Grid).Columns.Item(column);
        }

        public static string GetTableName(this SAPbouiCOM.Item itm)
        {
            try
            {
                return GetDataBindProperty(itm, "TableName").ToString();
            }
            catch (Exception)
            {
            }

            try
            {
                return GetDataTableProperty(itm, "UniqueID").ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string GetAlias(this SAPbouiCOM.Item itm)
        {
            try
            {
                return GetDataBindProperty(itm, "Alias").ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static object GetValue(this SAPbouiCOM.Item itm)
        {
            try
            {
                var obj = itm.Specific;

                switch (itm.Type)
                {
                    case SAPbouiCOM.BoFormItemTypes.it_EDIT:
                    case SAPbouiCOM.BoFormItemTypes.it_EXTEDIT:
                        return (itm.Specific as SAPbouiCOM.EditText).Value;
                    case SAPbouiCOM.BoFormItemTypes.it_COMBO_BOX:
                        return (itm.Specific as SAPbouiCOM.ComboBox).Selected.Value;
                    case SAPbouiCOM.BoFormItemTypes.it_CHECK_BOX:
                        return (itm.Specific as SAPbouiCOM.CheckBox).Checked;
                }

                Type type = obj.GetSAPType();
                return type.GetProperty("Value").GetValue(obj);
            }
            catch (Exception)
            {
                return null;
            }
        }
        
        public static void SetValue(this SAPbouiCOM.Item itm, object value)
        {
            var obj = itm.Specific;

            switch (itm.Type)
            {
                case SAPbouiCOM.BoFormItemTypes.it_EDIT:
                case SAPbouiCOM.BoFormItemTypes.it_EXTEDIT:
                    (itm.Specific as SAPbouiCOM.EditText).Value = value.ToString();
                    return;
                case SAPbouiCOM.BoFormItemTypes.it_COMBO_BOX:
                    (itm.Specific as SAPbouiCOM.ComboBox).Select(value, SAPbouiCOM.BoSearchKey.psk_ByValue);
                    return;
                case SAPbouiCOM.BoFormItemTypes.it_CHECK_BOX:
                    (itm.Specific as SAPbouiCOM.CheckBox).Checked = Convert.ToBoolean(value);
                    return;
            }

            Type type = obj.GetSAPType();
            type.GetProperty("Value").GetValue(obj);
        }

        private static object GetDataBindProperty(SAPbouiCOM.Item itm, string propertyname)
        {
            var obj = itm.Specific;
            Type type = obj.GetSAPType();
            obj = type.GetProperty("DataBind").GetValue(obj);
            type = obj.GetSAPType();
            return type.GetProperty(propertyname).GetValue(obj);
        }

        private static object GetDataTableProperty(SAPbouiCOM.Item itm, string propertyname)
        {
            var obj = itm.Specific;
            Type type = obj.GetSAPType();
            obj = type.GetProperty("DataTable").GetValue(obj);
            type = obj.GetSAPType();
            return type.GetProperty(propertyname).GetValue(obj);
        }
    }
}

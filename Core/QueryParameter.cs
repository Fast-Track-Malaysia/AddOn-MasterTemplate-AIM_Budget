using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    static class QueryParameter
    {
        public static string GetValueFromParameters(SAPbouiCOM.Form oForm, string param1, string param2, int row)
        {
            string value = _GetValueFromParameters(oForm, param1, param2, row);
            return value == String.Empty ? "null" : value;
        }

        private static string _GetValueFromParameters(SAPbouiCOM.Form oForm, string param1, string param2, int row)
        {
            if (String.IsNullOrEmpty(param1))
            {
                if (oForm.HasUserSource(param2)) return oForm.GetUserSourceValue(param2, true);
            }

            SAPbouiCOM.DataTable dt = null;
            SAPbouiCOM.Item itm = null;
            string table = param1;
            string alias = param2;
            string value = String.Empty;

            if (oForm.TryGetItem(param1, out itm))
            {
                switch (itm.Type)
                {
                    case SAPbouiCOM.BoFormItemTypes.it_MATRIX:
                        GetMatrixInfo(itm, param2, row, out value, out table, out alias);
                        break;
                    case SAPbouiCOM.BoFormItemTypes.it_COMBO_BOX:
                        GetObjectUIInfo(itm, out value, out table, out alias);
                        break;
                    case SAPbouiCOM.BoFormItemTypes.it_EDIT:
                    case SAPbouiCOM.BoFormItemTypes.it_EXTEDIT:
                        GetObjectUIInfo(itm, out value, out table, out alias);
                        break;
                    case SAPbouiCOM.BoFormItemTypes.it_CHECK_BOX:
                        GetCheckBoxInfo(itm, out value, out table, out alias);
                        break;
                    case SAPbouiCOM.BoFormItemTypes.it_GRID:
                        GetGridInfo(itm, param2, out table, out alias);
                        break;
                }
            }
            else if (oForm.TryGetItem(param2, out itm))
            {
                switch (itm.Type)
                {
                    case SAPbouiCOM.BoFormItemTypes.it_COMBO_BOX:
                        GetObjectUIInfo(itm, out value, out table, out alias);
                        break;
                    case SAPbouiCOM.BoFormItemTypes.it_EDIT:
                    case SAPbouiCOM.BoFormItemTypes.it_EXTEDIT:
                        GetObjectUIInfo(itm, out value, out table, out alias);
                        break;
                    case SAPbouiCOM.BoFormItemTypes.it_CHECK_BOX:
                        GetCheckBoxInfo(itm, out value, out table, out alias);
                        break;
                }
            }

            if (!String.IsNullOrEmpty(table))
            {
                if (oForm.TryGetDataTable(table, out dt))
                {
                    return dt.GetSqlValue(value, alias, row);
                }

                if (oForm.TryGetDataSource(table, out var db))
                {
                    return db.GetSqlValue(value, alias, row);
                }
            }
            else if (oForm.HasUserSource(alias))
            {
                return oForm.GetUserSourceValue(alias, true);
            }

            return value;
        }

        private static void GetGridInfo(SAPbouiCOM.Item itm, string colid, out string table, out string alias)
        {
            table = itm.GetTableName();
            var col = itm.GetGridColumn(colid);
            alias = col.UniqueID;
        }
        
        private static void GetMatrixInfo(SAPbouiCOM.Item itm, string colid, int row, out string value, out string table, out string alias)
        {
            var col = itm.GetMatrixColumn(colid);
            value = col.GetValueFromColumn(row);
            table = itm.GetTableName();
            alias = itm.GetAlias();
        }

        private static void GetCheckBoxInfo(SAPbouiCOM.Item itm, out string value, out string table, out string alias)
        {
            var col = (itm.Specific as SAPbouiCOM.CheckBox);
            table = col.DataBind.TableName;
            alias = col.DataBind.Alias;
            value = col.Checked ? "Y" : "N";
        }

        private static void GetObjectUIInfo(SAPbouiCOM.Item itm, out string value, out string table, out string alias)
        {
            table = itm.GetTableName();
            alias = itm.GetAlias();

            object tempvalue = itm.GetValue();
            value = tempvalue == null ? String.Empty : tempvalue.ToString();
        }

        private static string GetSqlValue(this SAPbouiCOM.DBDataSource ds, string value, string alias, int row)
        {
            if (String.IsNullOrEmpty(value))
            {
                value = ds.GetValue(alias, row >= ds.Size ? ds.Size - 1 : row).Trim();
            }

            switch (ds.Fields.Item(alias).Type)
            {
                case SAPbouiCOM.BoFieldsType.ft_AlphaNumeric:
                case SAPbouiCOM.BoFieldsType.ft_Text:
                    return $"'{ value }'";
                case SAPbouiCOM.BoFieldsType.ft_Date:
                    return $"{ value.Substring(0, 4) }-{ value.Substring(4, 2) }-{ value.Substring(6, 2) }";
                default:
                    return value;
            }
        }

        private static string GetSqlValue(this SAPbouiCOM.DataTable dt, string value, string alias, int row)
        {
            if (String.IsNullOrEmpty(value))
            {
                value = dt.GetValue(alias, row >= dt.Rows.Count ? dt.Rows.Count - 1 : row).ToString().Trim();
            }

            switch (dt.Columns.Item(alias).Type)
            {
                case SAPbouiCOM.BoFieldsType.ft_AlphaNumeric:
                case SAPbouiCOM.BoFieldsType.ft_Text:
                    return $"'{ value }'";
                case SAPbouiCOM.BoFieldsType.ft_Date:
                    return $"{ value.Substring(0, 4) }-{ value.Substring(4, 2) }-{ value.Substring(6, 2) }";
                default:
                    return value;
            }
        }
    }
}

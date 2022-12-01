using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    static class FormExtensions
    {
        const string docentry = "DocEntry";

        public static int GetDocEntry(this SAPbouiCOM.Form oForm)
        {
            return Convert.ToInt32(oForm.DataSources.DBDataSources.Item(0).GetValue(docentry, 0));
        }

        public static string GetUserSourceValue(this SAPbouiCOM.Form oForm, string source, bool sqlformat = false)
        {
            var datasource = oForm.DataSources.UserDataSources.Item(source);
            string value = datasource.ValueEx;

            if (!sqlformat) return value;

            switch (datasource.DataType)
            {
                case SAPbouiCOM.BoDataType.dt_DATE:
                case SAPbouiCOM.BoDataType.dt_SHORT_TEXT:
                case SAPbouiCOM.BoDataType.dt_LONG_TEXT:
                    return $"'{ value }'";
                default:
                    return value;
            }
        }
        
        public static void SetUserSourceValue(this SAPbouiCOM.Form oForm, string source, string value)
        {
            var datasource = oForm.DataSources.UserDataSources.Item(source);
            datasource.ValueEx = value;
        }

        public static bool HasUserSource(this SAPbouiCOM.Form oForm, string source)
        {
            if (oForm.DataSources.UserDataSources.Count == 0) return false;

            try
            {
                return oForm.DataSources.UserDataSources.Item(source) != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool TryGetUserSource(this SAPbouiCOM.Form oForm, string source, out SAPbouiCOM.UserDataSource userDataSource)
        {
            userDataSource = null;

            if (oForm.DataSources.UserDataSources.Count == 0) return false;

            try
            {
                userDataSource = oForm.DataSources.UserDataSources.Item(source);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool HasDataSource(this SAPbouiCOM.Form oForm, string source)
        {
            if (oForm.DataSources.DBDataSources.Count == 0) return false;

            try
            {
                return oForm.DataSources.DBDataSources.Item(source) != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool TryGetDataSource(this SAPbouiCOM.Form oForm, string source, out SAPbouiCOM.DBDataSource dBDataSource)
        {
            dBDataSource = null;

            if (oForm.DataSources.DBDataSources.Count == 0) return false;

            try
            {
                dBDataSource = oForm.DataSources.DBDataSources.Item(source);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool HasDataTable(this SAPbouiCOM.Form oForm, string source)
        {
            if (oForm.DataSources.DataTables.Count == 0) return false;

            try
            {
                return oForm.DataSources.DataTables.Item(source) != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool TryGetDataTable(this SAPbouiCOM.Form oForm, string source, out SAPbouiCOM.DataTable dataTable)
        {
            dataTable = null;

            if (oForm.DataSources.DataTables.Count == 0) return false;

            try
            {
                dataTable = oForm.DataSources.DataTables.Item(source);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool HasItem(this SAPbouiCOM.Form oForm, string uniqueid)
        {
            if (oForm.Items.Count == 0) return false;

            try
            {
                return oForm.Items.Item(uniqueid) != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool TryGetItem(this SAPbouiCOM.Form oForm, string uniqueid, out SAPbouiCOM.Item item)
        {
            item = null;

            if (oForm.Items.Count == 0) return false;

            try
            {
                item = oForm.Items.Item(uniqueid);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

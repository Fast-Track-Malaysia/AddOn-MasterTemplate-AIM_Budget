#define REGULAR

using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;
using System.IO;
using System.Xml;
using System.Data.OleDb;
using System.Reflection;

namespace FT_ADDON
{
    class Common
    {
        public static string menuForm = "";
        public readonly static string sapdinamespace = typeof(SAPbobsCOM.Documents).Namespace;
        public readonly static string sapuinamespace = typeof(SAPbouiCOM.Form).Namespace;
        public readonly static Assembly sapdiasm = Assembly.GetAssembly(typeof(SAPbobsCOM.Documents));
        public readonly static Assembly sapuiasm = Assembly.GetAssembly(typeof(SAPbouiCOM.Form));

        public static void StartTransaction()
        {
            if (SAP.SBOCompany.InTransaction) SAP.SBOCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);

            SAP.SBOCompany.StartTransaction();
        }

        public static void RollBack()
        {
            if (SAP.SBOCompany.InTransaction) SAP.SBOCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
        }

        public static void Commit()
        {
            if (SAP.SBOCompany.InTransaction) SAP.SBOCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);
        }

        public static void selectNextColumn(SAPbouiCOM.Columns oColumns, ref SAPbouiCOM.ItemEvent pVal, int maxRowSize)
        {
            for (int col = 0; col < oColumns.Count; ++col)
            {
                if (oColumns.Item(col).Editable && pVal.ColUID != oColumns.Item(col).UniqueID) continue;

                while (++col < oColumns.Count)
                {
                    if (oColumns.Item(col).Editable)
                    {
                        oColumns.Item(col).Cells.Item(pVal.Row).Click();
                        return;
                    }
                }

                col = 0;
                int currow = pVal.Row == maxRowSize ? 1 : pVal.Row + 1;

                while (++col < oColumns.Count)
                {
                    if (oColumns.Item(col).Editable)
                    {
                        oColumns.Item(col).Cells.Item(currow).Click();
                        return;
                    }
                }
            }
        }

        public static string createSQLVariable(string name, SAPbouiCOM.BoFieldsType type, int size)
        {
            return $"{ name } { typeToString(type, size) }";
        }

        public static string typeToString(SAPbouiCOM.BoFieldsType type, int size)
        {
            switch (type)
            {
                case SAPbouiCOM.BoFieldsType.ft_Date:
                    return "datetime";
                case SAPbouiCOM.BoFieldsType.ft_ShortNumber:
                    return "smallint";
                case SAPbouiCOM.BoFieldsType.ft_Text:
                    return "ntext";
                case SAPbouiCOM.BoFieldsType.ft_Integer:
                case SAPbouiCOM.BoFieldsType.ft_Sum:
                    return "int";
                case SAPbouiCOM.BoFieldsType.ft_Float:
                case SAPbouiCOM.BoFieldsType.ft_Price:
                case SAPbouiCOM.BoFieldsType.ft_Quantity:
                case SAPbouiCOM.BoFieldsType.ft_Percent:
                case SAPbouiCOM.BoFieldsType.ft_Rate:
                case SAPbouiCOM.BoFieldsType.ft_Measure:
                    return $"numeric({ size }, 6)";
                case SAPbouiCOM.BoFieldsType.ft_AlphaNumeric:
                default:
                    return $"nvarchar({ size })";
            }
        }

        public static Type typeToType(SAPbouiCOM.BoFieldsType type)
        {
            switch (type)
            {
                case SAPbouiCOM.BoFieldsType.ft_Date:
                    return typeof(DateTime);
                case SAPbouiCOM.BoFieldsType.ft_ShortNumber:
                case SAPbouiCOM.BoFieldsType.ft_Integer:
                case SAPbouiCOM.BoFieldsType.ft_Sum:
                    return typeof(int);
                case SAPbouiCOM.BoFieldsType.ft_Float:
                case SAPbouiCOM.BoFieldsType.ft_Price:
                case SAPbouiCOM.BoFieldsType.ft_Quantity:
                case SAPbouiCOM.BoFieldsType.ft_Percent:
                case SAPbouiCOM.BoFieldsType.ft_Rate:
                case SAPbouiCOM.BoFieldsType.ft_Measure:
                    return typeof(double);
                default:
                    return typeof(string);
            }
        }

        public static Dictionary<string, List<string>> dataTableToContainer(SAPbouiCOM.DBDataSource dt)
        {
            Dictionary<string, List<string>> container = new Dictionary<string, List<string>>();

            for (int i = 0; i < dt.Fields.Count; ++i)
            {
                container.Add(dt.Fields.Item(i).Name, new List<string>());
            }

            for (int row = 0; row < dt.Size; ++row)
            {
                for (int i = 0; i < dt.Fields.Count; ++i)
                {
                    container[dt.Fields.Item(i).Name].Add(dt.GetValue(i, row).ToString());
                }
            }

            return container;
        }

        public static Dictionary<string, List<object>> dataTableToContainer(SAPbouiCOM.DataTable dt)
        {
            Dictionary<string, List<object>> container = new Dictionary<string, List<object>>();

            for (int i = 0; i < dt.Columns.Count; ++i)
            {
                container.Add(dt.Columns.Item(i).Name, new List<object>());
            }

            for (int row = 0; row < dt.Rows.Count; ++row)
            {
                for (int i = 0; i < dt.Columns.Count; ++i)
                {
                    container[dt.Columns.Item(i).Name].Add(dt.GetValue(i, row));
                }
            }

            return container;
        }

        public static void rightClickNewSubMenu(SAPbouiCOM.Form oForm, string uniqueID, string name)
        {
            if (SAP.SBOApplication.Menus.Item("1280").SubMenus.Exists(uniqueID)) return;

            SAPbouiCOM.MenuCreationParams creationPackage = (SAPbouiCOM.MenuCreationParams)SAP.SBOApplication.CreateObject(SAPbouiCOM.BoCreatableObjectType.cot_MenuCreationParams);
            creationPackage.Type = SAPbouiCOM.BoMenuType.mt_STRING;
            creationPackage.UniqueID = uniqueID;
            creationPackage.Position = 2;
            creationPackage.String = name;
            creationPackage.Enabled = true;
            SAPbouiCOM.MenuItem oMenuItem = SAP.SBOApplication.Menus.Item("1280");
            SAPbouiCOM.Menus oMenus = oMenuItem.SubMenus;
            oMenus.AddEx(creationPackage);
            menuForm = oForm.UniqueID;
        }

        public static void getChooseFromList(SAPbouiCOM.EditText oEditText, string source, string sourceTable, SAPbouiCOM.DataTable oDataTable)
        {
            try
            {
                if (oDataTable == null) return;

                try
                {
                    oEditText.Value = oDataTable.GetValue(source, 0).ToString().Trim();
                }
                catch
                {
                    SAPbobsCOM.Recordset rc = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                    rc.DoQuery($"Select \"{ source }\" from \"{ sourceTable }\" Where \"{ source }\"='{ oEditText.Value }'");
                    oEditText.Value = rc.Fields.Item(source).Value.ToString();
                    rc = null;
                }
            }
            catch (Exception ex)
            {
                SAP.SBOApplication.StatusBar.SetText(ex.Message, SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
            }
            finally
            {
                oDataTable = null;
            }
        }

        public static void findBlockMatrix(SAPbouiCOM.Form oForm, string matrixName)
        {
            oForm.Items.Item(matrixName).SetAutoManagedAttribute(SAPbouiCOM.BoAutoManagedAttr.ama_Editable, 4, SAPbouiCOM.BoModeVisualBehavior.mvb_False);
            oForm.Items.Item(matrixName).SetAutoManagedAttribute(SAPbouiCOM.BoAutoManagedAttr.ama_Editable, 11, SAPbouiCOM.BoModeVisualBehavior.mvb_True);
        }

        private static bool IsFormattedSearchExist(string formID, string itemID, string colID, out int key)
        {
            SAPbobsCOM.Recordset rc = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            key = -1;

            try
            {
                rc.DoQuery($"Select * From \"CSHS\" Where \"FormID\"= '{ formID }' AND \"ItemID\"='{ itemID }' AND \"ColID\"='{ colID }'");

                //following column has a formatted search applied
                if (rc.RecordCount == 0) return false;

                key = int.Parse(rc.Fields.Item("IndexID").Value.ToString());
                return true;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(rc);
                rc = null;
                GC.Collect();
            }
        }

        private static bool UpdateFormattedSearch(int key, int qrId)
        {
            SAPbobsCOM.FormattedSearches fs = (SAPbobsCOM.FormattedSearches)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oFormattedSearches);

            try
            {
                if (!fs.GetByKey(key))
                {
                    SAP.SBOApplication.MessageBox(SAP.SBOCompany.GetLastErrorDescription(), 1, "&Ok", "", "");
                    return false;
                }

                if (fs.QueryID != qrId)
                {
                    fs.QueryID = qrId;

                    if (fs.Update() != 0)
                    {
                        SAP.SBOApplication.MessageBox(SAP.SBOCompany.GetLastErrorDescription(), 1, "&Ok", "", "");
                        return false;
                    }
                }

                return true;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(fs);
                fs = null;
                GC.Collect();
            }
        }

        private static bool CreateNewFormattedSearch(string formID, int qrID, string itemID, string colID, string fieldId)
        {
            SAPbobsCOM.BoYesNoEnum yes = SAPbobsCOM.BoYesNoEnum.tYES;
            SAPbobsCOM.FormattedSearches fs = (SAPbobsCOM.FormattedSearches)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oFormattedSearches);

            try
            {
                fs.FormID = formID;
                fs.ItemID = itemID;
                fs.ColumnID = colID;
                fs.FieldID = fieldId;
                fs.Action = SAPbobsCOM.BoFormattedSearchActionEnum.bofsaQuery;
                fs.QueryID = qrID;
                fs.Refresh = yes;
                fs.ForceRefresh = yes;
                fs.ByField = yes;

                if (fs.Add() == 0) return true;

                SAP.SBOApplication.MessageBox(SAP.SBOCompany.GetLastErrorDescription(), 1, "&Ok", "", "");
                return false;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(fs);
                fs = null;
                GC.Collect();
            }
        }

        public static bool createFormattedSearch(string formID, int qrID, string itemID, string colID = "-1", string fieldId = "-1", bool update = false)
        {
            if (IsFormattedSearchExist(formID, itemID, colID, out int key))
            {
                if (!update) return true;

                return UpdateFormattedSearch(key, qrID);
            }

            return CreateNewFormattedSearch(formID, qrID, itemID, colID, fieldId);
        }

        public static int columnByTitle(SAPbouiCOM.Matrix oMatrix, string title)
        {
            for (int col = 0; col < oMatrix.Columns.Count; ++col)
            {
                if (oMatrix.Columns.Item(col).Title == title)
                {
                    return col;
                }
            }

            return -1;
        }

        public static string GetConnectionString()
        {
            Dictionary<string, string> props = new Dictionary<string, string>();

            //props["User id"] = SAP.SBOCompany.UserName;
            props["Integrated Security"] = "SSPI";
            props["Initial Catalog"] = SAP.SBOCompany.CompanyDB;
            props["Data Source"] = SAP.SBOCompany.Server;

            StringBuilder sb = new StringBuilder();

            foreach (KeyValuePair<string, string> prop in props)
            {
                sb.Append(prop.Key);
                sb.Append('=');
                sb.Append(prop.Value);
                sb.Append(';');
            }

            return sb.ToString();
        }

        public static DataSet GetDataSet(string query)
        {
            DataSet oDS = new DataSet();
            string ConnectionString = GetConnectionString();
            SqlConnection myConnection = new SqlConnection(ConnectionString);
            myConnection.Open();
            SqlDataAdapter oDA = new SqlDataAdapter(query, myConnection);
            oDA.Fill(oDS);
            myConnection.Close();
            return oDS;
        }

        public static void ExportDatas(string query, string filepath, string tableNames = "")
        {
            DataSet oDS = GetDataSet(query);

            if (tableNames.Length > 0)
            {
                try
                {
                    int index = 0;

                    foreach (string name in tableNames.Split('|'))
                    {
                        oDS.Tables[index++].TableName = name;
                    }
                }
                catch (Exception)
                {
                }
            }

            //Creae an Excel application instance
            Excel.Application excelApp = new Excel.Application();
            excelApp.DisplayAlerts = false;

            try
            {
                //Create an Excel workbook instance and open it from the predefined location
                Excel.Workbook excelWorkBook = excelApp.Workbooks.Add("");

                foreach (DataTable table in oDS.Tables)
                {
                    //Add a new worksheet to workbook with the Datatable name
                    Excel.Worksheet excelWorkSheet = (Excel.Worksheet)excelWorkBook.Sheets.Add();
                    excelWorkSheet.Name = table.TableName;

                    for (int i = 1; i < table.Columns.Count + 1; i++)
                    {
                        excelWorkSheet.Cells[1, i] = table.Columns[i - 1].ColumnName;
                    }

                    for (int j = 0; j < table.Rows.Count; j++)
                    {
                        for (int k = 0; k < table.Columns.Count; k++)
                        {
                            excelWorkSheet.Cells[j + 2, k + 1] = table.Rows[j].ItemArray[k].ToString();
                        }
                    }
                }

                excelWorkBook.SaveAs(filepath, Microsoft.Office.Interop.Excel.XlFileFormat.xlWorkbookDefault, Type.Missing, Type.Missing, false, false,
                    Excel.XlSaveAsAccessMode.xlShared, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                excelWorkBook.Close();
            }
            finally
            {
                excelApp.Quit();
            }
        }

        public static DataSet ExcelParse(string fileName)
        {
            string connectionString = string.Format("provider=Microsoft.ACE.OLEDB.12.0; data source=\"{0}\";Extended Properties='Excel 12.0;IMEX=1;;HDR=Yes'", fileName);
            DataSet data = new DataSet();

            foreach (var sheetName in GetExcelSheetNames(connectionString))
            {
                using (OleDbConnection con = new OleDbConnection(connectionString))
                {
                    var dataTable = new DataTable();
                    string query = string.Format("SELECT * FROM [{0}]", sheetName);
                    con.Open();
                    OleDbDataAdapter adapter = new OleDbDataAdapter(query, con);
                    adapter.Fill(dataTable);
                    data.Tables.Add(dataTable);
                }
            }

            return data;
        }

        private static string[] GetExcelSheetNames(string connectionString)
        {
            OleDbConnection con = new OleDbConnection(connectionString);
            con.Open();
            DataTable dt = con.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);

            if (dt == null) return null;

            String[] excelSheetNames = new String[dt.Rows.Count];
            int i = 0;

            foreach (DataRow row in dt.Rows)
            {
                excelSheetNames[i] = row["TABLE_NAME"].ToString();
                i++;
            }

            return excelSheetNames;
        }

        public static double labourQuantity(string itemcode, double rate)
        {
            SAPbobsCOM.Recordset rc = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            rc.DoQuery($"select T0.\"U_Quantity\" from \"@FT_BOQL1\" T0 join \"@FT_BOQL\" T1 on T0.\"Code\"=T1.\"Code\" where T0.\"U_ItemCode\"='{ itemcode }" +
                       $"' and T1.\"U_Rate\"={ rate }");

            if (rc.RecordCount > 0) return rcToDouble(rc, "U_Quantity");

            return 0;
        }

        public static bool isLabour(string itemcode)
        {
            SAPbobsCOM.Recordset rc = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            rc.DoQuery($"select \"U_FT_Labour\" from \"OITM\" where \"ItemCode\"='{ itemcode }'");

            return rc.Fields.Item("U_FT_Labour").Value.ToString() == "Y";
        }

        public static bool createQuery(string DataName, string query, out int QRKey)
        {
            SAPbobsCOM.Recordset rc = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            SAPbobsCOM.UserQueries qr = (SAPbobsCOM.UserQueries)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oUserQueries);

            try
            {
                rc.DoQuery($"Select \"QName\", \"QString\", \"IntrnalKey\", \"QCategory\" From \"OUQR\" Where \"QName\"='{ DataName }'");

                if (rc.RecordCount <= 0)
                {
                    qr.QueryCategory = -1;
                    qr.Query = query;
                    qr.QueryDescription = DataName;

                    if (qr.Add() != 0)
                    {
                        SAP.SBOApplication.MessageBox(SAP.SBOCompany.GetLastErrorDescription(), 1, "&Ok", "", "");
                        QRKey = 0;
                        qr = null;
                        GC.Collect();
                        return false;
                    }
                }
                else if (rc.Fields.Item("QString").Value.ToString() != query)
                {
                    if (qr.GetByKey(rcToInt(rc, "IntrnalKey"), rcToInt(rc, "QCategory")))
                    {
                        qr.Query = query;

                        if (qr.Update() != 0)
                        {
                            SAP.SBOApplication.MessageBox(SAP.SBOCompany.GetLastErrorDescription(), 1, "&Ok", "", "");
                            QRKey = 0;
                            qr = null;
                            GC.Collect();
                            return false;
                        }
                    }
                }

                rc.DoQuery($"Select \"IntrnalKey\" From \"OUQR\" Where \"QName\"='{ DataName }'");
                QRKey = int.Parse(rc.Fields.Item("IntrnalKey").Value.ToString());
                return true;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(rc);
                rc = null;
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(qr);
                qr = null;
                GC.Collect();
            }
        }

        public static void dataTableToSQL(SAPbouiCOM.DBDataSource db, SAPbouiCOM.DataTable dt)
        {
            if (db.Size == 0) db.InsertRecord(0);

            while (db.Size > 1) db.RemoveRecord(db.Size - 1);

            for (int row = 0; row < dt.Rows.Count; ++row)
            {
                for (int col = 0; col < dt.Columns.Count; ++col)
                {
                    db.SetValue(dt.Columns.Item(col).Name, db.Size - 1, dt.GetValue(col, row).ToString());
                }

                db.InsertRecord(db.Size);
            }
        }

        public static void dataTableToSQL(SAPbouiCOM.DataTable dt, string tableName, string[] add_columns, string[] add_entries, string refreshKey)
        {
            SAPbobsCOM.Recordset rc = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            string cols = "";

            rc.DoQuery($"delete \"{ tableName }\" where { refreshKey }");

            for (int col = 0; col < dt.Columns.Count; ++col)
            {
                cols += ($"\"{ dt.Columns.Item(col).Name }\", ");
            }

            for (int col = 0; col < add_columns.Length; ++col)
            {
                cols += ($"\"{ add_columns[col] }\", ");
            }

            cols += " \"LineId\"";
            string query = $"insert into \"{ tableName }\" ({ cols }) values (";

            for (int row = 0; row < dt.Rows.Count; ++row)
            {
                string tryQuery = query;

                for (int col = 0; col < dt.Columns.Count; ++col)
                {
                    tryQuery += ($"'{ dt.GetValue(col, row) }', ");
                }

                for (int col = 0; col < add_entries.Length; ++col)
                {
                    if (add_columns[col] == "VisOrder") add_entries[col] = (col + 1).ToString();

                    tryQuery += ($"'{ add_entries[col] }', ");
                }

                string count = "1";
                rc.DoQuery($"select MAX(\"LineId\") as \"LineId\" from \"{ tableName }\" where { refreshKey }");

                if (rc.RecordCount > 0) count = (Convert.ToInt32(rc.Fields.Item("LineId").Value) + 1).ToString();

                tryQuery += ($"'{ count }')");
                rc.DoQuery(tryQuery);
            }
        }

        public static void deleteEmptySource(SAPbouiCOM.DBDataSource db, string key)
        {
            while (db.Size > 0 && db.GetValue(key, db.Size - 1).Length == 0)
            {
                db.RemoveRecord(db.Size - 1);
            }
        }

        public static void addMatrixNumbering(SAPbouiCOM.Matrix oMatrix, SAPbouiCOM.DBDataSource db, bool add = true)
        {
            if (add && db.Size == 0) db.InsertRecord(0);

            for (int i = 0; i < db.Size; ++i)
            {
                db.SetValue(0, i, i.ToString());
            }

            oMatrix.LoadFromDataSource();
        }

        public static void addMatrixNumbering(SAPbouiCOM.Matrix oMatrix, bool add = true)
        {
            oMatrix.LoadFromDataSource();

            if (add && oMatrix.RowCount == 0) oMatrix.AddRow();

            for (int i = 1; i < oMatrix.RowCount + 1; ++i)
            {
                ((SAPbouiCOM.EditText)oMatrix.Columns.Item("V_-1").Cells.Item(i).Specific).Value = i.ToString();
            }

            oMatrix.FlushToDataSource();
        }

        public static void addGridNumbering(SAPbouiCOM.Grid oGrid, bool add = true)
        {
            SAPbouiCOM.DataTable dt = oGrid.DataTable;

            if (add && dt.Rows.Count == 0) dt.Rows.Add();

            for (int i = 0; i < oGrid.Rows.Count; ++i)
            {
                oGrid.RowHeaders.SetText(i, (i + 1).ToString());
            }
        }

        public static void getDocNum(SAPbouiCOM.Form oForm, string obj)
        {
            SAPbobsCOM.Recordset rc = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            rc.DoQuery($"select \"NextNumber\" from NNM1 where \"ObjectCode\"='{ obj }'");
            ((SAPbouiCOM.EditText)oForm.Items.Item("t_DocNum").Specific).Value = rc.Fields.Item("NextNumber").Value.ToString();
        }

        public static SAPbouiCOM.Form getParentForm(SAPbouiCOM.Form oForm)
        {
            object uid = oForm.DataSources.DataTables.Item("FormDetails").GetValue("Parent", 0);
            return SAP.SBOApplication.Forms.Item(uid);
        }

        public static void addMatrixRow(SAPbouiCOM.Form oForm, string tableName, string matrixName, int matrixrowno, string colheader)
        {
            try
            {
                //oForm.Freeze(true);
                SAPbouiCOM.DBDataSource ds = oForm.DataSources.DBDataSources.Item(tableName);
                SAPbouiCOM.Matrix oMatrix = (SAPbouiCOM.Matrix)oForm.Items.Item(matrixName).Specific;
                //SAP.SBOApplication.MessageBox(oMatrix.GetNextSelectedRow(0, SAPbouiCOM.BoOrderType.ot_RowOrder).ToString(), 1, "&Ok", "", "");

                if (matrixrowno == 0 || oMatrix.VisualRowCount == 0)
                {
                    oMatrix.AddRow(1, -1);
                    oMatrix.FlushToDataSource();
                    if (colheader.Length > 0)
                    {
                        for (int i = 1; i <= ds.Size; i++)
                        {
                            ds.SetValue(colheader, i - 1, i.ToString());
                        }
                    }
                    //ds.SetValue("U_ODRNo", ds.Size -1, ds.GetValue("U_ODRNo",matrixrowno));
                    oMatrix.LoadFromDataSource();
                }
                else
                {
                    oMatrix.FlushToDataSource();
                    ds.InsertRecord(ds.Size);
                    ds.Offset = ds.Size - 1;
                    if (colheader.Length > 0)
                    {
                        for (int i = 1; i <= ds.Size; i++)
                        {
                            ds.SetValue(colheader, i - 1, i.ToString());
                        }
                    }
                    oMatrix.AddRow(1, matrixrowno);
                    oMatrix.LoadFromDataSource();
                }
                oForm.Freeze(false);
                ds = null;
                oMatrix = null;
                GC.Collect();
            }
            catch (Exception ex)
            {
                SAP.SBOApplication.MessageBox(ReadException(ex), 1, "&Ok", "", "");
                oForm.Freeze(false);
            }
        }

        public static string ReadException(Exception ex)
        {
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
            }

            var msg = $"{ ex.Message }\n{ ex.StackTrace }";
            return msg.Length <= 700 ? msg : msg.Substring(0, 700);
        }

        public static int rcToInt(SAPbobsCOM.Recordset rc, string field)
        {
            try
            {
                return Convert.ToInt32(rc.Fields.Item(field).Value);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static string rcToStr(SAPbobsCOM.Recordset rc, string field)
        {
            return rc.Fields.Item(field).Value.ToString();
        }

        public static double rcToDouble(SAPbobsCOM.Recordset rc, string field)
        {
            try
            {
                return Convert.ToDouble(rc.Fields.Item(field).Value);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static DataTable xmlToDataTable(string xml)
        {
            XmlTextReader reader = null;

            try
            {
                DataTable oDT = new DataTable();
                StringReader stream = new StringReader(xml);
                reader = new XmlTextReader(stream);
                oDT.ReadXml(reader);
                return oDT;
            }
            finally
            {
                if (reader != null) reader.Close();
            }
        }

        public static string dataTableToXml(DataTable oDT)
        {
            XmlTextWriter writer = null;

            try
            {
                MemoryStream stream = new MemoryStream();
                writer = new XmlTextWriter(stream, Encoding.ASCII);
                oDT.WriteXml(writer);
                int count = (int)stream.Length;
                byte[] byteArray = new byte[count];
                stream.Seek(0, SeekOrigin.Begin);
                stream.Read(byteArray, 0, count);
                return byteArray.ToString().Trim();
            }
            finally
            {
                if (writer != null) writer.Close();
            }
        }

        public static bool matrixDataTable(SAPbouiCOM.Matrix oMatrix, SAPbouiCOM.DataTable dt, string[] columns, string[] fields, bool add = false)
        {
            if (columns.Length != fields.Length || dt.Columns.Count != fields.Length || oMatrix.Columns.Count != columns.Length)
            {
                SAP.SBOApplication.MessageBox("Mismatch of columns and fields", 1, "&Ok", "", "");
                return false;
            }

            oMatrix.Clear();

            for (int row = 1; row <= dt.Rows.Count; ++row)
            {
                oMatrix.AddRow(1, oMatrix.RowCount);
                oMatrix.ClearRowData(oMatrix.RowCount);

                for (int i = 0; i < columns.Length; ++i)
                {
                    getMatrixText(oMatrix, columns[i], row).Value = getDataTable(dt, fields[i], row).Value.ToString();
                }
            }

            if (add)
            {
                oMatrix.AddRow(1, oMatrix.RowCount);
                oMatrix.ClearRowData(oMatrix.RowCount);
            }

            return true;
        }

        static bool keysMatches(SAPbouiCOM.DataTable dt, int row, string[] keys, string[] matches)
        {
            int size = keys.Length > matches.Length ? keys.Length : matches.Length;

            for (int i = 0; i < size; ++i)
            {
                if (dt.GetValue(keys[i], row).ToString() != matches[i]) return false;
            }

            return true;
        }

        static bool keysMatches(SAPbouiCOM.DBDataSource dt, int row, string[] keys, string[] matches)
        {
            int size = keys.Length > matches.Length ? keys.Length : matches.Length;

            for (int i = 0; i < size; ++i)
            {
                if (dt.GetValue(keys[i], row) != matches[i]) return false;
            }

            return true;
        }

        public static void copyDataTable(SAPbouiCOM.DBDataSource dt, SAPbouiCOM.DBDataSource dt2, string key = "", string match = "", bool delete = false)
        {
            dt.Clear();
            string[] keys = key.Split('|');
            string[] matches = match.Split('|');

            if (delete)
            {
                for (int row = 0; row < dt2.Size - 1; ++row)
                {
                    if ((key.Length == 0 && match.Length == 0) || keysMatches(dt2, row, keys, matches))
                    {
                        for (int i = 0; i < dt2.Fields.Count; ++i)
                        {
                            dt.SetValue(i, dt.Size - 1, dt2.GetValue(i, row).ToString());
                        }

                        dt.InsertRecord(dt.Size);
                        dt2.RemoveRecord(row);
                        --row;
                    }
                }
            }
            else
            {
                for (int row = 0; row < dt2.Size - 1; ++row)
                {
                    if ((key.Length == 0 && match.Length == 0) || keysMatches(dt2, row, keys, matches))
                    {
                        for (int i = 0; i < dt2.Fields.Count; ++i)
                        {
                            dt.SetValue(i, dt.Size - 1, dt2.GetValue(i, row));
                        }

                        dt.InsertRecord(dt.Size);
                    }
                }
            }
        }

        public static void copyDataTable(SAPbouiCOM.DBDataSource dt, SAPbouiCOM.DataTable dt2, string key = "", string match = "", bool delete = false)
        {
            while (dt.Size > 0) dt.RemoveRecord(dt.Size - 1);

            string[] keys = key.Split('|');
            string[] matches = match.Split('|');

            if (delete)
            {
                for (int row = 0; row < dt2.Rows.Count; ++row)
                {
                    if ((key.Length == 0 && match.Length == 0) || keysMatches(dt2, row, keys, matches))
                    {
                        for (int i = 0; i < dt2.Columns.Count; ++i)
                        {
                            dt.SetValue(dt2.Columns.Item(i).Name, dt.Size - 1, dt2.GetValue(i, row).ToString());
                        }

                        dt.InsertRecord(dt.Size);
                        dt2.Rows.Remove(row);
                        --row;
                    }
                }
            }
            else
            {
                for (int row = 0; row < dt2.Rows.Count; ++row)
                {
                    if ((key.Length == 0 && match.Length == 0) || keysMatches(dt2, row, keys, matches))
                    {
                        for (int i = 0; i < dt2.Columns.Count; ++i)
                        {
                            dt.SetValue(dt2.Columns.Item(i).Name, dt.Size - 1, dt2.GetValue(i, row).ToString());
                        }

                        dt.InsertRecord(dt.Size);
                    }
                }
            }
        }

        public static void copyDataTable(SAPbouiCOM.DataTable dt, SAPbouiCOM.DBDataSource dt2, string key = "", string match = "", bool delete = false)
        {
            string[] keys = key.Split('|');
            string[] matches = match.Split('|');

            if (dt.Columns.Count == 0)
            {
                dt.Clear();

                for (int i = 0; i < dt2.Fields.Count; ++i)
                {
                    SAPbouiCOM.Field col = dt2.Fields.Item(i);

                    if ((col.Name[0] != 'U' || col.Name[1] != '_') && col.Name != "VisOrder") continue;

                    dt.Columns.Add(col.Name, col.Type);
                }
            }
            else
            {
                dt.Rows.Clear();
            }

            if (delete)
            {
                for (int row = 0; row < dt2.Size - 1; ++row)
                {
                    if ((key.Length == 0 && match.Length == 0) || keysMatches(dt2, row, keys, matches))
                    {
                        dt.Rows.Add();

                        for (int i = 0; i < dt.Columns.Count; ++i)
                        {
                            string data = dt2.GetValue(dt.Columns.Item(i).Name, row);

                            switch (dt.Columns.Item(i).Type)
                            {
                                case SAPbouiCOM.BoFieldsType.ft_Date:
                                    dt.SetValue(i, dt.Rows.Count - 1, DateTime.Parse(data));
                                    break;
                                case SAPbouiCOM.BoFieldsType.ft_Float:
                                case SAPbouiCOM.BoFieldsType.ft_Quantity:
                                case SAPbouiCOM.BoFieldsType.ft_Rate:
                                case SAPbouiCOM.BoFieldsType.ft_Percent:
                                case SAPbouiCOM.BoFieldsType.ft_Price:
                                    dt.SetValue(i, dt.Rows.Count - 1, double.Parse(data));
                                    break;
                                case SAPbouiCOM.BoFieldsType.ft_Integer:
                                    dt.SetValue(i, dt.Rows.Count - 1, int.Parse(data));
                                    break;
                                default:
                                    dt.SetValue(i, dt.Rows.Count - 1, data);
                                    break;
                            }
                        }

                        dt2.RemoveRecord(row);
                        --row;
                    }
                }
            }
            else
            {
                for (int row = 0; row < dt2.Size - 1; ++row)
                {
                    if ((key.Length == 0 && match.Length == 0) || keysMatches(dt2, row, keys, matches))
                    {
                        dt.Rows.Add();

                        for (int i = 0; i < dt.Columns.Count; ++i)
                        {
                            switch (dt.Columns.Item(i).Type)
                            {
                                case SAPbouiCOM.BoFieldsType.ft_Date:
                                    dt.SetValue(i, dt.Rows.Count - 1, DateTime.Parse(dt2.GetValue(dt.Columns.Item(i).Name, row)));
                                    break;
                                case SAPbouiCOM.BoFieldsType.ft_Float:
                                case SAPbouiCOM.BoFieldsType.ft_Quantity:
                                case SAPbouiCOM.BoFieldsType.ft_Rate:
                                case SAPbouiCOM.BoFieldsType.ft_Percent:
                                case SAPbouiCOM.BoFieldsType.ft_Price:
                                    dt.SetValue(i, dt.Rows.Count - 1, double.Parse(dt2.GetValue(dt.Columns.Item(i).Name, row)));
                                    break;
                                case SAPbouiCOM.BoFieldsType.ft_Integer:
                                    dt.SetValue(i, dt.Rows.Count - 1, int.Parse(dt2.GetValue(dt.Columns.Item(i).Name, row)));
                                    break;
                                default:
                                    dt.SetValue(i, dt.Rows.Count - 1, dt2.GetValue(dt.Columns.Item(i).Name, row));
                                    break;
                            }
                        }
                    }
                }
            }
        }

        public static void copyDataTable(SAPbouiCOM.DataTable dt, SAPbouiCOM.DataTable dt2, string key = "", string match = "", bool delete = false)
        {
            dt.Clear();
            string[] keys = key.Split('|');
            string[] matches = match.Split('|');

            for (int i = 0; i < dt2.Columns.Count; ++i)
            {
                SAPbouiCOM.DataColumn col = dt2.Columns.Item(i);
                dt.Columns.Add(col.Name, col.Type);
            }

            if (delete)
            {
                for (int row = 0; row < dt2.Rows.Count; ++row)
                {
                    if ((key.Length == 0 && match.Length == 0) || keysMatches(dt2, row, keys, matches))
                    {
                        dt.Rows.Add();

                        for (int i = 0; i < dt2.Columns.Count; ++i)
                        {
                            dt.SetValue(i, dt.Rows.Count - 1, dt2.GetValue(i, row));
                        }

                        dt2.Rows.Remove(row);
                        --row;
                    }
                }
            }
            else
            {
                for (int row = 0; row < dt2.Rows.Count; ++row)
                {
                    if ((key.Length == 0 && match.Length == 0) || keysMatches(dt2, row, keys, matches))
                    {
                        dt.Rows.Add();

                        for (int i = 0; i < dt2.Columns.Count; ++i)
                        {
                            dt.SetValue(i, dt.Rows.Count - 1, dt2.GetValue(i, row));
                        }
                    }
                }
            }
        }

        public static SAPbouiCOM.EditText getText(SAPbouiCOM.Form oForm, string item)
        {
            return (SAPbouiCOM.EditText)oForm.Items.Item(item).Specific;
        }

        public static SAPbouiCOM.EditText getMatrixText(SAPbouiCOM.Matrix oMatrix, ref SAPbouiCOM.ItemEvent pVal)
        {
            return getMatrixText(oMatrix, pVal.ColUID, pVal.Row);
        }

        public static SAPbouiCOM.DataCell getDataTable(SAPbouiCOM.DataTable dt, int col, int row)
        {
            return dt.Columns.Item(col).Cells.Item(row);
        }

        public static SAPbouiCOM.DataCell getDataTable(SAPbouiCOM.DataTable dt, string col, int row)
        {
            return dt.Columns.Item(col).Cells.Item(row);
        }

        public static SAPbouiCOM.EditText getMatrixText(SAPbouiCOM.Matrix oMatrix, object col, int row)
        {
            return (SAPbouiCOM.EditText)oMatrix.Columns.Item(col).Cells.Item(row).Specific;
        }

        public static void addMatrixNumbering(string matrixName, SAPbouiCOM.Form oForm)
        {
            SAPbouiCOM.Matrix oMatrix = (SAPbouiCOM.Matrix)oForm.Items.Item(matrixName).Specific;

            for (int i = 1; i < oMatrix.RowCount + 1; ++i)
            {
                ((SAPbouiCOM.EditText)oMatrix.Columns.Item("V_-1").Cells.Item(i).Specific).Value = i.ToString();
            }
        }

        public static bool isSQLTableExist(string tableName)
        {
            SAPbobsCOM.Recordset rc = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            try
            {
                rc.DoQuery($"SELECT * FROM \"INFORMATION_SCHEMA\".\"TABLES\" WHERE \"TABLE_NAME\"= '{ tableName }'");
                return rc.RecordCount > 0;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(rc);
                rc = null;
                GC.Collect();
            }
        }

        public static T createSAPObject<T>(SAPbobsCOM.BoObjectTypes type)
        {
            return (T)SAP.SBOCompany.GetBusinessObject(type);
        }

        public static bool getDefaultValue(SAPbouiCOM.Form oForm, string key)
        {
            SAPbobsCOM.Recordset rc = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            try
            {
                rc.DoQuery($"select \"U_DftValue\" from \"{ FormVariables.TableName }\" where \"Code\"='{ oForm.TypeEx }.{ key }'");
            }
            catch (Exception)
            {
                return false;
            }

            if (rc.RecordCount == 0) return false;

            getText(oForm, key).Value = rc.Fields.Item(0).Value.ToString();
            return true;
        }


        public static string ChangeTypeToSql(object value, Type type)
        {
            if (!ChangeTypeToSql(value, type, out string data)) return "";

            return data;
        }

        public static bool ChangeTypeToSql(object value, Type type, out string data)
        {
            if (value == null)
            {
                data = "NULL";
                return true;
            }

            var t = type;

            if (type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                t = Nullable.GetUnderlyingType(t);
            }

            if (t == typeof(DateTime))
            {
                string date = Convert.ToDateTime(value).ToString("yyyyMMdd");

                if (date == "00010101")
                {
                    data = date;
                    return false;
                }

                data = $"'{ date }'";
            }
            else if (t == typeof(string))
            {
                data = $"'{ value }'";
            }
            else
            {
                data = value.ToString();
            }

            return true;
        }

        public static Type GetSAPCOMType(string typename)
        {
            Type type = sapdiasm.GetType($"{ sapdinamespace }.{ typename }");

            if (type == null) type = sapuiasm.GetType($"{ sapuinamespace }.{ typename }");

            return type;
        }

        public static Type GetSAPCOMType(object obj)
        {
            if (obj.GetType().FullName != "System.__ComObject") return obj.GetType();

            return GetSAPCOMType(ComUtils.ComHelper.GetTypeName(obj));
        }

        public static object GetInstance(string strFullyQualifiedName)
        {
            Type type = Type.GetType(strFullyQualifiedName);

            if (type != null) return Activator.CreateInstance(type);

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(strFullyQualifiedName);

                if (type != null) return Activator.CreateInstance(type);
            }
            return null;
        }

        public static object GetInstance(string strFullyQualifiedName, params object[] args)
        {
            Type type = Type.GetType(strFullyQualifiedName);

            if (type != null) return Activator.CreateInstance(type, args);

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(strFullyQualifiedName);

                if (type != null) return Activator.CreateInstance(type, args);

            }
            return null;
        }

        public static void SetConditionsToCLP(SAPbouiCOM.Form oForm, string item, Tuple<string, SAPbouiCOM.BoConditionOperation, string>[] args)
        {
            SAPbouiCOM.ChooseFromList oCFL = oForm.ChooseFromLists.Item(item);
            SAPbouiCOM.Conditions oConds = new SAPbouiCOM.Conditions();

            foreach (var arg in args)
            {
                SAPbouiCOM.Condition oCond = oConds.Add();
                oCond.Alias = arg.Item1;
                oCond.Operation = arg.Item2;
                oCond.CondVal = arg.Item3;
            }

            oCFL.SetConditions(oConds);
        }
    }
}

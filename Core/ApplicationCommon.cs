using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Linq;

namespace FT_ADDON
{
    class ApplicationCommon
    {
        // -------------------------------------------------------------
        // Create UDT, UDF, UDO and Add Menu Item
        // -------------------------------------------------------------
        public static Boolean createTable(string tableName, string tableDescription, SAPbobsCOM.BoUTBTableType tableType)
        {
            GC.Collect();
            SAPbobsCOM.UserTablesMD oUserTableMD = (SAPbobsCOM.UserTablesMD)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oUserTables);

            try
            {
                if (oUserTableMD.GetByKey(tableName)) return true;

                SAP.setStatus($"Creating Table : { tableName } - { tableDescription }");
                oUserTableMD.TableName = tableName;
                oUserTableMD.TableDescription = tableDescription;
                oUserTableMD.TableType = tableType;

                GC.Collect();
                GC.WaitForPendingFinalizers();

                if (oUserTableMD.Add() == 0) return true;

                SAP.SBOApplication.MessageBox(SAP.SBOCompany.GetLastErrorDescription(), 1, "&Ok", "", "");
                return false;
            }
            finally
            {
                Marshal.FinalReleaseComObject(oUserTableMD);
                oUserTableMD = null;
                GC.Collect();
            }
        }

        public static Boolean createField(string tableName,
                                          string fieldName,
                                          string fieldDescription,
                                          SAPbobsCOM.BoFieldTypes fieldType = SAPbobsCOM.BoFieldTypes.db_Alpha,
                                          int size = 254,
                                          string defaultValue = "",
                                          Boolean mandatory = false,
                                          SAPbobsCOM.BoFldSubTypes subType = SAPbobsCOM.BoFldSubTypes.st_None,
                                          string validValues = "",
                                          string linkedTable = "",
                                          bool forceupdate = false)
        {
            if (udfExist(tableName, fieldName))
            {
                if (!forceupdate) return true;

                return updateFieldMD(tableName, fieldName, fieldDescription, fieldType, size, defaultValue, mandatory, subType, validValues, linkedTable);
            }

            SAPbobsCOM.UserFieldsMD oUserFieldsMD = (SAPbobsCOM.UserFieldsMD)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oUserFields);

            try
            {
                SAP.setStatus($"Creating Field : { fieldName } - { fieldDescription }");

                addCommonFields(oUserFieldsMD, tableName, fieldName, fieldDescription, fieldType, size, defaultValue, mandatory, subType, validValues);
                oUserFieldsMD.LinkedTable = linkedTable;

                GC.Collect();
                GC.WaitForPendingFinalizers();

                if (oUserFieldsMD.Add() == 0) return true;

                SAP.SBOApplication.MessageBox($"Error : { SAP.SBOCompany.GetLastErrorDescription() }", 1, "&Ok", "", "");
                return false;
            }
            finally
            {
                Marshal.FinalReleaseComObject(oUserFieldsMD);
                oUserFieldsMD = null;
                GC.Collect();
            }
        }
        
        public static Boolean createField(string tableName,
                                          string fieldName,
                                          string fieldDescription,
                                          SAPbobsCOM.UDFLinkedSystemObjectTypesEnum sysTable,
                                          SAPbobsCOM.BoFieldTypes fieldType = SAPbobsCOM.BoFieldTypes.db_Alpha,
                                          int size = 254,
                                          string defaultValue = "",
                                          Boolean mandatory = false,
                                          SAPbobsCOM.BoFldSubTypes subType = SAPbobsCOM.BoFldSubTypes.st_None,
                                          string validValues = "",
                                          bool forceupdate = false)
        {
            if (udfExist(tableName, fieldName))
            {
                if (!forceupdate) return true;

                return updateFieldMD(tableName, fieldName, fieldDescription, fieldType, size, defaultValue, mandatory, subType, validValues, sysTable);
            }

            SAPbobsCOM.UserFieldsMD oUserFieldsMD = (SAPbobsCOM.UserFieldsMD)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oUserFields);

            try
            {
                SAP.setStatus($"Creating Field : { fieldName } - { fieldDescription }");

                addCommonFields(oUserFieldsMD, tableName, fieldName, fieldDescription, fieldType, size, defaultValue, mandatory, subType, validValues);

                if (sysTable != 0)
                {
                    oUserFieldsMD.LinkedSystemObject = sysTable;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();

                if (oUserFieldsMD.Add() == 0) return true;

                SAP.SBOApplication.MessageBox($"Error : { SAP.SBOCompany.GetLastErrorDescription() }", 1, "&Ok", "", "");
                return false;
            }
            finally
            {
                Marshal.FinalReleaseComObject(oUserFieldsMD);
                oUserFieldsMD = null;
                GC.Collect();
            }
        }

        private static void addCommonFields(SAPbobsCOM.UserFieldsMD oUserFieldsMD,
                                            string tableName,
                                            string fieldName,
                                            string fieldDescription,
                                            SAPbobsCOM.BoFieldTypes fieldType,
                                            int size,
                                            string defaultValue,
                                            Boolean mandatory,
                                            SAPbobsCOM.BoFldSubTypes subType,
                                            string validValues)
        {
            int IvalidValues = 0;
            oUserFieldsMD.TableName = tableName;
            oUserFieldsMD.Name = fieldName;
            oUserFieldsMD.Description = fieldDescription;
            oUserFieldsMD.Type = fieldType;

            if (subType != SAPbobsCOM.BoFldSubTypes.st_None) oUserFieldsMD.SubType = subType;

            if (size > 0)
            {
                switch (fieldType)
                {
                    case SAPbobsCOM.BoFieldTypes.db_Float:
                        oUserFieldsMD.EditSize = size > 16 ? 16 : size;
                        break;
                    case SAPbobsCOM.BoFieldTypes.db_Numeric:
                        oUserFieldsMD.EditSize = size > 11 ? 11 : size;
                        break;
                    default:
                        oUserFieldsMD.EditSize = size > 254 ? 254 : size;
                        break;
                }

                oUserFieldsMD.Size = oUserFieldsMD.EditSize;
            }

            if (defaultValue != "") oUserFieldsMD.DefaultValue = defaultValue;

            if (mandatory) oUserFieldsMD.Mandatory = SAPbobsCOM.BoYesNoEnum.tYES;

            if (validValues != "")
            {
                foreach (string value in validValues.Split('|'))
                {
                    IvalidValues++;
                    string[] parm = value.Split(':');
                    if (IvalidValues != 1) oUserFieldsMD.ValidValues.Add();
                    oUserFieldsMD.ValidValues.SetCurrentLine(IvalidValues - 1);
                    oUserFieldsMD.ValidValues.Value = parm[0];
                    oUserFieldsMD.ValidValues.Description = parm[1];
                }
            }
        }

        public static Boolean tableGotField(string tablename)
        {
            // Check if table has UDF, include @ if table is UDT
            SAPbobsCOM.UserFieldsMD oUserFieldsMD = (SAPbobsCOM.UserFieldsMD)(SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oUserFields));

            try
            {
                return oUserFieldsMD.GetByKey(tablename, 0);
            }
            finally
            {
                Marshal.FinalReleaseComObject(oUserFieldsMD);
                oUserFieldsMD = null;
                GC.Collect();
            }
        }

        public static Boolean udfExist(string tableName, string fieldName)
        {
            SAPbobsCOM.Recordset oRec = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            try
            {
                oRec.DoQuery($"SELECT \"AliasID\" FROM \"CUFD\" WHERE \"TableID\"='{ tableName }' AND \"AliasID\" = '{ fieldName }'");
                return oRec.RecordCount > 0;
            }
            finally
            {
                Marshal.FinalReleaseComObject(oRec);
                oRec = null;
                GC.Collect();
            }
        }

        private static bool updateTableMD(SAPbobsCOM.UserFieldsMD oUserFieldsMD, string linkedTable)
        {
            SAPbobsCOM.Recordset rc = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            try
            {
                if (linkedTable.Length == 0)
                {
                    if (oUserFieldsMD.LinkedSystemObject != 0)
                    {
                        oUserFieldsMD.LinkedSystemObject = 0;
                        return true;
                    }

                    return false;
                }

                rc.DoQuery($"SELECT * FROM \"OUDO\" WHERE \"Code\"='{ linkedTable }'");

                if (rc.RecordCount == 0)
                {
                    if (oUserFieldsMD.LinkedTable != linkedTable)
                    {
                        oUserFieldsMD.LinkedTable = linkedTable;
                        oUserFieldsMD.LinkedSystemObject = 0;
                        return true;
                    }
                }
                else if (oUserFieldsMD.LinkedUDO != linkedTable)
                {
                    oUserFieldsMD.LinkedUDO = linkedTable;
                    oUserFieldsMD.LinkedSystemObject = 0;
                    return true;
                }

                return false;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(rc);
                rc = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private static Boolean updateFieldMD(string tableName,
                                             string fieldName,
                                             string fieldDescription,
                                             SAPbobsCOM.BoFieldTypes fieldType,
                                             int size,
                                             string defaultValue,
                                             Boolean mandatory,
                                             SAPbobsCOM.BoFldSubTypes subType,
                                             string validValues,
                                             string linkedTable)
        {
            SAPbobsCOM.UserFieldsMD oUserFieldsMD = (SAPbobsCOM.UserFieldsMD)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oUserFields);

            try
            {
                int fieldID = 0;
                int record = fieldName != "" && fieldDescription != "" ? getFieldIDFromTable(tableName, fieldName, out fieldID) : 0;

                if (record != 1 || !oUserFieldsMD.GetByKey(tableName, fieldID)) return false;

                bool change = updateCommonFields(oUserFieldsMD, tableName, fieldName, fieldDescription, fieldType, size, defaultValue, mandatory, subType, validValues);

                if (updateTableMD(oUserFieldsMD, linkedTable))
                {
                    change = true;
                }

                if (!change) return true;

                GC.Collect();
                GC.WaitForPendingFinalizers();

                if (oUserFieldsMD.Update() == 0) return true;

                SAP.SBOApplication.MessageBox($"Error : { SAP.SBOCompany.GetLastErrorDescription() }", 1, "Ok", "", "");
                return false;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oUserFieldsMD);
                oUserFieldsMD = null;
                GC.Collect();
            }
        }

        private static Boolean updateFieldMD(string tableName,
                                             string fieldName,
                                             string fieldDescription,
                                             SAPbobsCOM.BoFieldTypes fieldType,
                                             int size,
                                             string defaultValue,
                                             Boolean mandatory,
                                             SAPbobsCOM.BoFldSubTypes subType,
                                             string validValues,
                                             SAPbobsCOM.UDFLinkedSystemObjectTypesEnum sysTable)
        {
            SAPbobsCOM.UserFieldsMD oUserFieldsMD = (SAPbobsCOM.UserFieldsMD)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oUserFields);

            try
            {
                int fieldID = 0;
                int record = fieldName != "" && fieldDescription != "" ? getFieldIDFromTable(tableName, fieldName, out fieldID) : 0;

                if (record != 1 || !oUserFieldsMD.GetByKey(tableName, fieldID)) return false;

                bool change = updateCommonFields(oUserFieldsMD, tableName, fieldName, fieldDescription, fieldType, size, defaultValue, mandatory, subType, validValues);

                if (oUserFieldsMD.LinkedSystemObject != sysTable)
                {
                    if (sysTable > 0)
                    {
                        oUserFieldsMD.LinkedTable = "";
                        oUserFieldsMD.LinkedUDO = "";
                    }

                    oUserFieldsMD.LinkedSystemObject = sysTable;
                    change = true;
                }

                if (!change) return true;

                GC.Collect();
                GC.WaitForPendingFinalizers();

                if (oUserFieldsMD.Update() == 0) return true;

                SAP.SBOApplication.MessageBox($"Error : { SAP.SBOCompany.GetLastErrorDescription() }", 1, "Ok", "", "");
                return false;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oUserFieldsMD);
                oUserFieldsMD = null;
                GC.Collect();
            }
        }

        private static bool updateCommonFields(SAPbobsCOM.UserFieldsMD oUserFieldsMD,
                                               string tableName,
                                               string fieldName,
                                               string fieldDescription,
                                               SAPbobsCOM.BoFieldTypes fieldType,
                                               int size,
                                               string defaultValue,
                                               Boolean mandatory,
                                               SAPbobsCOM.BoFldSubTypes subType,
                                               string validValues)
        {
            bool change = false;

            if (oUserFieldsMD.Description != fieldDescription)
            {
                oUserFieldsMD.Description = fieldDescription;
                change = true;
            }

            if (oUserFieldsMD.Type != fieldType)
            {
                oUserFieldsMD.Type = fieldType;
                change = true;
            }

            if (oUserFieldsMD.SubType != subType)
            {
                oUserFieldsMD.SubType = subType;
                change = true;
            }

            int limit;

            switch (fieldType)
            {
                case SAPbobsCOM.BoFieldTypes.db_Numeric:
                    limit = 11;
                    break;
                case SAPbobsCOM.BoFieldTypes.db_Memo:
                case SAPbobsCOM.BoFieldTypes.db_Date:
                    limit = 0;
                    break;
                case SAPbobsCOM.BoFieldTypes.db_Float:
                    limit = 16;
                    break;
                default:
                    limit = 254;
                    break;
            }

            if (!(oUserFieldsMD.Size == limit || size >= limit) && size > oUserFieldsMD.Size)
            {
                oUserFieldsMD.Size = size;
                change = true;
            }

            if (!(oUserFieldsMD.EditSize == limit || size >= limit) && size > oUserFieldsMD.EditSize)
            {
                oUserFieldsMD.EditSize = size;
                change = true;
            }

            if (oUserFieldsMD.DefaultValue != defaultValue)
            {
                oUserFieldsMD.DefaultValue = defaultValue;
                change = true;
            }

            if (validValues != "")
            {
                int IvalidValues = 0;
                int initialCount = oUserFieldsMD.ValidValues.Count;

                foreach (string value in validValues.Split('|'))
                {
                    IvalidValues++;
                    string[] parm = value.Split(':');
                    bool isNew = false;

                    if (IvalidValues > initialCount)
                    {
                        isNew = true;
                        oUserFieldsMD.ValidValues.Add();
                        change = true;
                    }

                    oUserFieldsMD.ValidValues.SetCurrentLine(IvalidValues - 1);

                    if (isNew)
                    {
                        oUserFieldsMD.ValidValues.Value = parm[0];
                        oUserFieldsMD.ValidValues.Description = parm[1];
                        change = true;
                        continue;
                    }

                    if (oUserFieldsMD.ValidValues.Value != parm[0])
                    {
                        oUserFieldsMD.ValidValues.Value = parm[0];
                        change = true;
                    }

                    if (oUserFieldsMD.ValidValues.Description != parm[1])
                    {
                        oUserFieldsMD.ValidValues.Description = parm[1];
                        change = true;
                    }
                }
            }

            var sapmondary = ApplicationCommon.SAPBoolConversion(mandatory);

            if (oUserFieldsMD.Mandatory != sapmondary)
            {
                oUserFieldsMD.Mandatory = sapmondary;
                change = true;
            }

            return change;
        }

        public static bool createFieldList(string[] tableName,
                                           string[] fieldName,
                                           string[] fieldDescription,
                                           SAPbobsCOM.BoFieldTypes[] fieldType,
                                           int[] sizes,
                                           string[] defaults,
                                           bool[] mandatory,
                                           SAPbobsCOM.BoFldSubTypes[] subType,
                                           string[] validvalues,
                                           string[] linkedtable,
                                           bool forceupdate = false)
        {
            if (tableName.Length != fieldName.Length ||
                tableName.Length != fieldDescription.Length ||
                tableName.Length != fieldType.Length ||
                tableName.Length != sizes.Length ||
                tableName.Length != defaults.Length ||
                tableName.Length != mandatory.Length ||
                tableName.Length != subType.Length)
                throw new Exception("Non-matching List detected");

            for (int i = 0; i < tableName.Length; ++i)
            {
                if (!ApplicationCommon.createField(tableName[i], fieldName[i], fieldDescription[i], fieldType[i], sizes[i], defaults[i],
                    mandatory[i], subType[i], validvalues[i], linkedtable[i], forceupdate)) return false;
            }

            return true;
        }

        public static string getMenuID(SAPbouiCOM.Menus menus, string fatherMenuName, string fatherMenuUID, string searchMenu)
        {
            if (menus != null && menus.Count > 0)
            {
                foreach (SAPbouiCOM.MenuItem menuItem in menus)
                {
                    if (menuItem.String.Contains(searchMenu)) return menuItem.UID;

                    string result = getMenuID(menuItem.SubMenus, menuItem.String, menuItem.UID, searchMenu);

                    if (result.Length > 0) return result;
                }
            }

            return "";
        }

        public static string getMenuID(string searchMenu)
        {
            SAPbouiCOM.SboGuiApi objSBOGuiApi = new SAPbouiCOM.SboGuiApi();
            objSBOGuiApi.Connect((string)Environment.GetCommandLineArgs().GetValue(1));
            SAPbouiCOM.Menus menus = objSBOGuiApi.GetApplication().Menus;

            try
            {
                if (menus == null || menus.Count == 0) return "";

                foreach (SAPbouiCOM.MenuItem menuItem in menus)
                {
                    if (menuItem.String.Contains(searchMenu))
                        return menuItem.String;

                    string result = getMenuID(menuItem.SubMenus, menuItem.String, menuItem.UID, searchMenu);

                    if (result.Length > 0) return result;
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(objSBOGuiApi);
                objSBOGuiApi = null;
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(menus);
                menus = null;
                GC.Collect();
            }

            return "";
        }

        public static int getFieldIDFromTable(string tableName, string fieldName, out int fieldID)
        {
            SAPbobsCOM.Recordset oRec = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            try
            {
                oRec.DoQuery($"SELECT \"FieldID\" from \"CUFD\" where \"TableID\"='{ tableName }' AND \"AliasID\"='{ fieldName }'");
                fieldID = Convert.ToInt32(oRec.Fields.Item("FieldID").Value.ToString());
                int result = oRec.RecordCount;
                return result;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oRec);
                oRec = null;
                GC.Collect();
            }
        }

        public static int getFieldIDFromTable(string tableName, string fieldName)
        {
            SAPbobsCOM.Recordset oRec = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            try
            {
                oRec.DoQuery($"SELECT \"FieldID\" from \"CUFD\" where \"TableID\"='{ tableName }' AND \"AliasID\"='{ fieldName }'");
                return Convert.ToInt32(oRec.Fields.Item("FieldID").Value.ToString());
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oRec);
                oRec = null;
                GC.Collect();
            }
        }

        public static SAPbobsCOM.BoYesNoEnum SAPBoolConversion(bool boolean)
        {
            return boolean ? SAPbobsCOM.BoYesNoEnum.tYES : SAPbobsCOM.BoYesNoEnum.tNO;
        }

        public static Boolean createUDO(string udoName, string udoDescription, SAPbobsCOM.BoUDOObjType objType, string tableName, string childName, string findColumns, string columnDesc,
            bool manageSeries, bool canCancel, bool canClose, bool canDelete, bool log, string logName, bool defaultForm = false, bool enhancedForm = false, string xml = "",
            string childColumns = "", string headerColumns = "")
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            SAPbobsCOM.UserObjectsMD oUserObjectMD = (SAPbobsCOM.UserObjectsMD)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oUserObjectsMD);

            try
            {
                if (oUserObjectMD.GetByKey(udoName)) return true;

                SAP.setStatus($"Creating UDO : { udoDescription }");
                oUserObjectMD.Code = udoName;
                oUserObjectMD.Name = udoDescription;
                oUserObjectMD.ObjectType = objType;
                oUserObjectMD.TableName = tableName;
                oUserObjectMD.CanCancel = SAPBoolConversion(canCancel);
                oUserObjectMD.CanClose = SAPBoolConversion(canClose);
                oUserObjectMD.CanDelete = SAPBoolConversion(canDelete);
                oUserObjectMD.CanFind = SAPbobsCOM.BoYesNoEnum.tYES;
                oUserObjectMD.CanLog = SAPBoolConversion(log);
                oUserObjectMD.LogTableName = logName;
                oUserObjectMD.CanYearTransfer = SAPbobsCOM.BoYesNoEnum.tNO;
                oUserObjectMD.ExtensionName = "";
                oUserObjectMD.ManageSeries = SAPBoolConversion(manageSeries && objType == SAPbobsCOM.BoUDOObjType.boud_Document);

                CreateDefaultForm(oUserObjectMD, defaultForm, enhancedForm, udoName, xml);
                CreateChildren(oUserObjectMD, childName);

                string temp = columnDesc.Replace("|", "");
                string temp2 = findColumns.Replace("|", "");

                if (findColumns.Length - temp2.Length != columnDesc.Length - temp.Length)
                {
                    SAP.SBOApplication.MessageBox("Error : Column Name and Column Description count not match", 1, "Ok", "", "");
                    return false;
                }

                string key1, key2;

                if (objType == SAPbobsCOM.BoUDOObjType.boud_Document)
                {
                    key1 = "DocEntry";
                    key2 = "DocNum";
                }
                else
                {
                    key1 = "Code";
                    key2 = "Name";
                }

                if (objType == SAPbobsCOM.BoUDOObjType.boud_MasterData)
                {
                    CreateKeyFindColumns(oUserObjectMD, key1, key2);
                }

                CreateFindColumns(oUserObjectMD, findColumns, columnDesc);
                CreateKeyHeaders(oUserObjectMD, key1, key2);
                CreateHeaders(oUserObjectMD, headerColumns);
                CreateChildrenColumns(oUserObjectMD, childColumns, key1);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                int retry = 0;

                while (oUserObjectMD.Add() != 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    System.Threading.Thread.Sleep(100);

                    if (++retry == 100) goto FAILED;
                }

                return true;

                FAILED:
                SAP.SBOApplication.MessageBox($"Error : { SAP.SBOCompany.GetLastErrorDescription() }", 1, "Ok", "", "");
                return false;
            }
            finally
            {
                Marshal.FinalReleaseComObject(oUserObjectMD);
                oUserObjectMD = null;
                GC.Collect();
            }
        }

        private static void CreateDefaultForm(SAPbobsCOM.UserObjectsMD oUserObjectMD, bool defaultForm, bool enhancedForm, string udoName, string xml)
        {
            if (!defaultForm)
            {
                oUserObjectMD.CanCreateDefaultForm = SAPbobsCOM.BoYesNoEnum.tNO;
                return;
            }

            oUserObjectMD.MenuUID = udoName;
            oUserObjectMD.CanCreateDefaultForm = SAPbobsCOM.BoYesNoEnum.tYES;
            oUserObjectMD.EnableEnhancedForm = enhancedForm ? SAPbobsCOM.BoYesNoEnum.tYES : SAPbobsCOM.BoYesNoEnum.tNO;

            if (xml == String.Empty) return;

            oUserObjectMD.FormSRF = xml;
        }

        private static void CreateChildren(SAPbobsCOM.UserObjectsMD oUserObjectMD, string childName)
        {
            if (childName == String.Empty) return;

            var childtable = oUserObjectMD.ChildTables;
            int Iindex = 0;

            try
            {
                foreach (string childTable in childName.Split('|'))
                {
                    if (Iindex != 0) childtable.Add();

                    childtable.SetCurrentLine(Iindex++);
                    childtable.TableName = childTable;
                }
            }
            finally
            {
                Marshal.FinalReleaseComObject(childtable);
                childtable = null;
            }
        }

        private static void CreateKeyFindColumns(SAPbobsCOM.UserObjectsMD oUserObjectMD, string key1, string key2)
        {
            var findcol = oUserObjectMD.FindColumns;

            try
            {
                findcol.SetCurrentLine(0);
                findcol.ColumnAlias = key1;
                findcol.ColumnDescription = key1;
                findcol.Add();
                findcol.SetCurrentLine(1);
                findcol.ColumnAlias = key2;
                findcol.ColumnDescription = key2;
            }
            finally
            {
                Marshal.FinalReleaseComObject(findcol);
                findcol = null;
            }
        }

        private static void CreateFindColumns(SAPbobsCOM.UserObjectsMD oUserObjectMD, string findColumns, string columnDesc)
        {
            if (findColumns == String.Empty) return;

            var findcol = oUserObjectMD.FindColumns;
            int oriindex = findcol.Count;
            int Iindex = oriindex;

            try
            {
                foreach (string colName in findColumns.Split('|'))
                {
                    findcol.Add();
                    findcol.SetCurrentLine(Iindex++);
                    findcol.ColumnAlias = colName;
                }

                Iindex = oriindex;

                foreach (string colName in columnDesc.Split('|'))
                {
                    findcol.SetCurrentLine(Iindex++);
                    findcol.ColumnDescription = colName;
                }
            }
            finally
            {
                Marshal.FinalReleaseComObject(findcol);
                findcol = null;
            }
        }

        private static void CreateKeyHeaders(SAPbobsCOM.UserObjectsMD oUserObjectMD, string key1, string key2)
        {
            var formcol = oUserObjectMD.FormColumns;
            SAPbobsCOM.BoYesNoEnum canedit = oUserObjectMD.ObjectType == SAPbobsCOM.BoUDOObjType.boud_Document ? SAPbobsCOM.BoYesNoEnum.tNO : SAPbobsCOM.BoYesNoEnum.tYES;

            try
            {
                formcol.SetCurrentLine(0);
                formcol.SonNumber = 0;
                formcol.FormColumnAlias = key1;
                formcol.FormColumnDescription = key1;
                formcol.Editable = canedit;
                formcol.Add();
                formcol.SetCurrentLine(1);
                formcol.SonNumber = 0;
                formcol.FormColumnAlias = key2;
                formcol.FormColumnDescription = key2;
                formcol.Editable = canedit;
            }
            finally
            {
                Marshal.FinalReleaseComObject(formcol);
                formcol = null;
            }
        }

        private static void CreateHeaders(SAPbobsCOM.UserObjectsMD oUserObjectMD, string headerColumns)
        {
            if (headerColumns == String.Empty) return;

            var formcol = oUserObjectMD.FormColumns;

            try
            {
                int Iindex = formcol.Count;

                foreach (var col in headerColumns.Split('|'))
                {
                    formcol.Add();
                    string[] parm = col.Split(':');
                    formcol.SetCurrentLine(Iindex++);
                    formcol.SonNumber = Convert.ToInt32(parm[0]);
                    formcol.FormColumnAlias = parm[1];
                    formcol.FormColumnDescription = parm[2];
                    formcol.Editable = parm.Length < 3 || parm[3] == "Y" ? SAPbobsCOM.BoYesNoEnum.tYES : SAPbobsCOM.BoYesNoEnum.tNO;
                }
            }
            finally
            {
                Marshal.FinalReleaseComObject(formcol);
                formcol = null;
            }
        }

        private static void CreateChildrenColumns(SAPbobsCOM.UserObjectsMD oUserObjectMD, string childColumns, string key1)
        {
            if (childColumns == String.Empty) return;

            var formcol = oUserObjectMD.EnhancedFormColumns;
            int Iindex = 0;

            try
            {
                Action<string, int> action = (string col, int row) =>
                {
                    formcol.SetCurrentLine(Iindex++);
                    formcol.ChildNumber = row;
                    formcol.ColumnAlias = col;
                    formcol.ColumnDescription = col;
                    formcol.ColumnIsUsed = SAPbobsCOM.BoYesNoEnum.tNO;
                    formcol.Editable = SAPbobsCOM.BoYesNoEnum.tNO;
                };

                for (int i = 1; i <= oUserObjectMD.ChildTables.Count; ++i)
                {
                    if (i != 1) formcol.Add();

                    action(key1, i);
                    formcol.Add();
                    action("LineId", i);
                    formcol.Add();
                    action("Object", i);
                    formcol.Add();
                    action("LogInst", i);
                }

                foreach (var cols in childColumns.Split('|'))
                {
                    formcol.Add();
                    string[] parm = cols.Split(':');
                    formcol.SetCurrentLine(Iindex++);
                    formcol.ChildNumber = Convert.ToInt32(parm[0]);
                    formcol.ColumnAlias = parm[1];
                    formcol.ColumnDescription = parm[2];
                    formcol.ColumnIsUsed = parm.Length < 3 || parm[3] == "Y" ? SAPbobsCOM.BoYesNoEnum.tYES : SAPbobsCOM.BoYesNoEnum.tNO;
                    formcol.Editable = formcol.ColumnIsUsed;
                }
            }
            finally
            {
                Marshal.FinalReleaseComObject(formcol);
                formcol = null;
            }
        }

        public static bool createUserKeys(string tableName, string keyName, string columns, SAPbobsCOM.BoYesNoEnum isUnique)
        {
            SAPbobsCOM.Recordset rc = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            try
            {
                rc.DoQuery($"SELECT * FROM \"OUKD\" WHERE \"TableName\"='@{ tableName }' AND \"KeyName\"='{ keyName }'");

                if (rc.RecordCount > 0) return true;
            }
            finally
            {
                Marshal.FinalReleaseComObject(rc);
                rc = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            SAPbobsCOM.UserKeysMD oUserKeys = (SAPbobsCOM.UserKeysMD)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oUserKeys);

            try
            {
                SAP.setStatus($"Creating User Keys : { tableName } - { keyName }");
                oUserKeys.TableName = tableName;
                oUserKeys.KeyName = keyName;

                int columnno = 0;

                foreach (string column in columns.Split('|'))
                {
                    columnno++;

                    if (columnno > 0)
                    {
                        oUserKeys.Elements.Add();
                        oUserKeys.Elements.SetCurrentLine(columnno);
                    }

                    oUserKeys.Elements.ColumnAlias = column;
                }

                oUserKeys.Unique = isUnique;

                GC.Collect();
                GC.WaitForPendingFinalizers();
                int retry = 0;

                while (oUserKeys.Add() != 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    System.Threading.Thread.Sleep(100);

                    if (++retry == 100) goto FAILED;
                }

                return true;

                FAILED:
                SAP.SBOApplication.MessageBox(SAP.SBOCompany.GetLastErrorDescription(), 1, "&Ok", "", "");
                return false;
            }
            finally
            {
                Marshal.FinalReleaseComObject(oUserKeys);
                oUserKeys = null;
                GC.Collect();
            }
        }

        private static MenuItem createCustomPO(string uniqueID, string name, int position)
        {
            return new MenuItem(uniqueID, name + " Purchase Order", position);
        }

        private static MenuItem[] loopMenu(List<MenuInfo> menuinfolist)
        {
            int pos = menuinfolist.Max(menuinfo => menuinfo.position) + 50;
            List<MenuItem> menulist = new List<MenuItem>();

            do
            {
                var curMenu = menuinfolist.First();

                if (!curMenu.menuid.Contains("."))
                {
                    var f_menuinfolist = menuinfolist.Where(menuinfo =>
                    {
                        menuinfo.position = menuinfo.position == -1 ? pos++ : menuinfo.position;
                        return menuinfo.menuid == curMenu.menuid;
                    });
                    menulist.AddRange(f_menuinfolist.OrderBy(menuinfo => menuinfo.position)
                                                    .Select(menuinfo => new MenuItem(menuinfo)));
                    menuinfolist.RemoveAll(menuinfo => menuinfo.menuid == curMenu.menuid);
                    continue;
                }

                string[] split = curMenu.menuid.Split('.');
                string baseMenu = split[0];
                string curMenuHeader = split[1];
                MenuItem col = new MenuItem(curMenuHeader, curMenuHeader.Replace('_', ' '), pos++);
                var temp_menuinfolist = menuinfolist.Where(menuinfo => menuinfo.menuid.StartsWith($"{ baseMenu }."))
                                                    .ToList();
                temp_menuinfolist.ForEach(menuinfo => menuinfo.menuid = menuinfo.menuid.Substring(baseMenu.Length + 1));

                foreach (var menu in loopMenu(temp_menuinfolist))
                {
                    col.addChild(menu);
                }

                menulist.Add(col);
                menuinfolist.RemoveAll(menuinfo => menuinfo.menuid.StartsWith($"{ baseMenu }."));
            } while (menuinfolist.Count > 0);

            return menulist.ToArray();
        }

        public static Boolean createMainMenu()
        {
            Dictionary<string, List<MenuInfo>> formMap = new Dictionary<string, List<MenuInfo>>();

            foreach (var formtype in FormManager.list)
            {
                if (!formtype.HasMenuId()) continue;

                var menuid = formtype.GetMenuId();
                string menuname = formtype.GetMenuName();

                if (!formMap.ContainsKey(menuid)) formMap.Add(menuid, new List<MenuInfo>());

                formMap[menuid].Add(new MenuInfo()
                {
                    formcode = formtype.GetFormCode(),
                    menuid = menuid,
                    menuname = menuname,
                    position = menuid.pos
                });
            }

            if (formMap.Count > 0)
            {
                foreach (string menuId in formMap.Keys)
                {
                    formMap[menuId].Sort((x, y) => string.Compare(x.formcode, y.formcode));
                    MenuItem[] menus = loopMenu(formMap[menuId]);

                    foreach (var menu in menus)
                    {
                        menu.Create(menuId);
                    }
                }
            }

            return true;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetMethodName(int level = 1)
        {
            return new StackFrame(level).GetMethod().Name;
        }

        public static string QueryCode(SAPbouiCOM.Form oForm, int row = 0, params object[] args) => SQLQuery.QueryCode(oForm, row, args);
        public static string QueryCode(QueryInfo info) => SQLQuery.QueryCode(info);
        public static string QueryCode(string code) => SQLQuery.QueryCode(code);
        public static string QueryCode(SAPbouiCOM.Form oForm, QueryInfo info, int row = 0, params object[] args) => SQLQuery.QueryCode(oForm, info, row, args);
        public static string QueryCode(SAPbouiCOM.Form oForm, string code, int row = 0, params object[] args) => SQLQuery.QueryCode(oForm, code, row, args);
        public static string QueryCode(DataTable dt, QueryInfo info, params object[] args) => SQLQuery.QueryCode(dt, info, args);
        public static string QueryCode(DataTable dt, string code, params object[] args) => SQLQuery.QueryCode(dt, code, args);
        public static string QueryCode(QueryInfo info, params object[] args) => SQLQuery.QueryCode(info, args);
        public static string QueryCode(string code, params object[] args) => SQLQuery.QueryCode(code, args);
        public static void FillFromSAPSQL<T>(Type type, out IEnumerable<T> output, params object[] args) => SQLQuery.FillFromSAPSQL<T>($"{ type.Name }.{ GetMethodName(2) }", out output, args);
        public static void FillFromSAPSQL<T>(string code, out IEnumerable<T> output, params object[] args) => SQLQuery.FillFromSAPSQL<T>(code, out output, args);
        public static void FillFromSAPSQL(Type type, out SAPbobsCOM.Recordset output, params object[] args) => SQLQuery.FillFromSAPSQL($"{ type.Name }.{ GetMethodName(2) }", out output, args);
        public static void FillFromSAPSQL(string code, out SAPbobsCOM.Recordset output, params object[] args) => SQLQuery.FillFromSAPSQL(code, out output, args);
        public static void FillFromSAPSQL<T>(SAPbouiCOM.Form oForm, out IEnumerable<T> output, int row = 0, params object[] args) => SQLQuery.FillFromSAPSQL<T>(oForm, $"{ oForm.TypeEx }.{ GetMethodName(2) }", out output, row, args);
        public static void FillFromSAPSQL<T>(SAPbouiCOM.Form oForm, string code, out IEnumerable<T> output, int row = 0, params object[] args) => SQLQuery.FillFromSAPSQL<T>(oForm, code, out output, row, args);
        public static void FillFromSAPSQL(SAPbouiCOM.Form oForm, out SAPbobsCOM.Recordset output, int row = 0, params object[] args) => SQLQuery.FillFromSAPSQL(oForm, $"{ oForm.TypeEx }.{ GetMethodName(2) }", out output, row, args);
        public static void FillFromSAPSQL(SAPbouiCOM.Form oForm, string code, out SAPbobsCOM.Recordset output, int row = 0, params object[] args) => SQLQuery.FillFromSAPSQL(oForm, code, out output, row, args);
        public static void FillFromSAPSQL(Type type, SAPbouiCOM.DataTable dt) => SQLQuery.FillFromSAPSQL($"{ type.Name }.{ GetMethodName(2) }", dt);
        public static void FillFromSAPSQL(string code, SAPbouiCOM.DataTable dt) => SQLQuery.FillFromSAPSQL(code, dt);
        public static void FillFromSAPSQL(SAPbouiCOM.Form oForm, SAPbouiCOM.DataTable dt, int row = 0, params object[] args) => SQLQuery.FillFromSAPSQL(oForm, $"{ oForm.TypeEx }.{ GetMethodName(2) }", dt, row, args);
        public static void FillFromSAPSQL(SAPbouiCOM.Form oForm, string code, SAPbouiCOM.DataTable dt, int row = 0, params object[] args) => SQLQuery.FillFromSAPSQL(oForm, code, dt, row, args);
        public static void ExeFromSAPSQL(Type type) => SQLQuery.ExeFromSAPSQL($"{ type.Name }.{ GetMethodName(2) }");
        public static void ExeFromSAPSQL(string code) => SQLQuery.ExeFromSAPSQL(code);
        public static void ExeFromSAPSQL(SAPbouiCOM.Form oForm, int row = 0, params object[] args) => SQLQuery.ExeFromSAPSQL(oForm, $"{ oForm.TypeEx }.{ GetMethodName(2) }", row, args);
        public static void ExeFromSAPSQL(SAPbouiCOM.Form oForm, string code, int row = 0, params object[] args) => SQLQuery.ExeFromSAPSQL(oForm, code, row, args);
        public static void ExeFromSAPSQL(DataTable dt, params object[] args) => SQLQuery.ExeFromSAPSQL($"{ dt.TableName }.{ GetMethodName(2) }", dt, args);
        public static void ExeFromSAPSQL(string code, DataTable dt, params object[] args) => SQLQuery.ExeFromSAPSQL(code, dt, args);
        public static object SecureQuery(string query, Func<object> action) => SQLQuery.SecureQuery(query, action);
        public static void SecureQuery(string query, Action action) => SQLQuery.SecureQuery(query, action);
    }
}

using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FT_ADDON.UserTable;

namespace FT_ADDON
{
    public class UserObject
    {
        public UserTable Table { get; set; }

        private string Name { get => Table.TableName; }
        private string Description { get => Table.TableInfo; }
        public SAPbobsCOM.BoUDOObjType ObjType { get => Table.TableType == SAPbobsCOM.BoUTBTableType.bott_MasterData ? SAPbobsCOM.BoUDOObjType.boud_MasterData : SAPbobsCOM.BoUDOObjType.boud_Document; }
        public bool CanCancel { get; set; }
        public bool CanClose { get; set; }
        public bool CanDelete { get; set; }
        public bool CanLog { get; set; }
        public string LogTableName { get; set; }
        public bool ManagedSeries { get; set; }
        public bool DefaultForm { get; set; }
        public bool EnhancedForm { get; set; }
        public string XmlForm { get; set; }
        public string KeyColumn1 { get => ObjType == SAPbobsCOM.BoUDOObjType.boud_Document ? "DocEntry" : "Code"; }
        public string KeyColumn2 { get => ObjType == SAPbobsCOM.BoUDOObjType.boud_Document ? "DocNum" : "Name"; }
        public List<string> Children { get => Table.Children.Select(child => child.TableName).ToList(); }

        public UserObject(UserTable table)
        {
            Table = table;
        }

        public bool Create()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            SAPbobsCOM.UserObjectsMD oUserObjectMD = (SAPbobsCOM.UserObjectsMD)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oUserObjectsMD);

            try
            {
                if (oUserObjectMD.GetByKey(Name)) return true;

                SAP.setStatus($"Creating UDO : { Description }");

                oUserObjectMD.SetUDOCode(Name);
                oUserObjectMD.SetUDOName(Description);
                oUserObjectMD.SetObjType(ObjType);
                oUserObjectMD.SetCanCancel(CanCancel);
                oUserObjectMD.SetCanClose(CanClose);
                oUserObjectMD.SetCanDelete(CanDelete);
                oUserObjectMD.SetCanFind(true);
                oUserObjectMD.SetCanLog(CanLog);
                oUserObjectMD.SetLogTableName(LogTableName);
                oUserObjectMD.SetCanYearTransfer(false);
                oUserObjectMD.SetExtensionName("");
                oUserObjectMD.SetManageSeries(ManagedSeries && ObjType == SAPbobsCOM.BoUDOObjType.boud_Document);
                oUserObjectMD.SetDefaultForm(DefaultForm, EnhancedForm, Name, XmlForm);

                oUserObjectMD.SetChildren(Table);
                oUserObjectMD.SetKeyColumn(this);
                oUserObjectMD.SetFormColumns(Table);
                oUserObjectMD.SetChildrenColumns(this);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                int retry = 0;

                while (oUserObjectMD.Add() != 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    System.Threading.Thread.Sleep(100);

                    if (++retry < 100) continue;

                    SAP.SBOApplication.MessageBox($"Error : { SAP.SBOCompany.GetLastErrorDescription() }", 1, "Ok", "", "");
                    return false;
                }

                return true;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oUserObjectMD);
                oUserObjectMD = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

    }

    public static class UserObjectExtensions
    {
        private static SAPbobsCOM.BoYesNoEnum ConvertToEnum(bool value) => value ? SAPbobsCOM.BoYesNoEnum.tYES : SAPbobsCOM.BoYesNoEnum.tNO;

        public static void SetUDOCode(this SAPbobsCOM.UserObjectsMD oUserObjectMD, string Name)
        {
            oUserObjectMD.Code = Name;
            oUserObjectMD.TableName = Name;
        }

        public static void SetUDOName(this SAPbobsCOM.UserObjectsMD oUserObjectMD, string Description) => oUserObjectMD.Name = Description;
        public static void SetObjType(this SAPbobsCOM.UserObjectsMD oUserObjectMD, SAPbobsCOM.BoUDOObjType ObjType) => oUserObjectMD.ObjectType = ObjType;
        public static void SetCanCancel(this SAPbobsCOM.UserObjectsMD oUserObjectMD, bool CanCancel) => oUserObjectMD.CanCancel = ConvertToEnum(CanCancel);
        public static void SetCanClose(this SAPbobsCOM.UserObjectsMD oUserObjectMD, bool CanClose) => oUserObjectMD.CanClose = ConvertToEnum(CanClose);
        public static void SetCanDelete(this SAPbobsCOM.UserObjectsMD oUserObjectMD, bool CanDelete) => oUserObjectMD.CanDelete = ConvertToEnum(CanDelete);
        public static void SetCanFind(this SAPbobsCOM.UserObjectsMD oUserObjectMD, bool CanFind) => oUserObjectMD.CanFind = ConvertToEnum(CanFind);
        public static void SetCanLog(this SAPbobsCOM.UserObjectsMD oUserObjectMD, bool CanLog) => oUserObjectMD.CanLog = ConvertToEnum(CanLog);
        public static void SetLogTableName(this SAPbobsCOM.UserObjectsMD oUserObjectMD, string LogTableName) => oUserObjectMD.LogTableName = LogTableName;
        public static void SetCanYearTransfer(this SAPbobsCOM.UserObjectsMD oUserObjectMD, bool CanYearTransfer) => oUserObjectMD.CanYearTransfer = ConvertToEnum(CanYearTransfer);
        public static void SetExtensionName(this SAPbobsCOM.UserObjectsMD oUserObjectMD, string ExtensionName) => oUserObjectMD.ExtensionName = ExtensionName;
        public static void SetManageSeries(this SAPbobsCOM.UserObjectsMD oUserObjectMD, bool ManageSeries) => oUserObjectMD.ManageSeries = ConvertToEnum(ManageSeries);

        public static void SetDefaultForm(this SAPbobsCOM.UserObjectsMD oUserObjectMD, bool DefaultForm, bool EnhancedForm, string UDOName, string XmlForm)
        {
            if (!DefaultForm)
            {
                oUserObjectMD.CanCreateDefaultForm = SAPbobsCOM.BoYesNoEnum.tNO;
                return;
            }

            oUserObjectMD.MenuUID = UDOName;
            oUserObjectMD.CanCreateDefaultForm = SAPbobsCOM.BoYesNoEnum.tYES;
            oUserObjectMD.EnableEnhancedForm = ConvertToEnum(EnhancedForm);

            if (String.IsNullOrEmpty(XmlForm)) return;

            oUserObjectMD.FormSRF = XmlForm;
        }

        public static void SetFormColumns(this SAPbobsCOM.UserObjectsMD oUserObjectMD, UserTable Table)
        {
            oUserObjectMD.SetModFormColumns(Table);
            oUserObjectMD.SetFindFormColumns(Table);
        }

        private static void SetModFormColumns(this SAPbobsCOM.UserObjectsMD oUserObjectMD, UserTable Table)
        {
            var ModColumns = Table.Fields.Where(field => field.canmodify);

            if (ModColumns.Count() == 0) return;

            var formcol = oUserObjectMD.FormColumns;

            try
            {
                foreach (var col in ModColumns)
                {
                    if (!String.IsNullOrEmpty(formcol.FormColumnAlias)) formcol.Add();

                    formcol.SonNumber = 0;
                    formcol.FormColumnAlias = col.fieldname;
                    formcol.FormColumnDescription = col.fieldinfo;
                    formcol.Editable = SAPbobsCOM.BoYesNoEnum.tYES;
                }

                for (int i = 0; i < Table.Children.Count; i++)
                {
                    var ChildModColumns = Table.Children[i].Fields.Where(field => field.canmodify);

                    if (ChildModColumns.Count() == 0) continue;

                    foreach (var col in ChildModColumns)
                    {
                        formcol.SonNumber = i + 1;
                        formcol.FormColumnAlias = col.fieldname;
                        formcol.FormColumnDescription = col.fieldinfo;
                        formcol.Editable = SAPbobsCOM.BoYesNoEnum.tYES;
                    }
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(formcol);
                formcol = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private static void SetFindFormColumns(this SAPbobsCOM.UserObjectsMD oUserObjectMD, UserTable Table)
        {
            var FindColumns = Table.Fields.Where(field => field.canfind);

            if (FindColumns.Count() == 0) return;

            var findcol = oUserObjectMD.FindColumns;

            try
            {
                foreach (var col in FindColumns)
                {
                    if (!String.IsNullOrEmpty(findcol.ColumnAlias)) findcol.Add();

                    findcol.ColumnAlias = col.fieldname;
                    findcol.ColumnDescription = col.fieldinfo;
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(findcol);
                findcol = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        public static void SetChildren(this SAPbobsCOM.UserObjectsMD oUserObjectMD, UserTable Table)
        {
            if (Table.Children.Count == 0) return;

            var childtable = oUserObjectMD.ChildTables;

            try
            {
                for (int i = 0; i < Table.Children.Count; i++)
                {
                    if (!String.IsNullOrEmpty(childtable.TableName)) childtable.Add();

                    childtable.TableName = Table.Children[i].TableName;
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(childtable);
                childtable = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        public static void SetKeyColumn(this SAPbobsCOM.UserObjectsMD oUserObjectMD, UserObject UO)
        {
            oUserObjectMD.SetModKeyColumns(UO);
            oUserObjectMD.SetFindKeyColumns(UO);
        }

        public static void SetModKeyColumns(this SAPbobsCOM.UserObjectsMD oUserObjectMD, UserObject UO)
        {
            var formcol = oUserObjectMD.FormColumns;

            SAPbobsCOM.BoYesNoEnum canedit = ConvertToEnum(UO.ObjType == SAPbobsCOM.BoUDOObjType.boud_Document);

            try
            {
                formcol.SonNumber = 0;
                formcol.FormColumnAlias = UO.KeyColumn1;
                formcol.FormColumnDescription = UO.KeyColumn2;
                formcol.Editable = canedit;
                formcol.Add();
                formcol.SonNumber = 0;
                formcol.FormColumnAlias = UO.KeyColumn2;
                formcol.FormColumnDescription = UO.KeyColumn2;
                formcol.Editable = canedit;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(formcol);
                formcol = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        public static void SetFindKeyColumns(this SAPbobsCOM.UserObjectsMD oUserObjectMD, UserObject UO)
        {
            if (UO.ObjType != SAPbobsCOM.BoUDOObjType.boud_MasterData) return;

            var findcol = oUserObjectMD.FindColumns;

            try
            {
                findcol.ColumnAlias = UO.KeyColumn1;
                findcol.ColumnDescription = UO.KeyColumn1;
                findcol.Add();
                findcol.ColumnAlias = UO.KeyColumn2;
                findcol.ColumnDescription = UO.KeyColumn2;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(findcol);
                findcol = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        public static void SetChildrenColumns(this SAPbobsCOM.UserObjectsMD oUserObjectMD, UserObject UO)
        {
            if (UO.Table.Children.Count == 0) return;

            var formcol = oUserObjectMD.EnhancedFormColumns;

            try
            {
                Action<string, int> Add_Column_Action = (string col, int row) =>
                {
                    if (!String.IsNullOrEmpty(formcol.ColumnAlias)) formcol.Add();

                    formcol.ChildNumber = row;
                    formcol.ColumnAlias = col;
                    formcol.ColumnDescription = col;
                    formcol.ColumnIsUsed = SAPbobsCOM.BoYesNoEnum.tNO;
                    formcol.Editable = SAPbobsCOM.BoYesNoEnum.tNO;
                };

                for (int i = 1; i <= oUserObjectMD.ChildTables.Count; ++i)
                {
                    int row = i + 1;
                    Add_Column_Action(UO.KeyColumn1, row);
                    Add_Column_Action("LineId", row);
                    Add_Column_Action("Object", row);
                    Add_Column_Action("LogInst", row);
                }

                for (int i = 0; i < UO.Table.Children.Count; i++)
                {
                    foreach (var field in UO.Table.Children[i].Fields)
                    {
                        if (!String.IsNullOrEmpty(formcol.ColumnAlias)) formcol.Add();

                        formcol.ChildNumber = i + 1;
                        formcol.ColumnAlias = field.fieldname;
                        formcol.ColumnDescription = field.fieldinfo;
                        formcol.ColumnIsUsed = ConvertToEnum(field.canmodify);
                        formcol.Editable = formcol.ColumnIsUsed;
                    }
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(formcol);
                formcol = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
}

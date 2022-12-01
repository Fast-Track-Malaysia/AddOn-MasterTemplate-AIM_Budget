using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    public class UserTable
    {
        public enum FormType
        {
            None,
            Matrix,
            HeaderLines,
        }

        public UserTable(string tablename, string tableinfo, SAPbobsCOM.BoUTBTableType type = SAPbobsCOM.BoUTBTableType.bott_NoObject)
        {
            _TableName = tablename;
            _TableInfo = tableinfo;
            _TableType = type;

            createTable();
        }

        //private List<Tuple<string, string>> fieldlist = new List<Tuple<string, string>>();
        //private List<Tuple<string, string>> canfindfields = new List<Tuple<string, string>>();
        //private List<Tuple<string, string>> canmodfields = new List<Tuple<string, string>>();


        private string _TableName;
        public string TableName { get => _TableName; }

        private string _TableInfo;
        public string TableInfo { get => _TableInfo; }

        private SAPbobsCOM.BoUTBTableType _TableType;
        public SAPbobsCOM.BoUTBTableType TableType { get => _TableType; }

        public List<UserTable> Children { get; set; } = new List<UserTable>();
        public List<UserField> Fields { get; set; } = new List<UserField>();
        private UserObject UDO { get; set; }

        private bool createTable()
        {
            GC.Collect();
            SAPbobsCOM.UserTablesMD oUserTableMD = (SAPbobsCOM.UserTablesMD)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oUserTables);

            try
            {
                if (oUserTableMD.GetByKey(TableName)) return true;

                SAP.setStatus($"Creating Table : { TableName } - { TableInfo }");
                oUserTableMD.TableName = TableName;
                oUserTableMD.TableDescription = TableInfo;
                oUserTableMD.TableType = TableType;

                GC.Collect();
                GC.WaitForPendingFinalizers();

                if (oUserTableMD.Add() == 0) return true;

                SAP.SBOApplication.MessageBox(SAP.SBOCompany.GetLastErrorDescription(), 1, "&Ok", "", "");
                return false;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oUserTableMD);
                oUserTableMD = null;
                GC.Collect();
            }
        }

        public static bool createField(string tablename,
                                       string fieldname,
                                       string fieldinfo,
                                       SAPbobsCOM.BoFieldTypes fieldtype = SAPbobsCOM.BoFieldTypes.db_Alpha,
                                       int fieldsize = 254,
                                       string defaultvalue = "",
                                       bool mandatory = false,
                                       SAPbobsCOM.BoFldSubTypes subtype = SAPbobsCOM.BoFldSubTypes.st_None,
                                       string validvalue = "",
                                       string linkedtable = "",
                                       bool forceupdate = false)
        {
            UserField field = new UserField(tablename, fieldname)
            {
                fieldinfo = fieldinfo,
                fieldtype = fieldtype,
                fieldsize = fieldsize,
                defaultvalue = defaultvalue,
                mandatory = mandatory,
                subtype = subtype,
                validvalues = validvalue,
                linkedtable = linkedtable
            };

            if (!field.Exists()) return field.Create();

            if (!forceupdate) return true;

            return field.Update();
            //return ApplicationCommon.createField(tablename, fieldname, fieldinfo, fieldtype, fieldsize, defaultvalue, mandatory, subtype, validvalue, linkedtable, forceupdate);
        }
        
        public static bool createField(string tablename,
                                       string fieldname,
                                       string fieldinfo,
                                       SAPbobsCOM.UDFLinkedSystemObjectTypesEnum sysTable,
                                       SAPbobsCOM.BoFieldTypes fieldtype = SAPbobsCOM.BoFieldTypes.db_Alpha,
                                       int fieldsize = 254,
                                       string defaultvalue = "",
                                       bool mandatory = false,
                                       SAPbobsCOM.BoFldSubTypes subtype = SAPbobsCOM.BoFldSubTypes.st_None,
                                       string validvalue = "",
                                       bool forceupdate = false)
        {
            UserField field = new UserField(tablename, fieldname)
            {
                fieldinfo = fieldinfo,
                fieldtype = fieldtype,
                fieldsize = fieldsize,
                defaultvalue = defaultvalue,
                mandatory = mandatory,
                subtype = subtype,
                validvalues = validvalue,
                systable = sysTable
            };

            if (!field.Exists()) return field.Create();

            if (!forceupdate) return true;

            return field.Update();
            //return ApplicationCommon.createField(tablename, fieldname, fieldinfo, sysTable, fieldtype, fieldsize, defaultvalue, mandatory, subtype, validvalue, forceupdate);
        }

        public bool createField(string fieldname, 
                                string fieldinfo, 
                                SAPbobsCOM.BoFieldTypes fieldtype = SAPbobsCOM.BoFieldTypes.db_Alpha, 
                                int fieldsize = 254, 
                                string defaultvalue = "",
                                bool mandatory = false,
                                SAPbobsCOM.BoFldSubTypes subtype = SAPbobsCOM.BoFldSubTypes.st_None,
                                bool canFind = false,
                                bool canMod = false,
                                string validvalue = "",
                                string linkedtable = "",
                                bool forceupdate = false)
        {
            //if (!fieldCheckOut(fieldname, fieldinfo, canFind, canMod)) return true;

            UserField field = new UserField($"@{ TableName }", fieldname)
            {
                fieldinfo = fieldinfo,
                fieldtype = fieldtype,
                fieldsize = fieldsize,
                defaultvalue = defaultvalue,
                mandatory = mandatory,
                subtype = subtype,
                validvalues = validvalue,
                linkedtable = linkedtable
            };

            field.canfind = canFind;
            field.canmodify = canMod;

            Fields.Add(field);

            if (!field.Exists()) return field.Create();

            if (!forceupdate) return true;

            return field.Update();
            //return ApplicationCommon.createField($"@{ _TableName }", fieldname, fieldinfo, fieldtype, fieldsize, defaultvalue, mandatory, subtype, validvalue, linkedtable, forceupdate);
        }

        public bool createField(string fieldname, 
                                string fieldinfo,
                                SAPbobsCOM.UDFLinkedSystemObjectTypesEnum sysTable,
                                SAPbobsCOM.BoFieldTypes fieldtype = SAPbobsCOM.BoFieldTypes.db_Alpha, 
                                int fieldsize = 254, 
                                string defaultvalue = "",
                                bool mandatory = false,
                                SAPbobsCOM.BoFldSubTypes subtype = SAPbobsCOM.BoFldSubTypes.st_None,
                                bool canFind = false,
                                bool canMod = false,
                                string validvalue = "",
                                bool forceupdate = false)
        {
            //if (!fieldCheckOut(fieldname, fieldinfo, canFind, canMod)) return true;

            UserField field = new UserField($"@{ TableName }", fieldname)
            {
                fieldinfo = fieldinfo,
                fieldtype = fieldtype,
                fieldsize = fieldsize,
                defaultvalue = defaultvalue,
                mandatory = mandatory,
                subtype = subtype,
                validvalues = validvalue,
                systable = sysTable
            };

            field.canfind = canFind;
            field.canmodify = canMod;

            Fields.Add(field);

            if (!field.Exists()) return field.Create();

            if (!forceupdate) return true;

            return field.Update();
            //return ApplicationCommon.createField($"@{ _TableName }", fieldname, fieldinfo, sysTable, fieldtype, fieldsize, defaultvalue, mandatory, subtype, validvalue, forceupdate);
        }

        //private bool fieldCheckOut(string fieldname, string fieldinfo, bool canFind, bool canMod)
        //{
        //    if (fieldlist.Any(x => x.Item1 == $"U_{ fieldname }"))
        //    {
        //        return false;
        //    }

        //    fieldlist.Add(new Tuple<string, string>($"U_{ fieldname }", fieldinfo));

        //    if (canFind)
        //    {
        //        canfindfields.Add(fieldlist.Last());
        //    }

        //    if (canMod)
        //    {
        //        canmodfields.Add(fieldlist.Last());
        //    }

        //    return true;
        //}

        //private void GetChildInfo(out string childrenstring, out string canfindfield, out string canfindfieldinfo, out string canmodfield, out string canmodchildfield)
        //{
        //    childrenstring = "";
        //    canfindfield = "";
        //    canfindfieldinfo = "";
        //    canmodfield = ""; 
        //    canmodchildfield = "";

        //    foreach (var each in Children)
        //    {
        //        childrenstring += each.TableName + "|";
        //    }

        //    if (childrenstring.Length > 0) childrenstring = childrenstring.Remove(childrenstring.Length - 1);

        //    foreach (var each in canfindfields)
        //    {
        //        canfindfield += each.Item1 + "|";
        //        canfindfieldinfo += each.Item2 + "|";
        //    }

        //    if (canfindfield.Length > 0)
        //    {
        //        canfindfield = canfindfield.Remove(canfindfield.Length - 1);
        //        canfindfieldinfo = canfindfieldinfo.Remove(canfindfieldinfo.Length - 1);
        //    }

        //    foreach (var each in canmodfields)
        //    {
        //        canmodfield += $"0:{ each.Item1 }:{ each.Item2 }:Y|";
        //    }

        //    for (int i = 0; i < Children.Count; ++i)
        //    {
        //        foreach (var each in Children[i].canmodfields)
        //        {
        //            canmodfield += $"{(i + 1) }:{ each.Item1 }:{ each.Item2 }:N|";
        //        }
        //    }

        //    if (canmodfield.Length > 0)
        //    {
        //        canmodfield = canmodfield.Remove(canmodfield.Length - 1);
        //    }

        //    for (int i = 0; i < Children.Count; ++i)
        //    {
        //        foreach (var each in Children[i].fieldlist)
        //        {
        //            canmodchildfield += $"{ (i + 1) }:{ each.Item1 }:{ each.Item2 }:{ (Children[i].canmodfields.Contains(each) ? "Y" : "N") }|";
        //        }
        //    }

        //    if (canmodchildfield.Length > 0)
        //    {
        //        canmodchildfield = canmodchildfield.Remove(canmodchildfield.Length - 1);
        //    }
        //}

        public bool createUDO(FormType viewType = FormType.None, string xmlform = "", bool managedSeries = false, bool canCancel = true, bool canClose = true, bool canDelete = true, bool log = false, string logName = "")
        {
            //GetChildInfo(out string childrenstring, out string canfindfield, out string canfindfieldinfo, out string canmodfield, out string canmodchildfield);

            if (UDO != null) return true;

            UDO = new UserObject(this);
            UDO.XmlForm = xmlform;
            UDO.ManagedSeries = managedSeries;
            UDO.CanCancel = canCancel;
            UDO.CanClose = canClose;
            UDO.CanDelete = canDelete;
            UDO.CanLog = log;
            UDO.LogTableName = logName;
            UDO.DefaultForm = viewType != FormType.None;
            UDO.EnhancedForm = viewType == FormType.HeaderLines;
            return UDO.Create();

            //return ApplicationCommon.createUDO(_TableName,
            //                                   TableInfo,
            //                                   TableType == SAPbobsCOM.BoUTBTableType.bott_MasterData ?
            //                                                  SAPbobsCOM.BoUDOObjType.boud_MasterData :
            //                                                  SAPbobsCOM.BoUDOObjType.boud_Document,
            //                                   TableName,
            //                                   childrenstring,
            //                                   canfindfield,
            //                                   canfindfieldinfo,
            //                                   managedSeries,
            //                                   canCancel,
            //                                   canClose,
            //                                   canDelete,
            //                                   log,
            //                                   logName,
            //                                   viewType != FormType.None,
            //                                   viewType == FormType.HeaderLines,
            //                                   xml,
            //                                   canmodchildfield,
            //                                   canmodfield);
        }

        public bool createUDO(string xmlform, bool managedSeries = false, bool canCancel = true, bool canClose = true, bool canDelete = true, bool log = false, string logName = "")
        {
            //GetChildInfo(out string childrenstring, out string canfindfield, out string canfindfieldinfo, out string canmodfield, out string canmodchildfield);

            if (UDO != null) return true;

            UDO = new UserObject(this);
            UDO.XmlForm = xmlform;
            UDO.ManagedSeries = managedSeries;
            UDO.CanCancel = canCancel;
            UDO.CanClose = canClose;
            UDO.CanDelete = canDelete;
            UDO.CanLog = log;
            UDO.LogTableName = logName;
            UDO.DefaultForm = true;
            UDO.EnhancedForm = true;
            return UDO.Create();

            //return ApplicationCommon.createUDO(TableName,
            //                                   TableInfo,
            //                                   TableType == SAPbobsCOM.BoUTBTableType.bott_MasterData ? 
            //                                                  SAPbobsCOM.BoUDOObjType.boud_MasterData :
            //                                                  SAPbobsCOM.BoUDOObjType.boud_Document, 
            //                                   TableName,
            //                                   childrenstring,
            //                                   canfindfield,
            //                                   canfindfieldinfo,
            //                                   managedSeries,
            //                                   canCancel,
            //                                   canClose,
            //                                   canDelete,
            //                                   log,
            //                                   logName,
            //                                   true,
            //                                   true,
            //                                   xmlform,
            //                                   canmodchildfield,
            //                                   canmodfield);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    public class UserField
    {
        public string tablename { get; set; }
        private int fieldid
        {
            get
            {
                using (RecordSet rc = new RecordSet())
                {
                    rc.DoQuery($"SELECT \"FieldID\" from \"CUFD\" where \"TableID\"='{ tablename }' AND \"AliasID\"='{ fieldname }'");

                    if (rc.RecordCount == 0) return -1;

                    return Convert.ToInt32(rc.GetValue("FieldID").ToString());
                }
            }
        }
        public string fieldname { get; set; }
        public string fieldinfo { get; set; }
        public SAPbobsCOM.BoFieldTypes fieldtype { get; set; }
        public int fieldsize { get; set; }
        public string defaultvalue { get; set; }
        public bool mandatory { get; set; }
        public SAPbobsCOM.BoFldSubTypes subtype { get; set; }
        public string validvalues { get; set; }
        public string linkedtable { get; set; } = "";
        public SAPbobsCOM.UDFLinkedSystemObjectTypesEnum systable { get; set; } = 0;
        public bool canfind { get; set; }
        public bool canmodify { get; set; }

        bool? exists = null;

        public UserField(string tablename, string fieldname)
        {
            this.tablename = tablename;
            this.fieldname = fieldname;
        }

        public bool Create()
        {
            using (UserFieldCore core = new UserFieldCore())
            {
                core.tablename = tablename;
                core.fieldname = fieldname;
                core.fieldinfo = fieldinfo;
                core.fieldtype = fieldtype;
                core.fieldsize = fieldsize;
                core.defaultvalue = defaultvalue;
                core.mandatory = mandatory;
                core.subtype = subtype;
                core.validvalues = validvalues;
                core.linkedtable = linkedtable;
                core.systable = systable;

                if (!core.Add())
                {
                    SAP.SBOApplication.MessageBox($"Error { SAP.SBOCompany.GetLastErrorCode() }: { SAP.SBOCompany.GetLastErrorDescription() }", 1, "Ok", "", "");
                    return false;
                }

                return true;
            }
        }

        public bool Update()
        {
            using (UserFieldCore core = new UserFieldCore())
            {
                if (!core.GetByKey(tablename, fieldid)) throw new Exception(SAP.SBOCompany.GetLastErrorDescription());

                core.tablename = tablename;
                core.fieldname = fieldname;
                core.fieldinfo = fieldinfo;
                core.fieldtype = fieldtype;
                core.fieldsize = fieldsize;
                core.defaultvalue = defaultvalue;
                core.mandatory = mandatory;
                core.subtype = subtype;
                core.validvalues = validvalues;
                core.linkedtable = linkedtable;
                core.systable = systable;

                if (!core.changed) return true;

                if (!core.Update())
                {
                    SAP.SBOApplication.MessageBox($"Error { SAP.SBOCompany.GetLastErrorCode() }: { SAP.SBOCompany.GetLastErrorDescription() }", 1, "Ok", "", "");
                    return false;
                }

                return true;
            }
        }

        public bool Exists()
        {
            if (exists.HasValue) return exists.Value;

            using (RecordSet rc = new RecordSet())
            {
                rc.DoQuery($"SELECT \"AliasID\" FROM \"CUFD\" WHERE \"TableID\"='{ tablename }' AND \"AliasID\" = '{ fieldname }'");
                exists = rc.RecordCount > 0;
                return exists.Value;
            }
        }

        class UserFieldCore : IDisposable
        {
            public string tablename
            {
                get => oUserFieldsMD.TableName;
                set => oUserFieldsMD.TableName = value;
            }
            public string fieldname
            {
                get => oUserFieldsMD.Name;
                set => oUserFieldsMD.Name = value;
            }
            public string fieldinfo
            {
                get => oUserFieldsMD.Description;
                set => UpdateFieldInfo(value);
            }
            public SAPbobsCOM.BoFieldTypes fieldtype
            {
                get => oUserFieldsMD.Type;
                set => UpdateFieldType(value);
            }
            public int fieldsize
            {
                get => oUserFieldsMD.Size;
                set => UpdateSize(value);
            }
            public string defaultvalue
            {
                get => oUserFieldsMD.DefaultValue;
                set => UpdateDefaultValue(value);
            }
            public bool mandatory
            {
                get => oUserFieldsMD.Mandatory == SAPbobsCOM.BoYesNoEnum.tYES;
                set => UpdateMandatory(value);
            }
            public SAPbobsCOM.BoFldSubTypes subtype
            {
                get => oUserFieldsMD.SubType;
                set => UpdateSubType(value);
            }
            public string validvalues
            {
                set => UpdateValidValues(value);
            }
            public string linkedtable
            {
                get => oUserFieldsMD.LinkedTable;
                set => UpdateLinkedTable(value);
            }
            public SAPbobsCOM.UDFLinkedSystemObjectTypesEnum systable
            {
                get => oUserFieldsMD.LinkedSystemObject;
                set => UpdateSystemTable(value);
            }

            public bool changed { get; set; } = false;

            SAPbobsCOM.UserFieldsMD oUserFieldsMD = (SAPbobsCOM.UserFieldsMD)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oUserFields);

            ~UserFieldCore()
            {
                Dispose();
            }

            public void Dispose()
            {
                if (oUserFieldsMD == null) return;

                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oUserFieldsMD);
                oUserFieldsMD = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            public bool GetByKey(string tablename, int fieldid) => oUserFieldsMD.GetByKey(tablename, fieldid);

            public bool Update() => oUserFieldsMD.Update() == 0;

            public bool Add() => oUserFieldsMD.Add() == 0;

            private int GetLimit()
            {
                switch (fieldtype)
                {
                    case SAPbobsCOM.BoFieldTypes.db_Numeric:
                        return 11;
                    case SAPbobsCOM.BoFieldTypes.db_Memo:
                    case SAPbobsCOM.BoFieldTypes.db_Date:
                        return 0;
                    case SAPbobsCOM.BoFieldTypes.db_Float:
                        return 16;
                    default:
                        return 254;
                }
            }

            private void UpdateFieldInfo(string value)
            {
                if (oUserFieldsMD.Description == value) return;

                oUserFieldsMD.Description = value;
                changed = true;
            }

            private void UpdateFieldType(SAPbobsCOM.BoFieldTypes value)
            {
                if (oUserFieldsMD.Type != value)
                {
                    changed = true;
                }

                oUserFieldsMD.Type = value;
            }

            private void UpdateSubType(SAPbobsCOM.BoFldSubTypes value)
            {
                if (oUserFieldsMD.SubType == value) return;

                oUserFieldsMD.SubType = value;
                changed = true;
            }

            private void UpdateSize(int value)
            {
                int limit = GetLimit();
                int size = Math.Min(limit, value);

                if (oUserFieldsMD.EditSize < size)
                {
                    oUserFieldsMD.EditSize = size;
                    changed = true;
                }

                if (oUserFieldsMD.Size < size)
                {
                    oUserFieldsMD.Size = size;
                    changed = true;
                }
            }

            private void UpdateDefaultValue(string value)
            {
                if (oUserFieldsMD.DefaultValue == value) return;

                oUserFieldsMD.DefaultValue = value;
                changed = true;
            }

            private void UpdateValidValues(string value)
            {
                if (String.IsNullOrEmpty(value)) return;

                int IvalidValues = 0;
                int initialCount = oUserFieldsMD.ValidValues.Count;

                foreach (string vv in value.Split('|'))
                {
                    IvalidValues++;
                    string[] parm = vv.Split(':');
                    bool isNew = false;

                    if (IvalidValues > initialCount)
                    {
                        isNew = true;
                        oUserFieldsMD.ValidValues.Add();
                        changed = true;
                    }

                    oUserFieldsMD.ValidValues.SetCurrentLine(IvalidValues - 1);

                    if (isNew)
                    {
                        oUserFieldsMD.ValidValues.Value = parm[0];
                        oUserFieldsMD.ValidValues.Description = parm[1];
                        changed = true;
                        continue;
                    }

                    if (oUserFieldsMD.ValidValues.Value != parm[0])
                    {
                        oUserFieldsMD.ValidValues.Value = parm[0];
                        changed = true;
                    }

                    if (oUserFieldsMD.ValidValues.Description != parm[1])
                    {
                        oUserFieldsMD.ValidValues.Description = parm[1];
                        changed = true;
                    }
                }
            }

            private void UpdateMandatory(bool value)
            {
                var sapmondary = value ? SAPbobsCOM.BoYesNoEnum.tYES : SAPbobsCOM.BoYesNoEnum.tNO;

                if (oUserFieldsMD.Mandatory == sapmondary) return;

                oUserFieldsMD.Mandatory = sapmondary;
                changed = true;
            }

            private void UpdateLinkedTable(string value)
            {
                using (RecordSet rc = new RecordSet())
                {
                    rc.DoQuery($"SELECT * FROM \"OUDO\" WHERE \"Code\"='{ value }'");

                    if (rc.RecordCount == 0)
                    {
                        if (oUserFieldsMD.LinkedTable == value) return;

                        oUserFieldsMD.LinkedSystemObject = 0;
                        oUserFieldsMD.LinkedTable = value;
                        oUserFieldsMD.LinkedUDO = "";
                        changed = true;
                        return;
                    }

                    if (oUserFieldsMD.LinkedUDO == value) return;

                    oUserFieldsMD.LinkedSystemObject = 0;
                    oUserFieldsMD.LinkedTable = "";
                    oUserFieldsMD.LinkedUDO = value;
                    changed = true;
                }
            }

            private void UpdateSystemTable(SAPbobsCOM.UDFLinkedSystemObjectTypesEnum value)
            {
                if (oUserFieldsMD.LinkedSystemObject == value) return;

                oUserFieldsMD.LinkedSystemObject = systable;
                oUserFieldsMD.LinkedTable = "";
                oUserFieldsMD.LinkedUDO = "";
                changed = true;
            }
        }
    }
}

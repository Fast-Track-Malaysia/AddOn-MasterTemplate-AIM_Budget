using Microsoft.VisualBasic.FileIO;
using MS.WindowsAPICodePack.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FT_ADDON
{
    abstract class JournalEntry_Base
    {
        protected class Field
        {
            public string name { get; set; }
            public object value { get; set; }

            public static Field[] GetFields(SAPbobsCOM.Recordset rs)
            {
                return rs.Fields.OfType<SAPbobsCOM.Field>().Select(f => new Field { name = f.Name, value = f.Value }).ToArray();
            }
        }

        protected SAPbobsCOM.JournalEntries _oJE = (SAPbobsCOM.JournalEntries)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oJournalEntries);

        protected virtual SAPbobsCOM.JournalEntries oJE { get => _oJE; set => _oJE = value; }
        protected SAPbobsCOM.JournalEntries_Lines oJELine { get => oJE.Lines; }

        protected virtual Dictionary<string, Action<string>> HeaderEvents { get; set; } = new Dictionary<string, Action<string>>();
        protected virtual Dictionary<string, Action<string>> RowEvents { get; set; } = new Dictionary<string, Action<string>>();

        protected string header_file { get; set; } = null;
        protected virtual DataTable header_dt { get; set; } = null;
        protected string row_file { get; set; } = null;
        protected virtual DataTable row_dt { get; set; } = null;

        protected virtual string ParentKeyColumn { get; set; } = "ParentKey";
        protected virtual string EntryIdColumn { get; set; } = "TransId";
        protected virtual string LineIdColumn { get; set; } = "LineId";

        protected virtual string DocumentName { get { return this.GetType().Name.ToString().Replace(MethodBase.GetCurrentMethod().DeclaringType.Name, ""); } }

        #region DO NOT MODIFIY
        protected int[] validHeaders = null;
        protected int[] validColumns = null;
        public List<ActionResult> actionResults;
        protected CSVDataTableConverter csvdt_converter = new CSVDataTableConverter();
        #endregion

        #region SETTINGS
        public virtual void SetHeaderFile(string file)
        {
            header_file = file;
            header_dt = csvdt_converter.Convert(header_file);
        }

        public virtual void SetRowFile(string file)
        {
            row_file = file;
            row_dt = csvdt_converter.Convert(row_file);
        }

        protected virtual void SetDelimiter()
        {
            ApplicationCommon.FillFromSAPSQL(this.GetType(), out var output);
            csvdt_converter.delimiter = output.Fields.Item(0).Value.ToString();
        }

        public virtual void Clear()
        {
            header_file = null;
            header_dt = null;
            row_file = null;
            row_dt = null;
            csvdt_converter.delimiter = null;
        }
        #endregion

        #region DYNAMIC_SETTINGS
        protected Field[] GetHeaders()
        {
            ApplicationCommon.FillFromSAPSQL(this.GetType(), out var output);
            return Field.GetFields(output);
        }

        protected Field[] GetLines()
        {
            ApplicationCommon.FillFromSAPSQL(this.GetType(), out var output);
            return Field.GetFields(output);
        }
        #endregion

        #region SETUP
        protected JournalEntry_Base()
        {
            InitializeEntry();
        }

        protected virtual void InitializeEntry()
        {
            SetDelimiter();
            HeaderEvents.Clear();
            RowEvents.Clear();
            RegisterEvent(GetHeaders(), () => oJE, HeaderEvents);
            RegisterEvent(GetLines(), () => oJE.Lines, RowEvents);
        }

        protected virtual void RegisterEvent(Field[] fields, Func<object> getobject_func, Dictionary<string, Action<string>> event_map)
        {
            foreach (var field in fields)
            {
                string colstr = field.value.ToString();
                string expression = field.name.ToString();

                if (colstr.StartsWith("[") && colstr.EndsWith("]"))
                {
                    colstr = colstr.Substring(1, colstr.Length - 2);
                }

                AddEvent(colstr, getobject_func, GetObjectProperty(getobject_func(), expression), event_map);
            }
        }

        protected virtual void AddEvent(string colstr, Func<object> getobject_func, PropertyInfo propinfo, Dictionary<string, Action<string>> event_ref)
        {
            if (propinfo == null) return;

            if (!propinfo.CanWrite) return;

            if (propinfo.PropertyType.IsEnum)
            {
                event_ref.Add(colstr, (string data) =>
                {
                    if (String.IsNullOrEmpty(data)) return;

                    propinfo.SetValue(getobject_func(), Enum.Parse(propinfo.PropertyType, data));
                });

                return;
            }

            event_ref.Add(colstr, (string data) =>
            {
                if (String.IsNullOrEmpty(data)) return;

                propinfo.SetValue(getobject_func(), Convert.ChangeType(data, propinfo.PropertyType));
            });
        }
        #endregion

        #region UTILITY
        protected PropertyInfo GetObjectProperty(object obj, string expression)
        {
            if (expression.StartsWith("U_"))
            {
                expression = expression.Substring(2);
                obj = GoToObject(obj, "UserFields");
                obj = GoToObject(obj, "Fields");
                string propstr = expression.Substring(expression.IndexOf('.'));
                obj = GetMethodReturn(obj, "Item", propstr);
                expression = expression.Substring(propstr.Length + 1);
            }

            return Common.GetSAPCOMType(obj).GetProperty(expression);
        }

        protected object GoToObject(object obj, string property)
        {
            Type type = Common.GetSAPCOMType(obj);
            var prop = type.GetProperty(property);
            return prop.GetValue(obj);
        }

        protected object GetMethodReturn(object obj, string method, params object[] args)
        {
            Type type = Common.GetSAPCOMType(obj);
            var meth = type.GetMethod(method);
            return meth.Invoke(obj, args);
        }
        #endregion

        public virtual List<ActionResult> Add(List<ActionResult> actionResults)
        {
            try
            {
                SAP.SBOApplication.StatusBar.SetText("Importing Document...", SAPbouiCOM.BoMessageTime.bmt_Medium, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
                ProgressBarHandler.Stop();
                ProgressBarHandler.CurStep = 0;
                this.actionResults = actionResults;
                AddJournalEntries();
                return this.actionResults;
            }
            finally
            {
                ProgressBarHandler.Stop();
                SAP.SBOApplication.StatusBar.SetText("Generating Import Result....", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
                ProgressBarHandler.CurStep = 0;
            }
        }

        protected virtual void AddJournalEntries(string parent_filter = null)
        {
            var field2 = from crow in header_dt.AsEnumerable()
                         group crow by new { ParentKey = crow.Field<string>(ParentKeyColumn), EntryId = crow.Field<string>(EntryIdColumn) } into grp
                         where parent_filter == null || grp.Key.ParentKey == parent_filter
                         select new
                         {
                             ParentKey = grp.Key.ParentKey,
                             EntryId = grp.Key.EntryId
                         };

            int TotalLine = field2.Count();

            if (TotalLine == 0) return;

            SAP.SBOApplication.RemoveWindowsMessage(SAPbouiCOM.BoWindowsMessageType.bo_WM_TIMER, true);
            ProgressBarHandler.Start("Importing Document...", 0, TotalLine, true);

            foreach (var field in field2)
            {
                try
                {
                    DataRow[] data = header_dt.Select($"[{ ParentKeyColumn }] = '{ field.ParentKey }' AND [{ EntryIdColumn }] = '{ field.EntryId }'");

                    foreach (var datarow in data)
                    {
                        AddJournalEntry(datarow);
                    }
                }
                finally
                {
                    ProgressBarHandler.Increment("Importing Document...", 1);
                }
            }
        }

        protected virtual void AddJournalEntry(DataRow row)
        {
            if (SAP.SBOCompany.InTransaction) SAP.SBOCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);

            SAP.SBOCompany.StartTransaction();

            oJE = (SAPbobsCOM.JournalEntries)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oJournalEntries);

            if (!ImportHeader(row)) return;

            AddJournalLines(row[ParentKeyColumn].ToString(), row[EntryIdColumn].ToString());

            while (oJE.Add() != 0)
            {
                var msg = SAP.SBOCompany.GetLastErrorDescription();

                if (!msg.Contains("2038")) throw new MessageException(msg);

                Thread.Sleep(500);
                ProgressBarHandler.Stop();
            }

            if (SAP.SBOCompany.InTransaction) SAP.SBOCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);

            string key = SAP.SBOCompany.GetNewObjectKey();
            actionResults.Add(new ActionResult("Success", key, $"Journal Voucher Number:{ key }"));
        }

        protected virtual bool ImportHeader(DataRow row)
        {
            return Import(ref validHeaders, HeaderEvents, row);
        }

        protected virtual void AddJournalLines(string parent_filter = null, string entry_filter = null)
        {
            var field3 = from crow in row_dt.AsEnumerable()
                         group crow by new { ParentKey = crow.Field<string>(ParentKeyColumn), EntryId = crow.Field<string>(EntryIdColumn), LineId = crow.Field<string>(LineIdColumn) } into grp
                         where (parent_filter == null || grp.Key.ParentKey == parent_filter) && (entry_filter == null || grp.Key.EntryId == entry_filter)
                         select new
                         {
                             ParentKey = grp.Key.ParentKey,
                             EntryId = grp.Key.EntryId,
                             LineId = grp.Key.LineId,
                         };

            foreach (var field in field3)
            {
                DataRow[] data = row_dt.Select($"[{ ParentKeyColumn }] = '{ field.ParentKey }' AND [{ EntryIdColumn }] = '{ field.EntryId }' AND [{ LineIdColumn }] = '{ field.LineId }'");

                foreach (var datarow in data)
                {
                    AddJournalLine(datarow);
                }
            }
        }

        protected virtual void AddJournalLine(DataRow row)
        {
            if (!ImportLine(row)) return;

            oJELine.Add();
        }

        protected virtual bool ImportLine(DataRow row)
        {
            return Import(ref validColumns, RowEvents, row);
        }

        protected virtual bool Import(ref int[] validfields, Dictionary<string, Action<string>> events, DataRow row)
        {
            if (validfields == null)
            {
                validfields = row.Table.Columns.OfType<DataColumn>()
                    .Where(c => events.ContainsKey(c.ColumnName))
                    .Select(c => row.Table.Columns.IndexOf(c.ColumnName))
                    .ToArray();
            }

            if (validfields.Length == 0) return false;

            foreach (var col in validfields)
            {
                events[row.Table.Columns[col].ColumnName](row[col].ToString());
            }

            return true;
        }
    }
}

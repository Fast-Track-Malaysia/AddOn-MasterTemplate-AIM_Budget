using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using System.Data.SqlClient;
using System.Threading;
using System.Numerics;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;
using System.Drawing;
using MS.WindowsAPICodePack.Internal;

namespace FT_ADDON
{
    abstract class JournalVoucher_Base : JournalEntry_Base
    {
        public static Type[] list = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                                     from assemblyType in domainAssembly.GetTypes()
                                     where typeof(JournalVoucher_Base).IsAssignableFrom(assemblyType)
                                     where !assemblyType.IsAbstract
                                     select assemblyType).ToArray();

        protected SAPbobsCOM.JournalVouchers _oJV = (SAPbobsCOM.JournalVouchers)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oJournalVouchers);

        protected virtual SAPbobsCOM.JournalVouchers oJV { get => _oJV; set => _oJV = value; }
        protected override SAPbobsCOM.JournalEntries oJE { get => _oJV.JournalEntries; set { } }

        protected virtual Dictionary<string, Action<string>> VoucherEvents { get; set; } = new Dictionary<string, Action<string>>();

        protected string voucher_file { get; set; } = null;
        protected virtual DataTable voucher_dt { get; set; } = null;

        #region DO NOT MODIFIY
        protected int[] validVouchers = null;
        #endregion

        #region SETTINGS
        public virtual void SetVoucherFile(string file)
        {
            voucher_file = file;
            voucher_dt = csvdt_converter.Convert(voucher_file);
        }

        protected void Clear_Base()
        {
            base.Clear();
        }

        public override void Clear()
        {
            voucher_file = null;
            voucher_dt = null;
            base.Clear();
        }
        #endregion

        #region DYNAMIC SETTINGS
        protected Field[] GetVouchers()
        {
            ApplicationCommon.FillFromSAPSQL(this.GetType(), out var output);
            return Field.GetFields(output);
        }
        #endregion

        #region SETUP
        protected JournalVoucher_Base()
        {
            InitializeVoucher();
        }

        protected virtual void InitializeVoucher()
        {
            VoucherEvents.Clear();
            RegisterEvent(GetVouchers(), () => oJV, VoucherEvents);
        }
        #endregion

        public List<ActionResult> Add_Base(List<ActionResult> actionResults)
        {
            return base.Add(actionResults);
        }

        public override List<ActionResult> Add(List<ActionResult> actionResults)
        {
            try
            {
                SAP.SBOApplication.StatusBar.SetText("Importing Document...", SAPbouiCOM.BoMessageTime.bmt_Medium, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
                ProgressBarHandler.Stop();
                ProgressBarHandler.CurStep = 0;
                this.actionResults = actionResults;
                AddJournalVouchers();
                return this.actionResults;
            }
            finally
            {
                ProgressBarHandler.Stop();
                SAP.SBOApplication.StatusBar.SetText("Generating Import Result....", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
                ProgressBarHandler.CurStep = 0;
            }
        }

        protected virtual void AddJournalVouchers()
        {
            var field2 = from crow in voucher_dt.AsEnumerable()
                          group crow by new { ParentKey = crow.Field<string>(ParentKeyColumn) } into grp
                          select new
                          {
                              ParentKey = grp.Key.ParentKey,
                          };

            int TotalLine = field2.Count();

            if (TotalLine == 0) return;

            SAP.SBOApplication.RemoveWindowsMessage(SAPbouiCOM.BoWindowsMessageType.bo_WM_TIMER, true);
            ProgressBarHandler.Start("Importing Document...", 0, TotalLine, true);

            foreach (var field in field2)
            {
                try
                {
                    DataRow[] data = voucher_dt.Select($"[{ ParentKeyColumn }] = '{ field.ParentKey }'");

                    foreach (var datarow in data)
                    {
                        AddJournalVoucher(datarow);
                    }
                }
                finally
                {
                    ProgressBarHandler.Increment("Importing Document...", 1);
                }
            }
        }

        protected virtual void AddJournalVoucher(DataRow row)
        {
            try
            {
                if (SAP.SBOCompany.InTransaction) SAP.SBOCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);

                SAP.SBOCompany.StartTransaction();

                oJV = (SAPbobsCOM.JournalVouchers)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oJournalVouchers);

                if (!ImportVoucher(row)) return;

                AddJournalEntries(row[ParentKeyColumn].ToString());

                while (oJV.Add() != 0)
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
            finally
            {
                if (SAP.SBOCompany.InTransaction) SAP.SBOCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);

                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oJV);
                oJV = null;
                GC.Collect();
            }
        }

        protected virtual bool ImportVoucher(DataRow row)
        {
            return Import(ref validVouchers, VoucherEvents, row);
        }

        protected void AddJournalEntries_Base(string parent_filter = null)
        {
            base.AddJournalEntries(parent_filter);
        }

        protected override void AddJournalEntries(string parent_filter = null)
        {
            var field2 = from crow in header_dt.AsEnumerable()
                         group crow by new { ParentKey = crow.Field<string>(ParentKeyColumn), EntryId = crow.Field<string>(EntryIdColumn) } into grp
                         where parent_filter == null || grp.Key.ParentKey == parent_filter
                         select new
                         {
                             ParentKey = grp.Key.ParentKey,
                             EntryId = grp.Key.EntryId
                         };

            foreach (var field in field2)
            {
                DataRow[] data = header_dt.Select($"[{ ParentKeyColumn }] = '{ field.ParentKey }' AND [{ EntryIdColumn }] = '{ field.EntryId }'");

                foreach (var datarow in data)
                {
                    AddJournalEntry(datarow);
                }
            }
        }

        protected void AddJournalEntry_Base(DataRow row)
        {
            base.AddJournalEntry(row);
        }

        protected override void AddJournalEntry(DataRow row)
        {
            if (!ImportHeader(row)) return;

            AddJournalLines(row[ParentKeyColumn].ToString(), row[EntryIdColumn].ToString());

            while (oJE.Add() != 0)
            {
                var msg = SAP.SBOCompany.GetLastErrorDescription();

                if (!msg.Contains("2038")) throw new MessageException(msg);

                Thread.Sleep(500);
                ProgressBarHandler.Stop();
            }
        }
    }
}

//#define HANA

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Data.OleDb;
using System.Reflection;
using System.Threading.Tasks;

namespace FT_ADDON.Example
{
    [FormCode("example_form")]
    [MenuId("1536", 10)]
    [MenuName("Example Form")]
    [ContextMenu("Upload", "upload_id", SAPbouiCOM.BoFormMode.fm_ADD_MODE)]
    class ExampleForm : Form_Base
    {
        class AsyncRow
        {
        }

        class AsyncDataTable
        {
            private object _lockDT = new object();

            private List<AsyncRow> rows { get; set; } = new List<AsyncRow>();
            private SAPbouiCOM.DataTable dt { get; set; }

            public AsyncDataTable(SAPbouiCOM.DataTable dt) => this.dt = dt;

            public void UpdateCounter(AsyncRow row, int counter)
            {
                lock (_lockDT)
                {
                    dt.SetValue(0, rows.IndexOf(row), counter);
                }
            }

            public AsyncRow NewRow()
            {
                lock (_lockDT)
                {
                    dt.Rows.Add();
                    var row = new AsyncRow();
                    rows.Add(row);
                    return row;
                }
            }

            public void RemoveRow(AsyncRow row)
            {
                lock (_lockDT)
                {
                    dt.Rows.Remove(rows.IndexOf(row));
                    rows.Remove(row);
                }
            }
        }

        const string cooldown1_str = "Cooldown1";
        const string counter1_str = "Counter1";
        const string counter2_str = "Counter2";

        const string click1_btn = "btnClick1";
        const string click2_btn = "btnClick2";
        const string reset1_btn = "btnReset1";
        const string reset2_btn = "btnReset2";

        const string cooldown_grid = "CDGrid";
        
        AsyncDataTable _async_dt;
        AsyncDataTable async_dt
        {
            get
            {
                if (_async_dt != null) return _async_dt;

                var dt = GetGrid(cooldown_grid).DataTable;
                dt.Rows.Clear();
                _async_dt = new AsyncDataTable(dt);
                return _async_dt;
            }
        }

        SAPbouiCOM.DataTable cd_dt { get => GetGrid(cooldown_grid).DataTable; }
        private int cooldown1
        {
            get => Convert.ToInt32(oForm.GetUserSourceValue(cooldown1_str));
            set => oForm.SetUserSourceValue(cooldown1_str, value.ToString());
        }
        private int counter1
        {
            get => Convert.ToInt32(oForm.GetUserSourceValue(counter1_str));
            set => oForm.SetUserSourceValue(counter1_str, value.ToString());
        }
        private int counter2
        {
            get => Convert.ToInt32(oForm.GetUserSourceValue(counter2_str));
            set => oForm.SetUserSourceValue(counter2_str, value.ToString());
        }

        public ExampleForm()
        {
            AddAfterItemFunc(SAPbouiCOM.BoEventTypes.et_ITEM_PRESSED, PressedCounter);
            AddAfterItemFunc(SAPbouiCOM.BoEventTypes.et_ITEM_PRESSED, PressedReset);

            AddAfterItemFunc(SAPbouiCOM.BoEventTypes.et_ITEM_PRESSED, PressedCounterAsync);
            AddAfterItemFunc(SAPbouiCOM.BoEventTypes.et_ITEM_PRESSED, PressedResetAsync);
        }

        protected override void runtimeTweakAfter()
        {
            counter1 = 0;
            counter2 = 0;
            cooldown1 = 0;
        }

        void PressedReset()
        {
            if (currentId != reset1_btn) return;

            counter1 = 0;
        }

        void PressedCounter()
        {
            if (currentId != click1_btn) return;

            cooldown1 = 3;
            Thread.Sleep(1000);

            cooldown1 = 2;
            Thread.Sleep(1000);

            cooldown1 = 1;
            Thread.Sleep(1000);

            cooldown1 = 0;
            counter1 += 1;
        }

        void PressedResetAsync()
        {
            if (currentId != reset2_btn) return;

            counter2 = 0;
        }

        async Task PressedCounterAsync()
        {
            if (currentId != click2_btn) return;

            var row = async_dt.NewRow();
            UpdateCounterAsync(row, 3);
            await Task.Delay(1000);

            UpdateCounterAsync(row, 2);
            await Task.Delay(1000);

            UpdateCounterAsync(row, 1);
            await Task.Delay(1000);

            async_dt.RemoveRow(row);
            counter2 += 1;
        }

        private void UpdateCounterAsync(AsyncRow row, int counter)
        {
            try
            {
                Monitor.Enter(oForm);
                oForm.Freeze(true);
                async_dt.UpdateCounter(row, counter);
            }
            finally
            {
                oForm.Freeze(false);
                Monitor.Exit(oForm);
            }
        }
    }
}

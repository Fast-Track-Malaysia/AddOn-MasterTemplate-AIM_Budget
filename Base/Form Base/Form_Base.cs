//#define HANA

using System;
using System.Collections.Generic;
using System.Threading;
using System.Data;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Threading.Tasks;

namespace FT_ADDON
{
    abstract partial class Form_Base
    {
        #region HIDDEN
        [Obsolete("Form_Base.list has been deprecated, please use FormManager.list", true)]
        public static Type[] list { get => FormManager.list; }

        public static UInt64 runningProcess = 0;

        const string docid_txt = "###docid";
        const string link_btn = "###link";

        protected FormMutex cfl_mtx;
        protected FormMutex cflcond_mtx;
        protected FormMutex doclink_mtx;

        #region FORM PROPERTIES
        protected const string userdt = "CurrentUser";
        protected const string usercol = "UserName";

        const string curdoc = "CurrentDoc";
        const string docobjttype = "ObjType";
        const string doctablename = "TableName";
        const string doclinetablename = "LineTableName";
        const string docstatus = "DocStatus";
        const string docentry = "DocEntry";

        /// <summary>
        /// deprecated property, use formcode instead
        /// </summary>
        public string queryCode { get => GetType().GetFormCode(); }
        public string formcode { get => GetType().GetFormCode(); }

        public string formFileName { get => GetType().GetFileName(); }

        public string menuId { get => GetType().GetMenuId(); }

        public string menuName { get => GetType().GetMenuName(); }

        public bool hasDynamicCFL { get => GetType().GetCustomAttribute<NoDynamicCFL>() == null; }
        public bool hasDynamicCFLCondition { get => GetType().GetCustomAttribute<NoDynamicCFLCondition>() == null; }
        public bool hasMenu { get => GetType().HasMenuId(); }
        public bool hasForm { get => GetType().GetCustomAttribute<NoForm>() == null; }
        public bool isManaged { get => GetType().GetCustomAttribute<Unmanaged>() == null; }
        public bool hasContextMenus { get => GetType().GetCustomAttributes<ContextMenu>().Any(); }
        public bool isStaticForm { get => GetType().GetCustomAttribute<StaticForm>() != null; }
        #endregion

        public bool initializing = false;

        public SAPbouiCOM.Form oForm = null;
        private List<FormSession> sessioninfo_list = new List<FormSession>();
        private Dictionary<int, FormSession> task_sessioninfo = new Dictionary<int, FormSession>();

        private FormSession current_session
        {
            get
            {
                if (Task.CurrentId.HasValue && task_sessioninfo.TryGetValue(Task.CurrentId.Value, out var session)) return session;
                
                return sessioninfo_list.Last();
            }
        }
        public SAPbouiCOM.ItemEvent itemPVal { get => current_session.itemPVal; }
        public SAPbouiCOM.MenuEvent menuPVal { get => current_session.menuPVal; }
        public SAPbouiCOM.BusinessObjectInfo BusinessObjectInfo { get => current_session.BusinessObjectInfo; }
        public SAPbouiCOM.ContextMenuInfo rcPVal { get => current_session.rcPVal; }
        public List<ActionResult> actionResults
        {
            get => current_session.actionResults;
            set => current_session.actionResults = value;
        }
        public bool BubbleEvent
        {
            get
            {
                if (Task.CurrentId.HasValue) throw new Exception("BubbleEvent cannot be used in asynchronous programming");

                return sessioninfo_list.Last().BubbleEvent;
            }
            set
            {
                if (Task.CurrentId.HasValue) throw new Exception("BubbleEvent cannot be used in asynchronous programming");

                sessioninfo_list.Last().BubbleEvent = value;
            }
        }
        protected string currentId { get => current_session.currentId; }
        protected string colId { get => current_session.colId; }
        protected int currentRow { get => current_session.currentRow; }
        protected bool beforeAction { get => current_session.beforeAction; }
        protected bool actionSuccess { get => current_session.actionSuccess; }

        protected SAPbouiCOM.DataTable cflDataTable { get => (itemPVal as SAPbouiCOM.IChooseFromListEvent).SelectedObjects; }

        protected Dictionary<SAPbouiCOM.BoEventTypes, Action> beforeItem { get; private set; } = new Dictionary<SAPbouiCOM.BoEventTypes, Action>();
        protected Dictionary<SAPbouiCOM.BoEventTypes, Action> afterItem { get; private set; } = new Dictionary<SAPbouiCOM.BoEventTypes, Action>();

        protected Dictionary<string, Action> beforeMenu { get; private set; } = new Dictionary<string, Action>();
        protected Dictionary<string, Action> afterMenu { get; private set; } = new Dictionary<string, Action>();

        protected Dictionary<SAPbouiCOM.BoEventTypes, Action> beforeData { get; private set; } = new Dictionary<SAPbouiCOM.BoEventTypes, Action>();
        protected Dictionary<SAPbouiCOM.BoEventTypes, Action> afterData { get; private set; } = new Dictionary<SAPbouiCOM.BoEventTypes, Action>();

        protected Dictionary<string, Action> beforeRightClick { get; private set; } = new Dictionary<string, Action>();
        protected Dictionary<string, Action> afterRightClick { get; private set; } = new Dictionary<string, Action>();
        
        #region GET
        protected object GetObjectValue(object obj)
        {
            SAPbouiCOM.Item itm = oForm.Items.Item(obj);

            try
            {
                switch (itm.Type)
                {
                    case SAPbouiCOM.BoFormItemTypes.it_EDIT:
                    case SAPbouiCOM.BoFormItemTypes.it_EXTEDIT:
                        return (itm.Specific as SAPbouiCOM.EditText).Value;
                    case SAPbouiCOM.BoFormItemTypes.it_COMBO_BOX:
                        return (itm.Specific as SAPbouiCOM.ComboBox).Value;
                    case SAPbouiCOM.BoFormItemTypes.it_CHECK_BOX:
                        return (itm.Specific as SAPbouiCOM.CheckBox).Checked;
                }

                throw new Exception($"GetObjectValue does not support this object - { obj }");
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(itm);
                itm = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        
        protected object GetObjectValue(object obj, object col, int row)
        {
            SAPbouiCOM.Item itm = oForm.Items.Item(obj);

            try
            {
                switch (itm.Type)
                {
                    case SAPbouiCOM.BoFormItemTypes.it_GRID:
                        return (itm.Specific as SAPbouiCOM.Grid).DataTable.GetValue(col, row);
                    case SAPbouiCOM.BoFormItemTypes.it_MATRIX:
                        return (itm.Specific as SAPbouiCOM.ComboBox).Value;
                }

                throw new Exception($"GetObjectValue does not support this object - { obj }");
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(itm);
                itm = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        protected void GetCellValue(SAPbouiCOM.Column col, object row, object value)
        {
            SAPbouiCOM.Cell itm = col.Cells.Item(row);

            try
            {
                switch (col.Type)
                {
                    case SAPbouiCOM.BoFormItemTypes.it_EDIT:
                    case SAPbouiCOM.BoFormItemTypes.it_EXTEDIT:
                        (itm.Specific as SAPbouiCOM.EditText).Value = value.ToString();
                        break;
                    case SAPbouiCOM.BoFormItemTypes.it_COMBO_BOX:
                        (itm.Specific as SAPbouiCOM.ComboBox).Select(value, SAPbouiCOM.BoSearchKey.psk_ByValue);
                        break;
                    case SAPbouiCOM.BoFormItemTypes.it_CHECK_BOX:
                        (itm.Specific as SAPbouiCOM.CheckBox).Checked = Convert.ToBoolean(value);
                        break;
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(itm);
                itm = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        protected void GetCellValue(SAPbouiCOM.Column col, SAPbouiCOM.Cell itm, object value)
        {
            switch (col.Type)
            {
                case SAPbouiCOM.BoFormItemTypes.it_EDIT:
                case SAPbouiCOM.BoFormItemTypes.it_EXTEDIT:
                    (itm.Specific as SAPbouiCOM.EditText).Value = value.ToString();
                    break;
                case SAPbouiCOM.BoFormItemTypes.it_COMBO_BOX:
                    (itm.Specific as SAPbouiCOM.ComboBox).Select(value, SAPbouiCOM.BoSearchKey.psk_ByValue);
                    break;
                case SAPbouiCOM.BoFormItemTypes.it_CHECK_BOX:
                    (itm.Specific as SAPbouiCOM.CheckBox).Checked = Convert.ToBoolean(value);
                    break;
            }
        }

        protected SAPbouiCOM.EditText GetText(string itm)
        {
            return oForm.Items.Item(itm).Specific as SAPbouiCOM.EditText;
        }

        protected SAPbouiCOM.Grid GetGrid(string itm)
        {
            return oForm.Items.Item(itm).Specific as SAPbouiCOM.Grid;
        }

        protected SAPbouiCOM.Matrix GetMatrix(string itm)
        {
            return oForm.Items.Item(itm).Specific as SAPbouiCOM.Matrix;
        }

        protected SAPbouiCOM.ComboBox GetCombo(string itm)
        {
            return oForm.Items.Item(itm).Specific as SAPbouiCOM.ComboBox;
        }

        protected SAPbouiCOM.CheckBox GetCheckBox(string itm)
        {
            return oForm.Items.Item(itm).Specific as SAPbouiCOM.CheckBox;
        }

        protected SAPbouiCOM.Button GetButton(string itm)
        {
            return oForm.Items.Item(itm).Specific as SAPbouiCOM.Button;
        }

        protected SAPbouiCOM.ButtonCombo GetButtonCombo(string itm)
        {
            return oForm.Items.Item(itm).Specific as SAPbouiCOM.ButtonCombo;
        }
        #endregion

        #region SET
        protected void SetObjectValue(object obj, object value)
        {
            SAPbouiCOM.Item itm = oForm.Items.Item(obj);

            try
            {
                switch (itm.Type)
                {
                    case SAPbouiCOM.BoFormItemTypes.it_EDIT:
                    case SAPbouiCOM.BoFormItemTypes.it_EXTEDIT:
                        (itm.Specific as SAPbouiCOM.EditText).Value = value.ToString();
                        break;
                    case SAPbouiCOM.BoFormItemTypes.it_COMBO_BOX:
                        (itm.Specific as SAPbouiCOM.ComboBox).Select(value, SAPbouiCOM.BoSearchKey.psk_ByValue);
                        break;
                    case SAPbouiCOM.BoFormItemTypes.it_CHECK_BOX:
                        (itm.Specific as SAPbouiCOM.CheckBox).Checked = Convert.ToBoolean(value);
                        break;
                    default:
                        throw new Exception($"SetObjectValue does not support this object - { obj }");
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(itm);
                itm = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        
        protected void SetObjectValue(object obj, object col, int row, object value)
        {
            SAPbouiCOM.Item itm = oForm.Items.Item(obj);

            try
            {
                switch (itm.Type)
                {
                    case SAPbouiCOM.BoFormItemTypes.it_GRID:
                        (itm.Specific as SAPbouiCOM.Grid).DataTable.SetValue(col, row, value);
                        break;
                    case SAPbouiCOM.BoFormItemTypes.it_MATRIX:
                        SetCellValue((itm.Specific as SAPbouiCOM.Matrix).Columns.Item(col), row, value);
                        break;
                    default:
                        throw new Exception($"SetObjectValue does not support this object - { obj }");
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(itm);
                itm = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        protected void SetObjectValue(SAPbouiCOM.Item itm, object value)
        {
            switch (itm.Type)
            {
                case SAPbouiCOM.BoFormItemTypes.it_EDIT:
                case SAPbouiCOM.BoFormItemTypes.it_EXTEDIT:
                    (itm.Specific as SAPbouiCOM.EditText).Value = value.ToString();
                    break;
                case SAPbouiCOM.BoFormItemTypes.it_COMBO_BOX:
                    (itm.Specific as SAPbouiCOM.ComboBox).Select(value, SAPbouiCOM.BoSearchKey.psk_ByValue);
                    break;
                case SAPbouiCOM.BoFormItemTypes.it_CHECK_BOX:
                    (itm.Specific as SAPbouiCOM.CheckBox).Checked = Convert.ToBoolean(value);
                    break;
            }
        }

        /// <summary>
        /// swapped column parameter with row parameter
        /// </summary>
        /// <param name="col"></param>
        /// <param name="row"></param>
        /// <param name="value"></param>
        protected void SetCellValue(SAPbouiCOM.Column col, object row, object value)
        {
            SAPbouiCOM.Cell itm = col.Cells.Item(row);

            try
            {
                switch (col.Type)
                {
                    case SAPbouiCOM.BoFormItemTypes.it_EDIT:
                    case SAPbouiCOM.BoFormItemTypes.it_EXTEDIT:
                        (itm.Specific as SAPbouiCOM.EditText).Value = value.ToString();
                        break;
                    case SAPbouiCOM.BoFormItemTypes.it_COMBO_BOX:
                        (itm.Specific as SAPbouiCOM.ComboBox).Select(value, SAPbouiCOM.BoSearchKey.psk_ByValue);
                        break;
                    case SAPbouiCOM.BoFormItemTypes.it_CHECK_BOX:
                        (itm.Specific as SAPbouiCOM.CheckBox).Checked = Convert.ToBoolean(value);
                        break;
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(itm);
                itm = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        /// <summary>
        /// swapped column parameter with itm parameter
        /// </summary>
        /// <param name="col"></param>
        /// <param name="itm"></param>
        /// <param name="value"></param>
        protected void SetCellValue(SAPbouiCOM.Column col, SAPbouiCOM.Cell itm, object value)
        {
            switch (col.Type)
            {
                case SAPbouiCOM.BoFormItemTypes.it_EDIT:
                case SAPbouiCOM.BoFormItemTypes.it_EXTEDIT:
                    (itm.Specific as SAPbouiCOM.EditText).Value = value.ToString();
                    break;
                case SAPbouiCOM.BoFormItemTypes.it_COMBO_BOX:
                    (itm.Specific as SAPbouiCOM.ComboBox).Select(value, SAPbouiCOM.BoSearchKey.psk_ByValue);
                    break;
                case SAPbouiCOM.BoFormItemTypes.it_CHECK_BOX:
                    (itm.Specific as SAPbouiCOM.CheckBox).Checked = Convert.ToBoolean(value);
                    break;
            }
        }

        protected void SetGrid(string itm, object col, int row, object value)
        {
            (oForm.Items.Item(itm).Specific as SAPbouiCOM.Grid).DataTable.SetValue(col, row, value);
        }

        protected void SetGrid(SAPbouiCOM.Grid itm, object col, int row, object value)
        {
            itm.DataTable.SetValue(col, row, value);
        }

        protected void SetMatrix(string itm, object col, object row, object value)
        {
            SAPbouiCOM.Column column = (oForm.Items.Item(itm).Specific as SAPbouiCOM.Matrix).Columns.Item(col);

            try
            {
                SetCellValue(column, column.Cells.Item(row), value);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(column);
                column = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        protected void SetMatrix(SAPbouiCOM.Matrix itm, object col, int row, object value)
        {
            SAPbouiCOM.Column column = itm.Columns.Item(col);

            try
            {
                SetCellValue(column, column.Cells.Item(row), value);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(column);
                column = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private void AddBeforeFunc<D, K>(D delegatemap, K key, Action func) where D : IDictionary<K, Action>
        {
            if (!delegatemap.ContainsKey(key))
            {
                delegatemap.Add(key, func);
                delegatemap[key] += CheckBubble;
                return;
            }

            delegatemap[key] += func;
            delegatemap[key] += CheckBubble;
        }
        
        private void AddAfterFunc<D, K>(D delegatemap, K key, Action func) where D : IDictionary<K, Action>
        {
            if (!delegatemap.ContainsKey(key))
            {
                delegatemap.Add(key, func);
                return;
            }

            delegatemap[key] += func;
        }
        
        protected void AddBeforeItemFunc(SAPbouiCOM.BoEventTypes key, Action func) => AddBeforeFunc(beforeItem, key, func);
        protected void AddAfterItemFunc(SAPbouiCOM.BoEventTypes key, Action func) => AddAfterFunc(afterItem, key, func);
        protected void AddBeforeDataFunc(SAPbouiCOM.BoEventTypes key, Action func) => AddBeforeFunc(beforeData, key, func);
        protected void AddAfterDataFunc(SAPbouiCOM.BoEventTypes key, Action func) => AddAfterFunc(afterData, key, func);
        protected void AddBeforeMenuFunc(string key, Action func) => AddBeforeFunc(beforeMenu, key, func);
        protected void AddAfterMenuFunc(string key, Action func) => AddAfterFunc(afterMenu, key, func);
        protected void AddBeforeRightClickFunc(string key, Action func) => AddBeforeFunc(beforeRightClick, key, func);
        protected void AddAfterRightClickFunc(string key, Action func) => AddAfterFunc(afterRightClick, key, func);
        protected void AddBeforeItemFunc(SAPbouiCOM.BoEventTypes key, Func<Task> func) => AddBeforeFunc(beforeItem, key, () => Task.Run(() => ExeAction(func)));

        protected void AddAfterItemFunc(SAPbouiCOM.BoEventTypes key, Func<Task> func) => AddAfterFunc(afterItem, key, () => Task.Run(() => ExeAction(func)));
        protected void AddBeforeDataFunc(SAPbouiCOM.BoEventTypes key, Func<Task> func) => AddBeforeFunc(beforeData, key, () => Task.Run(() => ExeAction(func)));
        protected void AddAfterDataFunc(SAPbouiCOM.BoEventTypes key, Func<Task> func) => AddAfterFunc(afterData, key, () => Task.Run(() => ExeAction(func)));
        protected void AddBeforeMenuFunc(string key, Func<Task> func) => AddBeforeFunc(beforeMenu, key, () => Task.Run(() => ExeAction(func)));
        protected void AddAfterMenuFunc(string key, Func<Task> func) => AddAfterFunc(afterMenu, key, () => Task.Run(() => ExeAction(func)));
        protected void AddBeforeRightClickFunc(string key, Func<Task> func) => AddBeforeFunc(beforeRightClick, key, () => Task.Run(() => ExeAction(func)));
        protected void AddAfterRightClickFunc(string key, Func<Task> func) => AddAfterFunc(afterRightClick, key, () => Task.Run(() => ExeAction(func)));
        #endregion

        public Form_Base()
        {
            SetFormMutex();
            SetAutoFill();
            SetCFLCondition();
            SetDocLink();
            SetSysDocUpdate();
        }

        protected void CreateReferTableToDoc()
        {
            SAPbouiCOM.DataTable dt = null;

            try
            {
                string tablename = oForm.DataSources.DBDataSources.Item(0).TableName;
                object docobjtypevalue = oForm.DataSources.DBDataSources.Item(0).GetValue(docobjttype, 0);
                object docstatusvalue = oForm.DataSources.DBDataSources.Item(0).GetValue(docstatus, 0);

                dt = oForm.DataSources.DataTables.Add(userdt);
                dt.Columns.Add(usercol, SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
                dt.Rows.Add();
                dt.SetValue(usercol, 0, SAP.SBOCompany.UserName);

                dt = oForm.DataSources.DataTables.Add(curdoc);
                dt.Columns.Add(docobjttype, SAPbouiCOM.BoFieldsType.ft_Integer);
                dt.Columns.Add(doctablename, SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
                dt.Columns.Add(doclinetablename, SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
                dt.Columns.Add(docstatus, SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
                dt.Columns.Add(docentry, SAPbouiCOM.BoFieldsType.ft_Integer);
                dt.Rows.Add();
                dt.SetValue(docobjttype, 0, docobjtypevalue);
                dt.SetValue(doctablename, 0, tablename);
                dt.SetValue(doclinetablename, 0, tablename.Substring(1) + "1");
                dt.SetValue(docstatus, 0, docstatusvalue);
                dt.SetValue(docentry, 0, 0);
            }
            catch (Exception)
            {
            }
            finally
            {
                if (dt != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(dt);
                    dt = null;
                    GC.Collect();
                }
            }
        }

        private void DocLinkSetup()
        {
            if (doclink_mtx.IsMutexOwned()) return;

            oForm.DataSources.UserDataSources.Add(docid_txt, SAPbouiCOM.BoDataType.dt_SHORT_TEXT);

            try
            {
                oForm.Freeze(true);
                var item = oForm.Items.Add(docid_txt, SAPbouiCOM.BoFormItemTypes.it_EDIT);

                try
                {
                    item.Width = 1;
                    item.Height = 1;
                    (item.Specific as SAPbouiCOM.EditText).DataBind.SetBound(true, "", docid_txt);

                    item = oForm.Items.Add(link_btn, SAPbouiCOM.BoFormItemTypes.it_LINKED_BUTTON);
                    item.Visible = false;
                    item.Width = 10;
                    item.Height = 10;
                    item.LinkTo = docid_txt;
                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(item);
                    item = null;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
            finally
            {
                oForm.Freeze(false);
            }
        }

        protected void UpdateSystemDocStatus()
        {
            if (!oForm.HasDataTable(curdoc)) return;

            var dt = oForm.DataSources.DataTables.Item(curdoc);

            try
            {
                dt.SetValue(docstatus, 0, oForm.DataSources.DBDataSources.Item(0).GetValue(docstatus, 0));
                dt.SetValue(docentry, 0, oForm.DataSources.DBDataSources.Item(0).GetValue(docentry, 0));
                dt.SetValue(docobjttype, 0, oForm.DataSources.DBDataSources.Item(0).GetValue(docobjttype, 0));
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(dt);
                dt = null;
                GC.Collect();
            }
        }

        protected void OpenFormByKey(SAPbouiCOM.BoLinkedObject type, string key)
        {
            oForm.DataSources.UserDataSources.Item(docid_txt).Value = key;
            var lb = oForm.Items.Item(link_btn).Specific as SAPbouiCOM.LinkedButton;

            try
            {
                lb.LinkedObject = type;
                var item = oForm.Items.Item(link_btn);

                try
                {
                    oForm.Freeze(true);
                    item.Visible = true;
                    item.Click();
                    item.Visible = false;
                }
                finally
                {
                    oForm.Freeze(false);
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(item);
                    item = null;
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(lb);
                lb = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        
        protected void OpenFormByKey(string udoname, string key)
        {
            oForm.DataSources.UserDataSources.Item(docid_txt).Value = key;
            var lb = oForm.Items.Item(link_btn).Specific as SAPbouiCOM.LinkedButton;

            try
            {
                lb.LinkedObject = SAPbouiCOM.BoLinkedObject.lf_None;
                lb.LinkedObjectType = udoname;
                var item = oForm.Items.Item(link_btn);

                try
                {
                    oForm.Freeze(true);
                    item.Visible = true;
                    item.Click();
                    item.Visible = false;
                }
                finally
                {
                    oForm.Freeze(false);
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(item);
                    item = null;
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(lb);
                lb = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        protected void SetCurrentDocEntry(int docentry)
        {
            var dt = oForm.DataSources.DataTables.Item(curdoc);

            dt.SetValue(docstatus, 0, docentry);
        }

        protected void SetCurrentDocStatus(string status)
        {
            var dt = oForm.DataSources.DataTables.Item(curdoc);

            dt.SetValue(docstatus, 0, status);
        }

        protected void SetCurrentObjType(string objtype)
        {
            var dt = oForm.DataSources.DataTables.Item(curdoc);

            dt.SetValue(docobjttype, 0, objtype);
        }

        /// <summary>
        /// for overriding purpose, to inject processes before any default form of setup to the form such as from UDT (@FT_VARIABLES) and before form becomes visible
        /// </summary>
        protected virtual void runtimeTweakBefore()
        {
        }

        /// <summary>
        /// for overriding purpose, to inject processes after any default form of setup to the form such as from UDT (@FT_VARIABLES), but before form becomes visible
        /// </summary>
        protected virtual void runtimeTweakAfter()
        {
        }

        /// <summary>
        /// deprecated method. Use GenerateFormFromXML
        /// </summary>
        protected virtual void initialize()
        {
            GenerateFormFromXML();
        }

        protected virtual void GenerateFormFromXML()
        {
            if (!hasForm) return;

            bool done = false;

            try
            {
                initializing = true;
                System.Xml.XmlDocument xmlDoc = new System.Xml.XmlDocument();
                string path = System.Windows.Forms.Application.StartupPath;
                string name = this.GetType().Namespace.Replace(MethodBase.GetCurrentMethod().DeclaringType.Namespace + ".", "")
                                                      .Replace(".", "\\");
                xmlDoc.Load($"{ path }\\{ name }\\{ formFileName }");
                System.Xml.XmlAttributeCollection xmlCol = xmlDoc.LastChild.FirstChild.FirstChild.FirstChild.Attributes;

                foreach (System.Xml.XmlAttribute att in xmlCol)
                {
                    if (att.Value != "FT_Type") continue;

                    att.Value = formcode;
                }

                System.Xml.XmlNode node = xmlDoc.LastChild.FirstChild.FirstChild.FirstChild;
                xmlCol = node.Attributes;
                SAPbouiCOM.FormCreationParams creationPackage = (SAPbouiCOM.FormCreationParams)SAP.SBOApplication.CreateObject(SAPbouiCOM.BoCreatableObjectType.cot_FormCreationParams);
                creationPackage.UniqueID = $"FT_{ SAP.getNewformUID() }";
                creationPackage.XmlData = xmlDoc.InnerXml;     // Load form from xml 
                oForm = SAP.SBOApplication.Forms.AddEx(creationPackage);
                oForm.AutoManaged = isManaged;
                oForm.Mode = SAPbouiCOM.BoFormMode.fm_OK_MODE;

                // force client height

                if (int.TryParse(xmlCol.GetNamedItem("client_height").Value, out var client_height) &&
                    int.TryParse(xmlCol.GetNamedItem("height").Value, out var height))
                {
                    oForm.ClientHeight = Math.Max(client_height, height);
                }

                // force client width
                if (int.TryParse(xmlCol.GetNamedItem("client_width").Value, out var client_width) &&
                    int.TryParse(xmlCol.GetNamedItem("width").Value, out var width))
                {
                    oForm.ClientWidth = Math.Max(client_width, width);
                }

                if (oForm.Items.Count > 0)
                {
                    oForm.Items.OfType<SAPbouiCOM.Item>()
                               .Where(itm => itm.UniqueID.ToLower().Contains("loading"))
                               .ToList()
                               .ForEach(itm =>
                               {
                                   SAPbouiCOM.PictureBox pbox = itm.Specific as SAPbouiCOM.PictureBox;
                                   pbox.Picture = $"{ path }\\Resources\\Loading.jpg";
                               });
                }

                try
                {
                    InitializeFormMutex();
                    runtimeTweakBefore();
                    CFLSetup();
                    CFLConditionSetup();
                    DocLinkSetup();
                    SetFormDefaultValue();
                }
                finally
                {
                    runtimeTweakAfter();
                }

                oForm.Visible = true;
                done = true;
            }
            catch (Exception ex)
            {
                SAP.SBOApplication.MessageBox(Common.ReadException(ex), 1, "OK", "", "");
            }
            finally
            {
                initializing = false;

                if (oForm != null && !done) oForm.Close();
            }
        }

        private void SetFormDefaultValue()
        {
            Dictionary<string, Dictionary<string, string>> store = new Dictionary<string, Dictionary<string, string>>();
            SAPbobsCOM.Recordset rc = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            rc.DoQuery($"SELECT * FROM \"{ FormVariables.TableName }\" WHERE \"Code\" LIKE '{ oForm.TypeEx }.%'");

            if (rc.RecordCount == 0) return;

            while (!rc.EoF)
            {
                try
                {
                    string query = rc.Fields.Item("U_Query").Value.ToString();
                    string key = rc.Fields.Item("Code").Value.ToString().Split('.')[1];
                    string value = rc.Fields.Item("U_DftValue").Value.ToString();

                    if (value.Length == 0)
                    {
                        SAPbobsCOM.Recordset rc2 = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                        rc2.DoQuery(query);
                        value = rc2.Fields.Item(0).Value.ToString();
                    }

                    switch (oForm.Items.Item(key).Type)
                    {
                        case SAPbouiCOM.BoFormItemTypes.it_COMBO_BOX:
                            (oForm.Items.Item(key).Specific as SAPbouiCOM.ComboBox).Select(value, SAPbouiCOM.BoSearchKey.psk_ByValue);
                            break;
                        case SAPbouiCOM.BoFormItemTypes.it_PICTURE:
                            (oForm.Items.Item(key).Specific as SAPbouiCOM.PictureBox).Picture = value;
                            break;
                        case SAPbouiCOM.BoFormItemTypes.it_EDIT:
                        case SAPbouiCOM.BoFormItemTypes.it_EXTEDIT:
                        default:
                            GetText(key).Value = value;
                            break;
                    }
                }
                catch (Exception)
                {
                    try
                    {
                        string key = rc.Fields.Item("Code").Value.ToString().Split('.')[1];
                        string value = rc.Fields.Item("U_DftValue").Value.ToString();
                        oForm.DataSources.UserDataSources.Item(key).Value = value;
                    }
                    catch (Exception)
                    {
                    }
                }

                rc.MoveNext();
            }
        }

        private void SetFormMutex()
        {
            if (hasForm) return;

            AddAfterItemFunc(SAPbouiCOM.BoEventTypes.et_FORM_LOAD, InitializeFormMutex);
        }

        private void SetAutoFill()
        {
            var autofillstatus = this.GetType().GetAutoFillFromList();

            if (autofillstatus == null) return;

            AddAfterItemFunc(SAPbouiCOM.BoEventTypes.et_CHOOSE_FROM_LIST, AutoFillChooseFromList);
        }
        
        private void SetCFLCondition()
        {
            if (!hasForm) return;

            if (!hasDynamicCFLCondition) return;

            AddAfterItemFunc(SAPbouiCOM.BoEventTypes.et_FORM_DRAW, CFLConditionSetup);
        }

        private void SetDocLink()
        {
            if (hasForm) return;

            AddAfterItemFunc(SAPbouiCOM.BoEventTypes.et_FORM_LOAD, DocLinkSetup);
        }

        private void SetSysDocUpdate() => AddAfterDataFunc(SAPbouiCOM.BoEventTypes.et_FORM_DATA_LOAD, UpdateSystemDocStatus);

        private void CFLSetup()
        {
            if (!hasDynamicCFL) return;

            if (cfl_mtx.IsMutexOwned()) return;

            if (oForm.Items.Count == 0) return;

            var itemlist = oForm.Items.OfType<SAPbouiCOM.Item>().Where(item => IsItemValidForCFL(item));

            if (!itemlist.Any()) return;

            SAPbouiCOM.ChooseFromList cfl = null;
            SAPbouiCOM.EditText txt = null;

            try
            {
                foreach (var item in itemlist)
                {
                    if (!DynamicChooseFromList.TryGetChooseFromList($"{ oForm.TypeEx }.{ item.UniqueID }", out var dcfl)) continue;

                    try
                    {
                        if (oForm.ChooseFromLists.HasItem(dcfl.parameters)) return;

                        cfl = oForm.ChooseFromLists.Add(dcfl.parameters);
                        txt = item.Specific as SAPbouiCOM.EditText;
                        txt.ChooseFromListUID = cfl.UniqueID;
                        txt.ChooseFromListAlias = dcfl.alias;
                    }
                    finally
                    {
                        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(dcfl.parameters);
                        dcfl.parameters = null;
                    }
                }
            }
            finally
            {
                if (cfl != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(cfl);
                    cfl = null;
                }

                if (txt != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(txt);
                    txt = null;
                }
            }
        }

        private bool IsItemValidForCFL(SAPbouiCOM.Item item)
        {
            if (item.Type != SAPbouiCOM.BoFormItemTypes.it_EDIT && item.Type != SAPbouiCOM.BoFormItemTypes.it_EXTEDIT) return false;

            SAPbouiCOM.EditText txt = item.Specific as SAPbouiCOM.EditText;
            SAPbouiCOM.DataTable dt = null;
            SAPbouiCOM.DBDataSource db = null;
            SAPbouiCOM.UserDataSource uds = null;
            SAPbouiCOM.DataBind dbind = txt.DataBind;

            try
            {
                if (!dbind.DataBound) return false;

                if (oForm.TryGetDataSource(dbind.TableName, out db)) return db.Fields.Item(dbind.Alias).Type == SAPbouiCOM.BoFieldsType.ft_AlphaNumeric;

                if (oForm.TryGetDataTable(dbind.TableName, out dt)) return dt.Columns.Item(dbind.Alias).Type == SAPbouiCOM.BoFieldsType.ft_AlphaNumeric;

                if (oForm.TryGetUserSource(dbind.TableName, out uds)) return uds.DataType == SAPbouiCOM.BoDataType.dt_SHORT_TEXT || uds.DataType == SAPbouiCOM.BoDataType.dt_LONG_TEXT;

                return false;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(dbind);
                dbind = null;
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(txt);
                txt = null;

                if (db != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(db);
                    db = null;
                }

                if (dt != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(dt);
                    dt = null;
                }

                if (uds != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(uds);
                    uds = null;
                }
            }
        }

        private void CFLConditionSetup()
        {
            if (cflcond_mtx.IsMutexOwned()) return;

            if (oForm.ChooseFromLists.Count == 0) return;

            var cfllist = oForm.ChooseFromLists.OfType<SAPbouiCOM.ChooseFromList>();

            foreach (var cfl in cfllist)
            {
                try
                {
                    if (!ChooseFromListCondition.TryGetConditions($"{ queryCode }.{ cfl.UniqueID }", out var conditions)) continue;

                    cfl.SetConditions(conditions);
                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(cfl);
                }
            }
        }

        private void AutoFillChooseFromList()
        {
            if (cflDataTable == null) return;

            var item = oForm.Items.Item(currentId);
            SAPbouiCOM.DBDataSource db = null;
            SAPbouiCOM.DataTable dt = null;

            try
            {
                string code;
                string tablename;
                string alias;
                int row = 0;

                switch (item.Type)
                {
                    case SAPbouiCOM.BoFormItemTypes.it_EDIT:
                    case SAPbouiCOM.BoFormItemTypes.it_EXTEDIT:
                        GetTextCFLInfo(item, out code, out tablename, out alias);
                        break;
                    case SAPbouiCOM.BoFormItemTypes.it_COMBO_BOX:
                        GetComboBoxCFLInfo(item, out code, out tablename, out alias);
                        break;
                    case SAPbouiCOM.BoFormItemTypes.it_GRID:
                        GetGridCFLInfo(item, out code, out tablename, out alias);
                        row = currentRow;
                        break;
                    default:
                        return;
                }

                if (alias == null) return;

                oForm.Freeze(true);

                try
                {
                    if (tablename == null)
                    {
                        if (alias == String.Empty || !oForm.HasUserSource(alias)) return;

                        oForm.SetUserSourceValue(alias, code);
                    }
                    else if (oForm.TryGetDataSource(tablename, out db))
                    {
                        db.SetValue(alias, row, code);
                    }
                    else if (oForm.TryGetDataTable(tablename, out dt))
                    {
                        dt.SetValue(alias, row, code);
                    }
                }
                finally
                {
                    oForm.Freeze(false);
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(item);
                item = null;
                
                if (db != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(db);
                    db = null;
                }
                
                if (dt != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(dt);
                    dt = null;
                }

                GC.Collect();
            }
        }

        private void GetTextCFLInfo(SAPbouiCOM.Item item, out string code, out string tablename, out string alias)
        {
            code = null;
            tablename = null;
            alias = null;
            var txt = item.Specific as SAPbouiCOM.EditText;

            try
            {
                code = cflDataTable.GetValue(txt.ChooseFromListAlias, 0).ToString();
                tablename = txt.DataBind.TableName;
                alias = txt.DataBind.Alias;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(txt);
                txt = null;
                GC.Collect();
            }
        }

        private void GetComboBoxCFLInfo(SAPbouiCOM.Item item, out string code, out string tablename, out string alias)
        {
            code = null;
            tablename = null;
            alias = null;
            var cbox = item.Specific as SAPbouiCOM.ComboBox;

            try
            {
                code = cflDataTable.GetValue(0, 0).ToString();
                tablename = cbox.DataBind.TableName;
                alias = cbox.DataBind.Alias;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(cbox);
                cbox = null;
                GC.Collect();
            }
        }

        private void GetGridCFLInfo(SAPbouiCOM.Item item, out string code, out string tablename, out string alias)
        {
            code = null;
            tablename = null;
            alias = null;

            var grid = item.Specific as SAPbouiCOM.Grid;
            var gdt = grid.DataTable;
            var coltxt = grid.Columns.Item(colId) as SAPbouiCOM.EditTextColumn;

            try
            {
                code = cflDataTable.GetValue(coltxt.ChooseFromListAlias, 0).ToString();
                tablename = gdt.UniqueID;
                alias = coltxt.UniqueID;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(grid);
                grid = null;
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(gdt);
                gdt = null;
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(coltxt);
                coltxt = null;
                GC.Collect();
            }
        }

        private bool blockItem(string item)
        {
            try
            {
                return item.Length == 0 || oForm.Items.Item(item).Enabled;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void CheckBubble()
        {
            if (!BubbleEvent) throw new BubbleCrash();
        }

        private bool PreRunCheck(SAPbouiCOM.ItemEvent pVal, string uid)
        {
            if (initializing)
            {
                if (!pVal.BeforeAction && (beforeItem.ContainsKey(pVal.EventType) || afterItem.ContainsKey(pVal.EventType)) &&
                    (pVal.EventType == SAPbouiCOM.BoEventTypes.et_FORM_LOAD || pVal.EventType == SAPbouiCOM.BoEventTypes.et_FORM_DRAW))
                {
                    throw new Exception(pVal.EventType.ToString() + " is not supported for custom form, use runtimeTweakBefore/runtimeTweakAfter instead");
                }

                return false;
            }

            if (!blockItem(uid)) return false;

            return true;
        }

        /// <summary>
        /// Reset all delegates for current form object. Deprecated function
        /// </summary>
        protected void Reset()
        {
            beforeItem = new Dictionary<SAPbouiCOM.BoEventTypes, Action>();
            afterItem = new Dictionary<SAPbouiCOM.BoEventTypes, Action>();

            beforeMenu = new Dictionary<string, Action>();
            afterMenu = new Dictionary<string, Action>();

            beforeData = new Dictionary<SAPbouiCOM.BoEventTypes, Action>();
            afterData = new Dictionary<SAPbouiCOM.BoEventTypes, Action>();

            beforeRightClick = new Dictionary<string, Action>();
            afterRightClick = new Dictionary<string, Action>();
        }

        private void AddCurrentSession(FormSession session)
        {
            sessioninfo_list.Add(session);
        }

        private void TerminateCurrentSession()
        {
            sessioninfo_list.Remove(sessioninfo_list.Last());
        }

        /// <summary>
        /// for overriding purpose. Will be triggered on form removal from add-on container
        /// </summary>
        public virtual void FormRemovalEvent()
        {
        }

        [Obsolete("Form_Base.ClearEmptyForms has been deprecated, please use FormManager.ClearEmptyForms", true)]
        public static void ClearEmptyForms() => FormManager.ClearEmptyForms();
        [Obsolete("Form_Base.GetFormCode has been deprecated, please use FormManager.GetFormCode", true)]
        public static string GetFormCode(Type formtype) => FormManager.GetFormCode(formtype);
        [Obsolete("Form_Base.GetFormType has been deprecated, please use FormManager.GetFormType", true)]
        public static bool GetFormType(string formTypeEx, out Type formtype) => FormManager.GetFormType(formTypeEx, out formtype);
        [Obsolete("Form_Base.CreateForm has been deprecated, please use FormManager.AddForm", true)]
        public static void CreateForm(string formuid, Type formtype) => FormManager.AddForm(formuid, formtype);
        [Obsolete("Form_Base.CreateForm has been deprecated, please use FormManager.AddForm", true)]
        public static void CreateForm(string formuid, Form_Base formobj) => FormManager.AddForm(formuid, formobj);
        [Obsolete("Form_Base.GetForm has been deprecated, please use FormManager.GetForm", true)]
        public static bool GetForm(string formuid, out Form_Base formobj) => FormManager.GetForm(SAP.SBOApplication.Forms.Item(formuid), out formobj);
        [Obsolete("Form_Base.GetForm has been deprecated, please use FormManager.GetForm", true)]
        public static bool GetForm(SAPbouiCOM.Form form, out Form_Base formobj) => FormManager.GetForm(form, out formobj);
        [Obsolete("Form_Base.OpenNewForm has been deprecated, please use FormManager.OpenNewForm", true)]
        public static Form_Type OpenNewForm<Form_Type>() where Form_Type : Form_Base => FormManager.OpenNewForm(typeof(Form_Type)) as Form_Type;
        [Obsolete("Form_Base.OpenNewForm has been deprecated, please use FormManager.OpenNewForm", true)]
        public static Form_Base OpenNewForm(Type formtype) => FormManager.OpenNewForm(formtype);
        [Obsolete("Form_Base.NewForm has been deprecated, please use FormManager.NewForm", true)]
        public static Form_Type NewForm<Form_Type>() where Form_Type : Form_Base => FormManager.NewForm(typeof(Form_Type)) as Form_Type;
        [Obsolete("Form_Base.NewForm has been deprecated, please use FormManager.NewForm", true)]
        public static Form_Base NewForm(Type formtype) => FormManager.NewForm(formtype);

        /// <summary>
        /// Open the form window the form object
        /// </summary>
        public void OpenForm()
        {
            if (oForm != null) throw new Exception("Form window is already running");

            initialize();

            if (oForm == null) return;

            FormManager.AddForm(oForm.UniqueID, this);
        }

        /// <summary>
        /// Can only be used in overriding process event before or after
        /// </summary>
        /// <param name="action">action method</param>
        /// <param name="evnt">event info</param>
        /// <returns></returns>
        protected bool ExeActionWithEvent(Action action, SAPbouiCOM.ItemEvent evnt) => ExeActionWithEvent(action, new FormSession(evnt));
        /// <summary>
        /// Can only be used in overriding process event before or after
        /// </summary>
        /// <param name="action">action method</param>
        /// <param name="evnt">event info</param>
        /// <returns></returns>
        protected bool ExeActionWithEvent(Action action, SAPbouiCOM.MenuEvent evnt) => ExeActionWithEvent(action, new FormSession(evnt));
        /// <summary>
        /// Can only be used in overriding process event before or after
        /// </summary>
        /// <param name="action">action method</param>
        /// <param name="evnt">event info</param>
        /// <returns></returns>
        protected bool ExeActionWithEvent(Action action, SAPbouiCOM.BusinessObjectInfo evnt) => ExeActionWithEvent(action, new FormSession(evnt));
        /// <summary>
        /// Can only be used in overriding process event before or after
        /// </summary>
        /// <param name="action">action method</param>
        /// <param name="evnt">event info</param>
        /// <returns></returns>
        protected bool ExeActionWithEvent(Action action, SAPbouiCOM.ContextMenuInfo evnt) => ExeActionWithEvent(action, new FormSession(evnt));

        private bool ExeActionWithEvent(Action action, FormSession session)
        {
            AddCurrentSession(session);

            try
            {
                return ExeAction(action);
            }
            finally
            {
                TerminateCurrentSession();
            }
        }

        protected async Task ExeAction(Func<Task> action)
        {
            try
            {
                task_sessioninfo.Add(Task.CurrentId.Value, current_session);
                await action();

                if (actionResults.Count > 0) SAP.showActionResult(actionResults);
            }
            catch (MessageException ex)
            {
                if (GetType() == typeof(SystemForm)) return;

                SAP.stopProgressBar();
                SAP.SBOApplication.MessageBox(ex.Message, 1, "OK", "", "");
            }
            catch (Exception ex)
            {
                if (GetType() == typeof(SystemForm)) return;

                SAP.stopProgressBar();
                SAP.SBOApplication.MessageBox(Common.ReadException(ex), 1, "OK", "", "");
            }
            finally
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                task_sessioninfo.Remove(Task.CurrentId.Value);
            }
        }

        protected bool ExeAction(Action action)
        {
            try
            {
                try { action(); }
                catch (BubbleCrash) { }

                if (actionResults.Count > 0) SAP.showActionResult(actionResults);
            }
            catch (MessageException ex)
            {
                BubbleEvent = false;

                if (GetType() == typeof(SystemForm)) return BubbleEvent;

                SAP.stopProgressBar();
                SAP.SBOApplication.MessageBox(ex.Message, 1, "OK", "", "");
            }
            catch (Exception ex)
            {
                BubbleEvent = false;

                if (GetType() == typeof(SystemForm)) return BubbleEvent;

                SAP.stopProgressBar();
                SAP.SBOApplication.MessageBox(Common.ReadException(ex), 1, "OK", "", "");
            }
            finally
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            return BubbleEvent;
        }

        protected void TryAddContextMenu(SAPbouiCOM.ContextMenuInfo evnt)
        {
            if (!hasContextMenus) return;

            AddCurrentSession(new FormSession(evnt));

            try
            {
                ContextMenu.TryAddIn(this);
            }
            finally
            {
                TerminateCurrentSession();
            }

        }
        
        protected void TryRemoveContextMenu(SAPbouiCOM.ContextMenuInfo evnt)
        {
            if (!hasContextMenus) return;

            AddCurrentSession(new FormSession(evnt));

            try
            {
                ContextMenu.TryRemoveFrom(this);
            }
            finally
            {
                TerminateCurrentSession();
            }

        }

        public virtual void processItemEventbefore(SAPbouiCOM.ItemEvent pVal, ref bool _BubbleEvent)
        {
            try
            {
                if (!PreRunCheck(pVal, pVal.ItemUID)) return;

                if (beforeItem.Count == 0) return;

                if (beforeItem.TryGetValue(SAPbouiCOM.BoEventTypes.et_ALL_EVENTS, out var action))
                {
                    _BubbleEvent = ExeActionWithEvent(action, pVal);

                    if (!_BubbleEvent) return;
                }

                if (!beforeItem.TryGetValue(pVal.EventType, out action)) return;

                _BubbleEvent = ExeActionWithEvent(action, pVal);
            }
            finally
            {
                // NECESSARY TO PREVENT CRASH IN SAP
                if (!_BubbleEvent && pVal.EventType == SAPbouiCOM.BoEventTypes.et_FORM_LOAD) _BubbleEvent = true;
            }
        }

        public virtual void processItemEventafter(SAPbouiCOM.ItemEvent pVal)
        {
            if (!PreRunCheck(pVal, pVal.ItemUID)) return;

            if (afterItem.Count == 0) return;

            if (afterItem.TryGetValue(SAPbouiCOM.BoEventTypes.et_ALL_EVENTS, out var action))
            {
                ExeActionWithEvent(action, pVal);
            }

            if (!afterItem.TryGetValue(pVal.EventType, out action)) return;

            ExeActionWithEvent(action, pVal);
        }

        public virtual void processMenuEventbefore(SAPbouiCOM.MenuEvent pVal, ref bool _BubbleEvent)
        {
            if (!beforeMenu.TryGetValue(pVal.MenuUID, out var action)) return;

            _BubbleEvent = ExeActionWithEvent(action, pVal);
        }

        public virtual void processMenuEventafter(SAPbouiCOM.MenuEvent pVal)
        {
            if (!afterMenu.TryGetValue(pVal.MenuUID, out var action)) return;

            ExeActionWithEvent(action, pVal);
        }

        public virtual void processDataEventbefore(SAPbouiCOM.BusinessObjectInfo _BusinessObjectInfo, ref bool _BubbleEvent)
        {
            if (beforeData.Count == 0) return;

            if (beforeData.TryGetValue(SAPbouiCOM.BoEventTypes.et_ALL_EVENTS, out var action))
            {
                _BubbleEvent = ExeActionWithEvent(action, _BusinessObjectInfo);

                if (!_BubbleEvent) return;
            }

            if (!beforeData.TryGetValue(_BusinessObjectInfo.EventType, out action)) return;

            _BubbleEvent = ExeActionWithEvent(action, _BusinessObjectInfo);
        }

        public virtual void processDataEventafter(SAPbouiCOM.BusinessObjectInfo _BusinessObjectInfo)
        {
            if (afterData.Count == 0) return;

            if (afterData.TryGetValue(SAPbouiCOM.BoEventTypes.et_ALL_EVENTS, out var action))
            {
                ExeActionWithEvent(action, _BusinessObjectInfo);
            }

            if (!afterData.TryGetValue(_BusinessObjectInfo.EventType, out action)) return;

            ExeActionWithEvent(action, _BusinessObjectInfo);
        }

        public virtual void processRightClickEventbefore(SAPbouiCOM.ContextMenuInfo pVal, ref bool _BubbleEvent)
        {
            if (!blockItem(pVal.ItemUID)) return;

            TryAddContextMenu(pVal);

            if (!beforeRightClick.TryGetValue(pVal.ItemUID, out var action)) return;

            _BubbleEvent = ExeActionWithEvent(action, pVal);
        }

        public virtual void processRightClickEventafter(SAPbouiCOM.ContextMenuInfo pVal)
        {
            if (!blockItem(pVal.ItemUID)) return;

            TryRemoveContextMenu(pVal);

            if (!afterRightClick.TryGetValue(pVal.ItemUID, out var action)) return;

            ExeActionWithEvent(action, pVal);
        }
        #endregion
    }

    static class UserFormExtension
    {
        public static SAPbouiCOM.Form GetForm(this SAPbouiCOM.Item item)
        {
            if (SAP.SBOApplication.Forms.Count == 0) return null;

            var list = SAP.SBOApplication.Forms.OfType<SAPbouiCOM.Form>()
                                               .Select(f => f)
                                               .Where(f => f.Items.Count > 0 && 
                                                           f.Items.OfType<SAPbouiCOM.Item>()
                                                                  .Where(i => i == item)
                                                                  .Any())
                                               .ToList();
            return list.Count > 0 ? list.First() : null;
        }

        public static IEnumerable<SAPbouiCOM.Form> GetForms(this SAPbouiCOM.Forms forms)
        {
            if (SAP.SBOApplication.Forms.Count == 0) return new SAPbouiCOM.Form[0];

            return SAP.SBOApplication.Forms.OfType<SAPbouiCOM.Form>();
        }
    }
}

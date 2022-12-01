using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    [NoForm]
    [FormCode("0")]
    [StaticForm]
    class SystemForm : Form_Base
    {
        private static SystemForm _sysForm = null;

        public static SAPbouiCOM.Form sysForm => _sysForm.oForm;
        public static string sysCurrentId => _sysForm.currentId;
        public static int sysCurrentRow => _sysForm.currentRow;
        public static bool sysBubbleEvent => _sysForm.BubbleEvent;

        private static Action action_queue = Proxy;

        public SystemForm()
        {
            if (this == _sysForm) return;

            if (_sysForm != null) return;

            _sysForm = this;
            action_queue();
        }

        public override void FormRemovalEvent()
        {
            _sysForm = null;
            base.FormRemovalEvent();
        }

        private static void Proxy()
        {
        }

        public static void AddSysBeforeItem(SAPbouiCOM.BoEventTypes evnt, Action func) => action_queue += () => { _sysForm.AddAfterItemFunc(evnt, func); };

        public static void AddSysAfterItem(SAPbouiCOM.BoEventTypes evnt, Action func) => action_queue += () => { _sysForm.AddAfterItemFunc(evnt, func); };

        public static void AddSysBeforeMenu(string evnt, Action func) => action_queue += () => { _sysForm.AddBeforeMenuFunc(evnt, func); };

        public static void AddSysAfterMenu(string evnt, Action func) => action_queue += () => { _sysForm.AddAfterMenuFunc(evnt, func); };

        public static void AddSysBeforeData(SAPbouiCOM.BoEventTypes evnt, Action func) => action_queue += () => { _sysForm.AddBeforeDataFunc(evnt, func); };

        public static void AddSysAfterData(SAPbouiCOM.BoEventTypes evnt, Action func) => action_queue += () => { _sysForm.AddAfterDataFunc(evnt, func); };

        public static void AddSysBeforeRightClick(string evnt, Action func) => action_queue += () => { _sysForm.AddBeforeRightClickFunc(evnt, func); };

        public static void AddSysAfterRightClick(string evnt, Action func) => action_queue += () => { _sysForm.AddAfterRightClickFunc(evnt, func); };

        #region GET
        public static SAPbouiCOM.EditText GetSysText(string itm) => _sysForm.GetText(itm);

        public static SAPbouiCOM.Grid GetSysGrid(string itm) => _sysForm.GetGrid(itm);

        public static SAPbouiCOM.Matrix GetSysMatrix(string itm) => _sysForm.GetMatrix(itm);

        public static SAPbouiCOM.ComboBox GetSysCombo(string itm) => _sysForm.GetCombo(itm);

        public static SAPbouiCOM.CheckBox GetSysCheckBox(string itm) => _sysForm.GetCheckBox(itm);
        #endregion
    }
}

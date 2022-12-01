using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    class FormSession
    {
        public SAPbouiCOM.ItemEvent itemPVal = null;
        public SAPbouiCOM.MenuEvent menuPVal = null;
        public SAPbouiCOM.BusinessObjectInfo BusinessObjectInfo = null;
        public SAPbouiCOM.ContextMenuInfo rcPVal = null;
        public List<ActionResult> actionResults = new List<ActionResult>();
        public bool BubbleEvent = true;

        public string currentId = "";
        public string colId = "";
        public int currentRow = 0;
        public bool beforeAction;
        public bool actionSuccess = false;

        public FormSession(SAPbouiCOM.ItemEvent itemPVal)
        {
            this.itemPVal = itemPVal;
            currentId = itemPVal.ItemUID;
            colId = itemPVal.ColUID;
            currentRow = itemPVal.Row;
            beforeAction = itemPVal.BeforeAction;
            actionSuccess = itemPVal.ActionSuccess;
        }

        public FormSession(SAPbouiCOM.MenuEvent menuPVal)
        {
            this.menuPVal = menuPVal;
            currentId = menuPVal.MenuUID;
            beforeAction = menuPVal.BeforeAction;
        }

        public FormSession(SAPbouiCOM.BusinessObjectInfo BusinessObjectInfo)
        {
            this.BusinessObjectInfo = BusinessObjectInfo;
            beforeAction = BusinessObjectInfo.BeforeAction;
            actionSuccess = BusinessObjectInfo.ActionSuccess;
        }
        
        public FormSession(SAPbouiCOM.ContextMenuInfo rcPVal)
        {
            this.rcPVal = rcPVal;
            beforeAction = rcPVal.BeforeAction;
            actionSuccess = rcPVal.ActionSuccess;
        }
    }
}

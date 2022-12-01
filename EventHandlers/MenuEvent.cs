using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace FT_ADDON
{
    class MenuEvent
    {
        static bool handling_exception = false;

        public static void processMenuEvent(ref SAPbouiCOM.MenuEvent pVal)
        {
            try
            {
                if (PurchaseOrder_Base.isCustomPurchaseOrder(pVal.MenuUID))
                {
                    InitPOForm.VInventory();
                }
                else if (FormManager.GetFormType(pVal.MenuUID, out var formtype))
                {
                    FormManager.OpenNewForm(formtype);
                }
            }
            catch (Exception ex)
            {
                if (handling_exception) return;

                handling_exception = true;
                SAP.stopProgressBar();
                SAP.SBOApplication.MessageBox(Common.ReadException(ex), 1, "OK", "", "");
            }
            finally
            {
                handling_exception = false;
            }
        }

        public static void processMenuEvent2(ref SAPbouiCOM.MenuEvent pVal, ref bool BubbleEvent)
        {
            try
            {
                SAPbouiCOM.Form oForm = null;

                try
                {
                    oForm = SAP.SBOApplication.Forms.ActiveForm;
                }
                catch
                {
                    return;
                }

                if (oForm == null) return;

                if (!FormManager.GetForm(oForm, out var formobj)) return;

                if (pVal.BeforeAction)
                {
                    formobj.processMenuEventbefore(pVal, ref BubbleEvent);
                    return;
                }

                formobj.processMenuEventafter(pVal);
            }
            catch (Exception ex)
            {
                if (handling_exception) return;

                handling_exception = true;
                SAP.stopProgressBar();
                SAP.SBOApplication.MessageBox(Common.ReadException(ex), 1, "OK", "", "");
            }
            finally
            {
                handling_exception = false;
            }
        }
    }
}

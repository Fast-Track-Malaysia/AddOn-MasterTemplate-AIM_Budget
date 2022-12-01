using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace FT_ADDON
{
    class ItemEvent
    {
        static bool handling_exception = false;

        private static void processBeforeEvent<Event>(SAPbouiCOM.Form oForm, Event evnt, ref bool BubbleEvent, MethodInfo methodinfo)
        {
            FormManager.ClearEmptyForms();

            if (!FormManager.GetForm(oForm, out var formobj)) return;

            object[] args = { evnt, BubbleEvent };
            methodinfo.Invoke(formobj, args);
            BubbleEvent = Convert.ToBoolean(args[1]);
        }

        private static void processAfterEvent<Event>(SAPbouiCOM.Form oForm, Event evnt, MethodInfo methodinfo)
        {
            FormManager.ClearEmptyForms();

            if (!FormManager.GetForm(oForm, out var formobj)) return;

            object[] args = { evnt };
            methodinfo.Invoke(formobj, args);
        }

        public static void processFormDataEvent(ref SAPbouiCOM.BusinessObjectInfo BusinessObjectInfo, ref bool BubbleEvent)
        {
            try
            {
                SAPbouiCOM.Form oForm = null;

                try
                {
                    oForm = SAP.SBOApplication.Forms.Item(BusinessObjectInfo.FormUID);
                }
                catch
                {
                }

                if (oForm == null) return;

                if (oForm.TypeEx != BusinessObjectInfo.FormTypeEx) return;

                if (BusinessObjectInfo.BeforeAction)
                {
                    const string methodname = "processDataEventbefore";
                    processBeforeEvent(oForm, BusinessObjectInfo, ref BubbleEvent, typeof(Form_Base).GetMethod(methodname));
                }
                else
                {
                    const string methodname = "processDataEventafter";
                    processAfterEvent(oForm, BusinessObjectInfo, typeof(Form_Base).GetMethod(methodname));
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

        public static void processItemEvent(string FormUID, ref SAPbouiCOM.ItemEvent pVal, ref bool BubbleEvent)
        {
            try
            {
                SAPbouiCOM.Form oForm = null;

                try
                {
                    oForm = SAP.SBOApplication.Forms.Item(FormUID);
                }
                catch
                {
                }

                if (oForm == null) return;

                if (oForm.TypeEx != pVal.FormTypeEx) return;

                if (pVal.BeforeAction)
                {
                    const string methodname = "processItemEventbefore";
                    processBeforeEvent(oForm, pVal, ref BubbleEvent, typeof(Form_Base).GetMethod(methodname));
                }
                else
                {
                    const string methodname = "processItemEventafter";
                    processAfterEvent(oForm, pVal, typeof(Form_Base).GetMethod(methodname));
                }

                try
                {
                    // Required to safely return closed/closing Form
                    bool check = oForm.Selected;
                }
                catch (Exception)
                {
                    return;
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

        public static void processRightClickEvent(string FormUID, ref SAPbouiCOM.ContextMenuInfo pVal, ref bool BubbleEvent)
        {
            try
            {
                SAPbouiCOM.Form oForm = null;

                try
                {
                    oForm = SAP.SBOApplication.Forms.Item(FormUID);
                }
                catch
                {
                }

                if (oForm == null) return;

                if (pVal.BeforeAction)
                {
                    const string methodname = "processRightClickEventbefore";
                    processBeforeEvent(oForm, pVal, ref BubbleEvent, typeof(Form_Base).GetMethod(methodname));
                }
                else
                {
                    const string methodname = "processRightClickEventafter";
                    processAfterEvent(oForm, pVal, typeof(Form_Base).GetMethod(methodname));
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
    }
}

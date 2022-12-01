//#define HANA

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FT_ADDON
{
    class AddOn
    {
        [Obsolete("AddOn.masterFormList has been deprecated, please use FormManager.FormList", true)]
        public static Dictionary<string, Form_Base> masterFormList { get => FormManager.FormList; }

        public AddOn()
        {
            // Display status
            SAP.SBOApplication.StatusBar.SetText("Addon Core Initializing...", SAPbouiCOM.BoMessageTime.bmt_Medium, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);

            // Add deligates to events
            SAP.SBOApplication.AppEvent += new SAPbouiCOM._IApplicationEvents_AppEventEventHandler(SBO_Application_AppEvent);
            SAP.SBOApplication.MenuEvent += new SAPbouiCOM._IApplicationEvents_MenuEventEventHandler(SBO_Application_MenuEvent);
            SAP.SBOApplication.ItemEvent += new SAPbouiCOM._IApplicationEvents_ItemEventEventHandler(SBO_Application_ItemEvent);
            //SAP.SBOApplication.ProgressBarEvent += new SAPbouiCOM._IApplicationEvents_ProgressBarEventEventHandler(SBO_Application_ProgressBarEvent);
            SAP.SBOApplication.RightClickEvent += new SAPbouiCOM._IApplicationEvents_RightClickEventEventHandler(SBO_Application_RightClickEvent);
            SAP.SBOApplication.FormDataEvent += new SAPbouiCOM._IApplicationEvents_FormDataEventEventHandler(ref SBO_Application_FormDataEvent);

            // Add UDT, UDF, Menu Item
            SAP.formUID = 0;
            SAP.createStatusForm();
            SAP.getStatusForm();
            initEnviroment();
            FormManager.RestoreExistingForms();

            GC.Collect();

            // Display status
            SAP.SBOApplication.StatusBar.SetText("Addon Core successfully initialized.", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Success);
        }

        private void SBO_Application_AppEvent(SAPbouiCOM.BoAppEventTypes EventType)
        {
            switch (EventType)
            {
                case SAPbouiCOM.BoAppEventTypes.aet_ShutDown:
                    //SBO_Application.MessageBox("A Shut Down Event has been caught" + Environment.NewLine + "Terminating Add On...", 1, "Ok", "", "");
                    System.Environment.Exit(0);
                    break;
                case SAPbouiCOM.BoAppEventTypes.aet_CompanyChanged:
                    //SBO_Application.MessageBox("A Company Change Event has been caught", 1, "Ok", "", "");
                    System.Environment.Exit(0);
                    break;
                case SAPbouiCOM.BoAppEventTypes.aet_LanguageChanged:
                    //SBO_Application.MessageBox("A Languge Change Event has been caught", 1, "Ok", "", "");
                    break;
            }
        }

        private void SBO_Application_ProgressBarEvent(ref SAPbouiCOM.ProgressBarEvent pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
        }

        private void SBO_Application_StatusBarEvent(string Text, SAPbouiCOM.BoStatusBarMessageType MessageType)
        {
            //SBO_Application.MessageBox(@"Status bar event with message: """ + Text + @""" has been sent", 1, "Ok", "", "");
        }

        private void SBO_Application_RightClickEvent(ref SAPbouiCOM.ContextMenuInfo EventInfo, out bool BubbleEvent)
        {
            BubbleEvent = true;
            ItemEvent.processRightClickEvent(EventInfo.FormUID, ref EventInfo, ref BubbleEvent);
        }

        private void SBO_Application_ItemEvent(string FormUID, ref SAPbouiCOM.ItemEvent pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            ItemEvent.processItemEvent(FormUID, ref pVal, ref BubbleEvent);
        }

        private void SBO_Application_FormDataEvent(ref SAPbouiCOM.BusinessObjectInfo BusinessObjectInfo, out bool BubbleEvent)
        {
            BubbleEvent = true;
            ItemEvent.processFormDataEvent(ref BusinessObjectInfo, ref BubbleEvent);
            //FormDataEvent.process_FormDataEvent(ref BusinessObjectInfo,ref BubbleEvent);
        }

        private void SBO_Application_MenuEvent(ref SAPbouiCOM.MenuEvent pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            if (!pVal.BeforeAction) MenuEvent.processMenuEvent(ref pVal);

            MenuEvent.processMenuEvent2(ref pVal, ref BubbleEvent);
        }

        private bool Setup()
        {
            Type[] list = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                           from assemblyType in domainAssembly.GetTypes()
                           where typeof(AddOnSettings).IsAssignableFrom(assemblyType)
                           where !assemblyType.IsAbstract
                           select assemblyType).ToArray();

            foreach (var type in list)
            {
                AddOnSettings settings = Activator.CreateInstance(type, null) as AddOnSettings;

                if (!settings.success) return false;
            }

            return true;
        }

        private void initEnviroment()
        {
            // -------------------------------------------------------
            // Add UDT, UDF, Add Menu Item
            // -------------------------------------------------------

            SAP.SBOCompany.StartTransaction();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            if (!Setup())
            {
                if (SAP.SBOCompany.InTransaction) SAP.SBOCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);

                SAP.SBOApplication.StatusBar.SetText("Addon was teminated.", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
                SAP.hideStatus();
                System.Environment.Exit(0);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            if (SAP.SBOCompany.InTransaction) SAP.SBOCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);

            SAP.hideStatus();
            SAP.SwitchCompany();

            try
            {
                ApplicationCommon.createMainMenu();
            }
            catch (Exception ex)
            {
                SAP.SBOApplication.StatusBar.SetText(ex.Message, SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
            }
            finally
            {
                //System.Runtime.InteropServices.Marshal.FinalReleaseComObject(rc);
                //rc = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
}

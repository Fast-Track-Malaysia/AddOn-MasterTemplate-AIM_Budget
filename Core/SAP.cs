using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Linq;

namespace FT_ADDON
{
    class SAP
    {
        const bool bDebugMode = true;

        public static SAPbouiCOM.Application SBOApplication;

        static SAPbobsCOM.Company _SBOCompany;
        static SAPbobsCOM.Company _SBOCompany2;
        public static SAPbobsCOM.Company SBOCompany { get => _SBOCompany; set => _SBOCompany2 = value; }
        public static SAPbouiCOM.ProgressBar progressBar;
        public static int formUID;
        public static SAPbouiCOM.Form statusForm;

        static Dictionary<string, int> table2obj = new Dictionary<string, int>();
        static Dictionary<int, string> obj2table = new Dictionary<int, string>(); 

        public static void SwitchCompany()
        {
            if (_SBOCompany2 == null) return;

            _SBOCompany = _SBOCompany2;
        }

        public static void createStatusForm()
        {
            SAPbouiCOM.Form oForm = null;
            SAPbouiCOM.Item oItem = null;
            SAPbouiCOM.FormCreationParams oCreationParams = ((SAPbouiCOM.FormCreationParams)(SBOApplication.CreateObject(SAPbouiCOM.BoCreatableObjectType.cot_FormCreationParams)));

            try
            {
                oCreationParams.BorderStyle = SAPbouiCOM.BoFormBorderStyle.fbs_Floating;
                oCreationParams.UniqueID = "FT_StatusForm";
                oCreationParams.FormType = "StatusForm";
                oForm = SBOApplication.Forms.AddEx(oCreationParams);
                //statusForm.Visible = false;
                oForm.ClientWidth = 450;
                oForm.ClientHeight = 80;
                oForm.Top = SBOApplication.Desktop.Top + 50;
                oForm.Left = (SBOApplication.Desktop.Width - 450) / 2;
                oItem = oForm.Items.Add("lblStatus", SAPbouiCOM.BoFormItemTypes.it_STATIC);
                oItem.Left = 10;
                oItem.Height = 15;
                oItem.Top = (oForm.ClientHeight - oItem.Height) / 2;
                oItem.Width = oForm.ClientWidth - 20;

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oCreationParams);
                oCreationParams = null;

                if (oForm != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oForm);
                    oForm = null;
                }

                if (oItem != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oItem);
                    oItem = null;
                }

                GC.Collect();
            }
        }

        public static void stopProgressBar()
        {
            if (progressBar == null) return;

            try { progressBar.Stop(); }
            catch { }
        }

        public static void showStatus()
        {
            if (statusForm == null) return;

            try { statusForm.Visible = true; }
            catch { }
        }

        public static void hideStatus()
        {
            if (statusForm == null) return;

            try
            {
                statusForm.Visible = false;
                ((SAPbouiCOM.StaticText)(statusForm.Items.Item("").Specific)).Caption = "";
            }
            catch { }
        }

        public static void getStatusForm()
        {
            try { statusForm = SBOApplication.Forms.Item("FT_StatusForm"); }
            catch { statusForm = null; };
        }

        public static DateTime getDateTime(string sDate)
        {
            SAPbobsCOM.SBObob dt = (SAPbobsCOM.SBObob)SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoBridge);
            SAPbobsCOM.Recordset rc = (SAPbobsCOM.Recordset)SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            try
            {
                rc = dt.Format_StringToDate(sDate);
                return DateTime.Parse(rc.Fields.Item(0).Value.ToString());
            }
            catch (Exception ex)
            {
                SBOApplication.MessageBox(Common.ReadException(ex), 1, "Ok", "", "");
                return DateTime.Today;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(dt);
                dt = null;
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(rc);
                rc = null;
                GC.Collect();
            }
        }

        public static string getDateTimeString(DateTime dDate)
        {
            SAPbobsCOM.SBObob dt = (SAPbobsCOM.SBObob)SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoBridge);

            try
            {
                string test = dt.Format_DateToString(dDate).Fields.Item(0).Value.ToString();
                return (dt.Format_DateToString(dDate).Fields.Item(0).Value.ToString());
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(dt);
                dt = null;
                GC.Collect();
            }
        }

        public static void setStatus(string message)
        {
            if (statusForm == null) return;

            try
            {
                if (!statusForm.Visible) statusForm.Visible = true;

                ((SAPbouiCOM.StaticText)(statusForm.Items.Item("lblStatus").Specific)).Caption = message;
            }
            catch (Exception ex)
            {
                SBOApplication.MessageBox(Common.ReadException(ex), 1, "Ok", "", "");
            }
        }

        public static int getNewformUID()
        {
            formUID++;

            while (true)
            {
                if (getUID()) break;

                formUID++;
            }

            return formUID;
        }

        private static Boolean getUID()
        {
            try
            {
                return !SBOApplication.Forms.OfType<SAPbouiCOM.Form>().Where(form =>
                {
                    try
                    {
                        return form.UniqueID == $"FT_{ formUID }";
                    }
                    finally
                    {
                        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(form);
                        form = null;
                    }
                }).Any();
            }
            finally
            {
                GC.Collect();
            }
        }

        public static void showDebugMessage(string Text, int defaultBtn, string btn1Caption, string btn2Caption, string btn3Caption)
        {
            if (!bDebugMode) return;
            
            SBOApplication.MessageBox(Text, defaultBtn, btn1Caption, btn2Caption, btn3Caption);
        }

        public static void showDebugMessage(string Text)
        {
            showDebugMessage(Text, 1, "Close", "", "");
        }

        public static void showActionResult(List<ActionResult> actionResults)
        {
            SAPbouiCOM.Form oForm = null;
            SAPbouiCOM.Grid oGrid = null;
            SAPbouiCOM.FormCreationParams creationPackage = (SAPbouiCOM.FormCreationParams)SBOApplication.CreateObject(SAPbouiCOM.BoCreatableObjectType.cot_FormCreationParams);

            try
            {
                System.Xml.XmlDocument xmlDoc = new System.Xml.XmlDocument();
                string path = System.Windows.Forms.Application.StartupPath;
                xmlDoc.Load(path + "\\Resources\\Form_ActionResult.xml");
                string formUID = $"FT_{ getNewformUID() }";
                creationPackage.UniqueID = formUID;
                creationPackage.XmlData = xmlDoc.InnerXml;     // Load form from xml 
                oForm = SBOApplication.Forms.AddEx(creationPackage);

                SAPbouiCOM.DataTable dataTable = oForm.DataSources.DataTables.Item("ActResult");

                for (int i = 0; i < actionResults.Count; i++)
                {
                    dataTable.SetValue(0, i, i + 1);
                    dataTable.SetValue(1, i, actionResults[i].status);
                    dataTable.SetValue(2, i, actionResults[i].key);
                    dataTable.SetValue(3, i, actionResults[i].reason);
                    dataTable.Rows.Add();
                }

                oGrid = (SAPbouiCOM.Grid)oForm.Items.Item("grid1").Specific;
                oGrid.AutoResizeColumns();
                oForm.Visible = true;
                oForm = null;
            }
            catch (Exception ex)
            {
                SBOApplication.MessageBox(Common.ReadException(ex), 1, "Ok", "", "");
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(creationPackage);
                creationPackage = null;

                if (oForm != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oForm);
                    oForm = null;
                }

                if (oGrid != null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oGrid);
                    oGrid = null;
                }

                GC.Collect();
            }
        }

        public static void setApplication()
        {
            try
            {
                SAPbouiCOM.SboGuiApi SboGuiApi = null;
                string sConnectionString = null;
                SboGuiApi = new SAPbouiCOM.SboGuiApi();

                // Connect to running SBO Application
#if DEBUG
                sConnectionString = "0030002C0030002C00530041005000420044005F00440061007400650076002C0050004C006F006D0056004900490056";
#else
                sConnectionString = Environment.GetCommandLineArgs().GetValue(1).ToString();
#endif

                // Fast Track SBOi AddOn License Key
                //SboGuiApi.AddonIdentifier = "56455230354241534953303030363030303439303A4C30353436383833333837BE2D8E3EA0DBD35826EF326077F8A12A43680561";

                SboGuiApi.Connect(sConnectionString);

                // Get an instantialized application object
                SBOApplication = SboGuiApi.GetApplication(-1);
            }

            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(Common.ReadException(ex));
                System.Environment.Exit(0);
            }
        }

        public static int connectToCompany()
        {
            _SBOCompany = (SAPbobsCOM.Company)SBOApplication.Company.GetDICompany();

            if (SBOCompany.Connected) return 0;

            // Connect to SBO company database
            return SBOCompany.Connect();
        }

        public static void SetupObjectTable()
        {
            if (SBOCompany == null) return;

            obj2table.Clear();
            table2obj.Clear();

            Enum.GetValues(typeof(SAPbobsCOM.BoObjectTypes))
                .Cast<SAPbobsCOM.BoObjectTypes>()
                .ToList()
                .ForEach(type =>
                {
                    try
                    {
                        SAPbobsCOM.Documents oDoc = (SAPbobsCOM.Documents)SBOCompany.GetBusinessObject(type);
                        string tablename = oDoc.GetTableName();
                        obj2table.Add((int)type, tablename);
                        table2obj.Add(tablename, (int)type);
                    }
                    catch (Exception)
                    {
                    }
                });
        }

        public static void StartTransaction()
        {
            if (SBOCompany.InTransaction) SBOCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);

            SBOCompany.StartTransaction();
        }

        public static void RollBack()
        {
            if (SBOCompany.InTransaction) SBOCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
        }

        public static void Commit()
        {
            if (SBOCompany.InTransaction) SBOCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);
        }

        public static string GetTableName(int objtype)
        {
            if (obj2table.TryGetValue(objtype, out var tablename)) return tablename;

            return String.Empty;
        }
        
        public static string GetLineTableName(int objtype)
        {
            if (obj2table.TryGetValue(objtype, out var tablename)) return $"{ tablename.Substring(1) }1";

            return String.Empty;
        }

        public static int GetObjectType(string tablename)
        {
            if (table2obj.TryGetValue(tablename, out var objtype)) return objtype;

            return 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace FT_ADDON
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ServiceStack.OrmLite.OrmLiteConfig.DialectProvider = ServiceStack.OrmLite.SqlServerDialect.Provider;
            SAPRun();
            AddOn addon = new AddOn();
            Application.Run();
        }

        public static void SAPRun()
        {
            // Get an instantialized application object 
            SAP.setApplication();

            // Connect to SBO Database through DIAPI
            if (SAP.connectToCompany() != 0)
            {
                SAP.SBOApplication.MessageBox($"{ SAP.SBOCompany.GetLastErrorCode() }: { SAP.SBOCompany.GetLastErrorDescription() }", 1, "OK", "", "");
                System.Environment.Exit(0); //  Terminating the Add-On Application
            }

            SAP.SetupObjectTable();
        }
    }
}
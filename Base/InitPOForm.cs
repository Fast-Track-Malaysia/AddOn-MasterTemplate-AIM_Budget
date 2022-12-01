using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace FT_ADDON
{
    class InitPOForm
    {
        public static bool VInventory()
        {
            SAPbouiCOM.Form oForm = null;
            bool done = false;

            try
            {
                System.Xml.XmlDocument xmlDoc = new System.Xml.XmlDocument();
                string path = System.Windows.Forms.Application.StartupPath;
                xmlDoc.Load(path + "\\Resources\\FT_PurchaseOrder.xml");

                System.Xml.XmlAttributeCollection xmlCol = xmlDoc.LastChild.FirstChild.FirstChild.FirstChild.Attributes;
                System.Xml.XmlNode node = xmlCol.GetNamedItem("title");
                string name = MethodBase.GetCurrentMethod().DeclaringType.Namespace.Replace("FT_ADDON.", "");
                node.Value = $"Customer Sales Order - { name }";

                foreach (System.Xml.XmlAttribute att in xmlCol)
                {
                    if (att.Value == "FT_PurchaseOrder")
                    {
                        att.Value = $"FT_{ name }{ typeof(PurchaseOrder_Base).Name }";
                    }
                }

                SAPbouiCOM.FormCreationParams creationPackage = (SAPbouiCOM.FormCreationParams)SAP.SBOApplication.CreateObject(SAPbouiCOM.BoCreatableObjectType.cot_FormCreationParams);
                creationPackage.UniqueID = $"FT_{ SAP.getNewformUID() }";
                creationPackage.XmlData = xmlDoc.InnerXml;     // Load form from xml 
                oForm = SAP.SBOApplication.Forms.AddEx(creationPackage);
                oForm.AutoManaged = true;
                oForm.Mode = SAPbouiCOM.BoFormMode.fm_ADD_MODE;

                // force client height
                oForm.ClientHeight = int.Parse(xmlCol.GetNamedItem("client_height").Value);

                oForm.Visible = true;
                done = true;
            }
            catch (Exception ex)
            {
                SAP.SBOApplication.MessageBox(Common.ReadException(ex), 1, "OK", "", "");
            }
            finally
            {
                if (oForm != null && !done) oForm.Close();
            }

            return true;
        }
    }
}

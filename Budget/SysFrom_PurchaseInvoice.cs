using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON.Budget
{
    [NoForm]
    [FormCode("141")]
    class SysFrom_PurchaseInvoice : Form_Base
    {
        public SysFrom_PurchaseInvoice()
        {
            AddAfterDataFunc(SAPbouiCOM.BoEventTypes.et_FORM_DATA_ADD, afterdataadd);
            //AddAfterDataFunc(SAPbouiCOM.BoEventTypes.et_FORM_DATA_UPDATE, afterdataupdate);
            AddAfterItemFunc(SAPbouiCOM.BoEventTypes.et_ITEM_PRESSED, afterclickupdate);
        }
        private void afterdataadd()
        {
            checkbudget();
        }
        //private void afterdataupdate()
        //{
        //    checkbudget();
        //    SAP.SBOApplication.ActivateMenuItem("1304");
        //}
        private void afterclickupdate()
        {
            if (itemPVal.ItemUID == "1" && oForm.Mode == SAPbouiCOM.BoFormMode.fm_OK_MODE)
            {
                checkbudget();
                SAP.SBOApplication.ActivateMenuItem("1304");
            }
        }
        private void checkbudget()
        {
            SAPbobsCOM.Recordset rs = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            SQLQuery.FillFromSAPSQL(oForm, $"{nameof(SysFrom_PurchaseInvoice)}.checkbudget", out rs);

            if (rs.RecordCount > 0)
            {
                if (rs.Fields.Item(0).Value.ToString() != "0")
                {
                    SAP.SBOApplication.MessageBox(rs.Fields.Item(1).Value.ToString());
                }
            }
            //SAPbouiCOM.DBDataSource dt = oForm.DataSources.DBDataSources.Item("OPCH");
            //string docentry = dt.GetValue("DocEntry", 0);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON.Budget
{
    [NoForm]
    [FormCode("UDO_FT_bdg")]
    class UDO_FT_bdg : Form_Base
    {
        private const string headerds = "@BUDGET_H";
        private const string detailds = "@BUDGET_D";
        private const string findbtn = "findbtn";
        private const string grid1 = "0_U_G";
        public UDO_FT_bdg()
        {
            AddAfterItemFunc(SAPbouiCOM.BoEventTypes.et_ITEM_PRESSED, pressfind);
            AddAfterItemFunc(SAPbouiCOM.BoEventTypes.et_FORM_LOAD, formload);
            AddAfterItemFunc(SAPbouiCOM.BoEventTypes.et_FORM_RESIZE, formresize);
        }
        private void formload()
        {
            var itm = oForm.Items.Add(findbtn, SAPbouiCOM.BoFormItemTypes.it_BUTTON);
            ((SAPbouiCOM.Button)itm.Specific).Caption = "Find";
        }
        private void formresize()
        {
            var relative = oForm.Items.Item("U_RC");
            var itm = oForm.Items.Item(findbtn);
            itm.Left = relative.Left + relative.Width - itm.Width;
            itm.Top = relative.Top - 20;

        }
        private void pressfind()
        {
            if (itemPVal.ItemUID != findbtn) return;
            var grid = oForm.Items.Item(grid1);
            var matrix = GetMatrix(grid1);
            matrix.FlushToDataSource();
            matrix.SelectionMode = SAPbouiCOM.BoMatrixSelect.ms_None;
            matrix.SelectionMode = SAPbouiCOM.BoMatrixSelect.ms_Single;

            var ds = oForm.DataSources.DBDataSources.Item(headerds);
            var ds1 = oForm.DataSources.DBDataSources.Item(detailds);

            string U_f_gl = ds.GetValue("U_f_gl", 0).Trim();
            string U_f_cw = ds.GetValue("U_f_cw", 0).Trim();
            string U_f_jb = ds.GetValue("U_f_jb", 0).Trim();
            string U_f_wl = ds.GetValue("U_f_wl", 0).Trim();
            string U_f_bg = ds.GetValue("U_f_bg", 0).Trim();
            string U_f_unit = ds.GetValue("U_f_unit", 0).Trim();
            int check = 0;
            if (string.IsNullOrEmpty(U_f_gl))
            {
                SAP.SBOApplication.SetStatusBarMessage("GL Code is mandatory.", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                return;
            }

            if (!string.IsNullOrEmpty(U_f_cw)) check++;
            if (!string.IsNullOrEmpty(U_f_jb)) check++;
            if (!string.IsNullOrEmpty(U_f_wl)) check++;
            if (!string.IsNullOrEmpty(U_f_bg)) check++;
            if (!string.IsNullOrEmpty(U_f_unit)) check++;

            if (check != 1)
            {
                SAP.SBOApplication.SetStatusBarMessage("Only 1 dimesion is allowed.", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                return;
            }

            bool found = false;
            for (int x = 0; x < ds1.Size; x++)
            {
                if (string.IsNullOrEmpty(U_f_gl) || U_f_gl == ds1.GetValue("U_gc", x).Trim())
                    if (string.IsNullOrEmpty(U_f_cw) || U_f_cw == ds1.GetValue("U_cw", x).Trim())
                        if (string.IsNullOrEmpty(U_f_jb) || U_f_jb == ds1.GetValue("U_jb", x).Trim())
                            if (string.IsNullOrEmpty(U_f_wl) || U_f_wl == ds1.GetValue("U_wc", x).Trim())
                                if (string.IsNullOrEmpty(U_f_bg) || U_f_bg == ds1.GetValue("U_bg", x).Trim())
                                    if (string.IsNullOrEmpty(U_f_unit) || U_f_unit == ds1.GetValue("U_unit", x).Trim())
                                    {
                                        found = true;
                                        matrix.SelectRow(x + 1, true, false);
                                        break;
                                    }
            }

            if (found)
                SAP.SBOApplication.MessageBox("Matched result highlighted.");
            else
                SAP.SBOApplication.SetStatusBarMessage("Result not matched.", SAPbouiCOM.BoMessageTime.bmt_Short, true);

        }

    }
}

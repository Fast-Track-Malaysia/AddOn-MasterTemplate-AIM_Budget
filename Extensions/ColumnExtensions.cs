using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    static class ColumnExtensions
    {
        public static string GetValueFromColumn(this SAPbouiCOM.Column col, int row)
        {
            SAPbouiCOM.Cell oCell = col.Cells.Item(row);
            var txt = oCell.Specific as SAPbouiCOM.EditText;

            if (txt != null) return txt.Value;

            var cb = oCell.Specific as SAPbouiCOM.ComboBox;

            if (cb != null) return cb.Value;

            return (oCell.Specific as SAPbouiCOM.CheckBox).Checked ? "Y" : "N";
        }
    }
}

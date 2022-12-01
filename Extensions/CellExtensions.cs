using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    static class CellExtensions
    {
        public static object GetValue(this SAPbouiCOM.Cell cell)
        {
            try { return (cell.Specific as SAPbouiCOM.EditText).Value; }
            catch (Exception) { }

            try { return (cell.Specific as SAPbouiCOM.ComboBox).Selected.Value; }
            catch (Exception) { }

            try { return (cell.Specific as SAPbouiCOM.CheckBox).Checked; }
            catch (Exception) { }

            var obj = cell.Specific;
            Type type = obj.GetSAPType();
            return type.GetProperty("Value").GetValue(obj);
        }

        public static void SetValue(this SAPbouiCOM.Cell cell, object value)
        {
            try { (cell.Specific as SAPbouiCOM.EditText).Value = value.ToString(); }
            catch (Exception) { }

            try { (cell.Specific as SAPbouiCOM.ComboBox).Select(value, SAPbouiCOM.BoSearchKey.psk_ByValue); }
            catch (Exception) { }

            try { (cell.Specific as SAPbouiCOM.CheckBox).Checked = Convert.ToBoolean(value); }
            catch (Exception) { }

            var obj = cell.Specific;
            Type type = obj.GetSAPType();
            type.GetProperty("Value").SetValue(obj, value);
        }
    }
}

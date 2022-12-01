using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    static class ComboBoxExtensions
    {
        public static void AddValidValuesFromQuery(this SAPbouiCOM.ComboBox cb, string query)
        {
            using (RecordSet rc = new RecordSet())
            {
                rc.DoQuery(query);

                while (!rc.EoF)
                {
                    cb.ValidValues.Add(rc.GetValue(0).ToString(), rc.GetValue(1).ToString());
                }
            }
        }
    }
}

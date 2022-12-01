using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    static class StockTransferLineExtensions
    {
        public static object GetUserFieldValue(this SAPbobsCOM.StockTransfer_Lines oDocLine, object field)
        {
            return oDocLine.UserFields.Fields.Item(field).Value;
        }

        public static void SetUserFieldValue(this SAPbobsCOM.StockTransfer_Lines oDocLine, object field, object value)
        {
            oDocLine.UserFields.Fields.Item(field).Value = value;
        }
    }
}

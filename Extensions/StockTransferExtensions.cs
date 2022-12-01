using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    static class StockTransferExtensions
    {
        public static object GetUserFieldValue(this SAPbobsCOM.StockTransfer oDoc, object field)
        {
            return oDoc.UserFields.Fields.Item(field).Value;
        }

        public static void SetUserFieldValue(this SAPbobsCOM.StockTransfer oDoc, object field, object value)
        {
            oDoc.UserFields.Fields.Item(field).Value = value;
        }
    }
}

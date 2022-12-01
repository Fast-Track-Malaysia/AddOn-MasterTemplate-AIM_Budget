using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace FT_ADDON.Example
{
    class PurchaseOrder : PurchaseOrder_Base
    {
        #region MODIFIABLE VARIABLES
        public override List<object> headers
        {
            get
            {
                return new List<object>
                {
                    /* 0 - Outlet */            "VENDOR BP",

                    /* 1 - Group Data */        154,

                    /* 2 - PO No. */            "REFNO",

                                                new Date(
                    /* 3 - Doc Date */          "",
                    /* Date format */           "yyyyMMdd"
                                                    ),

                                                new Date(
                    /* 4 - Tax Date */          "",
                    /* Date format */           "yyyyMMdd"
                                                    ),

                                                new Date(
                    /* 5 - Doc Due Date */      "",
                    /* Date format */           "yyyyMMdd"
                                                    ),

                                                new Date(
                    /* 6 - Cancel Date */       "",
                    /* Date format */           "yyyyMMdd"
                                                    ),

                    /* 7 - Non-SAP Item Code */ "ITEMCODE",

                    /* 8 - Quantity */          "QTY",

                    /* 9 - Unit price */        "UNIT PRICE",

                    /* 10 - Separator */        ',',

                    /* 11 - Warehouse */        "WAREHOUSE",
                };
            }
        }
        #endregion

        #region DO NOT MODIFY
        public static string UniqueCode
        {
            get
            {
                return $"FT_{ MethodBase.GetCurrentMethod().DeclaringType.Name }";
            }
        }
        #endregion
    }
}

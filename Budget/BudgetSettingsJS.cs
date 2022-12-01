using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON.Budget
{
    class BudgetSettingsJS : AddOnSettings
    {
        public override bool Setup()
        {
            //string tablename = "FTRENTALPROP";
            //string tabledesc = "Rental Property";
            //UserTable udtprop = new UserTable(tablename, tabledesc);
            ///// use which series number when invoice
            //if (!udtprop.createField("SeriesNo", "Rental Series No", SAPbobsCOM.BoFieldTypes.db_Numeric, 0, "0")) return false;
            ///// rental type, currently Monthly only, not allowed change once rental prop found in FTSORENTAL
            //if (!udtprop.createField("RType", "Rental Type", SAPbobsCOM.BoFieldTypes.db_Alpha, 1, "M", true, SAPbobsCOM.BoFldSubTypes.st_None, false, false, "M:Monthly")) return false;
            ///// how long is the interval per rental, not allowed change once rental prop found in FTSORENTAL
            //if (!udtprop.createField("Cycle", "Rental Cycle", SAPbobsCOM.BoFieldTypes.db_Numeric, 0, "1", true)) return false;
            ///// total how many times rental bills, not allowed change once rental prop found in FTSORENTAL
            //if (!udtprop.createField("Total", "Rental Total Count", SAPbobsCOM.BoFieldTypes.db_Numeric, 0, "12", true)) return false;

            ///// for SO to identify SO rental is active or not. can inactive in between of rental
            //if (!UserTable.createField("ORDR", "RentActive", "Rental Active", SAPbobsCOM.BoFieldTypes.db_Alpha, 1, "N", false, SAPbobsCOM.BoFldSubTypes.st_None, "Y:Yes|N:No")) return false;
            ///// for SO & invoice link to rental property
            //if (!UserTable.createField("ORDR", "RentPop", "Rental Property", SAPbobsCOM.BoFieldTypes.db_Alpha, 50, "", false, SAPbobsCOM.BoFldSubTypes.st_None, "", "FTRENTALPROP")) return false;
            ///// for invoice to record this invoice is from which SO
            //if (!UserTable.createField("ORDR", "SODocEntry", "SO DocEntry", SAPbobsCOM.BoFieldTypes.db_Numeric, 9)) return false;
            ///// for invoice to record this invoice is which number of rental invoice
            //if (!UserTable.createField("ORDR", "RentalNo", "Rental#", SAPbobsCOM.BoFieldTypes.db_Numeric, 0, "0")) return false;

            //tablename = "FTSORENTAL";
            //tabledesc = "Sales Order Rental";
            //UserTable udtso = new UserTable(tablename, tabledesc, SAPbobsCOM.BoUTBTableType.bott_DocumentLines);
            ///// calculated rental invoice date, may different from actual invoice date
            //if (!udtso.createField("DocDate", "Document Date", SAPbobsCOM.BoFieldTypes.db_Date)) return false;
            //if (!udtso.createField("CardCode", "Card Code", SAPbobsCOM.BoFieldTypes.db_Alpha, 50)) return false;
            //if (!udtso.createField("CardName", "Card Name", SAPbobsCOM.BoFieldTypes.db_Alpha, 50)) return false;
            ///// aka invoice amount
            //if (!udtso.createField("DocTotal", "Document Total", SAPbobsCOM.BoFieldTypes.db_Float, 0, "0", false, SAPbobsCOM.BoFldSubTypes.st_Sum)) return false;
            ///// aka SO total amount
            //if (!udtso.createField("SOTotal", "SO Total", SAPbobsCOM.BoFieldTypes.db_Float, 0, "0", false, SAPbobsCOM.BoFldSubTypes.st_Sum)) return false;
            ///// how many rental invoice billed
            //if (!udtso.createField("RentalNo", "Rental#", SAPbobsCOM.BoFieldTypes.db_Numeric)) return false;
            ///// for validate rental prop value
            //if (!udtso.createField("RentPop", "Rental Property", SAPbobsCOM.BoFieldTypes.db_Alpha, 50)) return false;
            //if (!udtso.createField("RentPopDesc", "Rental Property Description", SAPbobsCOM.BoFieldTypes.db_Alpha, 100)) return false;
            ///// for validate rental prop value
            //if (!udtso.createField("RType", "Rental Type", SAPbobsCOM.BoFieldTypes.db_Alpha, 1)) return false;
            ///// for validate rental prop value
            //if (!udtso.createField("Cycle", "Rental Cycle", SAPbobsCOM.BoFieldTypes.db_Numeric)) return false;
            ///// for validate rental prop value
            //if (!udtso.createField("Total", "Rental Total Count", SAPbobsCOM.BoFieldTypes.db_Numeric)) return false;
            ///// rental invoice posting status (Y = posted, N = not posted, C = cancel)
            //if (!udtso.createField("Posted", "Rental Posted", SAPbobsCOM.BoFieldTypes.db_Alpha, 1, "N")) return false;
            ///// rental invoice actual document number
            //if (!udtso.createField("IVDocNum", "Invoice Number", SAPbobsCOM.BoFieldTypes.db_Numeric, 9)) return false;
            ///// rental invoice actual document date
            //if (!udtso.createField("IVDocDate", "Invoice Document Date", SAPbobsCOM.BoFieldTypes.db_Date)) return false;

            //UserTable udt = new UserTable("SQLQuery", "Query Table");

            //if (!udt.createField("Query", "Query", SAPbobsCOM.BoFieldTypes.db_Memo, 254, "", false, SAPbobsCOM.BoFldSubTypes.st_None)) return false;

            GC.Collect();

            //if (Common.createQuery("Get Discountable Item Code", "SELECT \"ItemCode\" FROM \"OITM\" WHERE \"U_IncDiscount\"='Y'", out int qr))
            //{
            //    Common.createFormattedSearch("UDO_FT_DISTCUST", qr, "0_U_G", "C_0_2");
            //    Common.createFormattedSearch("UDO_FT_DISTGRP", qr, "0_U_G", "C_0_2");
            //}

            return true;
        }
    }
}

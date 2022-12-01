using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using System.Data.SqlClient;
using System.Threading;
using System.Numerics;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;

namespace FT_ADDON
{
    class Date
    {
        public string date;
        public string format;

        public Date(string n, string f)
        {
            date = n;
            format = f;
        }
    }

    abstract class PurchaseOrder_Base
    {
        public static Type[] list = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                                     from assemblyType in domainAssembly.GetTypes()
                                     where typeof(PurchaseOrder_Base).IsAssignableFrom(assemblyType)
                                     where !assemblyType.IsAbstract
                                     select assemblyType).ToArray();

        #region MODIFIABLE VARIABLES
        public virtual List<object> headers     // only copy this header except for some additional modificatoin in below variables
        {
            get
            {
                return new List<object>
                {
                    /* 0 - Outlet */            "ORG_NUMBER",

                    /* 1 - Group Data */        191,

                    /* 2 - PO No. */            "PO_NUMBER",

                                                new Date(
                    /* 3 - Doc Date */          "ENTRY_DATE",
                    /* Date format */           "yyyyMMdd"
                                                    ),

                                                new Date(
                    /* 4 - Cancel Date */       "EXPIRY_DATE",
                    /* Date format */           "yyyyMMdd"
                                                    ),

                                                new Date(
                    /* 5 - Doc Due Date */      "DELIVERY_DATE",
                    /* Date format */           "yyyyMMdd"
                                                    ),

                                                new Date(
                    /* 6 - Tax Date */          "ENTRY_DATE",
                    /* Date format */           "yyyyMMdd"
                                                    ),

                    /* 7 - Non-SAP Item Code */ "PRD_NUMBER",

                    /* 8 - Quantity */          "ORDER_QTY",

                    /* 9 - Unit price */        "UNIT_PRICE",

                    /* 10 - Separator */        ',',

                    /* 11 - Warehouse */        "WAREHOUSE",
                };
            }
        }

        #region NON-HEADERS
        protected virtual int streamColumnEnd
        {
            get
            {
                return 0;
            }
        }

        // do you need to check if the buyer (outlet) is valid?
        protected virtual bool checkBuyer
        {
            get
            {
                return true;
            }
        }

        // is the buyer (outlet) valid?
        protected virtual bool Buyer
        {
            get
            {
                return true;

                if (!checkBuyer) return true;

                SAPbobsCOM.Recordset rc = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                rc.DoQuery($"Select CardCode, SlpCode FROM OCRD WHERE U_OutletCode = '{ Outlet }' AND U_GROUP2={ Group_data.ToString() }");

                if (rc.RecordCount <= 0) throw new MessageException($" - Invalid Business Partner Mapping Code (Outlet not found) - { Outlet }");

                CardCodeVar = rc.Fields.Item("CardCode").Value.ToString();
                SlpVar = Convert.ToInt32(rc.Fields.Item("SlpCode").Value);
                return true;
            }
        }

        protected virtual string PurchaseOrderNo
        {
            get
            {
                return row[PurchaseOrderNo_data].ToString();
            }
        }

        protected virtual string ItemCode
        {
            get
            {
                return Item_raw;

                if (ItemVar == null)
                {
                    SAPbobsCOM.Recordset rc = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                    rc.DoQuery($"Select U_ItemCode FROM [@B2B] WHERE { SKU }='{ Item_raw }'");

                    if (rc.RecordCount <= 0)
                    {
                        throw new MessageException($" - Invalid Item Mapping Code - { Item_raw }");
                    }

                    ItemVar = rc.Fields.Item("U_ItemCode").Value.ToString();
                    rc.DoQuery($"Select U_SubBrand, VatGourpSa FROM OITM WHERE ItemCode = '{ ItemVar }'");

                    if (rc.RecordCount <= 0)
                    {
                        throw new MessageException($" - Invalid Item Code - { Item_raw }");
                    }

                    VatGroupVar = rc.Fields.Item("VatGourpSa").Value.ToString();
                    SubBrandVar = rc.Fields.Item("U_SubBrand").Value.ToString();
                    BrandFlag = SubBrandVar == "WARDAH" ? 1 : 2;
                }

                return ItemVar;
            }
        }

        protected virtual decimal NumInSale
        {
            get
            {
                if (NumInSaleVar == -1)
                {
                    SAPbobsCOM.Recordset rc = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                    rc.DoQuery("SELECT \"T1\".\"NumInSale\",\"T4\".\"Cardcode\",\"T0\".\"ItemCode\", \"T1\".\"ItemName\",\"T0\".\"Price\", \"T0\".\"PriceList\", \"T2\".\"ListName\", \"T1\".\"BuyUnitMsr\", " +
                        "\"T1\".\"NumInSale\", CONVERT(NUMERIC(18,3),(\"T0\".\"Price\"*\"T1\".\"NumInSale\")) " +
                        "AS \"SysPrice\" FROM \"ITM1\" \"T0\" INNER JOIN \"OITM\" \"T1\" ON \"T0\".\"ItemCode\" = \"T1\".\"ItemCode\" INNER JOIN \"OPLN\" \"T2\" ON \"T0\".\"PriceList\" = \"T2\".\"ListNum\" " +
                        "INNER JOIN \"OITB\" \"T3\" ON \"T1\".\"ItmsGrpCod\" = \"T3\".\"ItmsGrpCod\" LEFT JOIN \"OCRD\" \"T4\" ON " +
                        $"\"T4\".\"ListNum\" = \"T2\".\"ListNum\" WHERE \"T4\".\"CardCode\"= '{ CardCode }' AND \"T0\".\"ItemCode\"='{ ItemCode }'");
                    NumInSaleVar = decimal.Parse(rc.Fields.Item("NumInSale").Value.ToString());
                }

                return NumInSaleVar;
            }
        }

        protected virtual decimal UnitPrice
        {
            get
            {
                return Math.Round(UnitPrice_raw, 3);
            }
        }

        protected virtual int SalesPerson
        {
            get
            {
                return -1;

                if (SlpVar == -1)
                {
                    BuyerValidation();

                    if (!checkBuyer)
                    {
                        if (SlpColVar == -1)
                        {
                            string bp = BPCol;
                        }

                        return SlpColVar;
                    }
                }

                return SlpVar;
            }
        }

        protected virtual string CardCodeFixed
        {
            get
            {
                if (CardCodeVar == null) BuyerValidation();

                return CardCodeVar;
            }
        }

        protected virtual string CardCode
        {
            get
            {
                if (CardCodeVar == null)
                {
                    CardCodeVar = row[headers[0].ToString()].ToString();
                    SAPbobsCOM.Recordset rc = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                    rc.DoQuery($"SELECT \"SlpCode\" FROM \"OCRD\" WHERE \"CardCode\"='{ CardCodeVar }'");

                    if (rc.RecordCount <= 0)
                    {
                        throw new MessageException($" - Business Partner's Customer Code not found - { CardCodeVar }");
                    }
                }

                return CardCodeVar;
            }
        }

        protected virtual string BPCol
        {
            get
            {
                if (BPColVar == null)
                {
                    SAPbobsCOM.Recordset rc = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                    rc.DoQuery($"SELECT * FROM \"@B2BSetup\" WHERE \"Code\"='{ PurchaseOrderName }'");

                    if (rc.RecordCount <= 0)
                    {
                        throw new MessageException($" - B2B Code not found in B2B Setup Table - { PurchaseOrderName }");
                    }

                    WhsCodeVar = rc.Fields.Item("U_WhseNormal").Value.ToString();
                    BPColVar = rc.Fields.Item("U_ConsolBP").Value.ToString();
                    rc.DoQuery($"SELECT \"SlpCode\" FROM \"OCRD\" WHERE \"CardCode\"='{ BPColVar }'");

                    if (rc.RecordCount <= 0)
                    {
                        throw new MessageException($" - Consolidate Business Partner's Customer Code not found - { BPColVar }");
                    }

                    SlpColVar = Convert.ToInt32(rc.Fields.Item("SlpCode").Value);
                }

                return BPColVar;
            }
        }

        protected virtual string VatGroup
        {
            get
            {
                if (VatGroupVar == null)
                {
                    string item = ItemCode;
                }

                return VatGroupVar;
            }
        }

        protected virtual string WhsCode
        {
            get
            {
                if (WhsCodeVar == null)
                {
                    WhsCodeVar = row[headers[11].ToString()].ToString();
                    SAPbobsCOM.Recordset rc = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                    rc.DoQuery($"SELECT * FROM \"OWHS\" WHERE \"WhsCode\"='{ WhsCodeVar }'");

                    if (rc.RecordCount <= 0) throw new MessageException($" - Warehouse code not found - { WhsCodeVar }");
                }

                return WhsCodeVar;
            }
        }
        #endregion
        #endregion

        #region BASE FUNCTIONS
        protected static Type GetBaseClassType()
        {
            return MethodBase.GetCurrentMethod().DeclaringType;
        }

        protected virtual DateTime GetDateTime(object obj)
        {
            Date date = obj as Date;

            if (date != null && date.date.Length > 0 && date.format.Length > 0)
            {
                string strDate = row[date.date].ToString().Substring(0, 8);
                return DateTime.ParseExact(strDate, date.format, null);
            }
            else
            {
                return DateTime.Today;
            }
        }

        public static bool isCustomPurchaseOrder(string uid)
        {
            foreach (Type each in list)
            {
                if ($"FT_{ each.Name }" == uid) return true;
            }

            return false;
        }

        public static PurchaseOrder_Base newCustomPurchaseOrder(string uid)
        {
            foreach (Type each in list)
            {
                if ($"FT_{ each.Name }" == uid) return Activator.CreateInstance(each) as PurchaseOrder_Base;
            }

            return null;
        }

        protected void resetLine()
        {
            ItemVar = null;
            SlpVar = -1;
            SlpColVar = -1;
            BrandFlag = 0;
            SubBrandVar = null;
            CardCodeVar = null;
            NumInSaleVar = -1;
            WhsCodeVar = null;
            BPColVar = null;
            VatGroupVar = null;
            CustGroupVar = null;
            SalesDivVar = null;
        }

        protected string[] lineFilter(string l)
        {
            string sep = Separator.ToString();
            string line = l.Replace($"\"{ sep }", sep).Replace(sep + "\"", sep);

            if (line[0] == '\"') line = line.Substring(1);
            if (line[line.Length - 1] == '\"') line = line.Substring(0, line.Length - 1);

            return line.Split(Separator);
        }

        public static bool SizeAssertion()
        {
            foreach (Type type in list)
            {
                PurchaseOrder_Base order = Activator.CreateInstance(type) as PurchaseOrder_Base;

                if (order.headers.Count != 11)
                {
                    MessageBox.Show($"Assertion failed!!\nProgram: ...\nFile: { type.Name }.cs\n\nExpression: Header list size not at bound", "Critial Error", MessageBoxButtons.OK, MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1);
                    return false;
                }
            }

            return true;
        }

        public static void processItemEventafter(SAPbouiCOM.Form oForm, ref SAPbouiCOM.ItemEvent pVal)
        {
            List<ActionResult> actionResults = new List<ActionResult>();

            try
            {
                switch (pVal.EventType)
                {
                    case SAPbouiCOM.BoEventTypes.et_ITEM_PRESSED:
                        break;
                }

                if (actionResults.Count > 0)
                {
                    SAP.showActionResult(actionResults);
                }
            }
            catch (Exception ex)
            {
                GC.Collect();
                SAP.SBOApplication.RemoveWindowsMessage(SAPbouiCOM.BoWindowsMessageType.bo_WM_TIMER, true);
                SAP.SBOApplication.MessageBox(Common.ReadException(ex), 1, "Ok", "", "");
                SAP.stopProgressBar();
            }
        }
        #endregion

        #region DO NOT MODIFIY
        protected int TotalLine = 0;
        public int COUNTER = 0;
        protected DataRow row;
        protected int FlagFound = 0;

        // cache
        protected string VatGroupVar = null;
        protected string ItemVar = null;
        protected int SlpVar = -1;
        protected int SlpColVar = -1;
        protected int BrandFlag = 0;
        protected string SubBrandVar = null;
        protected string CardCodeVar = null;
        protected decimal NumInSaleVar = -1;
        protected string WhsCodeVar = null;
        protected string BPColVar = null;
        protected string CustGroupVar = null;
        protected string SalesDivVar = null;

        protected virtual string Outlet
        {
            get
            {
                return row[headers[0].ToString()].ToString();
            }
        }

        protected virtual int Group_data
        {
            get
            {
                return Convert.ToInt32(headers[1]);
            }
        }

        protected virtual string PurchaseOrderNo_data
        {
            get
            {
                return headers[2].ToString();
            }
        }

        protected virtual DateTime DocDate
        {
            get
            {
                return GetDateTime(headers[3]);
            }
        }

        protected virtual DateTime TaxDate
        {
            get
            {
                return GetDateTime(headers[4]);
            }
        }

        protected virtual DateTime DocDueDate
        {
            get
            {
                return GetDateTime(headers[5]);
            }
        }

        protected virtual DateTime CancelDate
        {
            get
            {
                return GetDateTime(headers[6]);
            }
        }

        protected virtual string Item_raw
        {
            get
            {
                return row[headers[7].ToString()].ToString();
            }
        }

        protected virtual decimal Quantity
        {
            get
            {
                return Convert.ToDecimal(row[headers[8].ToString()]);
            }
        }

        protected virtual decimal UnitPrice_raw
        {
            get
            {
                return Convert.ToDecimal(row[headers[9].ToString()]);
            }
        }

        protected virtual char Separator
        {
            get
            {
                return Convert.ToChar(headers[10]);
            }
        }

        public virtual string PurchaseOrderName
        {
            get
            {
                return this.GetType().Name.ToString().Replace(MethodBase.GetCurrentMethod().DeclaringType.Name, "");
            }
        }

        public virtual string SKU
        {
            get
            {
                return $"U_{ this.GetType().Name.Replace(MethodBase.GetCurrentMethod().DeclaringType.Name, "_SKU") }";
            }
        }

        protected virtual bool skipBPCheck
        {
            get
            {
                return false;
            }
        }

        protected virtual bool priceB2BAddWah
        {
            get
            {
                return false;
            }
        }

        protected virtual bool priceB2BAdd
        {
            get
            {
                return false;
            }
        }
        #endregion

        protected virtual DataTable ConvertCSVtoDataTable(string strFilePath)
        {
            bool hasQuote = false;

            using (StreamReader sr = new StreamReader(strFilePath))
            {
                string stringline = sr.ReadLine();
                string check = lineFilter(stringline)[0].ToString().Trim();

                if (check == "START" || check == "END")
                {
                    hasQuote = sr.ReadLine()[0] == '\"';
                }
                else
                {
                    hasQuote = stringline[0] == '\"';
                }

                sr.Close();
            }

            TextFieldParser textParse = new TextFieldParser(strFilePath);
            textParse.HasFieldsEnclosedInQuotes = hasQuote;
            textParse.SetDelimiters(Separator.ToString());

            DataTable dt = new DataTable();
            BigInteger taken = 0;
            bool startRecord = false;

            while (!textParse.EndOfData)
            {
                string[] fields = textParse.ReadFields();

                if (!startRecord || dt.Columns.Count == 0)
                {
                    int counter = 0;

                    foreach (string field in fields)
                    {
                        if (field == "START" || field == "END")
                        {
                            break;
                        }
                        else if (field != "")
                        {
                            if (dt.Rows.Count == 0)
                            {
                                if (dt.Columns.Contains(field))
                                {
                                    taken |= ((BigInteger)1 << counter);
                                }
                                else
                                {
                                    dt.Columns.Add(field);
                                }
                            }

                            ++counter;
                            startRecord = true;
                        }
                    }
                }
                else if (fields[0] != "START" && fields[0] != "END")
                {
                    int counter = 0;
                    int a = 0;
                    DataRow dr = dt.NewRow();

                    foreach (string field in fields)
                    {
                        BigInteger bit = ((BigInteger)1 << counter++);

                        if ((taken & bit) != bit)
                        {
                            dr[a++] = field;
                        }
                    }

                    dt.Rows.Add(dr);
                }
                else
                {
                    startRecord = false;
                }
            }

            textParse.Close();
            return dt;
        }

        public virtual void GABAdd(string path, ref List<ActionResult> actionResults)
        {
            DataTable dt = ConvertCSVtoDataTable(path);

            var result2 = from crow in dt.AsEnumerable()
                          group crow by new { InvNo = crow.Field<string>(PurchaseOrderNo_data) } into grp
                          select new
                          {
                              InvNo1 = grp.Key.InvNo
                          };

            foreach (var t in result2)
            {
                DataRow[] data = dt.Select($"[{ PurchaseOrderNo_data }] = '{ t.InvNo1 }'");
                SAP.SBOApplication.RemoveWindowsMessage(SAPbouiCOM.BoWindowsMessageType.bo_WM_TIMER, true);
                TotalLine = result2.Count();

                if (COUNTER == 0)
                {
                    ProgressBarHandler.Stop();
                    ProgressBarHandler.Start("Importing Document...", 0, 100, true);
                    COUNTER++;
                }

                resetLine();
                AddSO(data, ref actionResults);
            }
        }

        public virtual List<ActionResult> AddSO(DataRow[] data, ref List<ActionResult> actionResults)
        {
            string errMsg = "";
            int Steps = 0;
            int FlagCode = 0;

            DataRow[] dr = data;
            SAPbobsCOM.Documents oDoc = (SAPbobsCOM.Documents)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseOrders);
            SAPbobsCOM.Recordset rs2 = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            try
            {
                Dictionary<string, List<DataRow>> listRow = new Dictionary<string, List<DataRow>>();
                Steps = 100 / TotalLine;

                if (SAP.SBOCompany.InTransaction) SAP.SBOCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);

                SAP.SBOCompany.StartTransaction();

                foreach (DataRow drow in dr)
                {
                    row = drow;
                    string poNum = PurchaseOrderNo;

                    if (!listRow.ContainsKey(PurchaseOrderNo)) listRow.Add(PurchaseOrderNo, new List<DataRow>());

                    listRow[PurchaseOrderNo].Add(drow);
                }

                foreach (string poRow in listRow.Keys)
                {
                    row = listRow[poRow][0];
                    rs2.DoQuery($"SELECT \"DocNum\",\"NumAtCard\" FROM \"ORDR\" WHERE \"NumAtCard\" = '{ PurchaseOrderNo }'");

                    if (rs2.RecordCount > 0)
                    {
                        FlagCode = 2;
                        throw new MessageException($"- Order No. Found. Sales Order No: { PurchaseOrderNo }");
                    }

                    POHeader(oDoc);

                    foreach (DataRow lineRow in listRow[poRow])
                    {
                        row = lineRow;
                        BuyerValidation();

                        if (!POLines(oDoc)) continue;

                        resetLine();
                    }
                }

                //ProgressBarHandler.Increment("Importing Document...", Steps);
                while (oDoc.Add() != 0)
                {
                    SAP.SBOCompany.GetLastError(out int errCode, out errMsg);

                    if (!errMsg.Contains("2038")) throw new MessageException(errMsg);

                    Thread.Sleep(500);
                    ProgressBarHandler.Stop();
                }

                if (SAP.SBOCompany.InTransaction) SAP.SBOCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);

                string key = SAP.SBOCompany.GetNewObjectKey();
                SAPbobsCOM.Recordset rsDocNum = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

                if (FlagFound > 0)
                {
                    ProgressBarHandler.Increment("Importing Document...", Steps);
                    rsDocNum.DoQuery($"SELECT * FROM \"ODRF\" WHERE \"DocEntry\" = { int.Parse(key) }");
                    string Docnumber = rsDocNum.Fields.Item("DocEntry").Value.ToString();
                    actionResults.Add(new ActionResult("Draft", PurchaseOrderNo, $"Unit price does not match with price list. Draft No: { Docnumber }"));
                }
                else
                {
                    ProgressBarHandler.Increment("Importing Document...", Steps);
                    rsDocNum.DoQuery($"SELECT * FROM \"ORDR\" WHERE \"DocEntry\" = { int.Parse(key) }");
                    string Docnumber = rsDocNum.Fields.Item("DocNum").Value.ToString();
                    actionResults.Add(new ActionResult("Success", PurchaseOrderNo, $"SO Document Number:{ Docnumber }"));
                }

                return actionResults;
            }
            catch (MessageException ex)
            {
                ProgressBarHandler.Stop();
                errMsg = ex.Message;

            }
            catch (Exception ex)
            {
                ProgressBarHandler.Stop();
                errMsg = $"Exception caught : { ex.Message }";
            }
            finally
            {
                if (SAP.SBOCompany.InTransaction) SAP.SBOCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);

                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oDoc);
                oDoc = null;
                dr = null;
                GC.Collect();
            }

            if (FlagCode == 2)
            {
                //This is PO.
                ProgressBarHandler.Increment("Importing Document...", Steps);
                string Docnumber2 = rs2.Fields.Item("DocNum").Value.ToString();
                actionResults.Add(new ActionResult("Duplicate", PurchaseOrderNo, errMsg + Docnumber2));
            }
            else
            {
                ProgressBarHandler.Increment("Importing Document...", Steps);
                actionResults.Add(new ActionResult("Error", PurchaseOrderNo, errMsg));
            }

            return actionResults;
        }

        protected virtual void POHeader(SAPbobsCOM.Documents oDoc)
        {
            SAPbobsCOM.Recordset rsSERIES = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            rsSERIES.DoQuery("SELECT \"Series\", \"NextNumber\" FROM \"NNM1\" WHERE \"ObjectCode\" = '22' AND \"Locked\" = 'N'");

            if (rsSERIES.RecordCount > 0)
            {
                rsSERIES.MoveFirst();
            }
            
            oDoc.Series = int.Parse(rsSERIES.Fields.Item("Series").Value.ToString());
            oDoc.DocNum = int.Parse(rsSERIES.Fields.Item("NextNumber").Value.ToString());

            oDoc.CardCode = CardCode;
            oDoc.NumAtCard = PurchaseOrderNo;

            oDoc.DocDate = DocDate;
            oDoc.TaxDate = TaxDate;
            oDoc.DocDueDate = DocDueDate;

            oDoc.UserFields.Fields.Item("U_Bank").Value = this.GetType().Name.Replace(GetBaseClassType().Name, "");

            oDoc.DocType = SAPbobsCOM.BoDocumentTypes.dDocument_Items;
        }

        protected virtual bool POLines(SAPbobsCOM.Documents oDoc)
        {
            oDoc.Lines.ItemCode = ItemCode;
            oDoc.Lines.WarehouseCode = WhsCode;
            oDoc.Lines.Quantity = double.Parse(Quantity.ToString());
            oDoc.Lines.DiscountPercent = 0;
            oDoc.Lines.UnitPrice = 0;
            oDoc.Lines.UserFields.Fields.Item("U_Registered").Value = "N";
            oDoc.Lines.Add();
            return true;
        }

        protected virtual void BuyerValidation()
        {
            bool rtn = Buyer;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FT_ADDON
{
    static class DocumentLineExtensions
    {
        static string[] serialex = new string[]
        {
            "SystemSerialNumber",
            "BaseLineNumber",
        };

        public static string GetLineTableName(this SAPbobsCOM.Documents oDoc)
        {
            var temp = oDoc.DocDate;
            oDoc.DocDate = DateTime.Today;
            System.Xml.XmlDocument xmlDocument = new System.Xml.XmlDocument();
            xmlDocument.LoadXml(oDoc.GetAsXML());
            oDoc.DocDate = temp;

            try
            {
                return xmlDocument.SelectNodes("BOM/BO").Item(0).ChildNodes.Item(1).Name.Substring(1) + "1";
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static object GetUserFieldValue(this SAPbobsCOM.Document_Lines oDocLine, object field)
        {
            return oDocLine.UserFields.Fields.Item(field).Value;
        }

        public static void SetUserFieldValue(this SAPbobsCOM.Document_Lines oDocLine, object field, object value)
        {
            oDocLine.UserFields.Fields.Item(field).Value = value;
        }

        public static void SetUserFieldValue(this SAPbobsCOM.IDocument_Lines oDocLine, object field, object value)
        {
            oDocLine.UserFields.Fields.Item(field).Value = value;
        }

        public static bool SetCurrentLineByLineNum(this SAPbobsCOM.IDocument_Lines oDocLine, int linenum)
        {
            for (int i = 0; i < oDocLine.Count; i++)
            {
                oDocLine.SetCurrentLine(i);

                if (oDocLine.LineNum == linenum) return true;
            }

            return false;
        }

        public static void CopyLinesTo(this SAPbobsCOM.Documents docfrom, SAPbobsCOM.Documents docto)
        {
            var linefrom = docfrom.Lines;
            var lineto = docto.Lines;

            for (int i = 0; i < docfrom.Lines.Count; ++i)
            {
                linefrom.SetCurrentLine(i);

                if (i != 0) lineto.Add();

                linefrom.CopyLineTo((int)docfrom.DocObjectCode, lineto);
            }
        }

        public static void CopyLineTo(this SAPbobsCOM.Document_Lines linefrom, int basetype, SAPbobsCOM.Document_Lines lineto)
        {
            //linefrom.CopySerialTo(lineto);
            linefrom.CopyBatchTo(lineto);
            linefrom.CopyBinTo(lineto);
            linefrom.CopyExpensesTo(lineto);

            lineto.BaseType = basetype;
            lineto.BaseEntry = linefrom.DocEntry;
            lineto.BaseLine = linefrom.LineNum;
        }

        public static void CopySerialTo(this SAPbobsCOM.Document_Lines linefrom, SAPbobsCOM.Document_Lines lineto)
        {
            var serialfrom = linefrom.SerialNumbers;
            var serialto = lineto.SerialNumbers;

            try
            {
                Type serialtype = serialfrom.GetSAPType();

                for (int k = 0; k < serialfrom.Count; ++k)
                {
                    serialfrom.SetCurrentLine(k);

                    if (String.IsNullOrEmpty(serialfrom.InternalSerialNumber)) continue;

                    serialtype.GetProperties()
                        .Where(p => p.CanWrite && !p.PropertyType.IsCOMObject && !serialex.Contains(p.Name))
                        .ToList()
                        .ForEach(prop =>
                        {
                            object value = prop.GetValue(serialfrom);

                            if (value == null || String.IsNullOrEmpty(value.ToString())) return;

                            prop.SetValue(serialto, value);
                        });

                    serialfrom.UserFields.Fields
                        .OfType<SAPbobsCOM.Field>()
                        .Where(field => field.Value.ToString() != "")
                        .ToList()
                        .ForEach(field =>
                        {
                            try { serialto.UserFields.Fields.Item(field.Name).Value = field.Value; }
                            catch (Exception) { }
                        });

                    serialto.Add();
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(serialfrom);
                serialfrom = null;
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(serialto);
                serialto = null;
                GC.Collect();
            }
        }

        public static void CopyBatchTo(this SAPbobsCOM.Document_Lines linefrom, SAPbobsCOM.Document_Lines lineto)
        {
            var batchfrom = linefrom.BatchNumbers;
            var batchto = lineto.BatchNumbers;

            try
            {
                Type batchtype = batchfrom.GetSAPType();

                for (int k = 0; k < batchfrom.Count; ++k)
                {
                    batchfrom.SetCurrentLine(k);

                    if (String.IsNullOrEmpty(batchfrom.BatchNumber)) continue;

                    batchtype.GetProperties()
                        .Where(p => p.CanWrite && !p.PropertyType.IsCOMObject && p.Name != "BaseLineNumber")
                        .ToList()
                        .ForEach(prop =>
                        {
                            object value = prop.GetValue(batchfrom);

                            if (value == null || String.IsNullOrEmpty(value.ToString())) return;

                            prop.SetValue(batchto, value);
                        });

                    batchfrom.UserFields.Fields
                        .OfType<SAPbobsCOM.Field>()
                        .Where(field => field.Value.ToString() != "")
                        .ToList()
                        .ForEach(field =>
                        {
                            try { batchto.UserFields.Fields.Item(field.Name).Value = field.Value; }
                            catch (Exception) { }
                        });

                    batchto.Add();
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(batchfrom);
                batchfrom = null;
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(batchto);
                batchto = null;
                GC.Collect();
            }
        }

        public static void CopyBinTo(this SAPbobsCOM.Document_Lines linefrom, SAPbobsCOM.Document_Lines lineto)
        {
            var binfrom = linefrom.BinAllocations;
            var binto = lineto.BinAllocations;

            try
            {
                Type bintype = binfrom.GetSAPType();

                for (int j = 0; j < linefrom.BinAllocations.Count; ++j)
                {
                    binfrom.SetCurrentLine(j);

                    if (binfrom.BinAbsEntry <= 0) continue;

                    bintype.GetProperties()
                        .Where(p => p.CanWrite && !p.PropertyType.IsCOMObject && p.Name != "BaseLineNumber")
                        .ToList()
                        .ForEach(prop =>
                        {
                            object value = prop.GetValue(binfrom);

                            if (value == null || String.IsNullOrEmpty(value.ToString())) return;

                            prop.SetValue(binto, value);
                        });

                    binto.Add();
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(binfrom);
                binfrom = null;
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(binto);
                binto = null;
                GC.Collect();
            }
        }

        public static void CopyExpensesTo(this SAPbobsCOM.Document_Lines linefrom, SAPbobsCOM.Document_Lines lineto)
        {
            var expensefrom = linefrom.Expenses;
            var expenseto = lineto.Expenses;

            try
            {
                Type exptype = expensefrom.GetSAPType();

                for (int i = 0; i < expensefrom.Count; ++i)
                {
                    expensefrom.SetCurrentLine(i);

                    if (expensefrom.ExpenseCode <= 0 || expensefrom.BaseGroup == -1) continue;

                    exptype.GetProperties()
                        .Where(p => p.CanWrite && !p.PropertyType.IsCOMObject)
                        .ToList()
                        .ForEach(prop =>
                        {
                            object value = prop.GetValue(expensefrom);

                            if (value == null || String.IsNullOrEmpty(value.ToString())) return;

                            prop.SetValue(expenseto, value);
                        });

                    expensefrom.UserFields.Fields
                        .OfType<SAPbobsCOM.Field>()
                        .Where(field => field.Value.ToString() != "")
                        .ToList()
                        .ForEach(field =>
                        {
                            try { expenseto.UserFields.Fields.Item(field.Name).Value = field.Value; }
                            catch (Exception) { }
                        });

                    expenseto.Add();
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(expensefrom);
                expensefrom = null;
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(expenseto);
                expenseto = null;
                GC.Collect();
            }
        }
    }
}

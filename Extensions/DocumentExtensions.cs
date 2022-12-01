using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FT_ADDON
{
    static class DocumentExtensions
    {
        const string refentry = "U_RENTRY";

        static readonly string[] properties =
        {
            "CardCode",
            "DocDate",
            "TaxDate",
            "DocDueDate",
            "DocType",
            "SalesPersonCode",
            //"DocumentsOwner",
            "NumAtCard",
            "DiscountPercent",
            "ContactPersonCode",
            "JournalMemo",
            "Project",
            "DocCurrency",
            "ShipFrom",
            "ShipToCode",
            "TrackingNumber",
            "TransportationCode",
            "GroupNumber",
            "PaymentMethod",
            "Indicator",
            "ImportFileNum",
            "Reference1",
            "Reference2",
            "CashDiscountDateOffset",
            //"AttachmentEntry",
            //"DownPaymentTrasactionID",
            //"TaxInvoiceNo",
            "FederalTaxID",
        };

        public static string GetTableName(this SAPbobsCOM.Documents oDoc)
        {
            var temp = oDoc.DocDate;
            oDoc.DocDate = DateTime.Today;
            System.Xml.XmlDocument xmlDocument = new System.Xml.XmlDocument();
            xmlDocument.LoadXml(oDoc.GetAsXML());
            oDoc.DocDate = temp;

            try
            {
                return xmlDocument.SelectNodes("BOM/BO").Item(0).ChildNodes.Item(1).Name;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static object GetUserFieldValue(this SAPbobsCOM.Documents oDoc, object field)
        {
            return oDoc.UserFields.Fields.Item(field).Value;
        }
        
        public static object GetUserFieldValue(this SAPbobsCOM.IDocuments oDoc, object field)
        {
            return oDoc.UserFields.Fields.Item(field).Value;
        }

        public static void SetUserFieldValue(this SAPbobsCOM.Documents oDoc, object field, object value)
        {
            oDoc.UserFields.Fields.Item(field).Value = value;
        }
        
        public static void SetUserFieldValue(this SAPbobsCOM.IDocuments oDoc, object field, object value)
        {
            oDoc.UserFields.Fields.Item(field).Value = value;
        }

        public static bool CopyTo(this SAPbobsCOM.Documents docfrom, SAPbobsCOM.Documents docto)
        {
            docfrom.CopyHeaderTo(docto);
            docfrom.CopyLinesTo(docto);
            return docto.Add() == 0;
        }

        public static void CopyHeaderTo(this SAPbobsCOM.Documents docfrom, SAPbobsCOM.Documents docto)
        {
            Type type = docto.GetSAPType();

            properties
                .Select(p => type.GetProperty(p))
                .Where(prop => prop != null && prop.CanWrite && !prop.PropertyType.IsCOMObject)
                .ToList()
                .ForEach(prop => prop.SetValue(docto, prop.GetValue(docfrom)));

            docto.Comments = $"Based On { docfrom.DocObjectCode.ToString().Substring(1).NaturalSpacing() } { docfrom.DocNum }";

            docfrom.UserFields.Fields
                .OfType<SAPbobsCOM.Field>()
                .Where(field => field.Value.ToString() != "")
                .ToList()
                .ForEach(field =>
                {
                    try { docto.UserFields.Fields.Item(field.Name).Value = field.Value; }
                    catch (Exception) { }
                });

            docto.SetUserFieldValue(refentry, 0);
        }
    }
}

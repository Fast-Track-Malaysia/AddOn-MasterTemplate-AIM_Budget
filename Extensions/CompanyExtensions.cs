using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    static class CompanyExtensions
    {
        public static SAPbobsCOM.Documents NewDocumentObject(this SAPbobsCOM.Company company, int doctype)
        {
            return (SAPbobsCOM.Documents)company.GetBusinessObject((SAPbobsCOM.BoObjectTypes)doctype);
        }

        public static SAPbobsCOM.Documents NewDocumentObject(this SAPbobsCOM.Company company, SAPbobsCOM.BoObjectTypes doctype)
        {
            return (SAPbobsCOM.Documents)company.GetBusinessObject(doctype);
        }
    }
}

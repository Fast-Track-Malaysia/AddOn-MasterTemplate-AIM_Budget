using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    abstract partial class Document_Base
    {
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
        protected class DocumentRowTypeAttribute : Attribute
        {
            public SAPbobsCOM.BoDocumentTypes rowType { get; set; }

            public DocumentRowTypeAttribute(SAPbobsCOM.BoDocumentTypes _rowType)
            {
                rowType = _rowType;
            }

            public static implicit operator SAPbobsCOM.BoDocumentTypes(DocumentRowTypeAttribute doctype) => doctype.rowType;
        }
    }
}

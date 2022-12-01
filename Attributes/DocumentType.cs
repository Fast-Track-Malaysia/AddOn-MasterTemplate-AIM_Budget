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
        protected class DocumentTypeAttribute : Attribute
        {
            public SAPbobsCOM.BoObjectTypes type { get; set; }

            public DocumentTypeAttribute(SAPbobsCOM.BoObjectTypes _type)
            {
                type = _type;
            }

            public static implicit operator SAPbobsCOM.BoObjectTypes(DocumentTypeAttribute doctype) => doctype.type;
        }
    }
}

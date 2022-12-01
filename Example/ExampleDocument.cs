using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace FT_ADDON.Example
{
    [DocumentType(SAPbobsCOM.BoObjectTypes.oPurchaseOrders)]
    [DocumentRowType(SAPbobsCOM.BoDocumentTypes.dDocument_Items)]
    class ExampleDocument : Document_Base
    {
    }
}

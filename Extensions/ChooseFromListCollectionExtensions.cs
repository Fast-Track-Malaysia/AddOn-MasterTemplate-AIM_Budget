using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    static class ChooseFromListCollectionExtensions
    {
        public static bool HasItem(this SAPbouiCOM.ChooseFromListCollection chooseFromListCollection, object key)
        {
            try
            {
                chooseFromListCollection.Item(key);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

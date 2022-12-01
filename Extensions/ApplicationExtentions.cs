using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    static class ApplicationExtentions
    {
        public static void ActivateMenu(this SAPbouiCOM.Application app, MenuItemId menuItemId)
        {
            app.ActivateMenuItem(((int)menuItemId).ToString());
        }
    }
}

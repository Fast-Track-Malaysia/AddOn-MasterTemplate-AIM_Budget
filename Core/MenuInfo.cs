using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    class MenuInfo
    {
        public string formcode;
        public string menuid;
        public string menuname;
        public int position;

        public MenuInfo(Form_Base formobj)
        {
            formcode = formobj.queryCode;
            menuid = formobj.menuId;
            menuname = formobj.menuName;
        }

        public MenuInfo()
        {
        }
    }
}

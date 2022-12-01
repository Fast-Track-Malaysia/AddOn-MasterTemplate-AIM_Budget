using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace FT_ADDON
{
    class MenuItem
    {
        List<MenuItem> children = new List<MenuItem>();
        string uniqueID = "";
        string name = "";
        string image = "";
        int position = -1;

        public MenuItem(string uid, string nm, string img, int pos)
        {
            uniqueID = uid;
            name = nm;
            image = img;
            position = pos;
        }

        public MenuItem(string uid, string nm, int pos)
        {
            uniqueID = uid;
            name = nm;
            position = pos;
        }

        public MenuItem(MenuInfo menuinfo)
        {
            uniqueID = menuinfo.formcode;
            name = menuinfo.menuname;
            position = menuinfo.position;
        }

        public void Create(object parent)
        {
            //// Main Menu
            SAPbouiCOM.MenuItem oMenuItem = SAP.SBOApplication.Menus.Item(parent);
            SAPbouiCOM.Menus oMenus = SAP.SBOApplication.Menus;
            SAPbouiCOM.MenuCreationParams oCreationPackage = ((SAPbouiCOM.MenuCreationParams)(SAP.SBOApplication.CreateObject(SAPbouiCOM.BoCreatableObjectType.cot_MenuCreationParams)));
            oMenus = oMenuItem.SubMenus;

            oCreationPackage.Type = children.Count > 0 ? SAPbouiCOM.BoMenuType.mt_POPUP : SAPbouiCOM.BoMenuType.mt_STRING;
            oCreationPackage.UniqueID = uniqueID;
            oCreationPackage.String = name;
            oCreationPackage.Position = position;

            string sPath = null;
            sPath = Application.StartupPath;
            sPath = sPath + "\\";
            oCreationPackage.Image = sPath + image;

            try
            {
                oMenus.AddEx(oCreationPackage);
            }
            catch
            { }

            oMenus = null;
            oMenuItem = null;
            oCreationPackage = null;
            GC.Collect();

            foreach (var menu in children)
            {
                menu.Create(uniqueID);
            }
        }

        public void addChildren(List<MenuItem> childs)
        {
            foreach (var child in childs)
            {
                children.Add(child);
            }
        }

        public void addChild(MenuItem menuItem)
        {
            children.Add(menuItem);
        }

        public void addChild(string uid, string nm, string img, int pos)
        {
            children.Add(new MenuItem(uid, nm, img, pos));
        }

        public void addChild(string uid, string nm, int pos)
        {
            children.Add(new MenuItem(uid, nm, pos));
        }

        public MenuItem child(int pos)
        {
            return children[pos];
        }
    }
}

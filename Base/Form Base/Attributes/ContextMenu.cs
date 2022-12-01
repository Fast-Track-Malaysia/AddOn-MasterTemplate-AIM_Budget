using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace FT_ADDON
{
    abstract partial class Form_Base
    {
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
        internal protected class ContextMenu : Attribute
        {
            SAPbouiCOM.BoFormMode[] form_modes { get; set; } = null;
            SAPbouiCOM.MenuCreationParams menu_params { get; set; }
            string item_id { get; set; }
            Func<bool> condition { get; set; } = null;

            const string contextmenu_id = "1280";

            public ContextMenu(string title, string menu_id, SAPbouiCOM.BoFormMode form_mode, string item_id = "", int position = 0)
            {
                this.item_id = item_id;
                form_modes = new SAPbouiCOM.BoFormMode[] { form_mode };

                menu_params = SAP.SBOApplication.CreateObject(SAPbouiCOM.BoCreatableObjectType.cot_MenuCreationParams) as SAPbouiCOM.MenuCreationParams;
                menu_params.String = title;
                menu_params.Type = SAPbouiCOM.BoMenuType.mt_STRING;
                menu_params.Position = position;
                menu_params.UniqueID = menu_id;
            }

            public ContextMenu(string title, string menu_id, SAPbouiCOM.BoFormMode[] form_modes = null, string item_id = "", int position = 0)
            {
                this.item_id = item_id;
                this.form_modes = form_modes;

                menu_params = SAP.SBOApplication.CreateObject(SAPbouiCOM.BoCreatableObjectType.cot_MenuCreationParams) as SAPbouiCOM.MenuCreationParams;
                menu_params.String = title;
                menu_params.Type = SAPbouiCOM.BoMenuType.mt_STRING;
                menu_params.Position = position;
                menu_params.UniqueID = menu_id;
            }
            
            public ContextMenu(string title, string menu_id, Func<bool> condition, string item_id = "", int position = 0)
            {
                this.item_id = item_id;
                this.condition = condition;

                menu_params = SAP.SBOApplication.CreateObject(SAPbouiCOM.BoCreatableObjectType.cot_MenuCreationParams) as SAPbouiCOM.MenuCreationParams;
                menu_params.String = title;
                menu_params.Type = SAPbouiCOM.BoMenuType.mt_STRING;
                menu_params.Position = position;
                menu_params.UniqueID = menu_id;
            }

            static private IEnumerable<ContextMenu> GetListFromForm(Form_Base form)
            {
                return form.GetType().GetCustomAttributes<ContextMenu>()
                                         .Where(cm => String.IsNullOrEmpty(cm.item_id) || cm.item_id == form.rcPVal.ItemUID)
                                         .Where(cm =>
                                         {
                                             if (cm.form_modes != null) return cm.form_modes.Length == 0 || cm.form_modes.Contains(form.oForm.Mode);

                                             return cm.condition == null || cm.condition();
                                         });
            }

            static public bool TryAddIn(Form_Base form)
            {
                var list = GetListFromForm(form).Reverse();

                if (!list.Any()) return false;

                var menus = SAP.SBOApplication.Menus.Item(contextmenu_id).SubMenus;

                try
                {
                    foreach (var cmenu in list)
                    {
                        try { menus.AddEx(cmenu.menu_params); }
                        catch (Exception) { }
                    }

                    return true;
                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(menus);
                    menus = null;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }

            static public bool TryRemoveFrom(Form_Base form)
            {
                var list = form.GetType().GetCustomAttributes<ContextMenu>();

                if (!list.Any()) return false;

                foreach (var cmenu in list)
                {
                    try { SAP.SBOApplication.Menus.RemoveEx(cmenu.menu_params.UniqueID); }
                    catch (Exception) { }
                }

                return true;
            }
        }
    }

    static class ContextMenuExtensions
    {
        public static bool HasContextMenu(this Type type)
        {
            return type.GetCustomAttributes<Form_Base.ContextMenu>().Any();
        }
    }
}

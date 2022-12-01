using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    class FormManager
    {
        public static Dictionary<string, Form_Base> FormList = new Dictionary<string, Form_Base>();
        public static Type[] list = InitializeFormTypes();

        private static Type[] InitializeFormTypes()
        {
            var formtypes = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                             from assemblyType in domainAssembly.GetTypes()
                             where typeof(Form_Base).IsAssignableFrom(assemblyType)
                             where !assemblyType.IsAbstract
                             select assemblyType).ToArray();

            var duplicates = formtypes.Where(formtype => formtypes.Where(type => type.GetFormCode() == formtype.GetFormCode()).Count() > 1);

            if (!duplicates.Any()) return formtypes;

            System.Windows.Forms.MessageBox.Show($"Duplicated form code { duplicates.First().GetFormCode() } detected");
            Environment.Exit(0);
            return null;
        }

        public static void RestoreExistingForms()
        {
            SAP.SBOApplication.Forms.OfType<SAPbouiCOM.Form>()
                                    .Where(form => list.IfAny(each => each.GetFormCode() == form.TypeEx))
                                    .ToList()
                                    .ForEach(form => AddForm(form));
        }

        /// <summary>
        /// removed unused registered form objects
        /// </summary>
        public static void ClearEmptyForms()
        {
            for (int i = 0; i < FormList.Keys.Count; i++)
            {
                string key = FormList.Keys.ElementAt(i);

                try
                {
                    SAP.SBOApplication.Forms.Item(key);
                }
                catch (Exception)
                {
                    FormList[key].FormRemovalEvent();
                    FormList.Remove(key);
                    --i;
                }
            }
        }

        public static string GetFormCode(Type formtype)
        {
            var formcode = formtype.GetFormCode();
            string code = formcode != null ? formcode : formtype.Namespace.Substring(formtype.BaseType.Namespace.Length + 1);
            return code.Split('.').Last();
        }

        /// <summary>
        /// return false when form type cannot be found
        /// </summary>
        /// <param name="formTypeEx">form type</param>
        /// <param name="formtype">null when false</param>
        /// <returns></returns>
        public static bool GetFormType(string formTypeEx, out Type formtype)
        {
            formtype = null;
            var formtypes = list.Where(type => GetFormCode(type) == formTypeEx);

            if (!formtypes.Any()) return false;

            formtype = formtypes.FirstOrDefault();
            return true;
        }

        public static bool GetForm(string formuid, out Form_Base formobj) => GetForm(SAP.SBOApplication.Forms.Item(formuid), out formobj);

        public static bool GetForm(SAPbouiCOM.Form form, out Form_Base formobj)
        {
            if (FormList.TryGetValue(form.UniqueID, out formobj)) return true;

            if (!GetFormType(form.TypeEx, out var formtype)) return false;

            if (formtype.GetNoForm() == null) return false;

            formobj = AddForm(form);
            return true;
        }

        public static Form_Base AddForm(SAPbouiCOM.Form form)
        {
            if (!GetFormType(form.TypeEx, out var formtype)) throw new Exception("Invalid form type");

            var formobj = AddForm(form.UniqueID, formtype);
            formobj.oForm = form;
            return formobj;
        }

        /// <summary>
        /// register form uid for its events to be processed + creating form object
        /// </summary>
        /// <param name="formuid">form uid</param>
        /// <param name="formtype">form object type</param>
        public static Form_Base AddForm(string formuid, Type formtype) => AddForm(formuid, NewForm(formtype));

        /// <summary>
        /// register form uid for its events to be processed with the given form object
        /// </summary>
        /// <param name="formuid">form uid</param>
        /// <param name="formobj">form object</param>
        public static Form_Base AddForm(string formuid, Form_Base formobj)
        {
            FormList.Add(formuid, formobj);
            return formobj;
        }

        /// <summary>
        /// Open new form from existing form object class
        /// </summary>
        /// <typeparam name="Form_Type"></typeparam>
        /// <returns></returns>
        public static Form_Type OpenNewForm<Form_Type>() where Form_Type : Form_Base => OpenNewForm(typeof(Form_Type)) as Form_Type;

        /// <summary>
        /// Open new form from existing form object class via form type
        /// </summary>
        /// <param name="formtype"></param>
        /// <returns></returns>
        public static Form_Base OpenNewForm(Type formtype)
        {
            Form_Base formobj = NewForm(formtype);

            if (formobj == null) return null;

            formobj.OpenForm();
            return formobj;
        }

        /// <summary>
        /// Create new form object without opening the form
        /// </summary>
        /// <typeparam name="Form_Type"></typeparam>
        /// <returns></returns>
        public static Form_Type NewForm<Form_Type>() where Form_Type : Form_Base => NewForm(typeof(Form_Type)) as Form_Type;

        /// <summary>
        /// Create new form object without opening the form via type
        /// </summary>
        /// <param name="formtype"></param>
        /// <returns></returns>
        public static Form_Base NewForm(Type formtype)
        {
            if (!list.Contains(formtype)) return null;

            return Activator.CreateInstance(formtype) as Form_Base;
        }
    }
}

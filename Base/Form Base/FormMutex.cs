using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    abstract partial class Form_Base
    {
        public class FormMutex
        {
            public static readonly string tablename = "###mutex";

            public string name { get; set; }
            public Form_Base formbase { get; set; }

            public bool IsMutexOwned()
            {
                var dt = formbase.oForm.DataSources.DataTables.Item(tablename);

                try
                {
                    SAP.StartTransaction();
                    string key = formbase.oForm.DataSources.DataTables.Item(tablename).GetValue(name, 0).ToString();

                    if (key.Length > 0) return true;

                    dt.SetValue(name, 0, "Y");
                    return false;
                }
                finally
                {
                    SAP.RollBack();
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(dt);
                    dt = null;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
        }

        protected void InitializeFormMutex()
        {
            SAP.StartTransaction();

            try
            {
                if (oForm.HasDataTable(FormMutex.tablename)) return;

                var dt = oForm.DataSources.DataTables.Add(FormMutex.tablename);

                try
                {
                    Type type = GetType();
                    type.GetProperties(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        .Where(prop => prop.PropertyType == typeof(FormMutex))
                        .ToList()
                        .ForEach(prop => {
                            prop.SetValue(this, new FormMutex
                            {
                                name = prop.Name,
                                formbase = this
                            });
                            dt.Columns.Add(prop.Name, SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
                        });

                    type.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        .Where(field => field.FieldType == typeof(FormMutex))
                        .ToList()
                        .ForEach(field => {
                            field.SetValue(this, new FormMutex
                            {
                                name = field.Name,
                                formbase = this
                            });
                            dt.Columns.Add(field.Name, SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
                        });

                    dt.Rows.Add();
                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(dt);
                    dt = null;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
            finally
            {
                SAP.RollBack();
            }
        }
    }
}

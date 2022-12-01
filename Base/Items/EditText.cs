using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    class EditText : Base.Items.Item<SAPbouiCOM.EditText>
    {
        public EditText(string name, SAPbouiCOM.Form form)
            : base(name, form, SAPbouiCOM.BoFormItemTypes.it_EDIT)
        {
        }

        public string Text
        {
            get => source.Value;
            set => source.Value = value;
        }
    }
}

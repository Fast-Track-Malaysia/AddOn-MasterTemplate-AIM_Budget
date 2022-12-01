using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    class Button : Base.Items.Item<SAPbouiCOM.Button>
    {
        public Button(string name, SAPbouiCOM.Form form)
            : base(name, form, SAPbouiCOM.BoFormItemTypes.it_BUTTON)
        {
        }

        public string Caption
        {
            get => source.Caption;
            set => source.Caption = value;
        }
    }
}

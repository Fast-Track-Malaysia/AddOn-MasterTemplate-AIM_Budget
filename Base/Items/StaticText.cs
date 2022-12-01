using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    class StaticText : Base.Items.Item<SAPbouiCOM.StaticText>
    {
        public StaticText(string name, SAPbouiCOM.Form form)
            : base(name, form, SAPbouiCOM.BoFormItemTypes.it_STATIC)
        {
        }

        public string Caption
        {
            get => source.Caption;
            set => source.Caption = value;
        }
    }
}

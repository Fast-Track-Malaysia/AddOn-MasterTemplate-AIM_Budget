using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    class ComboButton : Base.Items.Item<SAPbouiCOM.ButtonCombo>
    {
        public ComboButton(string name, SAPbouiCOM.Form form)
            : base(name, form, SAPbouiCOM.BoFormItemTypes.it_BUTTON_COMBO)
        {
        }

        public string Caption
        {
            get => source.Caption;
            set => source.Caption = value;
        }

        public string Value
        {
            get => source.Selected.Value;
            set => source.Select(value, SAPbouiCOM.BoSearchKey.psk_ByValue);
        }

        public void AddValidValue(string value, string description)
        {
            source.ValidValues.Add(value, description);
        }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FT_ADDON
{
    static class ValidValuesExtensions
    {
        public static void Clear(this SAPbouiCOM.ValidValues vvlist)
        {
            while (vvlist.Count > 0)
            {
                vvlist.Remove(0, SAPbouiCOM.BoSearchKey.psk_Index);
            }
        }

        public static void LoadFromXml(this SAPbouiCOM.ValidValues vvlist, string xml)
        {
            XDocument doc = XDocument.Parse(xml);
            string json = JsonConvert.SerializeXNode(doc);
            dynamic dyn = JsonConvert.DeserializeObject<ExpandoObject>(json.Replace("@", ""));
            vvlist.Clear();

            foreach (var item in dyn.ValidValues.ValidValue)
            {
                vvlist.Add(item.Value, item.Description);
            }
        }
    }
}

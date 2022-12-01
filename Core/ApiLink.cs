using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    class ApiLink
    {
        class Link
        {
            public string Name { get; set; }
            public string Code { get; set; }
            public string U_DIAPI { get; set; }
        }

        List<Link> listlink = new List<Link>();

        /// <summary>
        /// Linking DI API, Name &amp; Code using UDT
        /// </summary>
        /// <param name="table">without @</param>
        public ApiLink(string table)
        {
            using (RecordSet rc = new RecordSet())
            {
                try
                {
                    listlink = rc.Query<Link>($"SELECT * FROM \"@{ table.ToUpper() }\"").ToList();
                }
                catch (Exception)
                {
                    UserTable udt = new UserTable(table, "Item master data link");

                    if (!udt.createField("DIAPI", "DI API Property", mandatory: true)) throw new MessageException(SAP.SBOCompany.GetLastErrorDescription());
                }
            }
        }

        public string GetCodeByName(string name) => listlink.Where(link => link.Name == name).Select(link => link.Code).FirstOrDefault();

        public string GetCodeByProp(string propname) => listlink.Where(link => link.U_DIAPI == propname).Select(link => link.Code).FirstOrDefault();

        public string GetNameByCode(string code) => listlink.Where(link => link.Code == code).Select(link => link.Name).FirstOrDefault();

        public string GetNameByProp(string propname) => listlink.Where(link => link.U_DIAPI == propname).Select(link => link.Name).FirstOrDefault();

        public string GetPropByCode(string code) => listlink.Where(link => link.Code == code).Select(link => link.U_DIAPI).FirstOrDefault();

        public string GetPropByName(string name) => listlink.Where(link => link.Name == name).Select(link => link.U_DIAPI).FirstOrDefault();
    }
}

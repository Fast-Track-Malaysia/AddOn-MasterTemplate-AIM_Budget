using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    class FormattedSearch
    {
        private string _query;
        public string query { get { return _query; } }

        private int _queryCode;
        public int queryCode { get { return _queryCode; } }

        public bool success { get; set; }

        public FormattedSearch(string name, string script)
        {
            _query = script;
            success = Common.createQuery(name, script, out _queryCode);
        }

        public void AddFormattedSearch(string formatID, string colID = "-1")
        {
            Common.createFormattedSearch(formatID, queryCode, colID);
        }
    }
}

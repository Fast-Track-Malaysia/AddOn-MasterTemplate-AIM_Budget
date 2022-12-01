using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    class QueryInfo
    {
        private string _code;
        public string code { get => _code; }

        private string _query;
        public string query { get => _query; }

        public QueryInfo(string code, string query)
        {
            _code = code;

            if (code.Length == 0) throw new Exception("Query code must has at least 1 character");

            _query = query;
        }
    }
}

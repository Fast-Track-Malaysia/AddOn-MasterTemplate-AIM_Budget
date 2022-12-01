using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    class MessageException : Exception
    {
        public MessageException()
        {
        }

        public MessageException(string msg) : base(msg)
        {
        }
    }
}

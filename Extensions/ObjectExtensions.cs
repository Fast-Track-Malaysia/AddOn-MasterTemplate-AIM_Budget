using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    public static class ObjectExtensions
    {
        public static Type GetSAPType(this Object obj)
        {
            return Common.GetSAPCOMType(obj);
        }
    }
}

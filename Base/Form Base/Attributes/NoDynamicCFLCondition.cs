using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    abstract partial class Form_Base
    {
        [AttributeUsage(AttributeTargets.Class)]
        internal protected class NoDynamicCFLCondition : Attribute
        {
        }
    }
}

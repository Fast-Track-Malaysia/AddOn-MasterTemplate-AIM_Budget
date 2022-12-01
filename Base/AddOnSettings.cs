using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    abstract class AddOnSettings
    {
        public bool success { get; set; }

        public AddOnSettings()
        {
            success = Setup();
        }

        public abstract bool Setup();
    }
}

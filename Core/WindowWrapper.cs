using System;

namespace FT_ADDON
{
    /*
     * Class Declared for FileBrowser
     * 
     * */

    public class WindowWrapper: System.Windows.Forms.IWin32Window
    {
        private IntPtr _hwnd;
 
        // Property
        public virtual IntPtr Handle
        {
            get { return _hwnd; }
        }
 
	// Constructor
        public WindowWrapper(IntPtr handle)
        {
            _hwnd = handle;
        }
    }
}


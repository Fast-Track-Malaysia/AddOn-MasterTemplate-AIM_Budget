using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;

namespace FT_ADDON
{
    /*
     * Class created to display OpenFileDialog
     * 
     * */

    class FileBrowser : STASafe
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        OpenFileDialog _oFileDialog;

        // Properties 
        public string FileName
        {
            get => _oFileDialog.FileName;
            set => _oFileDialog.FileName = value;
        }

        public string Filter
        {
            get => _oFileDialog.Filter;
            set => _oFileDialog.Filter = value;
        }

        public string InitialDirectory
        {
            get => _oFileDialog.InitialDirectory;
            set => _oFileDialog.InitialDirectory = value;
        }

        public string Title
        {
            get => _oFileDialog.Title;
            set => _oFileDialog.Title = value;
        }

        public bool MultiSelect
        {
            get => _oFileDialog.Multiselect;
            set =>_oFileDialog.Multiselect = value;
        }

        public FileBrowser()
        {
            _oFileDialog = new OpenFileDialog();

            AddSafeAction(() =>
            {
                IntPtr ptr = GetForegroundWindow();
                WindowWrapper oWindow = new WindowWrapper(ptr);

                if (_oFileDialog.ShowDialog(oWindow) != DialogResult.OK)
                {
                    _oFileDialog.FileName = string.Empty;
                }

                oWindow = null;
            });
        }

        public void Browse() => ExecuteSafeAction();
    }
}

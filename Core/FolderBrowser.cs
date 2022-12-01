using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FT_ADDON
{
    class FolderBrowser : STASafe
    {
        CommonOpenFileDialog _oFileDialog;

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
            set => _oFileDialog.Multiselect = value;
        }

        public bool AllowNonFileSystemItems
        {
            get => _oFileDialog.AllowNonFileSystemItems;
            set => _oFileDialog.AllowNonFileSystemItems = value;
        }

        public string FolderName { get; set; }

        CommonFileDialogResult rsp = CommonFileDialogResult.None;

        public FolderBrowser()
        {
            _oFileDialog = new CommonOpenFileDialog();

            AddSafeAction(() =>
            {
                rsp = _oFileDialog.ShowDialog();
            });
        }

        public void Browse()
        {
            _oFileDialog.IsFolderPicker = true;
            rsp = CommonFileDialogResult.None;

            ExecuteSafeAction();

            if (rsp != CommonFileDialogResult.Ok) return;

            FolderName = MultiSelect && _oFileDialog.FileNames.Count() > 1 ? string.Join("|", _oFileDialog.FileNames) : _oFileDialog.FileName;
        }
    }
}

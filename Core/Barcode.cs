using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FT_ADDON
{
    public class Barcode
    {
        string barcode { get; set; }
        int quantity { get; set; }

        public Barcode(string code)
        {
            barcode = code;
        }

        public void Print(string printer_name, string commands, int quantity)
        {
            this.quantity = quantity;
            Printer.Print(printer_name, commands, this);
        }
    }
}

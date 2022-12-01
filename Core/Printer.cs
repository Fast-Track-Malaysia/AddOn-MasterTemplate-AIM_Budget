using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace FT_ADDON
{
    public static class Printer
    {
#if AMD64
        [DllImport("Lib/TSCLIB_x64.dll", EntryPoint = "about")]
#else
        [DllImport("Lib/TSCLIB_x86.dll", EntryPoint = "about")]
#endif
        public static extern int about();

#if AMD64
        [DllImport("Lib/TSCLIB_x64.dll", EntryPoint = "openport")]
#else
        [DllImport("Lib/TSCLIB_x86.dll", EntryPoint = "openport")]
#endif
        public static extern int openport(string printername);

#if AMD64
        [DllImport("Lib/TSCLIB_x64.dll", EntryPoint = "barcode")]
#else
        [DllImport("Lib/TSCLIB_x86.dll", EntryPoint = "barcode")]
#endif
        public static extern int barcode(string x, string y, string type,
                    string height, string readable, string rotation,
                    string narrow, string wide, string code);

#if AMD64
        [DllImport("Lib/TSCLIB_x64.dll", EntryPoint = "clearbuffer")]
#else
        [DllImport("Lib/TSCLIB_x86.dll", EntryPoint = "clearbuffer")]
#endif
        public static extern int clearbuffer();

#if AMD64
        [DllImport("Lib/TSCLIB_x64.dll", EntryPoint = "closeport")]
#else
        [DllImport("Lib/TSCLIB_x86.dll", EntryPoint = "closeport")]
#endif
        public static extern int closeport();

#if AMD64
        [DllImport("Lib/TSCLIB_x64.dll", EntryPoint = "downloadpcx")]
#else
        [DllImport("Lib/TSCLIB_x86.dll", EntryPoint = "downloadpcx")]
#endif
        public static extern int downloadpcx(string filename, string image_name);

#if AMD64
        [DllImport("Lib/TSCLIB_x64.dll", EntryPoint = "formfeed")]
#else
        [DllImport("Lib/TSCLIB_x86.dll", EntryPoint = "formfeed")]
#endif
        public static extern int formfeed();

#if AMD64
        [DllImport("Lib/TSCLIB_x64.dll", EntryPoint = "nobackfeed")]
#else
        [DllImport("Lib/TSCLIB_x86.dll", EntryPoint = "nobackfeed")]
#endif
        public static extern int nobackfeed();

#if AMD64
        [DllImport("Lib/TSCLIB_x64.dll", EntryPoint = "printerfont")]
#else
        [DllImport("Lib/TSCLIB_x86.dll", EntryPoint = "printerfont")]
#endif
        public static extern int printerfont(string x, string y, string fonttype,
                        string rotation, string xmul, string ymul,
                        string text);

#if AMD64
        [DllImport("Lib/TSCLIB_x64.dll", EntryPoint = "printlabel")]
#else
        [DllImport("Lib/TSCLIB_x86.dll", EntryPoint = "printlabel")]
#endif
        public static extern int printlabel(string set, string copy);

#if AMD64
        [DllImport("Lib/TSCLIB_x64.dll", EntryPoint = "sendcommand")]
#else
        [DllImport("Lib/TSCLIB_x86.dll", EntryPoint = "sendcommand")]
#endif
        public static extern int sendcommand(string printercommand);

#if AMD64
        [DllImport("Lib/TSCLIB_x64.dll", EntryPoint = "setup")]
#else
        [DllImport("Lib/TSCLIB_x86.dll", EntryPoint = "setup")]
#endif
        public static extern int setup(string width, string height,
                  string speed, string density,
                  string sensor, string vertical,
                  string offset);

#if AMD64
        [DllImport("Lib/TSCLIB_x64.dll", EntryPoint = "windowsfont")]
#else
        [DllImport("Lib/TSCLIB_x86.dll", EntryPoint = "windowsfont")]
#endif
        public static extern int windowsfont(int x, int y, int fontheight,
                        int rotation, int fontstyle, int fontunderline,
                        string szFaceName, string content);

        public static void Print(string printer_name, string commands)
        {
            //string filePath = $"{ Application.StartupPath }\\Core\\TSPPL instructions.txt";
            //string[] lines = System.IO.File.ReadAllLines(filePath);

            string[] lines = commands.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            string setupWidth = "", setupHeight = "", setupSpeed = "", setupDensity = "", setupSensor = "", setupGap = "", setupShift = "0";
            StringBuilder builder = new StringBuilder();

            foreach (string line in lines)
            {
                string l = line.Trim();

                if (l.Length == 0) continue;

                if (l.StartsWith("--")) continue;

                if (!l.StartsWith("SETUP,"))
                {
                    builder.AppendLine(l);
                    continue;
                }

                string[] ss = l.Split(',');
                setupWidth = ss[1];
                setupHeight = ss[2];
                setupSpeed = ss[3];
                setupDensity = ss[4];
                setupSensor = ss[5];
                setupGap = ss[6];
                setupShift = ss[7];
            }

            Printer.openport(printer_name);
            Printer.setup(setupWidth, setupHeight, setupSpeed, setupDensity, setupSensor, setupGap, setupShift);
            Printer.clearbuffer();
            Printer.sendcommand(commands);
            Printer.closeport();
        }

        static string datetime_format = "dd/MM/yyyy";

        public static void Print(string printer_name, string commands, object data)
        {
            commands = CommandParam(commands, data);
            Print(printer_name, commands);
        }

        private static string CommandParam(string commands, object data)
        {
            Type type = data.GetSAPType();

            foreach (var prop in type.GetProperties())
            {
                if (prop.PropertyType == typeof(DateTime))
                {
                    commands = commands.Replace($"{{{ prop.Name }}}", ((DateTime)prop.GetValue(data)).ToString(datetime_format));
                    continue;
                }

                commands = commands.Replace($"{{{ prop.Name }}}", prop.GetValue(data).ToString());
            }

            return commands;
        }

        public static void DateTimeFormat(string format)
        {
            datetime_format = format;
        }
    }
}

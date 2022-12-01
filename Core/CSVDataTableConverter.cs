using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    class CSVDataTableConverter
    {
        public string delimiter { get; set; } = null;
        public string path { get; set; } = null;

        protected bool HasQuote(string strFilePath)
        {
            using (StreamReader sr = new StreamReader(strFilePath))
            {
                string stringline = sr.ReadLine();
                string check = LineFilter(stringline)[0].ToString().Trim();
                return stringline[0] == '\"';
            }
        }

        protected string[] LineFilter(string l)
        {
            if (String.IsNullOrEmpty(l)) return new string[1] { "" };

            string sep = delimiter;
            string line = l.Replace($"\"{ sep }", sep).Replace(sep + "\"", sep);

            if (line[0] == '\"')
            {
                line = line.Substring(1);
            }

            if (line[line.Length - 1] == '\"')
            {
                line = line.Substring(0, line.Length - 1);
            }

            return line.Split(System.Convert.ToChar(delimiter));
        }

        public DataTable Convert()
        {
            DataTable dt = new DataTable();

            using (TextFieldParser textParse = new TextFieldParser(path))
            {
                textParse.HasFieldsEnclosedInQuotes = HasQuote(path);
                textParse.SetDelimiters(delimiter);

                if (textParse.EndOfData) return dt;

                var fields = textParse.ReadFields();
                int column_num = fields.Length;

                for (int i = 0; i < column_num; ++i)
                {
                    dt.Columns.Add(fields[i]);
                }

                while (!textParse.EndOfData)
                {
                    fields = textParse.ReadFields();
                    DataRow dr = dt.NewRow();

                    for (int i = 0; i < column_num; ++i)
                    {
                        dr[i] = fields[i];
                    }

                    dt.Rows.Add(dr);
                }
            }

            return dt;
        }

        public DataTable Convert(string csv_path)
        {
            path = csv_path;
            return Convert();
        }

        public static DataTable GetDataTable(string csv_path, string delimiter)
        {
            CSVDataTableConverter csv_dt = new CSVDataTableConverter();
            csv_dt.delimiter = delimiter;
            return csv_dt.Convert(csv_path);
        }
    }
}

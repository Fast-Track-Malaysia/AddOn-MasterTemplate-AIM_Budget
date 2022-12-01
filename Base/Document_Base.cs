using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using System.Data.SqlClient;
using System.Threading;
using System.Numerics;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;

namespace FT_ADDON
{
    abstract partial class Document_Base
    {
        public static Type[] list = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                                     from assemblyType in domainAssembly.GetTypes()
                                     where typeof(Document_Base).IsAssignableFrom(assemblyType)
                                     where !assemblyType.IsAbstract
                                     select assemblyType).ToArray();

        #region NON-HEADERS
        protected SAPbobsCOM.BoObjectTypes DocType
        {
            get
            {
                var doctype = this.GetType().GetCustomAttribute<DocumentTypeAttribute>();

                if (doctype == null) throw new Exception("Invalid doc type");

                return doctype.type;
            }
        }

        protected SAPbobsCOM.BoDocumentTypes RowType
        {
            get
            {
                var rowtype = this.GetType().GetCustomAttribute<DocumentRowTypeAttribute>();

                if (rowtype == null) return SAPbobsCOM.BoDocumentTypes.dDocument_Items;

                return rowtype.rowType;
            }
        }

        protected virtual string DocumentNo
        {
            get
            {
                return row[NumAtCardColumn].ToString();
            }
        }
        #endregion

        #region BASE FUNCTIONS
        protected Document_Base()
        {
            oDoc = (SAPbobsCOM.Documents)SAP.SBOCompany.GetBusinessObject(DocType);
            HeaderEvents = new Dictionary<string, Action<string>>();
            RowEvents = new Dictionary<string, Action<string>>();
        }

        protected string[] lineFilter(string l)
        {
            string sep = Delimiter;
            string line = l.Replace($"\"{ sep }", sep).Replace(sep + "\"", sep);

            if (line[0] == '\"') line = line.Substring(1);
            if (line[line.Length - 1] == '\"') line = line.Substring(0, line.Length - 1);

            return line.Split(Convert.ToChar(Delimiter));
        }
        #endregion

        #region DO NOT MODIFIY
        protected int TotalLine = 0;
        public int COUNTER = 0;
        protected DataRow row;
        protected int[] validColumns = null;
        protected int[] validHeaders = null;

        protected virtual string Delimiter { get; set; }

        protected virtual string DocumentName { get { return this.GetType().Name.ToString().Replace(MethodBase.GetCurrentMethod().DeclaringType.Name, ""); } }
        #endregion

        const string numcard = "NumAtCard";
        const string cardcode = "CardCode";
        const string lineprop = "Lines.";

        protected SAPbobsCOM.Documents oDoc;
        protected string NumAtCardColumn = "DummyNumAtCard";
        protected string BPColumn = "DummyCardCode";
        protected SAPbobsCOM.Document_Lines oDocLine { get { return oDoc.Lines; } }
        protected Dictionary<string, Action<string>> HeaderEvents { get; set; }
        protected Dictionary<string, Action<string>> RowEvents { get; set; }

        protected virtual DataTable Initialize()
        {
            SetDelimiter();
            return RegisterEvents();
        }

        protected virtual DataTable RegisterEvents()
        {
            var result = GetHeaders().Fields.OfType<SAPbobsCOM.Field>().ToArray();
            HeaderEvents.Clear();
            RowEvents.Clear();
            DataTable dt = new DataTable();

            foreach (var field in result)
            {
                string colstr = field.Value.ToString();
                string expression = field.Name.ToString();

                if (colstr.StartsWith("[") && colstr.EndsWith("]"))
                {
                    colstr = colstr.Substring(1, colstr.Length - 2);

                    if (dt.Columns.Contains(colstr)) throw new Exception($"New duplicated header(s) detected. Header: { colstr }");

                    dt.Columns.Add(colstr);
                }

                AddEvent(expression, colstr);
            }

            if (!dt.Columns.Contains(NumAtCardColumn)) dt.Columns.Add(NumAtCardColumn);

            return dt;
        }

        void AddEvent(string expression, string colstr)
        {
            Tuple<object, PropertyInfo> pair;

            if (expression.StartsWith(lineprop))
            {
                expression = expression.Substring(lineprop.Length);
                pair = GetObjectProperty(oDocLine, expression);

                if (pair == null || pair.Item1 == null || pair.Item2 == null) return;

                if (pair.Item2.PropertyType.IsEnum)
                {
                    RowEvents.Add(colstr, (string data) =>
                    {
                        pair.Item2.SetValue(pair.Item1, Enum.Parse(pair.Item2.PropertyType, data));
                    });

                    return;
                }
                
                RowEvents.Add(colstr, (string data) =>
                {
                    pair.Item2.SetValue(pair.Item1, Convert.ChangeType(data, pair.Item2.PropertyType));
                });

                return;
            }

            if (numcard == expression)
            {
                NumAtCardColumn = colstr; 
            }

            if (cardcode == expression)
            {
                BPColumn = colstr;
            }

            pair = GetObjectProperty(oDoc, expression);

            if (pair == null || pair.Item1 == null || pair.Item2 == null) return;

            if (pair.Item2.PropertyType.IsEnum)
            {
                HeaderEvents.Add(colstr, (string data) =>
                {
                    pair.Item2.SetValue(pair.Item1, Enum.Parse(pair.Item2.PropertyType, colstr));
                });

                return;
            }

            HeaderEvents.Add(colstr, (string data) =>
            {
                pair.Item2.SetValue(pair.Item1, Convert.ChangeType(colstr, pair.Item2.PropertyType));
            });
        }

        protected Tuple<object, PropertyInfo> GetObjectProperty(object obj, string expression)
        {
            if (expression.StartsWith("U_"))
            {
                expression = expression.Substring(2);
                obj = GoToObject(obj, "UserFields");
                obj = GoToObject(obj, "Fields");
                string propstr = expression.Substring(expression.IndexOf('.'));
                obj = GetMethodReturn(obj, "Item", propstr);
                expression = expression.Substring(propstr.Length + 1);
            }

            return new Tuple<object, PropertyInfo>(obj, Common.GetSAPCOMType(obj).GetProperty(expression));
        }

        protected object GoToObject(object obj, string property)
        {
            Type type = Common.GetSAPCOMType(obj);
            var prop = type.GetProperty(property);
            return prop.GetValue(obj);
        }

        protected object GetMethodReturn(object obj, string method, params object[] args)
        {
            Type type = Common.GetSAPCOMType(obj);
            var meth = type.GetMethod(method);
            return meth.Invoke(obj, args);
        }

        protected SAPbobsCOM.Recordset GetHeaders()
        {
            ApplicationCommon.FillFromSAPSQL(this.GetType(), out var output);
            return output;
        }

        protected void SetDelimiter()
        {
            ApplicationCommon.FillFromSAPSQL(this.GetType(), out var output);
            Delimiter = output.Fields.Item(0).Value.ToString();
        }

        protected bool HasQuote(string strFilePath)
        {
            using (StreamReader sr = new StreamReader(strFilePath))
            {
                string stringline = sr.ReadLine();
                string check = lineFilter(stringline)[0].ToString().Trim();
                return stringline[0] == '\"';
            }
        }

        protected virtual DataTable ConvertCSVtoDataTable(string strFilePath)
        {
            DataTable dt = Initialize();
            TextFieldParser textParse = new TextFieldParser(strFilePath);
            textParse.HasFieldsEnclosedInQuotes = HasQuote(strFilePath);
            textParse.SetDelimiters(Delimiter);
            List<Tuple<int, int>> validfields = new List<Tuple<int, int>>();
            var fields = textParse.ReadFields();

            for (int i = 0; i < fields.Length; ++i)
            {
                if (dt.Columns.Contains(fields[i]))
                {
                    validfields.Add(new Tuple<int, int>(dt.Columns.IndexOf(fields[i]), i));
                }
            }

            while (!textParse.EndOfData)
            {
                fields = textParse.ReadFields();
                DataRow dr = dt.NewRow();
                dr[NumAtCardColumn] = "";

                foreach (var no in validfields)
                {
                    dr[no.Item1] = fields[no.Item2];
                }

                dt.Rows.Add(dr);
            }

            textParse.Close();
            return dt;
        }

        public virtual void GABAdd(string path, ref List<ActionResult> actionResults)
        {
            try
            {
                SAP.SBOApplication.StatusBar.SetText("Importing Document...", SAPbouiCOM.BoMessageTime.bmt_Medium, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
                ProgressBarHandler.Stop();
                ProgressBarHandler.CurStep = 0;
                DataTable dt = ConvertCSVtoDataTable(path);

                var result2 = from crow in dt.AsEnumerable()
                              group crow by new { BPCode = crow.Field<string>(BPColumn), InvNo = crow.Field<string>(NumAtCardColumn) } into grp
                              select new
                              {
                                  InvNo1 = grp.Key.InvNo,
                                  BPNo1 = grp.Key.BPCode
                              };

                foreach (var t in result2)
                {
                    DataRow[] data = dt.Select($"[{ BPColumn }] = '{ t.BPNo1 }' AND [{ NumAtCardColumn }] = '{ t.InvNo1 }'");

                    if (data.Length == 0) continue;

                    SAP.SBOApplication.RemoveWindowsMessage(SAPbouiCOM.BoWindowsMessageType.bo_WM_TIMER, true);
                    TotalLine = result2.Count();

                    if (COUNTER == 0)
                    {
                        COUNTER = 0;
                        ProgressBarHandler.Stop();
                        ProgressBarHandler.Start("Importing Document...", 0, 100, true);
                        COUNTER++;
                    }

                    AddDocument(data, ref actionResults);
                }
            }
            finally
            {
                ProgressBarHandler.Stop();
                SAP.SBOApplication.StatusBar.SetText("Generating Import Result....", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
                ProgressBarHandler.CurStep = 0;
            }
        }

        protected virtual List<ActionResult> AddDocument(DataRow[] data, ref List<ActionResult> actionResults)
        {
            int Steps = 0;
            int FlagCode = 0;
            string errMsg = "";

            oDoc = (SAPbobsCOM.Documents)SAP.SBOCompany.GetBusinessObject(DocType);
            SAPbobsCOM.Recordset rc = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            string tablename = oDoc.GetTableName();

            try
            {
                Dictionary<string, List<DataRow>> listRow = new Dictionary<string, List<DataRow>>();
                Steps = 100 / TotalLine;

                if (SAP.SBOCompany.InTransaction) SAP.SBOCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);

                SAP.SBOCompany.StartTransaction();

                row = data[0];
                string numcard = row[NumAtCardColumn].ToString();

                if (numcard.Length > 0)
                {
                    rc.DoQuery($"SELECT * FROM \"{ tablename }\" WHERE \"NumAtCard\"='{ numcard }'");

                    if (rc.RecordCount > 0)
                    {
                        FlagCode = 2;
                        throw new MessageException($"- Order No. Found. Document No: { NumAtCardColumn }");
                    }
                }

                POHeader(oDoc);

                foreach (var poRow in data)
                {
                    row = poRow;
                    POLines(oDoc);
                }

                while (oDoc.Add() != 0)
                {
                    SAP.SBOCompany.GetLastError(out int errCode, out var msg);

                    if (!msg.Contains("2038")) throw new MessageException(msg);

                    Thread.Sleep(500);
                    ProgressBarHandler.Stop();
                }

                if (SAP.SBOCompany.InTransaction) SAP.SBOCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);
                string key = SAP.SBOCompany.GetNewObjectKey();

                ProgressBarHandler.Increment("Importing Document...", Steps);
                rc.DoQuery($"SELECT * FROM \"{ tablename }\" WHERE \"DocEntry\" = { int.Parse(key) }");
                string Docnumber = rc.Fields.Item("DocNum").Value.ToString();
                actionResults.Add(new ActionResult("Success", DocumentNo, $"SO Document Number:{ Docnumber }"));
            }
            catch (MessageException ex)
            {
                ProgressBarHandler.Stop();
                errMsg = ex.Message;

            }
            catch (Exception ex)
            {
                ProgressBarHandler.Stop();
                errMsg = $"Exception caught : { ex.Message }";
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oDoc);
                oDoc = null;
                GC.Collect();
            }

            if (FlagCode == 2)
            {
                ProgressBarHandler.Increment("Importing Document...", Steps);
                string Docnumber2 = rc.Fields.Item("DocNum").Value.ToString();
                actionResults.Add(new ActionResult("Duplicate", DocumentNo, errMsg + Docnumber2));
            }
            else
            {
                ProgressBarHandler.Increment("Importing Document...", Steps);
                actionResults.Add(new ActionResult("Error", DocumentNo, errMsg));
            }

            return actionResults;
        }

        protected virtual void POHeader(SAPbobsCOM.Documents oDoc)
        {
            if (validHeaders == null)
            {
                validHeaders = row.Table.Columns.OfType<DataColumn>()
                    .Where(c => HeaderEvents.ContainsKey(c.ColumnName))
                    .Select(c => row.Table.Columns.IndexOf(c.ColumnName))
                    .ToArray();
            }

            SAPbobsCOM.Recordset rc = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            string query = $"SELECT \"Series\", \"NextNumber\" FROM \"NNM1\" WHERE \"ObjectCode\" = '{ ((int)DocType).ToString() }' AND \"Locked\" = 'N'";
            rc.DoQuery(query);
            var dic = rc.Fields.OfType<SAPbobsCOM.Field>().ToDictionary(f => f.Name, f => f.Value);
            oDoc.Series = int.Parse(dic["Series"].ToString());
            oDoc.DocNum = int.Parse(dic["NextNumber"].ToString());
            oDoc.DocType = RowType;
            
            foreach (var col in validHeaders)
            {
                HeaderEvents[row.Table.Columns[col].ColumnName](row[col].ToString());
            }
        }

        protected virtual void POLines(SAPbobsCOM.Documents oDoc)
        {
            if (validColumns == null)
            {
                validColumns = row.Table.Columns.OfType<DataColumn>()
                    .Where(c => RowEvents.ContainsKey(c.ColumnName))
                    .Select(c => row.Table.Columns.IndexOf(c.ColumnName))
                    .ToArray();
            }

            if (validColumns.Length == 0) return;

            foreach (var col in validColumns)
            {
                RowEvents[row.Table.Columns[col].ColumnName](row[col].ToString());
            }

            oDoc.Lines.Add();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FT_ADDON
{
    class SQLQuery : AddOnSettings
    {
        public const string TableName = "@SQLQUERY";
        const string RegisterTableName = "SQLQUERY";

        public override bool Setup()
        {
            UserTable udt = new UserTable(RegisterTableName, "Query Table");

            if (!udt.createField("Query", "Query", SAPbobsCOM.BoFieldTypes.db_Memo, 254, "", false, SAPbobsCOM.BoFldSubTypes.st_None)) return false;

            return true;
        }

        public static string QueryCode(SAPbouiCOM.Form oForm, int row = 0, params object[] args)
        {
            string code = $"{ oForm.TypeEx }.{ ApplicationCommon.GetMethodName(2) }";
            return QueryCode(oForm, code, row, args);
        }

        public static string QueryCode(QueryInfo info)
        {
            SAPbobsCOM.Recordset rc = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            try
            {
                rc.DoQuery($"SELECT * FROM \"{ TableName }\" where \"Code\"='{ info.code }'");

                if (rc.RecordCount != 0) return rc.Fields.Item("U_Query").Value.ToString();

                if (info.query.Length != 0) return info.query;

                Clipboard.SetText(info.code);
                throw new MessageException($"Query [{ info.code }] not found in [{ TableName }]. Please add the required query script with the respective code - { TableName }/{ info.code }");
            }
            finally
            {
                Marshal.FinalReleaseComObject(rc);
                rc = null;
                GC.Collect();
            }
        }

        public static string QueryCode(string code)
        {
            SAPbobsCOM.Recordset rc = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            try
            {
                rc.DoQuery($"SELECT * FROM \"{ TableName }\" where \"Code\"='{ code }'");

                if (rc.RecordCount != 0)
                {
                    object query = rc.Fields.Item("U_Query").Value;

                    if (query != null && query.ToString() != String.Empty) return query.ToString();
                }

                Clipboard.SetText(code);
                throw new MessageException($"Query [{ code }] not found in [{ TableName }]. Please add the required query script with the respective code - { TableName }/{ code }");
            }
            finally
            {
                Marshal.FinalReleaseComObject(rc);
                rc = null;
                GC.Collect();
            }
        }

        public static string QueryCode(SAPbouiCOM.Form oForm, QueryInfo info, int row = 0, params object[] args)
        {
            string query = QueryCode(info);
            return ReplaceParams(oForm, query, row, args);
        }

        public static string QueryCode(SAPbouiCOM.Form oForm, string code, int row = 0, params object[] args)
        {
            string query = QueryCode(code);
            return ReplaceParams(oForm, query, row, args);
        }

        public static string QueryCode(DataTable dt, QueryInfo info, params object[] args)
        {
            string query = QueryCode(info);
            return ReplaceParams(query, dt, args);
        }

        public static string QueryCode(DataTable dt, string code, params object[] args)
        {
            string query = QueryCode(code);
            return ReplaceParams(query, dt, args);
        }

        public static string QueryCode(QueryInfo info, params object[] args)
        {
            string query = QueryCode(info);
            return ReplaceParams(query, args);
        }

        public static string QueryCode(string code, params object[] args)
        {
            string query = QueryCode(code);
            return ReplaceParams(query, args);
        }

        private static bool ChangeTypeToSql(object value, Type type, out string data)
        {
            if (value == null)
            {
                data = "NULL";
                return true;
            }

            var t = type;

            if (type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                t = Nullable.GetUnderlyingType(t);
            }

            if (t == typeof(DateTime))
            {
                string date = Convert.ToDateTime(value).ToString("yyyyMMdd");

                if (date == "00010101")
                {
                    data = date;
                    return false;
                }

                data = $"'{ date }'";
            }
            else if (t == typeof(string))
            {
                data = $"'{ value }'";
            }
            else if (t == typeof(byte[]))
            {
                data = Encoding.Default.GetString(value as byte[]);
            }
            else
            {
                data = value.ToString();
            }

            return true;
        }

        public static string ReplaceParams(string query, DataTable dt, params object[] args)
        {
            query = ReplaceParams(query, dt);
            return ReplaceParams(query, args);
        }

        public static string ReplaceParams(string query, DataTable dt)
        {
            if (dt == null) return query;

            for (int i = 0; i < dt.Columns.Count; ++i)
            {
                if (!ChangeTypeToSql(dt.Rows[0][i], dt.Columns[i].DataType, out string data)) continue;

                query = Regex.Replace(query, $"\\$\\[{ dt.Columns[i].ColumnName }\\]", data, RegexOptions.IgnoreCase);
            }

            return query;
        }

        public static string ReplaceParams(string query, params object[] args)
        {
            if (args.Length > 0)
            {
                args = args.Select(arg => arg.GetType() == typeof(string) ? arg.ToString().Replace("'", "''") : arg).ToArray();
                return String.Format(query, args);
            }

            return query;
        }

        public static string ReplaceParams(SAPbouiCOM.Form oForm, string syntax, int row)
        {
            string rtn = "";
            int start = -1;
            int end = -1;
            string value;

            for (int i = 0; i < syntax.Length; i++)
            {
                if (syntax[i] != '$') continue;

                if (syntax[i + 1] != '[') continue;

                start = i;

                for (int j = start; j < syntax.Length; j++)
                {
                    if (syntax[j] != ']') continue;

                    end = j;
                    break;
                }

                break;
            }

            if (start >= end) return syntax;

            string param = syntax.Substring(start, end - start + 1);
            string[] paramarr = param.Split('.');

            if (paramarr.Length == 2)
            {
                string param1 = paramarr[0].Substring(2, paramarr[0].Length - 2);
                string param2 = paramarr[1].Substring(0, paramarr[1].Length - 1);
                value = QueryParameter.GetValueFromParameters(oForm, param1, param2, row);

                value = value.Replace('*', '%');
                rtn = syntax.Replace(param, value);
                return ReplaceParams(oForm, rtn, row);
            }

            if (paramarr.Length == 1 && param == "$[FormId]")
            {
                return oForm.TypeEx;
            }

            value = param.Substring(2, param.Length - 3);

            if (paramarr.Length == 1)
            {
                value = QueryParameter.GetValueFromParameters(oForm, "", value, row);
                rtn = syntax.Replace(param, value);
            }
            else
            {
                rtn = syntax.Replace(param, $"'{ value.Trim() }'");
            }

            return ReplaceParams(oForm, rtn, row);
        }

        public static string ReplaceParams(SAPbouiCOM.Form oForm, string syntax, int row, object[] args)
        {
            if (oForm == null) return syntax;

            string query = ReplaceParams(oForm, syntax, row);
            return ReplaceParams(query, args);
        }

        public static string GetTableName(SAPbobsCOM.BoObjectTypes objecttype)
        {
            SAPbobsCOM.Documents oDoc = (SAPbobsCOM.Documents)SAP.SBOCompany.GetBusinessObject(objecttype);

            try
            {
                oDoc.DocDate = DateTime.Today;
                System.Xml.XmlDocument xmlDocument = new System.Xml.XmlDocument();
                xmlDocument.LoadXml(oDoc.GetAsXML());

                try
                {
                    return xmlDocument.SelectNodes("BOM/BO").Item(0).ChildNodes.Item(1).Name;
                }
                catch (Exception)
                {
                    return "";
                }
            }
            finally
            {
                Marshal.FinalReleaseComObject(oDoc);
                oDoc = null;
                GC.Collect();
            }
        }

        public static void FillFromSAPSQL<T>(Type type, out IEnumerable<T> output, params object[] args)
        {
            FillFromSAPSQL($"{ type.Name }.{ ApplicationCommon.GetMethodName(2) }", out output);
        }

        public static void FillFromSAPSQL<T>(string code, out IEnumerable<T> output, params object[] args)
        {
            string query = QueryCode(code, args);
            output = SecureQueryToObjects<T>(query);
        }

        public static void FillFromSAPSQL(Type type, out SAPbobsCOM.Recordset output, params object[] args)
        {
            FillFromSAPSQL($"{ type.Name }.{ ApplicationCommon.GetMethodName(2) }", out output);
        }

        public static void FillFromSAPSQL(string code, out SAPbobsCOM.Recordset output, params object[] args)
        {
            string query = QueryCode(code, args);
            output = QueryToRecordSet(query);
        }

        public static void FillFromSAPSQL<T>(SAPbouiCOM.Form oForm, out IEnumerable<T> output, int row = 0, params object[] args)
        {
            FillFromSAPSQL(oForm, $"{ oForm.TypeEx }.{ ApplicationCommon.GetMethodName(2) }", out output, row, args);
        }

        public static void FillFromSAPSQL<T>(SAPbouiCOM.Form oForm, string code, out IEnumerable<T> output, int row = 0, params object[] args)
        {
            string query = QueryCode(oForm, code, row, args);
            output = SecureQueryToObjects<T>(query);
        }

        public static void FillFromSAPSQL(SAPbouiCOM.Form oForm, out SAPbobsCOM.Recordset output, int row = 0, params object[] args)
        {
            FillFromSAPSQL(oForm, $"{ oForm.TypeEx }.{ ApplicationCommon.GetMethodName(2) }", out output, row, args);
        }

        public static void FillFromSAPSQL(SAPbouiCOM.Form oForm, string code, out SAPbobsCOM.Recordset output, int row = 0, params object[] args)
        {
            string query = QueryCode(oForm, code, row, args);
            output = QueryToRecordSet(query);
        }

        public static void FillFromSAPSQL(Type type, SAPbouiCOM.DataTable dt)
        {
            FillFromSAPSQL($"{ type.Name }.{ ApplicationCommon.GetMethodName(2) }", dt);
        }

        public static void FillFromSAPSQL(string code, SAPbouiCOM.DataTable dt)
        {
            string query = QueryCode(code);
            SecureQueryToDataTable(query, dt);
        }

        public static void FillFromSAPSQL(SAPbouiCOM.Form oForm, SAPbouiCOM.DataTable dt, int row = 0, params object[] args)
        {
            FillFromSAPSQL(oForm, $"{ oForm.TypeEx }.{ ApplicationCommon.GetMethodName(2) }", dt, row, args);
        }

        public static void FillFromSAPSQL(SAPbouiCOM.Form oForm, string code, SAPbouiCOM.DataTable dt, int row = 0, params object[] args)
        {
            string query = QueryCode(oForm, code, row, args);
            SecureQueryToDataTable(query, dt);
        }

        public static void ExeFromSAPSQL(Type type)
        {
            ExeFromSAPSQL($"{ type.Name }.{ ApplicationCommon.GetMethodName(2) }");
        }

        public static void ExeFromSAPSQL(string code)
        {
            string query = QueryCode(code);
            QueryToRecordSet(query);
        }

        public static void ExeFromSAPSQL(SAPbouiCOM.Form oForm, int row = 0, params object[] args)
        {
            ExeFromSAPSQL(oForm, $"{ oForm.TypeEx }.{ ApplicationCommon.GetMethodName(2) }", row, args);
        }

        public static void ExeFromSAPSQL(SAPbouiCOM.Form oForm, string code, int row = 0, params object[] args)
        {
            string query = QueryCode(oForm, code, row, args);
            QueryToRecordSet(query);
        }

        public static void ExeFromSAPSQL(DataTable dt, params object[] args)
        {
            ExeFromSAPSQL($"{ dt.TableName }.{ ApplicationCommon.GetMethodName(2) }", dt, args);
        }

        public static void ExeFromSAPSQL(string code, DataTable dt, params object[] args)
        {
            string query = QueryCode(dt, code, args);
            QueryToRecordSet(query);
        }

        public static void SecureQuery(string query, Action action)
        {
            SecureQuery(query, () =>
            {
                action();
                return 0;
            });
        }

        public static object SecureQuery(string query, Func<object> action)
        {
            try
            {
                return action();
            }
            catch (Exception)
            {
                UpdateToErrorDump(query);
                throw;
            }
        }

        private static void UpdateToErrorDump(string query)
        {
            SAPbobsCOM.Recordset rc = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            try
            {
                const string key = "ErrorDump";
                const string field = "U_Query";
                rc.DoQuery($"SELECT * FROM \"{ TableName }\" WHERE \"Code\"='{ key }'");

                if (rc.RecordCount > 0)
                {
                    rc.DoQuery($"UPDATE \"{ TableName }\" SET \"{ field }\"='{ query.Replace("'", "''") }' WHERE \"Code\"='{ key }'");
                    return;
                }

                rc.DoQuery($"INSERT INTO \"{ TableName }\" (\"Code\", \"Name\", \"{ field }\") VALUES ('{ key }', '{ key }', '{ query.Replace("'", "''") }')");
            }
            finally
            {
                Marshal.FinalReleaseComObject(rc);
                rc = null;
                GC.Collect();
            }
        }

        private static IEnumerable<T> SecureQueryToObjects<T>(string query)
        {
            return SecureQuery(query, () =>
            {
                RecordSet rc = new RecordSet();
                return rc.Query<T>(query);
            }) as IEnumerable<T>;
        }

        private static SAPbobsCOM.Recordset QueryToRecordSet(string query)
        {
            return SecureQuery(query, () =>
            {
                SAPbobsCOM.Recordset output = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                output.DoQuery(query);
                return output;
            }) as SAPbobsCOM.Recordset;
        }

        private static void SecureQueryToDataTable(string query, SAPbouiCOM.DataTable dt)
        {
            SecureQuery(query, () =>
            {
                dt.ExecuteQuery(query);
                return dt;
            });
        }
    }
}

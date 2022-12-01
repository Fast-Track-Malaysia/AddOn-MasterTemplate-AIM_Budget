using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace FT_ADDON
{
    class WebAPI
    {
        HttpClient client { get; set; } = new HttpClient();

        static readonly Regex urlpath_rgx = new Regex("(?<=\\/)[^\\.:]+?$");

        public WebAPI()
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        
        public WebAPI(string url)
        {
            client.BaseAddress = new Uri(url);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private void LogResponse(string json, string code, string path, HttpStatusCode status)
        {
            using (RecordSet rs = new RecordSet())
            {
                string table = $"WEBAPI_{ code.ToUpper() }";
                string url = $"{ client.BaseAddress.AbsoluteUri }{ path }";
                CreateLogTable(rs, table);
                AddLogToTable(rs, table, url, json, status);
            }
        }
        
        private void LogRequest(string json, string code, string path)
        {
            using (RecordSet rs = new RecordSet())
            {
                string table = $"WEBAPI_{ code.ToUpper() }";
                string url = $"{ client.BaseAddress.AbsoluteUri }{ path }";
                CreateLogTable(rs, table);
                AddLogToTable(rs, table, url, json);
            }
        }

        private void AddLogToTable(RecordSet rs, string table, string url, string json)
        {
            string query = "INSERT INTO \"{0}\" (url, body, type) VALUES ('{1}', '{2}', 'request')";
            query = String.Format(query,
                                  table,
                                  url,
                                  json.Replace("'", "''"));
            rs.DoQuery(query);
        }
        
        private void AddLogToTable(RecordSet rs, string table, string url, string json, HttpStatusCode status)
        {
            string query = "INSERT INTO \"{0}\" (url, body, type, status) VALUES ('{1}', '{2}', 'response', {3})";
            query = String.Format(query,
                                  table,
                                  url,
                                  json.Replace("'", "''"),
                                  (int)status);
            rs.DoQuery(query);
        }

        private void CreateLogTable(RecordSet rs, string table)
        {
            string query = GetCreateLogTableQuery();
            query = String.Format(query, table);
            rs.DoQuery(query);
        }

        private string GetCreateLogTableQuery()
        {
            if (SAP.SBOCompany.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB)
            {
                return "DO " +
                       "BEGIN " +
                       "DECLARE myrowid INTEGER; " +
                       "SELECT COUNT(*) INTO myrowid FROM \"PUBLIC\".\"M_TABLES\" WHERE \"SCHEMA_NAME\"=CURRENT_SCHEMA AND \"TABLE_NAME\"='{0}'; " +
                       "IF (0 = myrowid) " +
                       "THEN " +
                       "CREATE COLUMN TABLE \"{0}\" (\"id\" INTEGER NOT NULL PRIMARY KEY, \"url\" TEXT NOT NULL, \"body\" TEXT NOT NULL, \"status\" INTEGER, \"createtime\" TIMESTAMP DEFAULT CURRENT_TIMESTAMP); " +
                       "END IF; " +
                       "END;";
            }

            return "IF NOT EXISTS (SELECT * FROM \"INFORMATION_SCHEMA\".\"TABLES\" WHERE \"TABLE_NAME\"='{0}') " +
                   "BEGIN " +
                   "CREATE TABLE \"dbo\".\"{0}\" (" +
                   "\"id\" INT NOT NULL IDENTITY(1,1) PRIMARY KEY," +
                   "\"url\" NTEXT NOT NULL," +
                   "\"body\" NTEXT NOT NULL," +
                   "\"type\" NVARCHAR(10) NOT NULL," +
                   "\"status\" INT NULL," +
                   "\"createtime\" DateTime CONSTRAINT DF_{0}_createtime DEFAULT GETDATE()) " +
                   "END";
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogRequest(object obj, string code, string path)
        {
            LogRequest(JsonConvert.SerializeObject(obj), code, path);
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogResponse(object obj, string code, string path, HttpStatusCode status)
        {
            LogResponse(JsonConvert.SerializeObject(obj), code, path, status);
        }

        public void SetAuthorization(AuthenticationHeaderValue authentication)
        {
            client.DefaultRequestHeaders.Authorization = authentication;
        }

        public async Task<HttpResponseMessage> PostAsync(IDictionary<string, string> formdata, string type, string path, AuthenticationHeaderValue authentication = null)
        {
            string code = $"{ type }_Post";
            LogRequest(formdata, code, path);
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, path);
            
            if (authentication != null)
            {
                request.Headers.Authorization = authentication;
            }

            request.Content = new FormUrlEncodedContent(formdata);
            HttpResponseMessage response = await client.SendAsync(request);
            string json = response.Content.ReadAsStringAsync().Result;
            LogResponse(json, code, path, response.StatusCode);
            return response;
        }
        
        public async Task<HttpResponseMessage> PostAsync(object obj, string type, string path, AuthenticationHeaderValue authentication = null)
        {
            string code = $"{ type }_Post";
            LogRequest(obj, code, path);
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (authentication != null)
            {
                request.Headers.Authorization = authentication;
            }

            request.Content = new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.SendAsync(request);
            string json = response.Content.ReadAsStringAsync().Result;
            LogResponse(json, code, path, response.StatusCode);
            return response;
        }

        public async Task<HttpResponseMessage> GetAsync(string type, string path, AuthenticationHeaderValue authentication = null)
        {
            string code = $"{ type }_Get";
            LogRequest("", code, path);
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, path);

            if (authentication != null)
            {
                request.Headers.Authorization = authentication;
            }

            HttpResponseMessage response = await client.SendAsync(request);
            string json = response.Content.ReadAsStringAsync().Result;
            LogResponse(json, code, path, response.StatusCode);
            return response;
        }

        public static HttpResponseMessage Post(IDictionary<string, string> formdata, SAPbouiCOM.Form oForm, string code, AuthenticationHeaderValue authentication = null)
        {
            var url = GetUrl(oForm, code, out var path);
            WebAPI webapi = new WebAPI(url);
            return PostAsync(formdata, oForm, code, authentication).Result;
        }
        
        public static HttpResponseMessage Post(object obj, SAPbouiCOM.Form oForm, string code, AuthenticationHeaderValue authentication = null)
        {
            var url = GetUrl(oForm, code, out var path);
            WebAPI webapi = new WebAPI(url);
            return PostAsync(obj, oForm, code, authentication).Result;
        }

        public static async Task<HttpResponseMessage> PostAsync(IDictionary<string, string> formdata, SAPbouiCOM.Form oForm, string code, AuthenticationHeaderValue authentication = null)
        {
            var url = GetUrl(oForm, code, out var path);
            WebAPI webapi = new WebAPI(url);
            return await webapi.PostAsync(formdata, code, path, authentication);
        }
        
        public static async Task<HttpResponseMessage> PostAsync(object obj, SAPbouiCOM.Form oForm, string code, AuthenticationHeaderValue authentication = null)
        {
            var url = GetUrl(oForm, code, out var path);
            WebAPI webapi = new WebAPI(url);
            return await webapi.PostAsync(obj, code, path, authentication);
        }

        public static HttpResponseMessage Get(SAPbouiCOM.Form oForm, string code, AuthenticationHeaderValue authentication = null)
        {
            var url = GetUrl(oForm, code, out var path);
            WebAPI webapi = new WebAPI(url);
            return webapi.GetAsync(code, path, authentication).Result;
        }
        
        public static async Task<HttpResponseMessage> GetAsync(SAPbouiCOM.Form oForm, string code, AuthenticationHeaderValue authentication = null)
        {
            var url = GetUrl(oForm, code, out var path);
            WebAPI webapi = new WebAPI(url);
            return await webapi.GetAsync(code, path, authentication);
        }
        
        public static string GetUrl(SAPbouiCOM.Form oForm, string code, out string path)
        {
            string url;
            path = "";
            code += $".{ nameof(GetUrl) }";
            string query = ApplicationCommon.QueryCode(oForm, code, 0);

            using (RecordSet rs = new RecordSet())
            {
                rs.DoQuery(query);

                if (rs.RecordCount == 0) throw new MessageException($"Create \"{ code }\" row with default value as the url to the api in \"@SQLQuery\" table");

                url = rs.GetValue(0).ToString();
            }

            if (url.Length == 0) throw new MessageException($"Error : Web API url cannot be found - { code } -");

            var match = urlpath_rgx.Match(url);

            if (match.Success)
            {
                var capture = match.Captures[0];
                path = capture.Value;
                url = url.Substring(0, capture.Index);
            }

            return url;
        }
    }
}

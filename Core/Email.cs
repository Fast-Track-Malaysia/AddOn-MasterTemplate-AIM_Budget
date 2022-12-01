using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace FT_ADDON
{
    class Email
    {
        string SenderEmail { get; set; }
        string SenderPass { get; set; }
        string RecipientEmail { get; set; }
        string Subject { get; set; }
        string Body { get; set; }
        string IsBodyHtml { get; set; }
        string Host { get; set; }
        int Port { get; set; }
        string EnableSsl { get; set; }
        
        private string GetUniqueSignature()
        {
            return HashString($"{ SenderEmail }{ Subject }{ Body }{ IsBodyHtml }{ EnableSsl }{ Host }");
        }

        /// <summary>Make sure SQL Query UDT has "Email.GetEmailInfo.[<u>code</u>]" without the brackets</summary>
        public static void SendEmail(string code)
        {
            if (code == null) return;

            var emailmap = GetEmailInfo(code);

            if (emailmap == null) return;

            SendEmail(emailmap);
        }

        public static void SendEmail(IEnumerable<Email> emails)
        {
            if (emails == null) return;

            var emailmap = emails.Where(email => email.Body != null && email.Body.Length > 0)
                                .GroupBy(key => key.GetUniqueSignature())
                                .ToDictionary(email => email.Key,
                                              email => emails.Where(each => each.GetUniqueSignature() == email.Key)
                                                             .ToArray());
            SendEmail(emailmap);
        }

        public static void SendEmail(IDictionary<string, Email[]> emailmap)
        {
            foreach (var list in emailmap.Select(info => info.Value))
            {
                var emailinfo = list.First();
                StringBuilder sb = new StringBuilder();
                list.ToList().ForEach(each => sb.Append($"{ each.RecipientEmail };"));

                try
                {
                    SmtpClient smtp = new SmtpClient(emailinfo.Host)
                    {
                        Port = emailinfo.Port,
                        EnableSsl = emailinfo.EnableSsl != "N",
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential
                        {
                            UserName = emailinfo.SenderEmail,
                            Password = emailinfo.SenderPass,
                        },
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                    };

                    MailMessage message = new MailMessage
                    {
                        From = new MailAddress(emailinfo.SenderEmail),
                        Subject = emailinfo.Subject,
                        IsBodyHtml = emailinfo.IsBodyHtml == "Y",
                        Body = emailinfo.Body,
                    };

                    foreach (var each in list)
                    {
                        message.To.Add(new MailAddress(each.RecipientEmail));
                    }

                    smtp.Send(message);
                    SAP.SBOApplication.StatusBar.SetSystemMessage($"Email sent - Sender: { emailinfo.SenderEmail } | Recipient(s): { sb }", Type: SAPbouiCOM.BoStatusBarMessageType.smt_Success);
                }
                catch (Exception ex)
                {
                    SAP.SBOApplication.MessageBox($"Sender: { emailinfo.SenderEmail }\nRecipient(s): { sb }\nError: { ex.Message }\n{ ex.StackTrace }");
                }
            }
        }

        private static string GetQueryForEmailQuery(string code)
        {
            return $"SELECT \"U_Query\" FROM \"@SQLQUERY\" WHERE \"Code\"='{ nameof(Email) }.{ nameof(GetEmailInfo) }.{ code }'";
        }

        private static string GetEmailQuery(string code)
        {
            RecordSet rc = new RecordSet();
            return rc.Query<string>(GetQueryForEmailQuery(code)).FirstOrDefault();
        }

        private static string HashString(string str)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(str))).Replace("-", String.Empty);
            }
        }

        private static Dictionary<string, Email[]> GetEmailInfo(string code)
        {
            string query = GetEmailQuery(code);

            if (query == null) return null;

            RecordSet rc = new RecordSet();
            var emaillist = rc.Query<Email>(query);
            return emaillist.Where(email => email.Body != null && email.Body.Length > 0)
                                .GroupBy(key => key.GetUniqueSignature())
                                .ToDictionary(email => email.Key,
                                              email => emaillist.Where(each => each.GetUniqueSignature() == email.Key)
                                                                .ToArray());
        }
    }
}

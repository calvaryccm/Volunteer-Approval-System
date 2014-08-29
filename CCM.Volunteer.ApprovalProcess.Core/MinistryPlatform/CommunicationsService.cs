/*
Copyright 2014 Calvary Chapel of Melbourne, Inc.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CCM.Volunteer.ApprovalProcess.Core.Interfaces;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using System.Data;
using System.Net.Mail;
using System.Net.Mime;
using CCM.Volunteer.ApprovalProcess.Core.Models;
using Dapper;

namespace CCM.Volunteer.ApprovalProcess.Core.MinistryPlatform
{
    public class CommunicationsService : ICommunicationsService
    {
        private string apiServerUrl = ConfigurationManager.AppSettings["server"];
        private string apiKey = ConfigurationManager.AppSettings["mpguid"];
        private string apiPassword = ConfigurationManager.AppSettings["mppw"];
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public string From { get; set; }

        public CommunicationsService()
        {
            if (string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["defaultFromEmail"]))
                throw new ConfigurationErrorsException("No default from email specified. Create an entry in the application's config settings with the key 'defaultFromEmail'.");
            From = ConfigurationManager.AppSettings["defaultFromEmail"];
        }

        public bool SendEmail(string to, int emailTemplateID, string cc = null, Dictionary<string, string> templateFields = null)
        {
            var template = GetEmailTemplate(emailTemplateID);
            if (template == null)
                return false;

            return SendEmail(to, template.Subject, template.Body, cc: cc, templateFields: templateFields);
        }

        public bool SendEmail(string to, string subject, int emailTemplateID, string cc = null, Dictionary<string, string> templateFields = null)
        {
            var template = GetEmailTemplate(emailTemplateID);
            if (template == null)
                return false;

            return SendEmail(to, subject, template.Body, cc: cc, templateFields: templateFields);
        }

        public bool SendEmail(string to, string subject, string body, string cc = null, bool isHtml = true, Dictionary<string, string> templateFields = null)
        {
            //populate message with all fields
            if (templateFields != null)
            {
                foreach (var item in templateFields)
                {
                    var regex = new Regex("\\[" + item.Key + "\\]", RegexOptions.IgnoreCase);

                    if (!string.IsNullOrEmpty(item.Value))
                        body = regex.Replace(body, item.Value);
                    else
                        body = regex.Replace(body, string.Empty);
                }
            }

            using (MailMessage mail = new MailMessage(From, to, subject, body))
            {
                if (isHtml)
                {
                    var alternameView = AlternateView.CreateAlternateViewFromString(body, new ContentType("text/html"));
                    mail.AlternateViews.Add(alternameView);
                }

                if (cc != null)
                {
                    mail.CC.Add(cc);
                }


                using (var smtpClient = new SmtpClient())
                {
                    //smtpClient.Credentials = new NetworkCredential(SmtpUsername, SmtpPassword);
                    try
                    {
                        smtpClient.Send(mail);
                    }
                    catch (Exception e)
                    {
                        ErrorLogging.Log(e);
                        return false;
                    }
                }
                return true;
            }
        }


        private EmailTemplate GetEmailTemplate(int emailTemplateID)
        {
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["MinistryPlatform"].ConnectionString))
            {
                connection.Open();

                var result = connection.Query<EmailTemplate>("api_CORE_GetTemplate", new { DomainID = 1, TemplateID = emailTemplateID }, commandType: CommandType.StoredProcedure);

                //return false if nothing is returned
                if (result.Count() < 1)
                    return null;

                return result.SingleOrDefault();
            }
        }
    }
}

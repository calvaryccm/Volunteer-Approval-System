using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCM.Volunteer.ApprovalProcess.Core.Interfaces
{
    public interface ICommunicationsService
    {
        string SmtpHost { get; set; }
        int SmtpPort { get; set; }
        string SmtpUsername { get; set; }
        string SmtpPassword { get; set; }
        string From { get; set; }

        bool SendEmail(string to, int emailTemplateID, string cc = null, Dictionary<string, string> templateFields = null);
        bool SendEmail(string to, string subject, int emailTemplateID, string cc = null, Dictionary<string, string> templateFields = null);
        //bool SendEmail(string to, string subject, int emailTemplateID, string cc, Dictionary<string, string> templateFields = null);
        bool SendEmail(string to, string subject, string body, string cc = null, bool isHtml = true, Dictionary<string, string> templateFields = null);
    }
}

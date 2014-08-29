using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCM.Volunteer.ApprovalProcess.Core.Models
{
    public class EmailTemplate
    {
        public int ID { get; set; }
        public Person Author { get; set; }
        public string Subject { get; set; }
        public string Summary { get; set; }
        public string Body { get; set; }
        public string Signature { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string ReplyLink { get; set; }
        public bool Sent { get; set; }
        public Person From { get; set; }
        public Person ReplyTo { get; set; }
    }
}

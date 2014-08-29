using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCM.Volunteer.ApprovalProcess.Core.Models
{
    public partial class MinistryQuestionaire
    {
        public MQStatus VolunteerStatus { get; set; }
        public Program MinistryToVolunteerFor { get; set; }
        public IEnumerable<MQReference> ReferenceList { get; set; }
        public Contact Contact { get; set; }
        public Congregation Campus { get; set; }
        public Frequency HowOftenDoYouAttend { get; set; }
    }
}

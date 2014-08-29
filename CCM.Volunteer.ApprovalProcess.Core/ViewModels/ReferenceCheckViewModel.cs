using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CCM.Volunteer.ApprovalProcess.Core.Models;

namespace CCM.Volunteer.ApprovalProcess.Core.ViewModels
{
    public class ReferenceCheckViewModel
    {
        public MQReference Reference { get; set; }
        public MinistryQuestionaire VolunteerApp { get; set; }
        public Contact VolunteerInfo { get; set; }
    }
}

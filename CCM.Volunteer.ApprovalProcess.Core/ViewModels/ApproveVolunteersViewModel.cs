using CCM.Volunteer.ApprovalProcess.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCM.Volunteer.ApprovalProcess.Core.ViewModels
{
    public class ApproveVolunteersViewModel
    {
        public MinistryQuestionaire VolunteerApplication { get; set; }
        public IEnumerable<VolunteerApproverComment> RedFlagNotes { get; set; }
        public Contact VolunteerInfo { get; set; }

        public IEnumerable<MQStatus> CurrentVolunteerStatuses { get; set; }
        public IEnumerable<MQStatusReason> CurrentVolunteerStatusReasons { get; set; }
    }
}

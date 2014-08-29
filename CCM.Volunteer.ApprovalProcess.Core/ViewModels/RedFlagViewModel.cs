using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CCM.Volunteer.ApprovalProcess.Core.Models;

namespace CCM.Volunteer.ApprovalProcess.Core.ViewModels
{
    public class RedFlagViewModel
    {
        public MinistryQuestionaire VolunteerApplication { get; set; }
        public Contact VolunteerInfo { get; set; }
        public IEnumerable<VolunteerApproverComment> RedFlagNotes { get; set; }
        public IEnumerable<ParticipantMilestone> Milestones { get; set; }
        public IEnumerable<MPRouterItemStatus> AppStatuses { get; set; }
        public IEnumerable<Contact> HouseholdMembers { get; set; }
        public IEnumerable<Group> CurrentGroups { get; set; }
        public VolunteerApproverComment CurrentRedFlagNotes { get; set; }
    }

    public class RedFlagEntryViewModel
    {
        public int StatusID { get; set; }
        public string Notes { get; set; }
    }
}

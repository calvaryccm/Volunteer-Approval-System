using CCM.Volunteer.ApprovalProcess.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCM.Volunteer.ApprovalProcess.Core.ViewModels
{
    public class ReturnToDirectorViewModel
    {
        public IEnumerable<Group> MinistryTeams { get; set; }
        public IEnumerable<Contact> HouseholdMembers { get; set; }
        public MinistryQuestionaire VolunteerApplication { get; set; }
        public Contact VolunteerInfo { get; set; }
        public Program Ministry { get; set; }
    }

}

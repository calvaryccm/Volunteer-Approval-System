using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCM.Volunteer.ApprovalProcess.Core.Models
{
    public partial class ParticipantMilestone
    {
        [Editable(false)]
        public string Milestone_Title { get; set; }
    }
}

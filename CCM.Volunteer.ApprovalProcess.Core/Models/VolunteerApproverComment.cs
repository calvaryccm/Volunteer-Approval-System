using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCM.Volunteer.ApprovalProcess.Core.Models
{
    public class VolunteerApproverComment
    {
        public MPRouterItem Details { get; set; }
        public Contact Approver { get; set; }
    }
}

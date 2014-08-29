using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCM.Volunteer.ApprovalProcess.Core.Models
{
    public partial class Program
    {
        public Contact Leader { get; set; }
        public Contact VolunteerCoordinator { get; set; }
        public Contact PrimaryContact { get; set; }
    }
}

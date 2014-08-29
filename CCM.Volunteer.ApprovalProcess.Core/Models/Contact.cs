using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCM.Volunteer.ApprovalProcess.Core.Models
{
    public partial class Contact
    {
        public MaritalStatus MaritalStatus { get; set; }
        public Household Household { get; set; }
    }
}

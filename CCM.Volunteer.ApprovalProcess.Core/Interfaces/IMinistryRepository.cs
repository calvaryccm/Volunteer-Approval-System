using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CCM.Volunteer.ApprovalProcess.Core.Models;

namespace CCM.Volunteer.ApprovalProcess.Core.Interfaces
{
    public interface IMinistryRepository
    {
        Program GetMinisty(int ministryID);
    }
}

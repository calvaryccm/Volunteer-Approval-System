using CCM.Volunteer.ApprovalProcess.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCM.Volunteer.ApprovalProcess.Core.Interfaces
{
    public interface IGroupsRepository
    {
        IEnumerable<Group> GetMinistryTeamsByProgramID(int programID);
        IEnumerable<Group> GetSmallGroupsByContactID(int contactID);
    }
}

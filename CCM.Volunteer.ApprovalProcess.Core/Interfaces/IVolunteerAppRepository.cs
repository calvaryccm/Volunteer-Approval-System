using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CCM.Volunteer.ApprovalProcess.Core.Models;

namespace CCM.Volunteer.ApprovalProcess.Core.Interfaces
{
    public interface IVolunteerAppRepository
    {
        IEnumerable<MinistryQuestionaire> GetVolunteerApps(object query = null);
        IEnumerable<MinistryQuestionaire> GetVolunteerAppsForRedFlag();
        IEnumerable<MinistryQuestionaire> GetVolunteerAppsReadyForApproval();
        IEnumerable<MinistryQuestionaire> GetVolunteerAppsNeedingFollowUp();
        MinistryQuestionaire GetVolunteerApp(int id);
        bool UpdateVolunteerApp(MinistryQuestionaire app);

        int AddNewRedFlagItem(int approverID, string notes, int statusID, MinistryQuestionaire app);
        bool UpdateExistingRedFlagItem(int id, string notes, int statusID);
        IEnumerable<MPRouterItemStatus> GetRedFlagAppStatuses();
        MQReference GetVolunteerAppReference(int refID);
        bool UpdateExistingAppReference(MQReference reference);
        IEnumerable<MQStatus> GetVolunteerAppStatuses();
        IEnumerable<MQStatusReason> GetVolunteerAppStatusReasons();
        IEnumerable<VolunteerApproverComment> GetRedFlagNotes(int mqID);

        //bool SetVolunteerStatus(int appID, int appStatus, int appStatusReason);
        bool SetVolunteerPlacementStatus(MinistryQuestionaire app, int programID, int placementStatus, string notes, int groupID = -1);

    }
}

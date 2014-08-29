using CCM.Volunteer.ApprovalProcess.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCM.Volunteer.ApprovalProcess.Core.Interfaces
{
    public interface IContactsRepository
    {
        Contact GetVolunteerInfo(int id);
        Contact GetContact(int id);
        bool UpdateVolunteerInfo(Contact updatedContact);
        IEnumerable<Contact> GetVolunteerHouseholdMembers(int householdID);
        IEnumerable<Contact> GetContactsToSendRedFlagEmails();
        IEnumerable<Contact> GetContactsToSendApprovalEmails();
    }
}

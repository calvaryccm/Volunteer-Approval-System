/*
Copyright 2014 Calvary Chapel of Melbourne, Inc.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/
using CCM.Volunteer.ApprovalProcess.Core.Interfaces;
using CCM.Volunteer.ApprovalProcess.Core.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCM.Volunteer.ApprovalProcess.Core.MinistryPlatform
{
    public class ContactsRepository : IContactsRepository
    {

        private MinistryPlatformDataContext dataContext;

        public ContactsRepository()
        {
            dataContext = new MinistryPlatformDataContext();
        }

        public Contact GetVolunteerInfo(int id)
        {
            return dataContext.GetRecord<Contact>(id);
        }

        public Contact GetContact(int id)
        {
            return dataContext.GetRecord<Contact>(id);
        }

        public bool UpdateVolunteerInfo(Contact updatedContact)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Contact> GetVolunteerHouseholdMembers(int householdID)
        {
            return dataContext.GetRecords<Contact>(new { Household_ID = householdID });
        }

        public IEnumerable<Contact> GetContactsToSendRedFlagEmails()
        {
            if (ConfigurationManager.AppSettings["ReviewPanelEmailGroups"] == null)
                throw new ConfigurationErrorsException("'ReviewPanelEmailGroups' is not declared in the application settings section.");

            //int[] groups = new int[3] { 773, 759, 1079 };
            int [] groups = ConfigurationManager.AppSettings["ReviewPanelEmailGroups"].Split(',').Select<string, int>(x => Int32.Parse(x.Trim())).ToArray();

            List<Contact> result = new List<Contact>();

            foreach (int i in groups)
                result.AddRange(GetContactsWithinGroupByGroupID(i));

            return result;
        }

        public IEnumerable<Contact> GetContactsToSendApprovalEmails()
        {
            if (ConfigurationManager.AppSettings["ClearingEmailGroups"] == null)
                throw new ConfigurationErrorsException("'ClearingEmailGroups' is not declared in the application settings section.");

            int[] groups = ConfigurationManager.AppSettings["ClearingEmailGroups"].Split(',').Select<string, int>(x => Int32.Parse(x.Trim())).ToArray();

            List<Contact> result = new List<Contact>();

            foreach (int i in groups)
                result.AddRange(GetContactsWithinGroupByGroupID(i));

            return result;
        }


        private IEnumerable<Contact> GetContactsWithinGroupByGroupID(int groupID)
        {
            return dataContext.GetContactsByGroupID(groupID);
        }
    }
}

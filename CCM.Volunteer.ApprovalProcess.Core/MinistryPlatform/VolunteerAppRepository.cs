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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCM.Volunteer.ApprovalProcess.Core.MinistryPlatform
{
    public class VolunteerAppRepository : IVolunteerAppRepository
    {
        private MinistryPlatformDataContext dataContext;

        public VolunteerAppRepository()
        {
            dataContext = new MinistryPlatformDataContext();
        }


        public IEnumerable<MinistryQuestionaire> GetVolunteerApps(object query = null)
        {
            return dataContext.GetRecords<MinistryQuestionaire>(query);
        }

        public IEnumerable<MinistryQuestionaire> GetVolunteerAppsForRedFlag()
        {
            return dataContext.GetVolunteerAppsForRedFlag();
        }

        public IEnumerable<MinistryQuestionaire> GetVolunteerAppsReadyForApproval()
        {
            return dataContext.GetVolunteerAppsReadyForApproval();
        }

        public IEnumerable<MinistryQuestionaire> GetVolunteerAppsNeedingFollowUp()
        {
            return dataContext.GetVolunteerAppsNeedingFollowUp();
        }

        public MinistryQuestionaire GetVolunteerApp(int id)
        {
            var result = dataContext.GetRecord<MinistryQuestionaire>(id);

            if(result.MQ_Status_ID.HasValue)
                result.VolunteerStatus = dataContext.GetRecord<MQStatus>(result.MQ_Status_ID.Value);

            if (result.Volunteer_Campus.HasValue)
                result.Campus = dataContext.GetRecord<Congregation>(result.Volunteer_Campus.Value);

            //determine latest ministry they've volunteered for
            var programs = dataContext.GetRecords<MQProgram>(new { Ministry_Questionaire_ID = id });

            result.MinistryToVolunteerFor = dataContext.GetMinistryToVolunteerFor(id);

            if(result.MinistryToVolunteerFor != null)
            {        
                //fill up all the ministry leader ones
                if (result.MinistryToVolunteerFor.Ministry_Leader.HasValue)
                    result.MinistryToVolunteerFor.Leader = dataContext.GetRecord<Contact>(result.MinistryToVolunteerFor.Ministry_Leader.Value);

                if (result.MinistryToVolunteerFor.Volunteer_Contact.HasValue)
                    result.MinistryToVolunteerFor.VolunteerCoordinator = dataContext.GetRecord<Contact>(result.MinistryToVolunteerFor.Volunteer_Contact.Value);

                result.MinistryToVolunteerFor.PrimaryContact = dataContext.GetRecord<Contact>(result.MinistryToVolunteerFor.Primary_Contact);
            }

            result.Contact = dataContext.GetRecordWithImage<Contact>(result.Contact_ID);

            if(result.Contact.Household_ID.HasValue)
            { 
                result.Contact.Household = dataContext.GetRecord<Household>(result.Contact.Household_ID.Value);

                if(result.Contact.Household.Address_ID.HasValue)
                    result.Contact.Household.Residence = dataContext.GetRecord<Address>(result.Contact.Household.Address_ID.Value);
            }

            if (result.Contact.Marital_Status_ID.HasValue)
                result.Contact.MaritalStatus = dataContext.GetRecord<MaritalStatus>(result.Contact.Marital_Status_ID.Value);

            if (result.How_Often.HasValue)
                result.HowOftenDoYouAttend = dataContext.GetRecord<Frequency>(result.How_Often.Value);

            result.ReferenceList = dataContext.GetRecords<MQReference>(new { Ministry_Questionaire_ID = id });

            //searching for contact record match in MP
            foreach(var item in result.ReferenceList)
            {
                try
                {                
                    //search by nickname and email               
                    item.ReferenceInfo = dataContext.GetRecordsWithImages<Contact>(new { Nickname = item.Name.Split(' ')[0], Email_Address = item.Email }).SingleOrDefault();

                    //lastly by first name and email
                    if (item.ReferenceInfo == null)
                        item.ReferenceInfo = dataContext.GetRecordsWithImages<Contact>(new { First_Name = item.Name.Split(' ')[0], Email_Address = item.Email }).SingleOrDefault();

                    //find by mobile phone and first name
                    if (item.ReferenceInfo == null)
                        item.ReferenceInfo = dataContext.GetRecordsWithImages<Contact>(new { Mobile_Phone = item.Phone }).SingleOrDefault();
                }
                catch
                {
                    item.ReferenceInfo = null;
                }
            }

            return result;
        }

        public int AddNewRedFlagItem(int approverID, string notes, int statusID, MinistryQuestionaire app)
        {

            app.Red_Flag_Complete = true;
            app.Red_Flag_Complete_Date = DateTime.Now;

            UpdateVolunteerApp(app);

            return dataContext.InsertRecord<MPRouterItem>(new MPRouterItem
            {
                Domain_ID = dataContext.DomainID,
                MPRouter_Item_Status_ID = statusID,
                Notes = notes,
                Page_ID = 426,
                Record_ID = app.Ministry_Questionaire_ID,
                Start_Date = DateTime.Now,
                User_ID = approverID
            });
        }


        public bool UpdateExistingRedFlagItem(int id, string notes, int statusID)
        {
            MPRouterItem currentItem = dataContext.GetRecord<MPRouterItem>(id);
            var updateSuccessful = true;

            var s = Snapshotter.Start<MPRouterItem>(currentItem);

            currentItem.Notes = notes;
            currentItem.MPRouter_Item_Status_ID = statusID;

            if (s.Diff().ParameterNames.Count() > 0)
                updateSuccessful = dataContext.UpdateRecord<MPRouterItem>(currentItem.MPRouter_Item_ID, s.Diff());

            return updateSuccessful;
        }

        public bool UpdateVolunteerApp(MinistryQuestionaire app)
        {
            MinistryQuestionaire currentApp = dataContext.GetRecord<MinistryQuestionaire>(app.Ministry_Questionaire_ID);
            var updateSuccessful = true;

            var s = Snapshotter.Start<MinistryQuestionaire>(currentApp);

            Utils.CopyPropertyValues(app, currentApp);

            if (s.Diff().ParameterNames.Count() > 0)
                updateSuccessful = dataContext.UpdateRecord<MinistryQuestionaire>(currentApp.Ministry_Questionaire_ID, s.Diff());

            return updateSuccessful;
        }

        public bool UpdateExistingAppReference(MQReference reference)
        {
            MQReference currentRef = dataContext.GetRecord<MQReference>(reference.MQ_Reference_ID);
            var updateSuccessful = true;

            var s = Snapshotter.Start<MQReference>(currentRef);

            Utils.CopyPropertyValues(reference, currentRef);

            if (s.Diff().ParameterNames.Count() > 0)
                updateSuccessful = dataContext.UpdateRecord<MQReference>(currentRef.MQ_Reference_ID, s.Diff());

            return updateSuccessful;
        }

        public IEnumerable<VolunteerApproverComment> GetRedFlagNotes(int mqID)
        {
            return dataContext.GetRecords<MPRouterItem>(new { Record_ID = mqID, Page_ID = 426, Domain_ID = dataContext.DomainID })
                            .Where(g => g.User_ID > 0)
                            .Select<MPRouterItem, VolunteerApproverComment>(f => new VolunteerApproverComment
                            {
                                Details = f,
                                Approver = dataContext.GetRecordWithImage<Contact>(dataContext.GetRecord<dpUser>(f.User_ID).Contact_ID)
                            }); //approver must have a user id in the database!
        }

        public MQReference GetVolunteerAppReference(int refID)
        {
            return dataContext.GetRecord<MQReference>(refID);
        }

        public IEnumerable<MPRouterItemStatus> GetRedFlagAppStatuses()
        {
            return dataContext.GetRecords<MPRouterItemStatus>();
        }

        public IEnumerable<MQStatus> GetVolunteerAppStatuses()
        {
            return dataContext.GetRecords<MQStatus>();
        }

        public IEnumerable<MQStatusReason> GetVolunteerAppStatusReasons()
        {
            return dataContext.GetRecords<MQStatusReason>();
        }

        //public bool SetVolunteerStatus(int appID, int appStatus, int appStatusReason)
        //{
        //    //save app status and app status reason

        //    //set milestone to ID 6
        //}

        public bool SetVolunteerPlacementStatus(MinistryQuestionaire app, int programID, int placementStatus, string notes, int groupID = -1)
        {
            //get latest MQ program search by MQ ID and program ID order by start date desc get top result.
            var result = dataContext.GetLatestMinistryAppliedFor(app.Ministry_Questionaire_ID, programID);

            if(placementStatus == 2)
            {
                var updateSuccessful = true;

                var s = Snapshotter.Start<MQProgram>(result);
                result.Placement_Status = placementStatus;
                result.Date_Placed = DateTime.Now;

                updateSuccessful = dataContext.UpdateRecord<MQProgram>(result.MQ_Program_ID, s.Diff());

                return dataContext.InsertRecord<GroupParticipant>(
                    new GroupParticipant 
                    { 
                        Domain_ID = dataContext.DomainID, 
                        Employee_Role = false, 
                        Group_ID = groupID, 
                        Group_Role_ID = 2, 
                        Need_Book = false, 
                        Start_Date = DateTime.Now, 
                        Notes = notes, 
                        Participant_ID = app.Contact.Participant_Record.Value
                    }) > 0 && updateSuccessful;
            }
            else
            {
                var s = Snapshotter.Start<MQProgram>(result);
                result.Placement_Status = placementStatus;
                //result.Date_Placed = DateTime.Now;

                return dataContext.UpdateRecord<MQProgram>(result.MQ_Program_ID, s.Diff());
            }
            //if placement status is a go
                //set placement status
                //set date placed
                //add group participant record
            //else
                //set placement status
            //throw new NotImplementedException();
        }
    }
}

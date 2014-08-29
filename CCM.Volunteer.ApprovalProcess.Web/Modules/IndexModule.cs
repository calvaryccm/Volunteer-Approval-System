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
using Nancy;
using Nancy.ModelBinding;
using CCM.Volunteer.ApprovalProcess.Core.Interfaces;
using System;
using CCM.Volunteer.ApprovalProcess.Core.Models;
using CCM.Volunteer.ApprovalProcess.Core.ViewModels;
using CCM.Volunteer.ApprovalProcess.Core;
using Nancy.Responses.Negotiation;
using Nancy.Cryptography;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Configuration;

namespace CCM.Volunteer.ApprovalProcess.Web.Modules
{
    public class IndexModule : NancyModule
    {

        public IndexModule(IVolunteerAppRepository appRepo,
            IContactsRepository contactRepo,
            IMilestoneRepository milestoneRepo,
            IGroupsRepository groupsRepo,
            IMinistryRepository ministryRepo,
            ICommunicationsService commService)
        {
            Before += ctx =>
            {
                //if (ctx.Parameters.id != null)
                //    Debug.WriteLine("Parameter ID before decryption {0}", ctx.Parameters.id);

                int resultid, contactid, id = 0;

                if (ctx.Parameters.id != null && !Int32.TryParse((string)ctx.Parameters.id, out resultid))
                    ctx.Parameters.id = ((string)ctx.Parameters.id).FromBase64().Decrypt();

                if (ctx.Request.Query.cid != null && !Int32.TryParse((string)ctx.Request.Query.cid, out contactid))
                    ctx.Request.Query.cid = ((string)ctx.Request.Query.cid).FromBase64().Decrypt();

                if (ctx.Request.Query.id != null && !Int32.TryParse((string)ctx.Request.Query.id, out id))
                    ctx.Request.Query.id = ((string)ctx.Request.Query.id).FromBase64().Decrypt();

                //if (ctx.Parameters.id != null)
                //    Debug.WriteLine("Parameter ID after decryption {0}", ctx.Parameters.id);
                return (Nancy.Response)null;
            };

            Get["/"] = parameters =>
            {
                return Response.AsText("Welcome and hello.");
            };

            Get["/reference-check/{id}"] = parameters =>
            {
                int resultid = 0;

                if (Int32.TryParse((string)parameters.id, out resultid))
                {
                    this.Bind<ReferenceCheckViewModel>();
                    var viewmodel = new ReferenceCheckViewModel();
                    viewmodel.Reference = appRepo.GetVolunteerAppReference(resultid);
                    viewmodel.VolunteerApp = appRepo.GetVolunteerApp(viewmodel.Reference.Ministry_Questionaire_ID);
                    viewmodel.VolunteerInfo = viewmodel.VolunteerApp.Contact;//contactRepo.GetVolunteerInfo(viewmodel.VolunteerApp.Contact_ID);

                    return View["reference-check", viewmodel];
                }

                return View["reference-check"];
            };

            Post["/reference-check/{id}"] = parameters =>
            {
                MediaRange mediaRange = new MediaRange
                {
                    Type = new MediaType("application/json")
                };

                int resultid = 0;

                if (Int32.TryParse((string)parameters.id, out resultid))
                {
                    this.Bind<ReferenceCheckViewModel>();
                    var viewmodel = new ReferenceCheckViewModel();
                    var reference = appRepo.GetVolunteerAppReference(resultid);
                    viewmodel.Reference = reference;
                    viewmodel.VolunteerApp = appRepo.GetVolunteerApp(viewmodel.Reference.Ministry_Questionaire_ID);
                    viewmodel.VolunteerInfo = contactRepo.GetVolunteerInfo(viewmodel.VolunteerApp.Contact_ID);

                    reference.How_Well_Know = Request.Form.howwelldoyouknow;
                    reference.Do_You_Recommend = Request.Form.wouldyourecommend;
                    reference.Recommend_With_Kids = Request.Form.trustwithchildren;
                    reference.Notes = Request.Form.notes;
                    reference.Date_Completed = DateTime.Now;


                    appRepo.UpdateExistingAppReference(reference);

                    return Negotiate
                        .WithModel(viewmodel)
                        .WithMediaRangeModel(mediaRange, new { success = true })
                        .WithView("reference-check");

                    //return View["reference-check", viewmodel];
                }

                return View["reference-check"];
            };

            Get["/approve-deny/{id}"] = parameters =>
            {
                int resultid = 0;

                if (Int32.TryParse((string)parameters.id, out resultid))
                {
                    this.Bind<ApproveVolunteersViewModel>();
                    var volApp = appRepo.GetVolunteerApp(resultid);
                    var contactInfo = volApp.Contact;//contactRepo.GetVolunteerInfo(volApp.Contact_ID);
                    var currentvolstats = appRepo.GetVolunteerAppStatuses();
                    var currentvolstatreasons = appRepo.GetVolunteerAppStatusReasons();
                    var redflagsnotes = appRepo.GetRedFlagNotes(volApp.Ministry_Questionaire_ID);


                    return View["approve-volunteers", new ApproveVolunteersViewModel
                    {
                        VolunteerApplication = volApp,
                        VolunteerInfo = contactInfo,
                        CurrentVolunteerStatuses = currentvolstats,
                        CurrentVolunteerStatusReasons = currentvolstatreasons,
                        RedFlagNotes = redflagsnotes
                    }];
                }

                return View["approve-volunteers"];
            };

            Post["/approve-deny/{id}"] = parameters =>
            {
                int resultid = 0;
                this.Bind<ApproveVolunteersViewModel>();

                if (Int32.TryParse((string)parameters.id, out resultid))
                {

                    var volApp = appRepo.GetVolunteerApp(resultid);
                    //var contactInfo = contactRepo.GetVolunteerInfo(volApp.Contact_ID);
                    var pictureUrl = string.Format(Constants.ContactImageBaseAppUrl, volApp.Contact.Contact_ID);
                    var ministry = volApp.MinistryToVolunteerFor;

                    //save app status and app status reason
                    if (Request.Form.statusid != null)
                    {
                        int status_id = Request.Form.statusid;
                        volApp.MQ_Status_ID = status_id;
                        volApp.Red_Flag_Complete_Date = DateTime.Now;

                        //if this person has not been approved for some reason, save the reason why
                        if (status_id != 1)
                            volApp.MQ_Status_Reason = Request.Form.statusidreason;

                        //if this person has been labeled for follow up, set the follow up date
                        if (status_id == 4)
                            volApp.MQ_Follow_Up = DateTime.Parse(Request.Form.followupdate);

                        //#if !DEBUG
                        //if this person has been set to approved or approved-limited
                        if (status_id == 1 || status_id == 8)
                            //set milestone to ID 6
                            milestoneRepo.AddNewMilestone(volApp.Participant_ID.Value, 6, volApp.MinistryToVolunteerFor.Program_ID);
                        //#endif


                        if (Request.Form.notes != null)
                            volApp.MQ_Status_Notes = Request.Form.notes;

                        //#if !DEBUG
                        bool success = appRepo.UpdateVolunteerApp(volApp);
                        //#endif

                        //get contacts of each person who needs to be emailed
                        //Contact ministryLeader, volunteerCoordinator, primaryContact = null;

                        //if(ministry.Ministry_Leader.HasValue)
                        //    ministryLeader = contactRepo.GetContact(ministry.Ministry_Leader.Value);

                        //if(ministry.Volunteer_Contact.HasValue)
                        //   volunteerCoordinator =  contactRepo.GetContact(ministry.Volunteer_Contact.Value);

                        //primaryContact = contactRepo.GetContact(ministry.Primary_Contact);

                        //if((ministryLeader.Contact_ID == primaryContact.Contact_ID) && ministryLeader.Contact_ID == volunteerCoordinator.Contact_ID)
                        //    commService.SendEmail(ministryLeader.Email_Address, Globals.PlacedTeamMemberTemplate, new Dictionary<string, string> { { "NICKNAME", contactInfo.Nickname }, {"LASTNAME", contactInfo.Last_Name}, {"MINISTRYNAME", ministry.Program_Name} });
                        //else if (ministryLeader.Contact_ID == primaryContact.Contact_ID)
                        //{
                        //    commService.SendEmail(ministryLeader.Email_Address, Globals.PlacedTeamMemberTemplate, new Dictionary<string, string> { { "NICKNAME", contactInfo.Nickname }, { "LASTNAME", contactInfo.Last_Name }, { "MINISTRYNAME", ministry.Program_Name } });
                        //    commService.SendEmail(ministryLeader.Email_Address, Globals.PlacedTeamMemberTemplate, new Dictionary<string, string> { { "NICKNAME", contactInfo.Nickname }, { "LASTNAME", contactInfo.Last_Name }, { "MINISTRYNAME", ministry.Program_Name } });
                        //}

                        if (status_id == 1 || status_id == 8)
                        {
                            var urltoplace = Constants.VolunteerAppBaseUrl + "/approval-process/place-volunteer/" + ministry.Program_ID.ToString().Encrypt().ToBase64() + "?id=" + volApp.Ministry_Questionaire_ID.ToString().Encrypt().ToBase64();
                            var urltoreturn = Constants.VolunteerAppBaseUrl + "/approval-process/return-to-director/" + ministry.Program_ID.ToString().Encrypt().ToBase64() + "?id=" + volApp.Ministry_Questionaire_ID.ToString().Encrypt().ToBase64();

                            // Email Ministry Leader
                            var fields = new Dictionary<string, string>{
                                {"NICKNAME", volApp.Contact.Nickname}, 
                                {"LASTNAME", volApp.Contact.Last_Name},
                                {"PICTURE", pictureUrl},
                                {"TODAYSDATE", DateTime.Now.ToString("MM/dd/yyyy")},
 
                                {"EMAILADDRESS", volApp.Contact.Email_Address},
                                {"STREETADDRESS", volApp.Contact.Household.Residence.Address_Line_1 + (volApp.Contact.Household.Residence.Address_Line_2 != null ? volApp.Contact.Household.Residence.Address_Line_2 : "")},
                                {"CITY", volApp.Contact.Household.Residence.City},
                                {"STATE", volApp.Contact.Household.Residence.State_Region},
                                {"ZIP",volApp.Contact.Household.Residence.Postal_Code},

                                
                                {"HOMEPHONE",volApp.Contact.Household.Home_Phone != null ? volApp.Contact.Household.Home_Phone : "No home phone listed" },
                                {"MOBILEPHONE", volApp.Contact.Mobile_Phone != null? volApp.Contact.Mobile_Phone : "No mobile phone listed"},

                                {"CAMPUS", volApp.Campus != null ? volApp.Campus.Congregation_Name : "No campus listed"},

                                {"MINISTRY", volApp.MinistryToVolunteerFor.Program_Name},
                                {"MINISTRYCOMMENT", volApp.New_Volunteer_Ministry_Comment ?? string.Empty},

                                {"DATESAVED", volApp.When_Saved.Value.ToString("MM/yyyy")},
                                {"DATESAVEDAGE", (((volApp.When_Saved.Value) - (volApp.Contact.Date_of_Birth.Value)).Days / 365).ToString()},

                                {"BAPTISMDATE", volApp.Baptized_When.Value.ToString("MM/yyyy")},
                                {"BAPTISMAGE", (((volApp.Baptized_When.Value) - (volApp.Contact.Date_of_Birth.Value)).Days / 365).ToString()},

                                {"CCMSTARTDATE", volApp.Started_Attending_Date.Value.ToString("MM/yyyy")},
                                {"CCMSTARTAGE", (volApp.Started_Attending_Date.Value.ToRelativeDateTime())},

                                {"AGE", volApp.Contact.__Age.ToString()},

                                {"STATEMENTOFFAITH", volApp.Statement_of_Faith == true ? "Agree" : "Disagree"},

                                {"CHURCHFREQUENCY", volApp.HowOftenDoYouAttend.Frequency_Name},

                                {"MARITALSTATUS", volApp.Contact.MaritalStatus != null ? volApp.Contact.MaritalStatus.Marital_Status : string.Empty },

                                {"TESTIMONY", volApp.Testimony},
                                {"DEVOLIFE", volApp.Devotional_Life},
                                {"TALENTS", volApp.Talents_Skills_Hobbies},
                                {"BIBLECOLLEGE", volApp.Bible_College_Workshops_Counseling},
                                {"ADDITIONALINFO", volApp.Additional_Info},
                                {"CLEARINGINFO", volApp.MQ_Status_Notes != null ? volApp.MQ_Status_Notes : "No notes from director"},

                                {"DECISION-PLACE", urltoplace},
                                {"DECISION-RETURN", urltoreturn}
                            };

                            var templateID = Globals.NewVolunteerApprovedTemplate;

                            if (!volApp.Statement_of_Faith.Value){
                                fields.Add("STATEMENTOFFAITHREASON", volApp.Not_Agree_Reason);
                                templateID = Globals.NewVolunteerApprovedTemplateNOSOF;                
                            }
                            
                            


                            //send ministry leader an email
                            if (ministry.Ministry_Leader.HasValue)
                            {
                                //if this isn't the same as the primary contact
                                if(ministry.Leader.Email_Address != ministry.PrimaryContact.Email_Address)
                                {
                                    //add the person who sent this
                                    fields["DECISION-RETURN"] = fields["DECISION-RETURN"] + "&from=" + ministry.Leader.Email_Address;//.Encrypt().ToBase64();
                                    commService.SendEmail(ministry.Leader.Email_Address, volApp.Contact.Nickname + " " + volApp.Contact.Last_Name + " has been approved for your team!", templateID, cc: Globals.VolunteerEmail, templateFields: fields);
                                }
                            }

                            //send volunteer coordinator an email
                            if(ministry.Volunteer_Contact.HasValue)
                            {
                                if(ministry.VolunteerCoordinator.Email_Address != ministry.PrimaryContact.Email_Address)
                                {                                    
                                    //add the person who sent this
                                    fields["DECISION-RETURN"] = fields["DECISION-RETURN"] + "&from=" + ministry.VolunteerCoordinator.Email_Address;//.Encrypt().ToBase64();
                                    commService.SendEmail(ministry.VolunteerCoordinator.Email_Address, volApp.Contact.Nickname + " " + volApp.Contact.Last_Name + " has been approved for your team!", templateID, cc: Globals.VolunteerEmail, templateFields: fields);
                                }
                            }
                                
                            //add the person who sent this
                            fields["DECISION-RETURN"] = fields["DECISION-RETURN"] + "&from=" + ministry.PrimaryContact.Email_Address;//.Encrypt().ToBase64();
                            //send primary contact an email
                            commService.SendEmail(ministry.PrimaryContact.Email_Address, volApp.Contact.Nickname + " " + volApp.Contact.Last_Name + " has been approved for your team!", templateID, cc: Globals.VolunteerEmail, templateFields: fields);

                            // Email Applicant (Steve face email)
                            commService.SendEmail(volApp.Contact.Email_Address, "Congratulations " + volApp.Contact.Nickname + "! Your application to volunteer at CCM has been approved!", Globals.WelcomeNewVolunteerTemplate, templateFields: new Dictionary<string, string> { { "NICKNAME", volApp.Contact.Nickname } });
                        }
                    }

                    return Negotiate.WithModel(new ApproveVolunteersViewModel
                    {
                        VolunteerApplication = volApp,
                        VolunteerInfo = volApp.Contact,
                        CurrentVolunteerStatuses = appRepo.GetVolunteerAppStatuses(),
                        CurrentVolunteerStatusReasons = appRepo.GetVolunteerAppStatusReasons(),
                        RedFlagNotes = appRepo.GetRedFlagNotes(volApp.Ministry_Questionaire_ID)
                    }).WithMediaRangeModel(new MediaRange
                        {
                            Type = new MediaType("application/json")
                        }, new { success = true });

                }


                //send an email to ministry leader, primary contact, volunteer leader and applicant (don't duplicate if same email)

                return Response.AsJson(new { success = false, reason = "The app id is not present or isn't valid" }, HttpStatusCode.InternalServerError);
            };


            Get["/red-flag/{id}"] = parameters =>
            {
                int resultid = 0;
                int redflagvoterid = 0;

                if (Int32.TryParse((string)parameters.id, out resultid))
                {

                    if (!Int32.TryParse((string)Request.Query.cid, out redflagvoterid))
                    {
                        //return Negotiate.WithMediaRangeModel(mediaRange, new { success = false, reason = "Contact ID is not present" });
                    }

                    var volApp = appRepo.GetVolunteerApp(resultid);
                    var contactInfo = volApp.Contact;//contactRepo.GetVolunteerInfo(volApp.Contact_ID);
                    var redFlagNotes = appRepo.GetRedFlagNotes(volApp.Ministry_Questionaire_ID);
                    var milestones = milestoneRepo.GetMilestones(volApp.Contact_ID);
                    var statuses = appRepo.GetRedFlagAppStatuses();
                    var householdMembers = contactRepo.GetVolunteerHouseholdMembers(contactInfo.Household_ID.HasValue ? contactInfo.Household_ID.Value : 0);
                    var currentGroups = groupsRepo.GetSmallGroupsByContactID(volApp.Contact_ID);
                    VolunteerApproverComment currentRedFlagNotes = redFlagNotes.Where(f => f.Approver.Contact_ID == redflagvoterid).SingleOrDefault();

                    this.Bind<RedFlagViewModel>();


                    return View["red-flag", new RedFlagViewModel
                    {
                        VolunteerApplication = volApp,
                        VolunteerInfo = contactInfo,
                        RedFlagNotes = redFlagNotes,
                        Milestones = milestones,
                        AppStatuses = statuses,
                        HouseholdMembers = householdMembers,
                        CurrentGroups = currentGroups,
                        CurrentRedFlagNotes = currentRedFlagNotes
                    }];
                }

                return View["red-flag"];
            };

            Post["/red-flag/{id}"] = parameters =>
            {
                MediaRange mediaRange = new MediaRange
                {
                    Type = new MediaType("application/json")
                };

                var cxt = Response.Context;


                int resultid, redflagvoterid = 0;

                if (!Int32.TryParse((string)cxt.Request.Query.cid, out redflagvoterid))
                {
                    return Negotiate.WithMediaRangeModel(mediaRange, new { success = false, reason = "Contact ID is not present" });
                }

                if (Int32.TryParse((string)parameters.id, out resultid))
                {
                    var item = this.Bind<RedFlagEntryViewModel>();
                    //appRepo.AddNewRedFlagItem(0, item.Notes, item.StatusID, Int32.Parse((string)parameters.id));

                    var volApp = appRepo.GetVolunteerApp(resultid);
                    var contactInfo = contactRepo.GetVolunteerInfo(volApp.Contact_ID);
                    var redFlagNotes = appRepo.GetRedFlagNotes(volApp.Ministry_Questionaire_ID);
                    var milestones = milestoneRepo.GetMilestones(volApp.Contact_ID);
                    var statuses = appRepo.GetRedFlagAppStatuses();
                    var householdMembers = contactRepo.GetVolunteerHouseholdMembers(contactInfo.Household_ID.HasValue ? contactInfo.Household_ID.Value : 0);
                    var currentGroups = groupsRepo.GetSmallGroupsByContactID(volApp.Contact_ID);
                    var redflagvoter = contactRepo.GetVolunteerInfo(redflagvoterid);
                    VolunteerApproverComment currentRedFlagNotes = redFlagNotes.Where(f => f.Approver.Contact_ID == redflagvoterid).SingleOrDefault();

                    this.Bind<RedFlagViewModel>();

                    int result = 0;

                    //save response (add a new one or update a current one)
                    if (currentRedFlagNotes != null)
                        result = appRepo.UpdateExistingRedFlagItem(currentRedFlagNotes.Details.MPRouter_Item_ID, (string)cxt.Request.Form.notes, (int)cxt.Request.Form.statusid) ? 1 : 0;
                    else
                        result = appRepo.AddNewRedFlagItem(redflagvoter.User_Account.HasValue ? redflagvoter.User_Account.Value : 0, (string)cxt.Request.Form.notes, (int)cxt.Request.Form.statusid, volApp);

                    if (result < 1)
                        return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError);

                    return Negotiate
                        .WithModel(new RedFlagViewModel
                        {
                            VolunteerApplication = volApp,
                            VolunteerInfo = contactInfo,
                            RedFlagNotes = redFlagNotes,
                            Milestones = milestones,
                            AppStatuses = statuses,
                            HouseholdMembers = householdMembers,
                            CurrentGroups = currentGroups
                        })
                        .WithMediaRangeModel(mediaRange, new { success = true, notes = (string)cxt.Request.Form.notes, contact = redflagvoter })
                        .WithView("red-flag");
                }

                return Negotiate
                        .WithMediaRangeModel(mediaRange, new { success = false, reason = "Volunteer app id is not present or invalid" })
                        .WithView("red-flag");
            };

            Get["/place-volunteer/{id}"] = parameters =>
            {
                this.Bind<PlaceVolunteerViewModel>();
                int resultid = 0, volunteerid = 0;

                if (Int32.TryParse((string)parameters.id, out resultid) && Int32.TryParse((string)Response.Context.Request.Query.id, out volunteerid))
                {
                    
                    var volApp = appRepo.GetVolunteerApp(volunteerid);
                    var contactInfo = contactRepo.GetVolunteerInfo(volApp.Contact_ID);
                    var ministryTeams = groupsRepo.GetMinistryTeamsByProgramID(resultid);
                    var householdMembers = contactRepo.GetVolunteerHouseholdMembers(contactInfo.Household_ID.HasValue ? contactInfo.Household_ID.Value : 0);
                    var ministry = ministryRepo.GetMinisty(resultid);

                    return View["place-volunteer", new PlaceVolunteerViewModel
                    {
                        MinistryTeams = ministryTeams,
                        VolunteerApplication = volApp,
                        VolunteerInfo = contactInfo,
                        HouseholdMembers = householdMembers,
                        Ministry = ministry
                    }];
                }

                return View["place-volunteer"];
            };

            Post["/place-volunteer/{id}"] = parameters =>
            {
                MediaRange mediaRange = new MediaRange
                {
                    Type = new MediaType("application/json")
                };

                int resultid = 0, volunteerid = 0;

                this.Bind<PlaceVolunteerViewModel>();

                if (Int32.TryParse((string)parameters.id, out resultid) && Int32.TryParse((string)Response.Context.Request.Query.id, out volunteerid))
                {
                    var volApp = appRepo.GetVolunteerApp(volunteerid);
                    var contactInfo = contactRepo.GetVolunteerInfo(volApp.Contact_ID);
                    var householdMembers = contactRepo.GetVolunteerHouseholdMembers(contactInfo.Household_ID.HasValue ? contactInfo.Household_ID.Value : 0);
                    var ministry = ministryRepo.GetMinisty(resultid);

                    //set placement status (MQ programs table)
                    //set date placed
                    appRepo.SetVolunteerPlacementStatus(volApp, volApp.MinistryToVolunteerFor.Program_ID, 2, Request.Form.notes, (int)Request.Form.ministryteamid);

                    //add Milestone 9
                    milestoneRepo.AddNewMilestone(volApp.Participant_ID.Value, 9, volApp.MinistryToVolunteerFor.Program_ID);

                    //commService.SendEmail("", "", 0, null);

                    return Negotiate.WithModel(new PlaceVolunteerViewModel
                    {
                        VolunteerApplication = volApp,
                        VolunteerInfo = contactInfo,
                        HouseholdMembers = householdMembers,
                        Ministry = ministry
                    }).WithMediaRangeModel(mediaRange, new { success = true });
                }

                return Response.AsJson(new { success = false, reason = "Volunteer app id is empty or not valid" });
            };

            Get["/return-to-director/{id}"] = parameters =>
            {
                this.Bind<ReturnToDirectorViewModel>();
                int resultid = 0, volunteerid = 0;

                if (Int32.TryParse((string)parameters.id, out resultid) && Int32.TryParse((string)Response.Context.Request.Query.id, out volunteerid))
                {

                    var volApp = appRepo.GetVolunteerApp(volunteerid);
                    var contactInfo = volApp.Contact;
                    var householdMembers = contactRepo.GetVolunteerHouseholdMembers(contactInfo.Household_ID.HasValue ? contactInfo.Household_ID.Value : 0);
                    var ministry = ministryRepo.GetMinisty(resultid);

                    return View["return-to-director", new ReturnToDirectorViewModel
                    {

                        VolunteerApplication = volApp,
                        VolunteerInfo = contactInfo,
                        HouseholdMembers = householdMembers,
                        Ministry = ministry
                    }];
                }
                return View["return-to-director"];
            };

            Post["/return-to-director/{id}"] = parameters =>
            {
                MediaRange mediaRange = new MediaRange
                {
                    Type = new MediaType("application/json")
                };

                int resultid = 0, volunteerid = 0;

                this.Bind<ReturnToDirectorViewModel>();

                if (Int32.TryParse((string)parameters.id, out resultid) && Int32.TryParse((string)Response.Context.Request.Query.id, out volunteerid))
                {
                    var volApp = appRepo.GetVolunteerApp(volunteerid);
                    var contactInfo = volApp.Contact;
                    var householdMembers = contactRepo.GetVolunteerHouseholdMembers(contactInfo.Household_ID.HasValue ? contactInfo.Household_ID.Value : 0);
                    var ministry = ministryRepo.GetMinisty(resultid);

                    //don't set date field (do nothing here)

                    string fromQuery = Response.Context.Request.Query.from;

                    //there was a return address supplied, go ahead and set it
                    if(!string.IsNullOrEmpty(fromQuery))
                        commService.From = fromQuery;//.FromBase64().Decrypt();

                    //email Steve
                    commService.SendEmail(Globals.VolunteerEmail, "[NOT PLACED] " + volApp.Contact.Nickname + " " + volApp.Contact.Last_Name + " could not be placed in " + volApp.MinistryToVolunteerFor.Program_Name, Globals.ReturnToVolunteerDirectorTemplate, templateFields: new Dictionary<string, string> { { "NICKNAME", contactInfo.Nickname }, { "LASTNAME", contactInfo.Last_Name }, { "MINISTRYNAME", ministry.Program_Name }, { "NOTES", Request.Form.notes }, { "PICTURE", contactInfo.DefaultImageUrl } });
                    

                    //set placement status
                    appRepo.SetVolunteerPlacementStatus(volApp, volApp.MinistryToVolunteerFor.Program_ID, 3, string.Empty);

                    return Negotiate.WithModel(new ReturnToDirectorViewModel
                    {

                        VolunteerApplication = volApp,
                        VolunteerInfo = contactInfo,
                        HouseholdMembers = householdMembers
                    }).WithMediaRangeModel(mediaRange, new { success = true });
                }

                return Response.AsJson(new { success = false, reason = "Volunteer app id is empty or not valid" });
            };
        }
    }
}
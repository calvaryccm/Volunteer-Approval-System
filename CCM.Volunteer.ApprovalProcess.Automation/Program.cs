using CCM.Volunteer.ApprovalProcess.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CCM.Volunteer.ApprovalProcess.Core.Models;
using System.Configuration;
using CCM.Volunteer.ApprovalProcess.Core;

namespace CCM.Volunteer.ApprovalProcess.Automation
{
    public class Program
    {
        static ICommunicationsService CommunicationsService;
        static IVolunteerAppRepository VolunteerAppRepo;
        static IContactsRepository ContactsRepo;

        static void Main(string[] args)
        {

            if (args.Length < 1)
            {

                Console.Write(@"   _____ _____ __  __  __      __   _             _                              
  / ____/ ____|  \/  | \ \    / /  | |           | |                             
 | |   | |    | \  / |  \ \  / /__ | |_   _ _ __ | |_ ___  ___ _ __              
 | |   | |    | |\/| |   \ \/ / _ \| | | | | '_ \| __/ _ \/ _ \ '__|             
 | |___| |____| |  | |    \  / (_) | | |_| | | | | ||  __/  __/ |                
  \_____\_____|_|  |_|     \/ \___/|_|\__,_|_| |_|\__\___|\___|_|                
     /\                                   | | |  __ \                            
    /  \   _ __  _ __  _ __ _____   ____ _| | | |__) | __ ___   ___ ___  ___ ___ 
   / /\ \ | '_ \| '_ \| '__/ _ \ \ / / _` | | |  ___/ '__/ _ \ / __/ _ \/ __/ __|
  / ____ \| |_) | |_) | | | (_) \ V / (_| | | | |   | | | (_) | (_|  __/\__ \__ \
 /_/    \_\ .__/| .__/|_|  \___/ \_/ \__,_|_| |_|   |_|  \___/ \___\___||___/___/
          | |   | |                                                              
          |_|   |_|                      _                                       
     /\        | |                      | |                                      
    /  \  _   _| |_ ___  _ __ ___   __ _| |_ ___  _ __                           
   / /\ \| | | | __/ _ \| '_ ` _ \ / _` | __/ _ \| '__|                          
  / ____ \ |_| | || (_) | | | | | | (_| | || (_) | |                             
 /_/    \_\__,_|\__\___/|_| |_| |_|\__,_|\__\___/|_|                             
---------------------------------------------------------------------------

");


                Console.WriteLine("Please use one of the following arguments: \n");
                Console.WriteLine("'RF' - This sends the Red Flag emails.\n");
                Console.WriteLine("'AV' - This sends the Approve Volunteers email.\n");
                Console.WriteLine("Press any Key to Exit... \n");

                Console.ReadKey();
                return;
            }

            //Auto register all interfaces
            TinyIoC.TinyIoCContainer.Current.AutoRegister();

            CommunicationsService = TinyIoC.TinyIoCContainer.Current.Resolve<ICommunicationsService>();
            VolunteerAppRepo = TinyIoC.TinyIoCContainer.Current.Resolve<IVolunteerAppRepository>();
            ContactsRepo = TinyIoC.TinyIoCContainer.Current.Resolve<IContactsRepository>();

            switch (args[0])
            {
                case "SendRedFlagEmails":
                case "RF":
                    SendRedFlagEmails();
                    break;
                case "SendApproveVolunteerEmail":
                case "AV":
                    SendApproveVolunteerEmail();
                    break;
                default:
                    break;
            }
        }

        private static void SendApproveVolunteerEmail()
        {
            Trace.WriteLine("Starting to send approve volunteer email");

            //get apps that are awaiting to be cleared
            IEnumerable<MinistryQuestionaire> apps = VolunteerAppRepo.GetVolunteerAppsReadyForApproval();
            //get apps that need follow up
            var followUpApps = VolunteerAppRepo.GetVolunteerAppsNeedingFollowUp();

            //get people we're sending this to
            var receipients = ContactsRepo.GetContactsToSendApprovalEmails();

            //start building the email templates
            var clearingTableString =
                @"
                    <table style='font-family:sans-serif; text-align:left;border-bottom: 1px solid #edebeb; border-left: 1px solid #edebeb; border-right: 0px solid #edebeb;background-color: #edebeb;padding: 10px;'  border='0' align='center' cellpadding='3' cellspacing='0'>
                        <tr>
                            <th>Name</th>
                            <th style='background-color:#5cb85c'>Yes</th>
                            <th style='background-color:#f0ad4e'>Wait</th>
                            <th style='background-color:#d9534f'>No</th>
                            <th style='width:75px'>References Returned</th>
                            <th>Background Check</th>
                            <th>Action</th>
                        </tr>";


            var followUpTableString =
                @"
                    <table style='font-family:sans-serif; text-align:left;border-bottom: 1px solid #edebeb; border-left: 1px solid #edebeb; border-right: 1px solid #edebeb;background-color: #edebeb;padding: 10px;'  border='0' align='center' cellpadding='3' cellspacing='0'>
                        <tr>
                            <th>Name</th>
                            <th style='background-color:#5cb85c'>Yes</th>
                            <th style='background-color:#f0ad4e'>Wait</th>
                            <th style='background-color:#d9534f'>No</th>
                            <th style='width:75px'>References Returned</th>
                            <th>Background Check</th>
                            <th style='width:100px'>Action</th>
                        </tr>"; 
            //for each volunteer application that's ready for approval
            foreach (var app in apps)
            {

                Trace.WriteLine("Processing app - id: " + app.Ministry_Questionaire_ID);

                //url for contact image/picture
                var pictureUrl = string.Format(Constants.ContactImageBaseAppUrl, app.Contact_ID);

                //url for clickthrough link
                var url = Constants.VolunteerAppBaseUrl + "/approval-process/approve-deny/" + app.Ministry_Questionaire_ID.ToString().Encrypt().ToBase64();

                //get all people who voted on this application
                var redFlagNotes = VolunteerAppRepo.GetRedFlagNotes(app.Ministry_Questionaire_ID);

                //count how many people voted yes for this application
                int yesVotes = redFlagNotes.Where(f => f.Details.MPRouter_Item_Status_ID == 1).Count();
                int holdVotes = redFlagNotes.Where(f => f.Details.MPRouter_Item_Status_ID == 2).Count();
                int noVotes = redFlagNotes.Where(f => f.Details.MPRouter_Item_Status_ID == 3).Count();

                //count how many references responded
                int referencesCount = app.ReferenceList.Where(f => f.Date_Completed.HasValue).Count();

                //determine background status
                string bkstatus = "<td style='border-bottom: 2px solid #000; background-color:#5bc0de;text-align: center; '>Not Done Yet</td>";

                if(app.Background_Status.HasValue)
                {
                    switch (app.Background_Status.Value)
                    {
                        case 1:
                            bkstatus = "<td style='border-bottom: 2px solid #000;background-color:#5cb85c;text-align: center; '>Pass</td>";
                        break;

                        case 2:
                        bkstatus = "<td style='border-bottom: 2px solid #000;background-color:#d9534f;text-align: center; '>Fail</td>";
                        break;

                        case 3:
                        bkstatus = "<td style='border-bottom: 2px solid #000;background-color:#f0ad4e;text-align: center; '>Hold</td>";
                        break;

                        case 4:
                        bkstatus = "<td style='border-bottom: 2px solid #000;background-color:#5bc0de;text-align: center; '>Not Done Yet</td>";
                        break;
                    }
                }

                clearingTableString += "<tr><td style='border-bottom: 2px solid #000;'>" + app.Contact.Nickname + " " + app.Contact.Last_Name + "</td>" + "<td style='border-bottom: 2px solid #000;background-color:#5cb85c;text-align: center; '>" + yesVotes + "</td>" + "<td style='border-bottom: 2px solid #000; background-color:#f0ad4e;text-align: center; '>" + holdVotes + "</td>" + "<td style='border-bottom: 2px solid #000;background-color:#d9534f;text-align: center; '>" + noVotes + "</td>" + "<td style='border-bottom: 2px solid #000;width:75px;text-align: center; '>" + referencesCount + "</td>" + bkstatus + "<td style='border-bottom: 2px solid #000;width:100px'>" + "<a href='" + url + "'>" + "<img src='https://s3.amazonaws.com/CCMEmailObjects/RedFlagEmail/MakeDecision.png' alt='Decision Button' />" + "</a>" + "</td>" + "</tr>";
            }

            clearingTableString += "</table>";

            foreach(var followUpApp in followUpApps)
            {

                Trace.WriteLine("Processing app - id: " + followUpApp.Ministry_Questionaire_ID);

                //url for contact image/picture
                var pictureUrl = string.Format(Constants.ContactImageBaseAppUrl, followUpApp.Contact_ID);

                //url for clickthrough link
                var url = Constants.VolunteerAppBaseUrl + "/approval-process/approve-deny/" + followUpApp.Ministry_Questionaire_ID.ToString().Encrypt().ToBase64();

                //get all people who voted on this application
                var redFlagNotes = VolunteerAppRepo.GetRedFlagNotes(followUpApp.Ministry_Questionaire_ID);

                //count how many people voted yes for this application

                int yesVotes = redFlagNotes.Where(f => f.Details.MPRouter_Item_Status_ID == 1).Count();
                int holdVotes = redFlagNotes.Where(f => f.Details.MPRouter_Item_Status_ID == 2).Count();
                int noVotes = redFlagNotes.Where(f => f.Details.MPRouter_Item_Status_ID == 3).Count();

                //count how many references responded
                int referencesCount = followUpApp.ReferenceList.Where(f => f.Date_Completed.HasValue).Count();

                //determine background status
                string bkstatus = "<td style='border-bottom: 2px solid #000; background-color:#5bc0de;text-align: center; '>Not Done Yet</td>";

                if (followUpApp.Background_Status.HasValue)
                {
                    switch (followUpApp.Background_Status.Value)
                    {
                        case 1:
                            bkstatus = "<td style='border-bottom: 2px solid #000;background-color:#5cb85c;text-align: center; '>Pass</td>";
                            break;

                        case 2:
                            bkstatus = "<td style='border-bottom: 2px solid #000;background-color:#d9534f;text-align: center; '>Fail</td>";
                            break;

                        case 3:
                            bkstatus = "<td style='border-bottom: 2px solid #000;background-color:#f0ad4e;text-align: center; '>Hold</td>";
                            break;

                        case 4:
                            bkstatus = "<td style='border-bottom: 2px solid #000;background-color:#5bc0de;text-align: center; '>Not Done Yet</td>";
                            break;

                        default:
                            bkstatus = "<td style='border-bottom: 2px solid #000;background-color:#5bc0de;text-align: center; '>Not Done Yet</td>";
                            break;
                    }
                }

                followUpTableString += "<tr><td style='border-bottom: 2px solid #000;'>" + followUpApp.Contact.Nickname + " " + followUpApp.Contact.Last_Name + "</td>" + "<td style='border-bottom: 2px solid #000;background-color:#5cb85c;text-align: center; '>" + yesVotes + "</td>" + "<td style='border-bottom: 2px solid #000;background-color:#f0ad4e;text-align: center; '>" + holdVotes + "</td>" + "<td style='border-bottom: 2px solid #000;background-color:#d9534f;text-align: center; '>" + noVotes + "</td>" + "<td style='border-bottom: 2px solid #000; width:75px;text-align: center; '>" + referencesCount + "</td>" + bkstatus + "<td style='border-bottom: 2px solid #000;'>" + "<a href='" + url + "'>" + "<img src='https://s3.amazonaws.com/CCMEmailObjects/RedFlagEmail/MakeDecision.png' alt='Decision Button' />" + "</a>" + "</td>" + "</tr>";
            }

            followUpTableString += "</table>";


            foreach (var receipient in receipients)
            {
                //check if email is null or empty
                if (string.IsNullOrEmpty(receipient.Email_Address))
                {
                    Trace.TraceError("Approve volunteer receipient doesn't have an email addrress. Name: " + receipient.Nickname + " " + receipient.Last_Name);
                    continue;
                }

                //we need to check if valid email
                if (!IsValidEmail(receipient.Email_Address))
                {
                    Trace.TraceError("Approve volunteer receipient doesn't have a valid email addrress. Name: " + receipient.Nickname + " " + receipient.Last_Name);
                    continue;
                }

                Trace.WriteLine("Sending review email to " + receipient.Nickname + " " + receipient.Last_Name);

                //build template
                var fields = new Dictionary<string, string>
                {
                    {"CLEARINGTABLE", clearingTableString},
                    {"FOLLOWUPTABLE", followUpTableString}
                };


                if (!CommunicationsService.SendEmail(receipient.Email_Address, "[CCM Red Flag] Clearing and Follow Up", Convert.ToInt32(ConfigurationManager.AppSettings["ClearingAndFollowUpTemplate"]), templateFields: fields))
                    Trace.TraceError("Email failed to send. Reason: " + CCM.Volunteer.ApprovalProcess.Core.ErrorLogging.GetLatestError().Message);
            }


            //throw new NotImplementedException();
        }

        private static void SendRedFlagEmails()
        {
            try
            {
                Trace.WriteLine("Starting to send Red Flag emails");

                IEnumerable<MinistryQuestionaire> apps = VolunteerAppRepo.GetVolunteerAppsForRedFlag();
                IEnumerable<Contact> receipients = ContactsRepo.GetContactsToSendRedFlagEmails();

                Trace.WriteLine("Retrieved apps and email receipients");

                //for each volunteer application that needs to be reviewed
                foreach (var app in apps)
                {
                    Trace.WriteLine("Processing app - id: " + app.Ministry_Questionaire_ID);

                    var pictureUrl = string.Format(Constants.ContactImageBaseAppUrl, app.Contact_ID);
                    var url = Constants.VolunteerAppBaseUrl + "/approval-process/red-flag/" + app.Ministry_Questionaire_ID.ToString().Encrypt().ToBase64();

                    //for each person on the review panel (Red Flag Volunteer Application)
                    foreach (var receipient in receipients)
                    {
                        //check if email is null or empty
                        if (string.IsNullOrEmpty(receipient.Email_Address))
                        {
                            Trace.TraceError("Red Flag receipient doesn't have an email addrress. Name: " + receipient.Nickname + " " + receipient.Last_Name);
                            continue;
                        }

                        //we need to check if valid email
                        if (!IsValidEmail(receipient.Email_Address))
                        {
                            Trace.TraceError("Red Flag receipient doesn't have a valid email addrress. Name: " + receipient.Nickname + " " + receipient.Last_Name);
                            continue;
                        }

                        Trace.WriteLine("Sending review email to " + receipient.Nickname + " " + receipient.Last_Name);

                        var queryContactID = "&cid=" + receipient.Contact_ID.ToString().Encrypt().ToBase64();

                        var fields = new Dictionary<string, string>{
                    {"NICKNAME", app.Contact.Nickname}, 
                    {"LASTNAME", app.Contact.Last_Name},
                    {"PICTURE", pictureUrl},
                    {"FOLLOWUPDATE", app.Start_Date.AddDays(3).ToString("MM/dd/yyyy")}, 

                    {"CAMPUS", app.Campus != null ? app.Campus.Congregation_Name : "No campus listed"},

                    {"MINISTRY", app.MinistryToVolunteerFor.Program_Name},
                    {"MINISTRYCOMMENT", app.New_Volunteer_Ministry_Comment ?? string.Empty},

                    {"DATESAVED", app.When_Saved.Value.ToString("MM/yyyy")},
                    {"DATESAVEDAGE", ((int) Math.Floor((double)(((app.When_Saved.Value) - (app.Contact.Date_of_Birth.Value)).Days / 365))).ToString()},

                    {"BAPTISMDATE", app.Baptized_When.Value.ToString("MM/yyyy")},
                    {"BAPTISMAGE", ((int) Math.Floor((double)(((app.Baptized_When.Value) - (app.Contact.Date_of_Birth.Value)).Days / 365))).ToString()},

                    {"CCMSTARTDATE", app.Started_Attending_Date.Value.ToString("MM/yyyy")},
                    {"CCMSTARTAGE", (app.Started_Attending_Date.Value.ToRelativeDateTime())},

                    {"AGE", app.Contact.__Age.ToString()},

                    {"STATEMENTOFFAITH", app.Statement_of_Faith == true ? "Agree" : "Disagree"},

                    {"CHURCHFREQUENCY", app.HowOftenDoYouAttend.Frequency_Name},

                    {"MARITALSTATUS", app.Contact.MaritalStatus != null ? app.Contact.MaritalStatus.Marital_Status : string.Empty },

                    {"TESTIMONY", app.Testimony},
                    {"DEVOLIFE", app.Devotional_Life},
                    {"TALENTS", app.Talents_Skills_Hobbies},
                    {"BIBLECOLLEGE", app.Bible_College_Workshops_Counseling},
                    {"ADDITIONALINFO", app.Additional_Info},
                    {"DECISION-YES", url + "?d=0" + queryContactID},
                    {"DECISION-HOLD", url + "?d=1" + queryContactID},
                    {"DECISION-NO", url + "?d=2" + queryContactID}
                    };
                        var templateID = Convert.ToInt32(ConfigurationManager.AppSettings["RedFlagTemplateNOSOF"]);

                        if (!app.Statement_of_Faith.Value)
                        {
                            fields.Add("STATEMENTOFFAITHREASON", app.Not_Agree_Reason);
                            templateID = Convert.ToInt32(ConfigurationManager.AppSettings["RedFlagTemplate"]);
                        }


                        if (!CommunicationsService.SendEmail(receipient.Email_Address, "[CCM Red Flag] - " + app.Contact.Nickname + " " + app.Contact.Last_Name + " - " + app.MinistryToVolunteerFor.Program_Name, templateID, templateFields: fields))
                            Trace.TraceError("Email failed to send. Reason: " + CCM.Volunteer.ApprovalProcess.Core.ErrorLogging.GetLatestError().Message);
                    }


                    //send out reference checks  (We Need your help email)
                    foreach (var reference in app.ReferenceList)
                    {
                        if (string.IsNullOrEmpty(reference.Email))
                        {
                            Trace.TraceError("Reference check receipient doesn't have an email addrress. Name: " + reference.Name);
                            continue;
                        }

                        //we need to check if valid email
                        if (!IsValidEmail(reference.Email))
                        {
                            Trace.TraceError("Reference check receipient doesn't have a valid email addrress. Name: " + reference.Name + " Email: " + reference.Email);
                            continue;
                        }

                        var referenceUrl = Constants.VolunteerAppBaseUrl + "/approval-process/reference-check/" + reference.MQ_Reference_ID.ToString().Encrypt().ToBase64();

                        Trace.WriteLine("Sending reference check email to " + reference.Name + " at " + reference.Email);

                        var fields = new Dictionary<string, string>{
                            {"NICKNAME", app.Contact.Nickname}, 
                            {"LASTNAME", app.Contact.Last_Name},
                            {"GENDER", app.Contact.Gender_ID == 1 ? "him" : "her"}, 
                            {"PICTURE", pictureUrl},
                            {"CLICK2URL", referenceUrl}
                        };

                        if (!CommunicationsService.SendEmail(reference.Email, "Personal reference for " + app.Contact.Nickname + " " + app.Contact.Last_Name, Convert.ToInt32(ConfigurationManager.AppSettings["ReferenceTemplate"]), templateFields: fields))
                            Trace.TraceError("Email failed to send. Reason: " + CCM.Volunteer.ApprovalProcess.Core.ErrorLogging.GetLatestError().Message);

                    }

                    //update application
                    app.Red_Flag_Submitted = DateTime.Now;

                    VolunteerAppRepo.UpdateVolunteerApp(app);
                }
            }
            catch (Exception exception)
            {
                Trace.TraceError("An error occured: " + exception.Message);
            }

        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}

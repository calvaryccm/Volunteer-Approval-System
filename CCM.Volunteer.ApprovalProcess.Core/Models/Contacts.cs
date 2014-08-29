using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CCM.Volunteer.ApprovalProcess.Core.Models
{

        /// <summary>
        /// A class which represents the Contacts table.
        /// </summary>
        [Table("Contacts")]
        public partial class Contact
        {
            [Key]
            public virtual int Contact_ID { get; set; }
            public virtual bool Company { get; set; }
            public virtual string Company_Name { get; set; }
            public virtual string Display_Name { get; set; }
            public virtual int? Prefix_ID { get; set; }
            public virtual string First_Name { get; set; }
            public virtual string Middle_Name { get; set; }
            public virtual string Last_Name { get; set; }
            public virtual int? Suffix_ID { get; set; }
            public virtual string Nickname { get; set; }
            public virtual DateTime? Date_of_Birth { get; set; }
            public virtual int? Gender_ID { get; set; }
            public virtual int? Marital_Status_ID { get; set; }
            public virtual int Contact_Status_ID { get; set; }
            public virtual int? Household_ID { get; set; }
            public virtual int? Household_Position_ID { get; set; }
            public virtual int? Participant_Record { get; set; }
            public virtual int? Donor_Record { get; set; }
            public virtual string Email_Address { get; set; }
            public virtual bool Bulk_Email_Opt_Out { get; set; }
            public virtual string Mobile_Phone { get; set; }
            public virtual bool? Do_Not_Text { get; set; }
            public virtual string Company_Phone { get; set; }
            public virtual string Pager_Phone { get; set; }
            public virtual string Fax_Phone { get; set; }
            public virtual int? User_Account { get; set; }
            public virtual string Web_Page { get; set; }
            public virtual int? Industry_ID { get; set; }
            public virtual int? Occupation_ID { get; set; }
            [Editable(false)]
            public virtual string SSN_EIN { get; set; }
            public virtual DateTime? Anniversary_Date { get; set; }
            public virtual short? HS_Graduation_Year { get; set; }
            public virtual int Domain_ID { get; set; }
            //public virtual string __Memo { get; set; }
            //public virtual DateTime? __SetupDate { get; set; }
            //public virtual int? __ShelbyID { get; set; }
            //public virtual Guid Contact_GUID { get; set; }
            public virtual string Facebook_Account { get; set; }
            public virtual string MySpace_Account { get; set; }
            public virtual string ID_Card { get; set; }
            //public virtual int? __Age { get; set; }
            public virtual string Twitter_Account { get; set; }
            public virtual string IM_Account { get; set; }
            public virtual string LinkedIn_Account { get; set; }
            //public virtual int? __PSID { get; set; }
            //public virtual string __Shelby_Mobile { get; set; }
            //public virtual string __Shelby_Email { get; set; }
            //public virtual bool? __PS_DOB_Updated { get; set; }
            //public virtual string __ShelbyFirstMiddle { get; set; }
            //public virtual string __ImportLastName { get; set; }
            //public virtual string __ImportFirstName { get; set; }
            //public virtual string __ImportNickname { get; set; }
            public virtual bool? Email_Unlisted { get; set; }
            public virtual bool? Mobile_Phone_Unlisted { get; set; }
            public virtual bool? Remove_From_Directory { get; set; }
            public virtual Guid? Subscription_GUID { get; set; }
            public virtual bool? HR_Data { get; set; }
            public virtual int? Staff_Type_ID { get; set; }
            public virtual int? Staff_Status_ID { get; set; }
            public virtual int? Supervisor { get; set; }
        }
    
}
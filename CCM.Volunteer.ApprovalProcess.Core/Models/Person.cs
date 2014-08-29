using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCM.Volunteer.ApprovalProcess.Core.Models
{
    public class Person
    {
        public int ID { get; set; }
        public string FirstName { get; set; }
        public string NickName { get; set; }
        public string LastName { get; set; }
        public Address Residence { get; set; }
        public string EmailAddress { get; set; }
        public string ImageUrl { get; set; }
        public string MobilePhone { get; set; }
        public string HomePhone { get; set; }
        public string FullName { get; set; }
        public DateTime? BirthDate { get; set; }
        public Gender Gender { get; set; }
        public Prefix Prefix { get; set; }
        public Suffix Suffix { get; set; }
        public MaritalStatus MaritalStatus { get; set; }
    }
}

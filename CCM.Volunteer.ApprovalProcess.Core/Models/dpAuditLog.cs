using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CCM.Volunteer.ApprovalProcess.Core.Models
{
    /// <summary>
    /// A class which represents the dp_Audit_Log table.
    /// </summary>
    [Table("dp_Audit_Log")]
    public partial class dpAuditLog
    {
        [Key]
        public virtual int Audit_Item_ID { get; set; }
        public virtual string Table_Name { get; set; }
        public virtual int Record_ID { get; set; }
        public virtual string Audit_Description { get; set; }
        public virtual string User_Name { get; set; }
        public virtual int User_ID { get; set; }
        public virtual DateTime Date_Time { get; set; }
    }
}
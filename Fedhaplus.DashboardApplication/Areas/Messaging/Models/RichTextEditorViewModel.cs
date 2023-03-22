using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using System.Web.Mvc;

namespace Fedhaplus.DashboardApplication.Areas.Messaging.Models
{
    public class RichTextEditorViewModel
    {
        [DataMember]
        [Display(Name = "Id")]
        public Guid Id { get; set; }

        [DataMember]
        [Display(Name = "Company")]
        public Guid? CompanyId { get; set; }

        [DataMember]
        [Display(Name = "Company Name")]
        public string CompanyDescription { get; set; }

        [DataMember]
        [Display(Name = "From")]
        [Required]
        public string MailMessageFrom { get; set; }

        [DataMember]
        [Display(Name = "Recipient")]
        [Required]
        [RegularExpression(@"\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*", ErrorMessage = "E-mail is not valid")]
        public string MailMessageTo { get; set; }

        [DataMember]
        [Display(Name = "CC")]
        public string MailMessageCC { get; set; }

        [DataMember]
        [Display(Name = "Subject")]
        [Required]
        public string MailMessageSubject { get; set; }

        [DataMember]
        [Display(Name = "Message")]
        [Required]
        [AllowHtml]
        public string MailMessageBody { get; set; }

        [DataMember]
        [Display(Name = "Message")]
        public string MaskedMailMessageBody { get; set; }

        [DataMember]
        [Display(Name = "Is Body Html?")]
        public bool MailMessageIsBodyHtml { get; set; }

        [DataMember]
        [Display(Name = "DLR Status")]
        public int MailMessageDLRStatus { get; set; }

        [DataMember]
        [Display(Name = "Origin")]
        public int MailMessageOrigin { get; set; }

        [DataMember]
        [Display(Name = "Priority")]
        public int MailMessagePriority { get; set; }

        [DataMember]
        [Display(Name = "Send Retry")]
        public int MailMessageSendRetry { get; set; }

        [DataMember]
        [Display(Name = "Security Critical")]
        public bool MailMessageSecurityCritical { get; set; }

        [DataMember]
        [Display(Name = "Attachments")]
        public string MailMessageAttachments { get; set; }

        [DataMember]
        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }

        [DataMember]
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }
    }
}
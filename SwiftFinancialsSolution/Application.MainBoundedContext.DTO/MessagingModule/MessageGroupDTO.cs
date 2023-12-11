
using Application.Seedwork;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Application.MainBoundedContext.DTO.MessagingModule
{
    public class MessageGroupDTO : BindingModelBase<MessageGroupDTO>
    {
        public MessageGroupDTO()
        {
            AddAllAttributeValidators();
        }

        [DataMember]
        [Display(Name = "Id")]
        public Guid Id { get; set; }

        [DataMember]
        [Display(Name = "Name")]
        [Required]
        public string Description { get; set; }

        [DataMember]
        [Display(Name = "Target")]
        public int Target { get; set; }

        [DataMember]
        [Display(Name = "Target")]
        public string TargetDescription
        {
            get
            {
                return Enum.IsDefined(typeof(MessageGroupTarget), Target) ? EnumHelper.GetDescription((MessageGroupTarget)Target) : string.Empty;
            }
        }

        [DataMember]
        [Display(Name = "Target Values")]
        [Required]
        public string TargetValues { get; set; }

        [DataMember]
        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }

        [DataMember]
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }
    }
}

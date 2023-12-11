using Application.Seedwork;

using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Application.MainBoundedContext.DTO.AdministrationModule
{
    public class SystemPermissionTypeInRoleDTO : BindingModelBase<SystemPermissionTypeInRoleDTO>
    {
        public SystemPermissionTypeInRoleDTO()
        {
            AddAllAttributeValidators();
        }

        [DataMember]
        [Display(Name = "Id")]
        public Guid Id { get; set; }

        [DataMember]
        [Display(Name = "Operation")]
        public int SystemPermissionType { get; set; }

        [DataMember]
        [Display(Name = "Operation")]
        public string SystemPermissionTypeDescription
        {
            get
            {
                return Enum.IsDefined(typeof(SystemPermissionType), SystemPermissionType) ? EnumHelper.GetDescription((SystemPermissionType)SystemPermissionType) : string.Empty;
            }
        }

        [DataMember]
        [Display(Name = "Role Name")]
        public string RoleName { get; set; }

        [DataMember]
        [Display(Name = "Required Approvers")]
        public int RequiredApprovers { get; set; }

        [DataMember]
        [Display(Name = "Approval Priority")]
        public int ApprovalPriority { get; set; }

        [DataMember]
        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }

        [DataMember]
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }
    }
}

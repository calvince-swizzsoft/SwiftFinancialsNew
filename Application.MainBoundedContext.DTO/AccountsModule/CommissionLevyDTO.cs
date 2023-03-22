using Application.Seedwork;
using Infrastructure.Crosscutting.Framework.Attributes;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Application.MainBoundedContext.DTO.AccountsModule
{
    public class CommissionLevyDTO : BindingModelBase<CommissionLevyDTO>
    {
        public CommissionLevyDTO()
        {
            AddAllAttributeValidators();
        }

        [DataMember]
        [Display(Name = "Id")]
        public Guid Id { get; set; }

        [DataMember]
        [Display(Name = "Commission")]
        [ValidGuid]
        public Guid CommissionId { get; set; }

        [DataMember]
        [Display(Name = "Commission")]
        public CommissionDTO Commission { get; set; }

        [DataMember]
        [Display(Name = "Levy")]
        [ValidGuid]
        public Guid LevyId { get; set; }

        [DataMember]
        [Display(Name = "Levy")]
        public LevyDTO Levy { get; set; }

        [DataMember]
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }
    }
}

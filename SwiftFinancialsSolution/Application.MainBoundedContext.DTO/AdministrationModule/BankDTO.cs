using Application.Seedwork;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Application.MainBoundedContext.DTO.AdministrationModule
{
    public class BankDTO : BindingModelBase<BankDTO>
    {
        public BankDTO()
        {
            AddAllAttributeValidators();
        }

        [DataMember]
        [Display(Name = "Id")]
        public Guid Id { get; set; }

        [DataMember]
        [Display(Name = "Code")]
        public int Code { get; set; }

        [DataMember]
        [Display(Name = "Code")]
        public string PaddedCode
        {
            get
            {
                return string.Format("{0}", Code).PadLeft(2, '0');
            }
        }

        [DataMember]
        [Display(Name = "Name")]
        [Required]
        public string Description { get; set; }

        [DataMember]
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        HashSet<BankBranchDTO> _bankBranches;
        [DataMember]
        [Display(Name = "Bank Branches")]
        public virtual ICollection<BankBranchDTO> BankBranches
        {
            get
            {
                if (_bankBranches == null)
                {
                    _bankBranches = new HashSet<BankBranchDTO>();
                }
                return _bankBranches;
            }
            private set
            {
                _bankBranches = new HashSet<BankBranchDTO>(value);
            }
        }
    }
}

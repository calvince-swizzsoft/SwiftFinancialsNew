using Application.MainBoundedContext.DTO.AccountsModule;
using Application.Seedwork;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Application.MainBoundedContext.DTO.AdministrationModule
{
    public class BankDTO : BindingModelBase<BankDTO>
    {
        public BankDTO()
        {
            AddAllAttributeValidators();
            BankBranche = new ObservableCollection<BankBranchDTO>();
        }

        [DataMember]
        [Display(Name = "Id")]
        public Guid Id { get; set; }

        [DataMember]
        [Display(Name = "Code")]
        public int Code { get; set; }

        [DataMember]
        [Display(Name = "Padded Code")]
        public string PaddedCode
        {
            get
            {
                return Code.ToString().PadLeft(2, '0');
            }
        }

        [DataMember]
        [Display(Name = "Name")]
        [Required]
        public string Description { get; set; }

        [DataMember]
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        // Using ObservableCollection for BankBranches
        private ObservableCollection<BankBranchDTO> _bankBranches;
        public BankBranchDTO BankBranches { get; set; }
     [DataMember]
        [Display(Name = "Bank Branches")]
        public ObservableCollection<BankBranchDTO> BankBranche
        {
            get
            {
                if (_bankBranches == null)
                {
                    _bankBranches = new ObservableCollection<BankBranchDTO>();
                }
                return _bankBranches;
            }
            set
            {
                _bankBranches = value;
            }
        }
        public ObservableCollection<BankBranchDTO> Bankbranch;
            // Other properties and methods...
    }
}

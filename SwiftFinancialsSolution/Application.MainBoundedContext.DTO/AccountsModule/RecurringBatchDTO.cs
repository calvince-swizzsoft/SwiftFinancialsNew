using Application.Seedwork;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Application.MainBoundedContext.DTO.AccountsModule
{
    public class RecurringBatchDTO : BindingModelBase<RecurringBatchDTO>
    {
        public RecurringBatchDTO()
        {
            AddAllAttributeValidators();
        }

        [DataMember]
        [Display(Name = "Id")]
        public Guid Id { get; set; }

        [DataMember]
        [Display(Name = "Branch")]
        public Guid? BranchId { get; set; }

        [Display(Name = "Branch")]
        public string BranchDescription { get; set; }

        [DataMember]
        [Display(Name = "Posting Period")]
        public Guid? PostingPeriodId { get; set; }

        [DataMember]
        [Display(Name = "Posting Period")]
        public string PostingPeriodDescription { get; set; }

        [DataMember]
        [Display(Name = "Batch Number")]
        public int BatchNumber { get; set; }

        [DataMember]
        [Display(Name = "Batch Number")]
        public string PaddedBatchNumber
        {
            get
            {
                return string.Format("{0}", BatchNumber).PadLeft(7, '0');
            }
        }

        [DataMember]
        [Display(Name = "Type")]
        public int Type { get; set; }

        [DataMember]
        [Display(Name = "Batch Type")]
        public string TypeDescription
        {
            get
            {
                return Enum.IsDefined(typeof(RecurringBatchType), Type) ? EnumHelper.GetDescription((RecurringBatchType)Type) : string.Empty;
            }
        }

        [DataMember]
        [Display(Name = "Month")]
        public int Month { get; set; }

        [DataMember]
        [Display(Name = "Month")]
        public string MonthDescription
        {
            get
            {
                return Enum.IsDefined(typeof(Month), Month) ? EnumHelper.GetDescription((Month)Month) : string.Empty;
            }
        }

        [DataMember]
        [Display(Name = "Interest Capitalization Months")]
       
        public int InterestCapitalizationMonths { get; set; }
        
        [DataMember]
        [Display(Name = "Reference")]
        public string Reference { get; set; }

        [DataMember]
        [Display(Name = "Priority")]
        public int Priority { get; set; }

        [DataMember]
        [Display(Name = "Priority")]
        public string PriorityDescription
        {
            get
            {
                return Enum.IsDefined(typeof(QueuePriority), Priority) ? EnumHelper.GetDescription((QueuePriority)Priority) : string.Empty;
            }
        }

        [DataMember]
        [Display(Name = "Status")]
        public int Status { get; set; }

        [DataMember]
        [Display(Name = "Status")]
        public string StatusDescription
        {
            get
            {
                return Enum.IsDefined(typeof(BatchStatus), Status) ? EnumHelper.GetDescription((BatchStatus)Status) : string.Empty;
            }
        }

        [DataMember]
        [Display(Name = "Enforce Month Value Date?")]
        public bool EnforceMonthValueDate { get; set; }

        [DataMember]
        [Display(Name = "Posted Entries")]
        public string PostedEntries { get; set; }

        [DataMember]
        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }

        [DataMember]
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        public List<RecurringBatchDTO> SelectedRows { get; set; }
        [DataMember]
        [Display(Name = "CheckedCheckboxIds?")]
        public bool CheckedCheckboxIds { get; set; }
        [DataMember]
        [Display(Name = "CheckedRowsData?")]
        public bool CheckedRowsData { get; set; }
        public List<RecurringBatchEntryDTO> RecouringBatchEntries { get; set; }

       
    }
   
}

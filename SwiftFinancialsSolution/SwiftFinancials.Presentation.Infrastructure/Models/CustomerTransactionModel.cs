using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Application.Seedwork;
using Infrastructure.Crosscutting.Framework.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace SwiftFinancials.Presentation.Infrastructure.Models
{
    public class CustomerTransactionModel : BindingModelBase<CustomerTransactionModel>
    {
        public CustomerTransactionModel()
        {
            ApportionmentWrapper = new ApportionmentWrapper();
            CustomerAccount = new CustomerAccountDTO();
            CashDepositRequest = new CashDepositRequestDTO();
            Teller = new TellerDTO();
            AddAllAttributeValidators();
        }

        Guid _branchId;
        [Display(Name = "Branch")]
        [ValidGuid]
        public Guid BranchId
        {
            get { return _branchId; }
            set
            {
                if (_branchId != value)
                {
                    _branchId = value;
                    OnPropertyChanged(() => BranchId);
                }
            }
        }

        Guid _postingPeriodId;
        [Display(Name = "Posting Period")]
        [ValidGuid]
        public Guid PostingPeriodId
        {
            get { return _postingPeriodId; }
            set
            {
                if (_postingPeriodId != value)
                {
                    _postingPeriodId = value;
                    OnPropertyChanged(() => PostingPeriodId);
                }
            }
        }

        decimal _totalValue;
        [Display(Name = "Total Value")]
        [RegularExpression(@"^\s*(?=.*[1-9])\d*(?:\.\d{1,2})?\s*$", ErrorMessage = "Transaction amount must be greater than zero!")]
        public decimal TotalValue
        {
            get { return _totalValue; }
            set
            {
                if (_totalValue != value)
                {
                    _totalValue = value;
                    OnPropertyChanged(() => TotalValue);
                }
            }
        }

        string _primaryDescription;
        [Display(Name = "Primary Description")]
        [Required]
        [StringLength(256)]
        public string PrimaryDescription
        {
            get { return _primaryDescription; }
            set
            {
                if (_primaryDescription != value)
                {
                    _primaryDescription = value;
                    OnPropertyChanged(() => PrimaryDescription);
                }
            }
        }

        string _secondaryDescription;
        [Display(Name = "Secondary Description")]
        [StringLength(256)]                                     
        public string SecondaryDescription
        {
            get { return _secondaryDescription; }
            set
            {
                if (_secondaryDescription != value)
                {
                    _secondaryDescription = value;
                    OnPropertyChanged(() => SecondaryDescription);
                }
            }
        }

        string _reference;
        [Display(Name = "Reference")]
        [StringLength(256)]
        public string Reference
        {
            get { return _reference; }
            set
            {
                if (_reference != value)
                {
                    _reference = value;
                    OnPropertyChanged(() => Reference);
                }
            }
        }

        int _moduleNavigationItemCode;
        [Display(Name = "Module Navigation Item Code")]
        public int ModuleNavigationItemCode
        {
            get { return _moduleNavigationItemCode; }
            set
            {
                if (_moduleNavigationItemCode != value)
                {
                    _moduleNavigationItemCode = value;
                    OnPropertyChanged(() => ModuleNavigationItemCode);
                }
            }
        }

        Guid _creditChartOfAccountId;
        [Display(Name = "Credit G/L Account Id")]
        [ValidGuid]
        public Guid CreditChartOfAccountId
        {
            get { return _creditChartOfAccountId; }
            set
            {
                if (_creditChartOfAccountId != value)
                {
                    _creditChartOfAccountId = value;
                    OnPropertyChanged(() => CreditChartOfAccountId);
                }
            }
        }

        Guid _debitChartOfAccountId;
        [Display(Name = "Debit G/L Account Id")]
        [ValidGuid]
        public Guid DebitChartOfAccountId
        {
            get { return _debitChartOfAccountId; }
            set
            {
                if (_debitChartOfAccountId != value)
                {
                    _debitChartOfAccountId = value;
                    OnPropertyChanged(() => DebitChartOfAccountId);
                }
            }
        }

        Guid _creditCustomerAccountId;
        [Display(Name = "Credit Customer Account")]
        [ValidGuid]
        public Guid CreditCustomerAccountId
        {
            get { return _creditCustomerAccountId; }
            set
            {
                if (_creditCustomerAccountId != value)
                {
                    _creditCustomerAccountId = value;
                    OnPropertyChanged(() => CreditCustomerAccountId);
                }
            }
        }

        CustomerAccountDTO _creditCustomerAccount;
        [Display(Name = "Credit Customer Account")]
        public CustomerAccountDTO CreditCustomerAccount
        {
            get { return _creditCustomerAccount; }
            set
            {
                if (_creditCustomerAccount != value)
                {
                    _creditCustomerAccount = value;
                    OnPropertyChanged(() => CreditCustomerAccount);
                }
            }
        }

        Guid _debitCustomerAccountId;
        [Display(Name = "Debit Customer Account")]
        [ValidGuid]
        public Guid DebitCustomerAccountId
        {
            get { return _debitCustomerAccountId; }
            set
            {
                if (_debitCustomerAccountId != value)
                {
                    _debitCustomerAccountId = value;
                    OnPropertyChanged(() => DebitCustomerAccountId);
                }
            }
        }

        CustomerAccountDTO _debitCustomerAccount;
        [Display(Name = "Debit Customer Account")]
        public CustomerAccountDTO DebitCustomerAccount
        {
            get { return _debitCustomerAccount; }
            set
            {
                if (_debitCustomerAccount != value)
                {
                    _debitCustomerAccount = value;
                    OnPropertyChanged(() => DebitCustomerAccount);
                }
            }
        }

        int _transactionCode;
        [Display(Name = "Transaction Code")]
        public int TransactionCode
        {
            get { return _transactionCode; }
            set
            {
                if (_transactionCode != value)
                {
                    _transactionCode = value;
                    OnPropertyChanged(() => TransactionCode);
                }
            }
        }

        DateTime? _valueDate;
        [Display(Name = "Value Date")]
        public DateTime? ValueDate
        {
            get { return _valueDate; }
            set
            {
                if (_valueDate != value)
                {
                    _valueDate = value;
                    OnPropertyChanged(() => ValueDate);
                }
            }
        }

  
        public CustomerAccountDTO CustomerAccount { get; set; }

  
        public CashDepositRequestDTO CashDepositRequest { get; set; }

        [DataMember]
        public CashWithdrawalRequestDTO CashWithdrawal { get; set; }

        
        public PaymentVoucherDTO PaymentVoucher { get; set; }

        
        public TellerDTO Teller { get; set; }

        
        public ExternalChequeDTO ChequeDeposit { get; set; }

        public PageCollectionInfo<GeneralLedgerTransaction> TellerStatements { get; set; }

        public CustomerDTO CustomerDTO { get; set; }

        public List<ExternalChequeDTO>  CustomerAccountUnclearedCheques { get; set; }

        public List<CustomerAccountSignatoryDTO> CustomerAccountSignatories { get; set; }

        public List<ElectronicStatementOrderDTO> CustomerAccountMiniStatement { get; set; }

        public List<ApportionmentWrapper> Apportionments { get; set; }

        public ApportionmentWrapper ApportionmentWrapper { get; set; }


    }
}

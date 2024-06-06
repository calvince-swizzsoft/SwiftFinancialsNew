using DistributedServices.MainBoundedContext.Identity;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using SwiftFinancials.Web.Attributes;
using SwiftFinancials.Web.Configuration;
using SwiftFinancials.Web.Helpers;
using SwiftFinancials.Web.Services;
using SwiftFinancials.Presentation.Infrastructure.Services;
using Application.MainBoundedContext.DTO.AdministrationModule;
using System.Collections.ObjectModel;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using SwiftFinancials.Presentation.Infrastructure.Models;

namespace SwiftFinancials.Web.Controllers
{
    /// <summary>
    /// Master controller that does setup of things that should always be done.
    /// </summary>
    [CustomErrorHandling]
    public class MasterController : Controller
    {
        public ObservableCollection<LevySplitDTO> LevySplitDTOs;

        public ObservableCollection<CommissionDTO> CommissionDTOs;

        public ObservableCollection<CommissionSplitDTO> ChargeSplitDTOs;

        public ObservableCollection<LoanGuarantorDTO> LoanGuarantorsDTO;

        public ObservableCollection<LoanGuarantorDTO> LoanGuarantorDTOs;

        //public ObservableCollection<LoanGuarantorDTO> StandingOrdersDTOs;

        public ObservableCollection<JournalVoucherEntryDTO> JournalVoucherEntryDTOs;
        public ObservableCollection<ExpensePayableEntryDTO> ExpensePayableEntryDTOs;
        public ObservableCollection<ExpensePayableEntryDTO> RecouringBatchDTOs;

        public ObservableCollection<TransactionModel> TransactionModels;

        public ObservableCollection<SalaryGroupEntryDTO> SalaryGroupEntryDTOs;

        public ObservableCollection<FixedDepositPayableDTO> FixedDepositPayableDTOs;
        private IChannelService channelService;
        public IChannelService _channelService
        {
            get
            {
                if (channelService == null)
                {
                    channelService = DependencyResolver.Current.GetService<IChannelService>();
                }
                return channelService;
            }
            set { channelService = value; }
        }

        private ApplicationRoleManager applicationRoleManager;
        public ApplicationRoleManager _applicationRoleManager
        {
            get
            {
                if (applicationRoleManager == null)
                {
                    applicationRoleManager = DependencyResolver.Current.GetService<ApplicationRoleManager>();
                }
                return applicationRoleManager;
            }
            set { applicationRoleManager = value; }
        }

        private ApplicationUserManager applicationUserManager;
        public ApplicationUserManager _applicationUserManager
        {
            get
            {
                if (applicationUserManager == null)
                {
                    applicationUserManager = DependencyResolver.Current.GetService<ApplicationUserManager>();
                }
                return applicationUserManager;
            }
            set { applicationUserManager = value; }
        }

        private IWebConfigurationService webConfigurationService;
        public IWebConfigurationService _webConfigurationService
        {
            get
            {
                if (webConfigurationService == null)
                {
                    webConfigurationService = DependencyResolver.Current.GetService<IWebConfigurationService>();
                }
                return webConfigurationService;
            }
            set { webConfigurationService = value; }
        }

        [NonAction]
        public ServiceHeader GetServiceHeader()
        {
            return _webConfigurationService.GetServiceHeader();
        }

        [NonAction]
        public async Task<ApplicationUser> GetCurrentUser()
        {
            return await _applicationUserManager.FindByNameAsync(User.Identity.Name);
        }

        [NonAction]
        public async Task ServeNavigationMenus()
        {
            if (User.IsInRole(WellKnownUserRoles.SuperAdministrator))
            {
                ViewBag.NavigationItems = await _channelService.FindNavigationItemsAsync(GetServiceHeader());
            }
            else
            {
                var user = await _applicationUserManager.FindByNameAsync(User.Identity.Name);

                var roles = await _applicationUserManager.GetRolesAsync(user.Id);

                var navigationItemsInRole = HttpRuntime.Cache[User.Identity.GetUserId()] as ICollection<NavigationItemInRoleDTO> ?? await _channelService.GetNavigationItemsInRoleAsync(roles.FirstOrDefault(), GetServiceHeader());

                var navigationItems = await _channelService.FindNavigationItemsAsync(GetServiceHeader());

                var parentsInNavigationItems = navigationItems.Where(x => x.ControllerName == null && x.ActionName == null).ToList();

                var userNavigationItems = navigationItems.Where(a => navigationItemsInRole.Any(b => a.Id == b.NavigationItemId)).ToList();

                userNavigationItems.AddRange(parentsInNavigationItems);

                userNavigationItems.ForEach(item => item.Child = userNavigationItems.Where(child => child.AreaCode == item.Code).ToList());

                userNavigationItems.RemoveAll(x => x.Child.Count == 0 && x.ControllerName == null && x.ActionName == null);

                ViewBag.NavigationItems = userNavigationItems;
            }
        }

        [NonAction]
        protected List<SelectListItem> GetTwoFactorProviders(string selectedValue)
        {
            List<SelectListItem> providers = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(TwoFactorProviders)).Cast<TwoFactorProviders>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            providers.AddRange(items);

            return providers;
        }

        [NonAction]
        public async Task LoadModuleAccessRights(string username)
        {
            var cacheExtensions = new CacheExtensions();

            var currentUser = await _applicationUserManager.FindByNameAsync(username);

            var roles = await _applicationUserManager.GetRolesAsync(currentUser.Id);

            if (roles.Any())
            {
                var moduleAccessRightsInRole = await _channelService.GetNavigationItemsInRoleAsync(roles[0], GetServiceHeader());

                cacheExtensions.CacheModuleAccessRightsInRole(moduleAccessRightsInRole, currentUser.Id, GetServiceHeader());
            }
        }


        [NonAction]
        protected static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attributes != null && attributes.Length > 0)
                return attributes[0].Description;
            else
                return value.ToString();
        }

        [NonAction]
        protected List<SelectListItem> GetRecordStatusSelectList(string selectedValue)
        {
            List<SelectListItem> recordStatusSelectList = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(RecordStatus)).Cast<RecordStatus>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            recordStatusSelectList.AddRange(items);

            return recordStatusSelectList;
        }
        [NonAction]
        protected List<SelectListItem> GetSystemTransactionTypeList(string selectedValue)
        {
            List<SelectListItem> systemTransactionType = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(SystemTransactionCode)).Cast<SystemTransactionCode>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            systemTransactionType.AddRange(items);

            return systemTransactionType;
        }








        [NonAction]
        protected List<SelectListItem> GetCustomerTypeSelectList(string selectedValue)
        {
            List<SelectListItem> customerTypes = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(CustomerType)).Cast<CustomerType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            customerTypes.AddRange(items);

            return customerTypes;
        }

        [NonAction]
        protected List<SelectListItem> GetCCustomerAccountManagementActionSelectList(string selectedValue)
        {
            List<SelectListItem> customerAccountManagementAction = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(CustomerAccountManagementAction)).Cast<CustomerAccountManagementAction>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            customerAccountManagementAction.AddRange(items);

            return customerAccountManagementAction;
        }


        [NonAction]
        protected List<SelectListItem> GetTransactionOwnershipSelectList(string selectedValue)
        {
            List<SelectListItem> transactionOwnerships = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(TransactionOwnership)).Cast<TransactionOwnership>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            transactionOwnerships.AddRange(items);

            return transactionOwnerships;
        }

        [NonAction]
        protected List<SelectListItem> GetFrontOfficeTransactionTypeSelectList(string selectedValue)
        {
            List<SelectListItem> frontOfficeTransactionType = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(FrontOfficeTransactionType)).Cast<FrontOfficeTransactionType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            frontOfficeTransactionType.AddRange(items);

            return frontOfficeTransactionType;
        }

        [NonAction]
        protected List<SelectListItem> GetJournalVoucherStatusSelectList(string selectedValue)
        {
            List<SelectListItem> journalVoucherStatuses = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(JournalVoucherStatus)).Cast<JournalVoucherStatus>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            journalVoucherStatuses.AddRange(items);

            return journalVoucherStatuses;
        }





        [NonAction]
        protected List<SelectListItem> GetTellerTypeSelectList(string selectedValue)
        {
            List<SelectListItem> tellerTypes = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(TellerType)).Cast<TellerType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            tellerTypes.AddRange(items);

            return tellerTypes;
        }

        protected List<SelectListItem> GetCashTransferTransactionTypeSelectList(string selectedValue)
        {
            List<SelectListItem> cashTransferTransactionType = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(CashTransferTransactionType)).Cast<CashTransferTransactionType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            cashTransferTransactionType.AddRange(items);

            return cashTransferTransactionType;
        }

        [NonAction]
        protected List<SelectListItem> GetGenderSelectList(string selectedValue)
        {
            List<SelectListItem> gender = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(Gender)).Cast<Gender>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            gender.AddRange(items);

            return gender;
        }

        [NonAction]
        protected List<SelectListItem> GetUnitTypes(string selectedValue)
        {
            List<SelectListItem> unitTypes = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(LeaveUnitTypes)).Cast<LeaveUnitTypes>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            unitTypes.AddRange(items);

            return unitTypes;
        }

        [NonAction]
        protected List<SelectListItem> GetLeaveAuthOption(string selectedValue)
        {
            List<SelectListItem> leaveAuthOptions = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(LeaveAuthOption)).Cast<LeaveAuthOption>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            leaveAuthOptions.AddRange(items);

            return leaveAuthOptions;
        }

        [NonAction]
        protected List<SelectListItem> GetIndividualTypeSelectList(string selectedValue)
        {
            List<SelectListItem> individualTypes = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(IndividualType)).Cast<IndividualType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            individualTypes.AddRange(items);

            return individualTypes;
        }

        [NonAction]
        protected List<SelectListItem> GetMaritalStatusSelectList(string selectedValue)
        {
            List<SelectListItem> maritalStatuses = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(MaritalStatus)).Cast<MaritalStatus>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            maritalStatuses.AddRange(items);

            return maritalStatuses;
        }

        [NonAction]
        protected List<SelectListItem> GetCustomerClassificationSelectList(string selectedValue)
        {
            List<SelectListItem> individualClassifications = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(CustomerClassification)).Cast<CustomerClassification>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            individualClassifications.AddRange(items);

            return individualClassifications;
        }

        [NonAction]
        protected List<SelectListItem> GetTermsOfServiceSelectList(string selectedValue)
        {
            List<SelectListItem> termsOfServices = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(TermsOfService)).Cast<TermsOfService>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            termsOfServices.AddRange(items);

            return termsOfServices;
        }

        [NonAction]
        protected List<SelectListItem> GetSalutationSelectList(string selectedValue)
        {
            List<SelectListItem> individualSalutations = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(Salutation)).Cast<Salutation>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            individualSalutations.AddRange(items);

            return individualSalutations;
        }

        [NonAction]
        protected List<SelectListItem> GetSalaryHeadTypeSelectList(string selectedValue)
        {
            List<SelectListItem> salaryHead = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(SalaryHeadType)).Cast<SalaryHeadType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            salaryHead.AddRange(items);

            return salaryHead;
        }

        [NonAction]
        protected List<SelectListItem> GetValueGroupTypeSelectList(string selectedValue)
        {
            List<SelectListItem> chargeType = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(ChargeType)).Cast<ChargeType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            chargeType.AddRange(items);

            return chargeType;
        }

        [NonAction]
        protected List<SelectListItem> GetRoundingTypeSelectList(string selectedValue)
        {
            List<SelectListItem> roudingType = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(RoundingType)).Cast<RoundingType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            roudingType.AddRange(items);

            return roudingType;
        }

        [NonAction]
        protected List<SelectListItem> GetIdentityCardTypeSelectList(string selectedValue)
        {
            List<SelectListItem> identityCardTypes = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(IdentityCardType)).Cast<IdentityCardType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            identityCardTypes.AddRange(items);

            return identityCardTypes;
        }

        [NonAction]
        protected List<SelectListItem> GetNationalitySelectList(string selectedValue)
        {
            List<SelectListItem> nationalities = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(Nationality)).Cast<Nationality>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            nationalities.AddRange(items);

            return nationalities;
        }

        [NonAction]
        protected List<SelectListItem> GetChartOfAccountTypeSelectList(string selectedValue)
        {
            List<SelectListItem> chartOfAccountTypes = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(ChartOfAccountType)).Cast<ChartOfAccountType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            chartOfAccountTypes.AddRange(items);

            return chartOfAccountTypes;
        }

        [NonAction]
        protected List<SelectListItem> GetChartOfAccountCategorySelectList(string selectedValue)
        {
            List<SelectListItem> chartOfAccountCategories = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(ChartOfAccountCategory)).Cast<ChartOfAccountCategory>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            chartOfAccountCategories.AddRange(items);

            return chartOfAccountCategories;
        }

        [NonAction]
        protected List<SelectListItem> GetChargeTypeSelectList(string selectedValue)
        {
            List<SelectListItem> ChargeType = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(ChargeType)).Cast<ChargeType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            ChargeType.AddRange(items);

            return ChargeType;
        }

        [NonAction]
        protected List<SelectListItem> GetDynamicChargeRecoveryModeSelectList(string selectedValue)
        {
            List<SelectListItem> dynamicChargeRecoveryMode = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(DynamicChargeRecoveryMode)).Cast<DynamicChargeRecoveryMode>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            dynamicChargeRecoveryMode.AddRange(items);

            return dynamicChargeRecoveryMode;
        }
        [NonAction]
        protected List<SelectListItem> GetDynamicChargeRecoverySourceSelectList(string selectedValue)
        {
            List<SelectListItem> dynamicChargeRecoverySource = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(DynamicChargeRecoverySource)).Cast<DynamicChargeRecoverySource>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            dynamicChargeRecoverySource.AddRange(items);

            return dynamicChargeRecoverySource;
        }

        
        [NonAction]
        protected List<SelectListItem> GetTreasuryTransactionTypeSelectList(string selectedValue)
        {
            List<SelectListItem> treasuryTransactionTypes = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(TreasuryTransactionType)).Cast<TreasuryTransactionType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            treasuryTransactionTypes.AddRange(items);

            return treasuryTransactionTypes;
        }

        [NonAction]
        protected List<SelectListItem> GetProductCodeSelectList(string selectedValue)
        {
            List<SelectListItem> productCode = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(ProductCode)).Cast<ProductCode>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            productCode.AddRange(items);

            return productCode;
        }

        [NonAction]
        protected List<SelectListItem> GetLoanInterestCalculationModeSelectList(string selectedValue)
        {
            List<SelectListItem> loanInterestCalculationModes = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(InterestCalculationMode)).Cast<InterestCalculationMode>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            loanInterestCalculationModes.AddRange(items);

            return loanInterestCalculationModes;
        }

        [NonAction]
        protected List<SelectListItem> GetLoanPaymentFrequencyPerYearSelectList(string selectedValue)
        {
            List<SelectListItem> loanPaymentFrequencyPerYear = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(PaymentFrequencyPerYear)).Cast<PaymentFrequencyPerYear>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            loanPaymentFrequencyPerYear.AddRange(items);

            return loanPaymentFrequencyPerYear;
        }

        [NonAction]
        protected List<SelectListItem> GetIncomeAdjustmentTypeSelectList(string selectedValue)
        {
            List<SelectListItem> incomeAdjustmentType = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(IncomeAdjustmentType)).Cast<IncomeAdjustmentType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            incomeAdjustmentType.AddRange(items);

            return incomeAdjustmentType;
        }

        [NonAction]
        protected List<SelectListItem> GetLoanRegistrationGuarantorSecurityModeSelectList(string selectedValue)
        {
            List<SelectListItem> loanRegistrationGuarantorSecurityModes = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(GuarantorSecurityMode)).Cast<GuarantorSecurityMode>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            loanRegistrationGuarantorSecurityModes.AddRange(items);

            return loanRegistrationGuarantorSecurityModes;
        }

        [NonAction]
        protected List<SelectListItem> GetLoanRegistrationRoundingTypeSelectList(string selectedValue)
        {
            List<SelectListItem> loanRegistrationRoundingTypes = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(RoundingType)).Cast<RoundingType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            loanRegistrationRoundingTypes.AddRange(items);

            return loanRegistrationRoundingTypes;
        }

        [NonAction]
        protected List<SelectListItem> GetLoanRegistrationLoanProductCategorySelectList(string selectedValue)
        {
            List<SelectListItem> loanRegistrationLoanProductCategories = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(LoanProductCategory)).Cast<LoanProductCategory>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            loanRegistrationLoanProductCategories.AddRange(items);

            return loanRegistrationLoanProductCategories;
        }

        [NonAction]
        protected List<SelectListItem> GetLoanInterestRecoveryModeSelectList(string selectedValue)
        {
            List<SelectListItem> loanInterestRecoveryModes = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(InterestRecoveryMode)).Cast<InterestRecoveryMode>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            loanInterestRecoveryModes.AddRange(items);

            return loanInterestRecoveryModes;
        }

        [NonAction]
        protected List<SelectListItem> GetLoanRegistrationPayoutRecoveryModeSelectList(string selectedValue)
        {
            List<SelectListItem> loanRegistrationPayoutRecoveryModes = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(PayoutRecoveryMode)).Cast<PayoutRecoveryMode>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            loanRegistrationPayoutRecoveryModes.AddRange(items);

            return loanRegistrationPayoutRecoveryModes;
        }

        [NonAction]
        protected List<SelectListItem> GetLoanRegistrationPaymentDueDateSelectList(string selectedValue)
        {
            List<SelectListItem> loanRegistrationPaymentDueDate = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(PaymentDueDate)).Cast<PaymentDueDate>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            loanRegistrationPaymentDueDate.AddRange(items);

            return loanRegistrationPaymentDueDate;
        }

        [NonAction]
        protected List<SelectListItem> GetTakeHomeTypeSelectList(string selectedValue)
        {
            List<SelectListItem> takeHomeTypes = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(ChargeType)).Cast<ChargeType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            takeHomeTypes.AddRange(items);

            return takeHomeTypes;
        }

        [NonAction]
        protected List<SelectListItem> GetLoanRegistrationLoanProductSectionsSelectList(string selectedValue)
        {
            List<SelectListItem> loanRegistrationLoanProductSections = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(LoanProductSection)).Cast<LoanProductSection>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            loanRegistrationLoanProductSections.AddRange(items);

            return loanRegistrationLoanProductSections;
        }

        [NonAction]
        protected List<SelectListItem> GetRecoveryPrioritySelectList(string selectedValue)
        {
            List<SelectListItem> priorities = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(RecoveryPriority)).Cast<RecoveryPriority>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            priorities.AddRange(items);

            return priorities;
        }

        [NonAction]
        protected List<SelectListItem> GetLoanRegistrationStandingOrderTriggerSelectList(string selectedValue)
        {
            List<SelectListItem> loanRegistrationStandingOrderTriggers = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(StandingOrderTrigger)).Cast<StandingOrderTrigger>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            loanRegistrationStandingOrderTriggers.AddRange(items);

            return loanRegistrationStandingOrderTriggers;
        }

        [NonAction]
        protected List<SelectListItem> GetLoanInterestChargeModeSelectList(string selectedValue)
        {
            List<SelectListItem> loanInterestChargeModes = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(InterestChargeMode)).Cast<InterestChargeMode>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            loanInterestChargeModes.AddRange(items);

            return loanInterestChargeModes;
        }

        [NonAction]
        protected List<SelectListItem> GetLoanApprovalOptionSelectList(string selectedValue)
        {
            List<SelectListItem> loanApprovalOption = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(LoanApprovalOption)).Cast<LoanApprovalOption>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            loanApprovalOption.AddRange(items);

            return loanApprovalOption;
        }

        [NonAction]
        protected List<SelectListItem> GetLoanAppraisalOptionSelectList(string selectedValue)
        {
            List<SelectListItem> loanAppraisalOption = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(LoanAppraisalOption)).Cast<LoanAppraisalOption>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            loanAppraisalOption.AddRange(items);

            return loanAppraisalOption;
        }

        [NonAction]
        protected List<SelectListItem> GetLoanCancellationOptionSelectList(string selectedValue)
        {
            List<SelectListItem> loanCancellationOptions = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(LoanCancellationOption)).Cast<LoanCancellationOption>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            loanCancellationOptions.AddRange(items);

            return loanCancellationOptions;
        }

        [NonAction]
        protected List<SelectListItem> GetLoanAuditOptionSelectList(string selectedValue)
        {
            List<SelectListItem> loanAuditOptions = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(LoanAuditOption)).Cast<LoanAuditOption>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            loanAuditOptions.AddRange(items);

            return loanAuditOptions;
        }

        [NonAction]
        protected List<SelectListItem> GetLoanRegistrationAggregateCheckOffRecoveryModeSelectList(string selectedValue)
        {
            List<SelectListItem> loanRegistrationAggregateCheckOffRecoveryModes = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(AggregateCheckOffRecoveryMode)).Cast<AggregateCheckOffRecoveryMode>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            loanRegistrationAggregateCheckOffRecoveryModes.AddRange(items);

            return loanRegistrationAggregateCheckOffRecoveryModes;
        }

        [NonAction]
        protected List<SelectListItem> GetChequeTypeChargeRecoveryModeSelectList(string selectedValue)
        {
            List<SelectListItem> chequeTypeChargeRecoveryModes = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(ChequeTypeChargeRecoveryMode)).Cast<ChequeTypeChargeRecoveryMode>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            chequeTypeChargeRecoveryModes.AddRange(items);

            return chequeTypeChargeRecoveryModes;
        }

        [NonAction]
        protected List<SelectListItem> GetWithdrawalNotificationCategorySelectList(string selectedValue)
        {
            List<SelectListItem> withdrawalNotificationCategories = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(WithdrawalNotificationCategory)).Cast<WithdrawalNotificationCategory>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            withdrawalNotificationCategories.AddRange(items);

            return withdrawalNotificationCategories;
        }
        [NonAction]
        protected List<SelectListItem> GetCustomerAccountManagementActionSelectList(string selectedValue)
        {
            List<SelectListItem> customerAccountManagementAction = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(CustomerAccountManagementAction)).Cast<CustomerAccountManagementAction>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            customerAccountManagementAction.AddRange(items);

            return customerAccountManagementAction;
        }
        [NonAction]
        protected List<SelectListItem> GetEmployeeCategorySelectList(string selectedValue)
        {
            List<SelectListItem> employeeCategories = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(EmployeeCategory)).Cast<EmployeeCategory>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            employeeCategories.AddRange(items);

            return employeeCategories;
        }

        [NonAction]
        protected List<SelectListItem> GetBatchAuthOptionSelectList(string selectedValue)
        {
            List<SelectListItem> batchAuthOptions = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(BatchAuthOption)).Cast<BatchAuthOption>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            batchAuthOptions.AddRange(items);

            return batchAuthOptions;
        }

        [NonAction]
        protected List<SelectListItem> GetSystemGeneralLedgerAccountCodeSelectList(string selectedValue)
        {
            List<SelectListItem> systemGeneralLedgerAccountCodes = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(SystemGeneralLedgerAccountCode)).Cast<SystemGeneralLedgerAccountCode>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            systemGeneralLedgerAccountCodes.AddRange(items);

            return systemGeneralLedgerAccountCodes;
        }

        [NonAction]
        protected List<SelectListItem> GetGLAccountsNameSelectList(string selectedValue)
        {
            List<SelectListItem> systemGeneralLedgerAccountCodes = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(SystemGeneralLedgerAccountCode)).Cast<SystemGeneralLedgerAccountCode>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            systemGeneralLedgerAccountCodes.AddRange(items);

            return systemGeneralLedgerAccountCodes;
        }



        [NonAction]
        protected List<SelectListItem> GetMonthsAsync(string selectedValue)
        {
            List<SelectListItem> Monthtype = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(Month)).Cast<Month>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            Monthtype.AddRange(items);

            return Monthtype;
        }





        [NonAction]
        protected List<SelectListItem> GetBloodGroupSelectList(string selectedValue)
        {
            List<SelectListItem> bloodGroups = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(BloodGroup)).Cast<BloodGroup>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            bloodGroups.AddRange(items);

            return bloodGroups;
        }


        [NonAction]
        protected List<SelectListItem> GetJournalVoucherTypeSelectList(string selectedValue)
        {
            List<SelectListItem> journalVoucherTypes = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(JournalVoucherType)).Cast<JournalVoucherType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            journalVoucherTypes.AddRange(items);

            return journalVoucherTypes;
        }

        [NonAction]

        protected List<SelectListItem> GetCreditBatchesAsync(string selectedValue)
        {
            List<SelectListItem> QueuePriority = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(CreditBatchType)).Cast<CreditBatchType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            QueuePriority.AddRange(items);

            return QueuePriority;
        }




        [NonAction]
        protected List<SelectListItem> GetQueuePriorityAsync(string selectedValue)
        {
            List<SelectListItem> creditBatchType = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(QueuePriority)).Cast<QueuePriority>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            creditBatchType.AddRange(items);

            return creditBatchType;
        }

        [NonAction]
        protected List<SelectListItem> GetExpensePayableAuthOptionSelectList(string selectedValue)
        {
            List<SelectListItem> expensePayableAuthOptions = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(ExpensePayableAuthOption)).Cast<ExpensePayableAuthOption>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            expensePayableAuthOptions.AddRange(items);

            return expensePayableAuthOptions;
        }

        [NonAction]
        protected List<SelectListItem> GetJournalVoucherEntryTypeSelectList(string selectedValue)
        {
            List<SelectListItem> journalVoucherEntryTypes = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(JournalVoucherEntryType)).Cast<JournalVoucherEntryType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            journalVoucherEntryTypes.AddRange(items);

            return journalVoucherEntryTypes;
        }

        [NonAction]
        protected List<SelectListItem> GetJournalVoucherAuthOptionSelectList(string selectedValue)
        {
            List<SelectListItem> journalVoucherAuthOptions = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(JournalVoucherAuthOption)).Cast<JournalVoucherAuthOption>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            journalVoucherAuthOptions.AddRange(items);

            return journalVoucherAuthOptions;
        }


        [NonAction]
        protected List<SelectListItem> RecoveryPrioritySelectList(string selectedValue)
        {
            List<SelectListItem> recoveryPriorities = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(RecoveryPriority)).Cast<RecoveryPriority>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            recoveryPriorities.AddRange(items);

            return recoveryPriorities;
        }

        [NonAction]
        protected List<SelectListItem> GetQueuePrioritySelectList(string selectedValue)
        {
            List<SelectListItem> queuePriorities = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(QueuePriority)).Cast<QueuePriority>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            queuePriorities.AddRange(items);

            return queuePriorities;
        }







        [NonAction]
        protected List<SelectListItem> GetAlternateChannelKnownChargeTypeSelectList(string selectedValue)
        {
            List<SelectListItem> alternateChannelKnownChargeType = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(AlternateChannelKnownChargeType)).Cast<AlternateChannelKnownChargeType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            alternateChannelKnownChargeType.AddRange(items);

            return alternateChannelKnownChargeType;
        }

        [NonAction]
        protected List<SelectListItem> GetAlternateChannelTypeSelectList(string selectedValue)
        {
            List<SelectListItem> alternateChannelType = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(AlternateChannelType)).Cast<AlternateChannelType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            alternateChannelType.AddRange(items);

            return alternateChannelType;
        }





        [NonAction]
        protected List<SelectListItem> GetChargeBenefactorSelectList(string selectedValue)
        {
            List<SelectListItem> ChargeBenefactor = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(ChargeBenefactor)).Cast<ChargeBenefactor>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            ChargeBenefactor.AddRange(items);

            return ChargeBenefactor;
        }


        [NonAction]
        public DashboardAppConfigSection GetDashboardAppConfiguration()
        {
            try
            {
                return (DashboardAppConfigSection)ConfigurationManager.GetSection("dashboardAppConfiguration");
            }
            catch { return null; }
        }


        [NonAction]
        protected List<SelectListItem> GetWireTransferBatchTypeSelectList(string selectedValue)
        {
            List<SelectListItem> wireTransferBatchType = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(WireTransferBatchType)).Cast<WireTransferBatchType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            wireTransferBatchType.AddRange(items);

            return wireTransferBatchType;
        }


        [NonAction]
        protected List<SelectListItem> GetLoanDisbursementTypeBatchTypeSelectList(string selectedValue)
        {
            List<SelectListItem> disbursementType = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(DisbursementType)).Cast<DisbursementType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            disbursementType.AddRange(items);

            return disbursementType;
        }
    }
}
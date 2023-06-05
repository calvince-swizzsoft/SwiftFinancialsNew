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

namespace SwiftFinancials.Web.Controllers
{
    /// <summary>
    /// Master controller that does setup of things that should always be done.
    /// </summary>
    [CustomErrorHandling]
    public class MasterController : Controller
    {
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
            List<SelectListItem> chargeTypes = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(ChargeType)).Cast<ChargeType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            chargeTypes.AddRange(items);

            return chargeTypes;
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
        public DashboardAppConfigSection GetDashboardAppConfiguration()
        {
            try
            {
                return (DashboardAppConfigSection)ConfigurationManager.GetSection("dashboardAppConfiguration");
            }
            catch { return null; }
        }
    }
}
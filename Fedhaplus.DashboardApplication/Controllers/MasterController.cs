using Application.MainBoundedContext.DTO.AdministrationModule;
using Application.MainBoundedContext.DTO.RegistryModule;
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
using Fedhaplus.DashboardApplication.Attributes;
using Fedhaplus.DashboardApplication.Configuration;
using Fedhaplus.DashboardApplication.Helpers;
using Fedhaplus.DashboardApplication.Services;
using Fedhaplus.Presentation.Infrastructure.Services;
using Application.MainBoundedContext.DTO.TransactionsModule;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace Fedhaplus.DashboardApplication.Controllers
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

                var NavigationItemsInRole = HttpRuntime.Cache[User.Identity.GetUserId()] as ICollection<NavigationItemInRoleDTO> ?? await _channelService.GetNavigationItemsInRoleAsync(roles.FirstOrDefault(), GetServiceHeader());

                var NavigationItems = await _channelService.FindNavigationItemsAsync(GetServiceHeader());

                var parentsInNavigationItems = NavigationItems.Where(x => x.ControllerName == null && x.ActionName == null).ToList();

                var userNavigationItems = NavigationItems.Where(a => NavigationItemsInRole.Any(b => a.Id == b.NavigationItemId)).ToList();

                userNavigationItems.AddRange(parentsInNavigationItems);

                userNavigationItems.ForEach(item => item.Child = userNavigationItems.Where(child => child.AreaCode == item.Code).ToList());

                userNavigationItems.RemoveAll(x => x.Child.Count == 0 && x.ControllerName == null && x.ActionName == null);

                ViewBag.NavigationItems = userNavigationItems;
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
        protected List<SelectListItem> GetCustomerTypesSelectList(string selectedValue)
        {
            List<SelectListItem> customerTypesSelectList = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(CustomerType)).Cast<CustomerType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            customerTypesSelectList.AddRange(items);

            return customerTypesSelectList;
        }

        [NonAction]
        protected List<SelectListItem> GetCustomerApprovalOptionsSelectList(string selectedValue)
        {
            List<SelectListItem> customerApprovalOptionsSelectList = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(CustomerApprovalOption)).Cast<CustomerApprovalOption>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            customerApprovalOptionsSelectList.AddRange(items);

            return customerApprovalOptionsSelectList;
        }

        [NonAction]
        protected List<SelectListItem> GetIdentityTypes(string selectedValue)
        {
            List<SelectListItem> identityTypes = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(IdentityType)).Cast<IdentityType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            identityTypes.AddRange(items);

            return identityTypes;
        }

        [NonAction]
        protected List<SelectListItem> GetGender(string selectedValue)
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
        protected List<SelectListItem> GetMaritalStatuses(string selectedValue)
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
        protected List<SelectListItem> GetNationalities(string selectedValue)
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
        protected List<SelectListItem> GetChartOfAccountTypes(string selectedValue)
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
        protected List<SelectListItem> GetChartOfAccountCategories(string selectedValue)
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
        protected List<SelectListItem> GetItemCategoryTypes(string selectedValue)
        {
            List<SelectListItem> itemCategoryTypes = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(ItemCategoryType)).Cast<ItemCategoryType>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            itemCategoryTypes.AddRange(items);

            return itemCategoryTypes;
        }

        [NonAction]
        protected List<SelectListItem> GetReportCategoriesSelectList(string selectedValue)
        {
            List<SelectListItem> reportcategorySelectList = new List<SelectListItem> { };

            var defaultReportCategoryItem = new SelectListItem
            {
                Selected = (selectedValue == string.Empty),
                Text = "Select Category",
                Value = string.Empty
            };

            reportcategorySelectList.Add(defaultReportCategoryItem);

            var items = Enum.GetValues(typeof(ReportCategory)).Cast<ReportCategory>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            reportcategorySelectList.AddRange(items);

            return reportcategorySelectList;
        }

        [NonAction]
        protected List<SelectListItem> GetTransactionPaymentMethods(string selectedValue)
        {
            List<SelectListItem> transactionPaymentMethods = new List<SelectListItem>();

            var items = Enum.GetValues(typeof(TransactionPaymentMethod)).Cast<TransactionPaymentMethod>().Select(v => new SelectListItem
            {
                Text = GetEnumDescription(v),
                Value = ((int)v).ToString(),
                Selected = ((int)v).ToString() == selectedValue,
            }).ToList();

            transactionPaymentMethods.AddRange(items);

            return transactionPaymentMethods;
        }

        [NonAction]
        protected List<SelectListItem> GetChargeTypes(string selectedValue)
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
        public DashboardAppConfigSection GetDashboardAppConfiguration()
        {
            try
            {
                return (DashboardAppConfigSection)ConfigurationManager.GetSection("dashboardAppConfiguration");
            }
            catch { return null; }
        }

        public ObservableCollection<PurchaseOrderItemBindingModel> PurchaseOrderItems;

        public ObservableCollection<PurchaseOrderItemDTO> PurchaseOrderItemDTOs;


        public ObservableCollection<SaleOrderItemBindingModel> SaleOrderItems;

        public ObservableCollection<SaleOrderItemDTO> SaleOrderItemDTOs;
    }
}
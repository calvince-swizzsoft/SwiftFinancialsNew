using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Presentation.Infrastructure.Services;
using SwiftFinancials.Web.Attributes;
using SwiftFinancials.Web.Configuration;
using SwiftFinancials.Web.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Controllers
{
    // GET: Master
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
    }
}
using Infrastructure.Crosscutting.Framework.Utils;
using System.Configuration;
using SwiftFinancials.Web.Configuration;

namespace SwiftFinancials.Web.Services
{
    public class WebConfigurationService : IWebConfigurationService
    {
        public ServiceHeader GetServiceHeader()
        {
            ServiceHeader serviceHeader = null;

            var dashboardAppConfigSection = (DashboardAppConfigSection)ConfigurationManager.GetSection("dashboardAppConfiguration");

            foreach (var settingsItem in dashboardAppConfigSection.DashboardAppSettingsItems)
            {
                var dashboardAppSettingsElement = (DashboardAppSettingsElement)settingsItem;

                if (dashboardAppSettingsElement != null && dashboardAppSettingsElement.Enabled == 1)
                {
                    serviceHeader = new ServiceHeader { ApplicationDomainName = dashboardAppSettingsElement.UniqueId };

                    break;
                }
            }

            return serviceHeader;
        }
    }
}
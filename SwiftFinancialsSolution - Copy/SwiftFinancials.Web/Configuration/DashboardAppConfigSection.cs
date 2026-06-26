using System.Configuration;

namespace SwiftFinancials.Web.Configuration
{
    public class DashboardAppConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("dashboardAppSettings")]
        public DashboardAppSettingsCollection DashboardAppSettingsItems
        {
            get { return ((DashboardAppSettingsCollection)(base["dashboardAppSettings"])); }
        }
    }
}
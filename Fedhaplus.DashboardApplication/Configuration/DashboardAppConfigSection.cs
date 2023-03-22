using System.Configuration;

namespace Fedhaplus.DashboardApplication.Configuration
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
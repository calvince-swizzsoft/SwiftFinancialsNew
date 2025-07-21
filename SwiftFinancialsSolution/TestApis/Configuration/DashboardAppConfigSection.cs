using System.Configuration;

namespace TestApis.Configuration
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
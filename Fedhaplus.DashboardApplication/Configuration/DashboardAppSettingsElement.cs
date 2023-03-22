using System.Configuration;

namespace Fedhaplus.DashboardApplication.Configuration
{
    public class DashboardAppSettingsElement : ConfigurationElement
    {
        [ConfigurationProperty("uniqueId", IsKey = true, IsRequired = true)]
        public string UniqueId
        {
            get { return ((string)(base["uniqueId"])); }
            set { base["uniqueId"] = value; }
        }

        [ConfigurationProperty("enabled", IsKey = false, IsRequired = true)]
        public int Enabled
        {
            get { return ((int)(base["enabled"])); }
            set { base["enabled"] = value; }
        }
    }
}
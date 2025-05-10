using System.Configuration;

namespace SwiftFinancials.Apis.Configuration
{
    public class WebApiConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("webApiSettings")]
        public WebApiSettingsCollection WebApiSettingsItems
        {
            get { return ((WebApiSettingsCollection)(base["webApiSettings"])); }
        }
    }
}
using System.Configuration;

namespace SwiftFinancials.WebApi.Configuration 
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
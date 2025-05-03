using System.Configuration;

namespace SwiftFinancials.WebApi.Configuration
{
    public class WebApiSettingsCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new WebApiSettingsElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((WebApiSettingsElement)(element)).UniqueId;
        }

        public WebApiSettingsElement this[int index]
        {
            get { return (WebApiSettingsElement)BaseGet(index); }
        }

        [ConfigurationProperty("name", IsRequired = false)]
        public string Name
        {
            get
            {
                return (string)base["name"];
            }
        }

        [ConfigurationProperty("mobileToBankQueuePath", IsRequired = true)]
        public string MobileToBankQueuePath
        {
            get
            {
                return (string)base["mobileToBankQueuePath"];
            }
        }

        [ConfigurationProperty("bankToMobileQueuePath", IsRequired = true)]
        public string BankToMobileQueuePath
        {
            get
            {
                return (string)base["bankToMobileQueuePath"];
            }
        }

        [ConfigurationProperty("citiusQueuePath", IsRequired = true)]
        public string CitiusQueuePath
        {
            get
            {
                return (string)base["citiusQueuePath"];
            }
        }

        [ConfigurationProperty("requireHttps", IsRequired = true)]
        public int RequireHttps
        {
            get
            {
                return ((int)(base["requireHttps"]));
            }
        }

        [ConfigurationProperty("logEnabled", IsRequired = true)]
        public int LogEnabled
        {
            get
            {
                return ((int)(base["logEnabled"]));
            }
        }
    }
}
using System.Configuration;

namespace Fedhaplus.DashboardApplication.Configuration
{
    public class DashboardAppSettingsCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new DashboardAppSettingsElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((DashboardAppSettingsElement)(element)).UniqueId;
        }

        public DashboardAppSettingsElement this[int index]
        {
            get { return (DashboardAppSettingsElement)BaseGet(index); }
        }

        [ConfigurationProperty("name", IsRequired = false)]
        public string Name
        {
            get
            {
                return (string)base["name"];
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

        [ConfigurationProperty("retryCount", IsRequired = true)]
        public int RetryCount
        {
            get
            {
                return ((int)(base["retryCount"]));
            }
        }

        [ConfigurationProperty("initialRetryDelay", IsRequired = true)]
        public int InitialRetryDelay
        {
            get
            {
                return ((int)(base["initialRetryDelay"]));
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

        [ConfigurationProperty("templatesPath", IsKey = true, IsRequired = true)]
        public string TemplatesPath
        {
            get { return ((string)(base["templatesPath"])); }
            set { base["templatesPath"] = value; }
        }

        [ConfigurationProperty("smtpUsername", IsKey = false, IsRequired = true)]
        public string SmtpUsername
        {
            get { return ((string)(base["smtpUsername"])); }
            set { base["smtpUsername"] = value; }
        }
    }
}
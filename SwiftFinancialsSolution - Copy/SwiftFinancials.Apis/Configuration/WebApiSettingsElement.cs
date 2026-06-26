using System.Configuration;

namespace SwiftFinancials.Apis.Configuration
{
    public class WebApiSettingsElement : ConfigurationElement
    {
        [ConfigurationProperty("uniqueId", IsKey = true, IsRequired = true)]
        public string UniqueId
        {
            get { return ((string)(base["uniqueId"])); }
            set { base["uniqueId"] = value; }
        }

        [ConfigurationProperty("bankToMobileBankId", IsKey = false, IsRequired = false)]
        public string BankToMobileBankId
        {
            get { return ((string)(base["bankToMobileBankId"])); }
            set { base["bankToMobileBankId"] = value; }
        }

        [ConfigurationProperty("bankToMobileApiUsername", IsKey = false, IsRequired = true)]
        public string BankToMobileApiUsername
        {
            get { return ((string)(base["bankToMobileApiUsername"])); }
            set { base["bankToMobileApiUsername"] = value; }
        }

        [ConfigurationProperty("bankToMobileApiPassword", IsKey = false, IsRequired = true)]
        public string BankToMobileApiPassword
        {
            get { return ((string)(base["bankToMobileApiPassword"])); }
            set { base["bankToMobileApiPassword"] = value; }
        }

        [ConfigurationProperty("citiusApiUsername", IsKey = false, IsRequired = true)]
        public string CitiusApiUsername
        {
            get { return ((string)(base["citiusApiUsername"])); }
            set { base["citiusApiUsername"] = value; }
        }

        [ConfigurationProperty("citiusApiPassword", IsKey = false, IsRequired = true)]
        public string CitiusApiPassword
        {
            get { return ((string)(base["citiusApiPassword"])); }
            set { base["citiusApiPassword"] = value; }
        }

        [ConfigurationProperty("internetBankingApiUsername", IsKey = false, IsRequired = true)]
        public string InternetBankingApiUsername
        {
            get { return ((string)(base["internetBankingApiUsername"])); }
            set { base["internetBankingApiUsername"] = value; }
        }

        [ConfigurationProperty("internetBankingApiPassword", IsKey = false, IsRequired = true)]
        public string InternetBankingApiPassword
        {
            get { return ((string)(base["internetBankingApiPassword"])); }
            set { base["internetBankingApiPassword"] = value; }
        }

        [ConfigurationProperty("enabled", IsKey = false, IsRequired = true)]
        public int Enabled
        {
            get { return ((int)(base["enabled"])); }
            set { base["enabled"] = value; }
        }
    }
}

using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Configuration;
using System.Web;
using TestApis.Configuration;

namespace TestApis.Services
{
    public class WebConfigurationService : IWebConfigurationService
    {
        public ServiceHeader GetServiceHeader()
        {
            var serviceHeader = new ServiceHeader
            {
                ApplicationUserName = "TestUser",
                ApplicationDomainName = "TestDomain",
                EnvironmentUserName = "JohnDoe",
                EnvironmentProcessorId = "john",
                EnvironmentMachineName = Environment.MachineName,
                EnvironmentMACAddress = "000000",
                EnvironmentMotherboardSerialNumber = "000000",
                EnvironmentDomainName = Environment.UserDomainName,
                EnvironmentOSVersion = Environment.OSVersion.ToString(),
                EnvironmentIPAddress = HttpContext.Current?.Request?.UserHostAddress ?? "127.0.0.1"
            };

            return serviceHeader;
        }
    }
}
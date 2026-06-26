using Infrastructure.Crosscutting.Framework.Utils;

namespace TestApis.Services
{
    public interface IWebConfigurationService
    {
        ServiceHeader GetServiceHeader();
    }
}
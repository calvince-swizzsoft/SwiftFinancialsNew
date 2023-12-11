using Infrastructure.Crosscutting.Framework.Utils;

namespace SwiftFinancials.Web.Services
{
    public interface IWebConfigurationService
    {
        ServiceHeader GetServiceHeader();
    }
}
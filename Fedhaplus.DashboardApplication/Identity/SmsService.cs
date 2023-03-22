using Application.MainBoundedContext.DTO.MessagingModule;
using Microsoft.AspNet.Identity;
using System.Threading.Tasks;
using Fedhaplus.DashboardApplication.Services;
using SwiftFinancials.Presentation.Infrastructure.Services;

namespace Fedhaplus.DashboardApplication.Identity
{
    public class SmsService : IIdentityMessageService
    {
        private readonly IChannelService _channelService;
        private readonly IWebConfigurationService _webConfigurationService;

        public SmsService(IChannelService channelService, IWebConfigurationService webConfigurationService)
        {
            _channelService = channelService;
            _webConfigurationService = webConfigurationService;
        }

        public async Task SendAsync(IdentityMessage message)
        {
            var bulkMessageDTO = new BulkMessageDTO
            {
                Recipients = message.Destination,
                TextMessage = message.Body,
            };

            await _channelService.AddBulkMessageAsync(bulkMessageDTO, _webConfigurationService.GetServiceHeader());
        }
    }
}
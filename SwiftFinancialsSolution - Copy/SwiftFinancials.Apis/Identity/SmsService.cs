using Application.MainBoundedContext.DTO.MessagingModule;
using Microsoft.AspNet.Identity;
using System.Threading.Tasks;
using VanguardFinancials.Presentation.Infrastructure.Services;

namespace SwiftFinancials.Apis.Identity
{
    public class SmsService : IIdentityMessageService
    {
        private readonly IChannelService _channelService;

        public SmsService(IChannelService channelService)
        {
            _channelService = channelService;
        }

        public async Task SendAsync(IdentityMessage message)
        {
            var bulkMessageDTO = new BulkMessageDTO
            {
                Recipients = message.Destination,
                TextMessage = message.Body,
            };

            await _channelService.AddBulkMessageAsync(bulkMessageDTO);
        }
    }
}
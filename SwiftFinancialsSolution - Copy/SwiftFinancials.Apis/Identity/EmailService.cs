using Application.MainBoundedContext.DTO.MessagingModule;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
using System.Threading.Tasks;
using VanguardFinancials.Presentation.Infrastructure.Services;

namespace SwiftFinancials.Apis.Identity
{
    public class EmailService : IIdentityMessageService
    {
        private readonly IChannelService _channelService;

        public EmailService(IChannelService channelService)
        {
            _channelService = channelService;
        }

        public async Task SendAsync(IdentityMessage message)
        {
            var emailAlertDTO = new EmailAlertDTO
            {
                MailMessageOrigin = (int)MessageOrigin.Within,
                MailMessageSubject = message.Subject,
                MailMessageBody = message.Body,
                MailMessageFrom = DefaultSettings.Instance.RootEmail,
                MailMessageTo = message.Destination,
                MailMessageIsBodyHtml = true,
            };

            await _channelService.AddEmailAlertAsync(emailAlertDTO);
        }
    }
}
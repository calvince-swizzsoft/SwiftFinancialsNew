using Application.MainBoundedContext.DTO.MessagingModule;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
using System.Threading.Tasks;
using SwiftFinancials.Presentation.Infrastructure.Services;
using TestApis.Services;

namespace TestApis.Identity
{
    public class EmailService : IIdentityMessageService
    {
        private readonly IChannelService _channelService;
        private readonly IWebConfigurationService _webConfigurationService;

        public EmailService(IChannelService channelService, IWebConfigurationService webConfigurationService)
        {
            _channelService = channelService;
            _webConfigurationService = webConfigurationService;
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

            await _channelService.AddEmailAlertAsync(emailAlertDTO, _webConfigurationService.GetServiceHeader());
        }
    }
}
using Infrastructure.Crosscutting.Framework.Logging;
using Infrastructure.Crosscutting.Framework.Utils;
using System.Configuration;
using System.Text;
using System.Threading.Tasks;
using SwiftFinancials.Apis.Configuration;

namespace SwiftFinancials.Apis.Filters
{
    public class MessageLoggingHandler : HttpMessageHandler
    {
        private readonly bool _messagelogEnabled;

        public MessageLoggingHandler()
        {
            var webApiConfigSection = (WebApiConfigSection)ConfigurationManager.GetSection("webApiConfiguration");

            if (webApiConfigSection != null)
            {
                _messagelogEnabled = webApiConfigSection.WebApiSettingsItems.LogEnabled == 1;
            }
        }

        protected override Task IncomingMessageAsync(string correlationId, string requestInfo, byte[] message)
        {
            return Task.Run(() =>
            {
                if (_messagelogEnabled)
                {
                    LoggerFactory.CreateLog().LogInfo("{0} - WebAPI->Request: {1}\r\n{2}", correlationId, requestInfo, Encoding.UTF8.GetString(message));
                }
            });
        }

        protected override Task OutgoingMessageAsync(string correlationId, string requestInfo, byte[] message)
        {
            return Task.Run(() =>
            {
                if (_messagelogEnabled)
                {
                    LoggerFactory.CreateLog().LogInfo("{0} - WebAPI->Response: {1}\r\n{2}", correlationId, requestInfo, Encoding.UTF8.GetString(message));
                }
            });
        }
    }
}
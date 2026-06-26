using Infrastructure.Crosscutting.Framework.Logging;
using Infrastructure.Crosscutting.Framework.Models;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Threading.Tasks;

namespace SwiftFinancials.Web.Attributes
{
    public class MessageLoggingHandler : CustomHttpMessageHandler
    {
        private readonly ILogger _logger;

        public MessageLoggingHandler(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override Task ProcessMessageAsync(ApiLoggingInfo apiLoggingInfo)
        {
            return Task.Run(() =>
            {
                _logger.LogInfo("Dashboard>MessageLoggingHandler\r\n{0}", apiLoggingInfo);
            });
        }
    }
}
using Application.MainBoundedContext.DTO.MessagingModule;
using Infrastructure.Crosscutting.Framework.Logging;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using VanguardFinancials.Presentation.Infrastructure.Services;
using WebApiThrottle;

namespace SwiftFinancials.Apis.Filters
{
    public class EntLibThrottleLogger : IThrottleLogger
    {
        private readonly IChannelService _channelService;

        public EntLibThrottleLogger()
        {
            _channelService = DependencyResolver.Current.GetService<IChannelService>();
        }

        public void Log(ThrottleLogEntry entry)
        {
            Task.Run(async () =>
            {
                LoggerFactory.CreateLog().LogInfo("WebApiThrottle: {0} Request {1} from {2} has been throttled (blocked), quota {3}/{4} exceeded by {5}",
                entry.LogDate,
                entry.RequestId,
                entry.ClientIp,
                entry.RateLimit,
                entry.RateLimitPeriod,
                entry.TotalRequests);

                var emailAlertDTO = new EmailAlertDTO
                {
                    MailMessageTo = "support@centrino.co.ke",
                    MailMessageCC = "paul.mungai@centrino.co.ke",
                    MailMessageSubject = string.Format("WebApiThrottle ({0})", GetIdentity(entry.Request).Item1),
                    MailMessageBody = string.Format("{0} Request {1} from {2} has been throttled (blocked), quota {3}/{4} exceeded by {5}", entry.LogDate, entry.RequestId, entry.ClientIp, entry.RateLimit, entry.RateLimitPeriod, entry.TotalRequests),
                    MailMessageOrigin = (int)MessageOrigin.Within,
                };

                await _channelService.AddEmailAlertAsync(emailAlertDTO);
            });
        }

        static Tuple<string, string> GetIdentity(HttpRequestMessage request)
        {
            if (request == null)
                return new Tuple<string, string>(string.Empty, string.Empty);

            string authHeader = null;
            var auth = request.Headers.Authorization;
            if (auth != null && auth.Scheme == "Basic")
                authHeader = auth.Parameter;

            if (string.IsNullOrEmpty(authHeader))
                return new Tuple<string, string>(string.Empty, string.Empty);

            authHeader = Encoding.Default.GetString(Convert.FromBase64String(authHeader));

            // find first : as password allows for :
            int idx = authHeader.IndexOf(':');
            if (idx < 0)
                return new Tuple<string, string>(string.Empty, string.Empty);

            return new Tuple<string, string>(authHeader.Substring(0, idx), authHeader.Substring(idx + 1));
        }
    }
}
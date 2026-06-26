using System;
using System.Net.Http;
using System.Text;
using WebApiThrottle;

namespace SwiftFinancials.Apis.Filters
{
    public class CustomThrottlingHandler : ThrottlingHandler
    {
        protected override RequestIdentity SetIdentity(HttpRequestMessage request)
        {
            string authHeader = null;
            var auth = request.Headers.Authorization;
            if (auth != null && auth.Scheme == "Basic")
                authHeader = auth.Parameter;

            if (string.IsNullOrEmpty(authHeader))
                return base.SetIdentity(request);

            authHeader = Encoding.Default.GetString(Convert.FromBase64String(authHeader));

            // find first : as password allows for :
            int idx = authHeader.IndexOf(':');
            if (idx < 0)
                return base.SetIdentity(request);

            string username = authHeader.Substring(0, idx);
            string password = authHeader.Substring(idx + 1);

            var requestIdentity = new RequestIdentity()
            {
                ClientKey = username,
                ClientIp = base.GetClientIp(request).ToString(),
                Endpoint = request.RequestUri.AbsolutePath
            };

            return requestIdentity;
        }
    }
}
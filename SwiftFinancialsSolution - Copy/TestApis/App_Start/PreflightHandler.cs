using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class PreflightHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method == HttpMethod.Options)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);

            // ?? ADD THESE HEADERS (important!)
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");

            return Task.FromResult(response);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
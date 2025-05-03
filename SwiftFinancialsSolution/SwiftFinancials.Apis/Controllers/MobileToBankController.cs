using Application.MainBoundedContext.Services;
using Infrastructure.Crosscutting.Framework.Logging;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.Practices.EnterpriseLibrary.Common.Utility;
using System;
using System.Configuration;
using System.Messaging;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using SwiftFinancials.Apis.Configuration;
using VanguardFinancials.Presentation.Infrastructure.Models;

namespace SwiftFinancials.Apis.Controllers
{
    public class MobileToBankController : ApiController
    {
        private readonly IMessageQueueService _messageQueueService;

        public MobileToBankController(
            IMessageQueueService messageQueueService)
        {
            Guard.ArgumentNotNull(messageQueueService, "messageQueueService");

            _messageQueueService = messageQueueService;
        }

        // GET api/mobiletobank
        public HttpResponseMessage Get()
        {
            var assemblyAttributes = new AssemblyAttributes();

            var httpResponseMessage = Request.CreateResponse(HttpStatusCode.OK);

            httpResponseMessage.Content = new StringContent(string.Format("Company: {0}\nProduct: {1}\nCopyright: {2}\nTrademark: {3}\nVersion: {4}\nDescription: {5}\nConfiguration: {6}", assemblyAttributes.Company, assemblyAttributes.Product, assemblyAttributes.Copyright, assemblyAttributes.Trademark, assemblyAttributes.Version, assemblyAttributes.Description, assemblyAttributes.Configuration));

            return httpResponseMessage;
        }

        // POST api/mobiletobank
        public HttpResponseMessage Post(InstantPaymentNotification data)
        {
            var httpResponseMessage = Request.CreateResponse(HttpStatusCode.OK);
            httpResponseMessage.Content = new StringContent("REQ_RECV");

            if (data == null)
                httpResponseMessage = Request.CreateErrorResponse(HttpStatusCode.Forbidden, "null payload");
            else
            {
                try
                {
                    var webApiConfigSection = (WebApiConfigSection)ConfigurationManager.GetSection("webApiConfiguration");

                    if (webApiConfigSection == null)
                        httpResponseMessage = Request.CreateErrorResponse(HttpStatusCode.Forbidden, "invalid configuration");
                    else
                    {
                        foreach (var settingsItem in webApiConfigSection.WebApiSettingsItems)
                        {
                            var webApiSettingsElement = (WebApiSettingsElement)settingsItem;

                            if (webApiSettingsElement != null && webApiSettingsElement.Enabled == 1)
                            {
                                data.AppDomainName = webApiSettingsElement.UniqueId;

                                _messageQueueService.Send(webApiConfigSection.WebApiSettingsItems.MobileToBankQueuePath, string.Format("{0}", data), MessageCategory.MobileToBank, MessagePriority.VeryHigh, 1440);

                                httpResponseMessage.Content = new StringContent("REQ_ACK");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLog().LogError("mobiletobank-in->", ex);

                    httpResponseMessage = Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message);
                }
            }

            return httpResponseMessage;
        }
    }
}

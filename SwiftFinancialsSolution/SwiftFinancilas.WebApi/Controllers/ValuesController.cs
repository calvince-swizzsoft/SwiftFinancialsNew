using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Threading.Tasks;
using System.Configuration;
using System.ServiceModel;
using DistributedServices.Seedwork;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;

namespace SwiftFinancials.WebApi.Controllers
{
    public class ValuesController : ApiController
    {
        // GET api/values
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }

        //[HttpPost]
        //[Route("api/values/AddJournalVoucher")]
        //public async Task<IHttpActionResult> AddJournalVoucher([FromBody] JournalVoucherDTO journalVoucherDTO)
        //{
        //    if (journalVoucherDTO == null)
        //        return BadRequest("JournalVoucherDTO body is required.");

        //    var serviceUrl = ConfigurationManager.AppSettings["JournalVoucherServiceUrl"];
        //    if (string.IsNullOrWhiteSpace(serviceUrl))
        //        return InternalServerError(new InvalidOperationException("Missing appSetting 'JournalVoucherServiceUrl'."));

        //    var binding = new BasicHttpBinding(BasicHttpSecurityMode.None)
        //    {
        //        MaxReceivedMessageSize = 1024 * 1024 * 10, // 10 MB
        //        OpenTimeout = TimeSpan.FromSeconds(15),
        //        CloseTimeout = TimeSpan.FromSeconds(15),
        //        SendTimeout = TimeSpan.FromSeconds(30),
        //        ReceiveTimeout = TimeSpan.FromSeconds(30)
        //    };

        //    var address = new EndpointAddress(serviceUrl);
        //    var factory = new ChannelFactory<IJournalVoucherService>(binding, address);
        //    IJournalVoucherService channel = null;

        //    try
        //    {
        //        channel = factory.CreateChannel();
        //        var created = await Task.Run(() => channel.AddJournalVoucher(journalVoucherDTO));
        //        (channel as ICommunicationObject)?.Close();
        //        factory.Close();

        //        if (created == null)
        //            return StatusCode(HttpStatusCode.NoContent);

        //        return Ok(created);
        //    }
        //    catch (Exception ex)
        //    {
        //        try { (channel as ICommunicationObject)?.Abort(); } catch { }
        //        try { factory.Abort(); } catch { }
        //        return InternalServerError(ex);
        //    }
        //}
    }
}

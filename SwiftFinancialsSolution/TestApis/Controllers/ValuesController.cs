using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Web.Http;
using TestApis.Services;
using Application.MainBoundedContext.DTO.AdministrationModule;
using SwiftFinancials.Presentation.Infrastructure.Services;
using TestApis.Models;
using Application.MainBoundedContext.DTO.RegistryModule;

namespace TestApis.Controllers
{
    [RoutePrefix("api/values")]
    public class ValuesController : ApiController
    {
        private readonly MasterController master;
        private IChannelService _channelService;

        public IChannelService ChannelService
        {
            get { return _channelService; }
            set { _channelService = value; }
        }


        public ValuesController()
        {
            master = new MasterController();
            var channelService = master._channelService;
        }

        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> Get()
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();
                var channelService = master._channelService;
                if (channelService == null)
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "ChannelService is not initialized.",
                        Data = null
                    });
                }


                var customers = await channelService.FindCustomersAsync(serviceHeader);

                return Json(new ApiResponse<ObservableCollection<CustomerDTO>>
                {
                    Success = true,
                    Message = (customers != null && customers.Count > 0)
                        ? $"{customers.Count} customers retrieved."
                        : "No customers found.",
                    Data = customers ?? new ObservableCollection<CustomerDTO>()
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving customers.",
                    Data = ex.Message
                });
            }
        }

        [HttpGet]
        [Route("test")]
        public IHttpActionResult Test()
        {
            return Ok(new
            {
                Success = true,
                Message = "Test GET endpoint is working!",
                Timestamp = DateTime.UtcNow
            });
        }

    }
}

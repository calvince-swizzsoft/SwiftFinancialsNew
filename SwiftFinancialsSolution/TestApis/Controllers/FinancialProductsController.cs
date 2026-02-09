using Application.MainBoundedContext.DTO.AccountsModule;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using TestApis.Models;

namespace TestApis.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [AllowAnonymous]
    public class FinancialProductsController : ApiController
    {
        private readonly MasterController master;
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        public FinancialProductsController()
        {
            master = new MasterController();
        }

        [HttpPost]
        [Route("addSystemMapping")]
        public async Task<IHttpActionResult> addSystemMapping([FromBody] SavingsProductDTO savingsProductDTO)
        {

            try
            {
                var serviceHeader = master.GetServiceHeader();

                savingsProductDTO.ValidateAll();

                var result = master._channelService.AddSavingsProductAsync(savingsProductDTO, serviceHeader);

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Mapping created successfully.",
                    Data = result
                });

            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the transactions.",
                    Data = ex.Message
                });
            }
        }


    }
}
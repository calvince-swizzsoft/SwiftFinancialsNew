
using Application.MainBoundedContext.DTO.AccountsModule;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;

namespace TestApis.Controllers
{ 

[System.Web.Http.RoutePrefix("api/tellers")]



public class TellersController : MasterController
{


        [HttpGet]

        [Route("")]
        public async Task<IHttpActionResult> Index()
    {
            try

            {


                //var tellers = await _channelService.FindTellersByTypeAsync(tellerDTO.Type, tellerDTO.Reference, true, GetServiceHeader());
                var tellers = await _channelService.FindTellersAsync(GetServiceHeader());


                if (tellers == null)
                {

                    return NotFound();
                }

                return Ok(tellers);

            }

            catch (Exception ex)
            {

                return InternalServerError(ex);
            }
    }

        [HttpPost]

        [Route("")]
        public async Task<IHttpActionResult> Create(TellerDTO tellerDTO)
        {
            try
            {
                tellerDTO.ValidateAll();

                UpdateTellerAccounts(tellerDTO);

                if (!tellerDTO.HasErrors)
                {
                    var createdTellerDTO = await _channelService.AddTellerAsync(tellerDTO);

                    return Ok(createdTellerDTO);
                }

                else
                {
                    return BadRequest(tellerDTO.ErrorMessages.ToString());
                }

            }

            catch (Exception ex)
            {

                return InternalServerError(ex);
            }
        }


        [HttpPut]
        [Route("{id}")]
        public async Task<IHttpActionResult> UpdateTeller(TellerDTO tellerDTO)
        {
            try
            {
                var updatedTellerDTO = await _channelService.UpdateTellerAsync(tellerDTO);

                return Ok(updatedTellerDTO);
            }

            catch(Exception ex)
            {

                return InternalServerError(ex);

            }
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IHttpActionResult> GetTeller(Guid tellerId)
        {
            try
            {
                var teller = await _channelService.FindTellerAsync(tellerId,true, GetServiceHeader()); 

                return Ok(teller);
            }

            catch (Exception ex)
            {

                return InternalServerError(ex);

            }
        }


        [HttpDelete]

        //public async Task<IHttpActionResult> DeleteTeller(Guid id)
        //{
        //    Guid parseId;

        //    try
        //    {

        //        if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
        //        {

        //            return BadRequest("Invalid Id");
                    
        //        }

        //    }

        //    catch (Exception ex)
        //    {

        //        return InternalServerError(ex);

        //    }
        //}
       

        private void UpdateTellerAccounts(TellerDTO tellerDTO)
        {
            switch ((TellerType)tellerDTO.Type)
            {
                case TellerType.InhousePointOfSale:
                case TellerType.AutomatedTellerMachine:
                    tellerDTO.ShortageChartOfAccountId = tellerDTO.ChartOfAccountId;
                    break;

                case TellerType.AgentPointOfSale:
                    tellerDTO.ChartOfAccountId = tellerDTO.CommissionCustomerAccountCustomerAccountTypeTargetProductId;
                    tellerDTO.ShortageChartOfAccountId = tellerDTO.CommissionCustomerAccountCustomerAccountTypeTargetProductId;
                    break;
            }
        }

        [HttpGet]
        [Route("teller")]
        public async Task<IHttpActionResult> GetTellerByEmployeeId(Guid employeeId)
        {

            bool includeBalance = default(bool);

            try
            {
                includeBalance = true;

                var teller = await _channelService.FindTellerByEmployeeIdAsync(employeeId, includeBalance, GetServiceHeader());

                return Ok(teller);

            }


            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }



    }


}
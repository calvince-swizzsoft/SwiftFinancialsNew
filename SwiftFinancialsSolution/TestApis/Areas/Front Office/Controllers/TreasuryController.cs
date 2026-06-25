using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using Infrastructure.Crosscutting.Framework.Utils;
//using SwiftFinancials.Web.Controllers;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;

namespace TestApis.Controllers
{
    [RoutePrefix("api/treasurys")]
    public class TreasurysController : MasterController
    {
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> Index()
        {
            Guid branchId;

            bool includeBalance = true; 
            
            try
            {

                //if (branchId == Guid.Empty || !Guid.TryParse(branchId.ToString(), out parseId))
                //{
                //           return BadRequest("Invalid Id");
                //}

               // branchId = Guid.Parse("D6537BE3-0F2B-4569-9DB1-25B580143C76");

                //var treasuries = await _channelService.FindTreasuryByBranchIdAsync(branchId, includeBalance, GetServiceHeader());

                var treasuries = await _channelService.FindTreasuriesAsync(true, GetServiceHeader());
               // var treasuries = await _channelService.FindTreasuriesByFilterInPageAsync
                if (treasuries  == null)
                {
                    return NotFound();
                }
                return Ok(treasuries);
            }

            catch (Exception ex)
            {

                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> Create(TreasuryDTO treasuryDTO)
        {
            try
            {
                treasuryDTO.ValidateAll();

                if (!treasuryDTO.HasErrors)
                {

                    var createdTreasuryDTO = await _channelService.AddTreasuryAsync(treasuryDTO, GetServiceHeader());

                    return Ok(createdTreasuryDTO);
                }

                else
                {
                    return BadRequest(treasuryDTO.ErrorMessages.ToString());
                }

            }

            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        [HttpPut]
        [Route("{id}")]
        public async Task<IHttpActionResult> UpdateTreasury(TreasuryDTO treasuryDTO)
        {
            try
            {
                var updatedTreasuryDTO = await _channelService.UpdateTreasuryAsync(treasuryDTO, GetServiceHeader());

                return Ok(updatedTreasuryDTO);
            }

            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        //employee
    }

}
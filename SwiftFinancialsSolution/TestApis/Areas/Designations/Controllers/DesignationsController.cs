
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;

namespace TestApis.Controllers
{
    [RoutePrefix("api/designations")]
    public class DesignationsController : MasterController
    {
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> Index()
        {
            try
            {
                var designations = await _channelService.FindDesignationsAsync();

                if (designations == null)
                {
                    return NotFound();
                }

                return Ok(designations);
            }

            catch (Exception ex)
            {

                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> Create(DesignationDTO designationDTO)
        {
            try
            {
                designationDTO.ValidateAll();

                if (!designationDTO.HasErrors)
                {
                    //var employeeDTO = await _channelService.AddTellerAsync(employeeDTO);

                    var createdDesignationDTO = await _channelService.AddDesignationAsync(designationDTO);

                    return Ok(designationDTO);
                }

                else
                {
                    return BadRequest(designationDTO.ErrorMessages.ToString());
                }

            }

            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        [HttpPut]
        [Route("{id}")]
        public async Task<IHttpActionResult> UpdateDesignation(DesignationDTO designationDTO)
        {

            try
            {
                var updatedDesignationDTO = await _channelService.UpdateDesignationAsync(designationDTO);

                return Ok(updatedDesignationDTO);
            }

            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        //employee
    }

}

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
    [RoutePrefix("api/employeetypes")]
    public class EmployeeTypesController : MasterController
    {
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> Index()
        {
            try
            {
                var employeeTypes = await _channelService.FindEmployeeTypesAsync();

                if (employeeTypes == null)
                {
                    return NotFound();
                }

                return Ok(employeeTypes);
            }

            catch (Exception ex)
            {

                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> Create(EmployeeTypeDTO employeeTypeDTO)
        {
            try
            {
                employeeTypeDTO.ValidateAll();

                if (!employeeTypeDTO.HasErrors)
                {
                   
                    var createdEmployeeTypeDTO = await _channelService.AddEmployeeTypeAsync(employeeTypeDTO);

                    return Ok(createdEmployeeTypeDTO);
                }

                else
                {
                    return BadRequest(employeeTypeDTO.ErrorMessages.ToString());
                }

            }

            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        [HttpPut]
        [Route("{id}")]
        public async Task<IHttpActionResult> UpdateEmployeeType(EmployeeTypeDTO employeeTypeDTO)
        {

            try
            {
                var updatedEmployeeTypeDTO = await _channelService.UpdateEmployeeTypeAsync(employeeTypeDTO);

                return Ok(updatedEmployeeTypeDTO);
            }

            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        //employee
    }

}
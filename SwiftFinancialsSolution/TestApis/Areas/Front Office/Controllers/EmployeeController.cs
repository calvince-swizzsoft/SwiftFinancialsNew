
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
    [RoutePrefix("api/employees")]
    public class EmployeesController : MasterController
    {
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> Index()
        {
            try
            {
                var employees = await _channelService.FindEmployeesAsync();

                if (employees == null)
                {
                    return NotFound();
                }

                return Ok(employees);
            }

            catch (Exception ex)
            {

                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> Create(EmployeeDTO employeeDTO)
        {
            try
            {
                employeeDTO.ValidateAll();

                if (!employeeDTO.HasErrors)
                {
                    //var employeeDTO = await _channelService.AddTellerAsync(employeeDTO);

                    var createdEmployeeDTO = await _channelService.AddEmployeeAsync(employeeDTO);

                    return Ok(createdEmployeeDTO);
                }

                else
                {
                    return BadRequest(employeeDTO.ErrorMessages.ToString());
                }

            }

            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        [HttpPut]
        [Route("{id}")]
        public async Task<IHttpActionResult> UpdateEmployee(EmployeeDTO employeeDTO)
        {

            try
            {
                var updatedEmployeeDTO = await _channelService.UpdateEmployeeAsync(employeeDTO);

                return Ok(updatedEmployeeDTO);
            }

            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        //employee
    }

}
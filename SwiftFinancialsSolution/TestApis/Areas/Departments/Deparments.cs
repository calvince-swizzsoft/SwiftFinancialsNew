
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
    [RoutePrefix("api/departments")]
    public class DepartmentsController : MasterController
    {
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> Index()
        {
            try
            {
                var departments = await _channelService.FindDepartmentsAsync();

                if (departments == null)
                {
                    return NotFound();
                }

                return Ok(departments);
            }

            catch (Exception ex)
            {

                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> Create(DepartmentDTO departmentDTO)
        {
            try
            {
                departmentDTO.ValidateAll();

                if (!departmentDTO.HasErrors)
                {
                    //var employeeDTO = await _channelService.AddTellerAsync(employeeDTO);

                    var createdDepartmentDTO = await _channelService.AddDepartmentAsync(departmentDTO);

                    return Ok(departmentDTO);
                }

                else
                {
                    return BadRequest(departmentDTO.ErrorMessages.ToString());
                }

            }

            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        [HttpPut]
        [Route("{id}")]
        public async Task<IHttpActionResult> UpdateDepartment(DepartmentDTO departmentDTO)
        {

            try
            {
                var updatedDepartmentDTO = await _channelService.UpdateDepartmentAsync(departmentDTO);

                return Ok(updatedDepartmentDTO);
            }

            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        //employee
    }

}
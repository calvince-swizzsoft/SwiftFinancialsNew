using SwiftFinancials.Web.Areas.Payroll.models;
using System;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/employee-profiles")]
    public class EmployeeProfilesController : ApiController
    {
        private readonly EmployeeProfileService _service = new EmployeeProfileService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var employees = _service.GetAll();
                return ApiResponse(true, "Employee profiles retrieved successfully", employees);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("{employeeNumber:int}")]
        public IHttpActionResult Get(int employeeNumber)
        {
            try
            {
                var employee = _service.GetById(employeeNumber);
                if (employee == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Employee not found" });

                return ApiResponse(true, "Employee profile retrieved successfully", employee);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] EmployeeProfile profile)
        {
            try
            {
                if (profile == null)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid employee data" });

                _service.Add(profile);
                return Content(System.Net.HttpStatusCode.Created,
                               new { success = true, message = "Employee profile created successfully", data = profile });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPut, Route("{employeeNumber:int}")]
        public IHttpActionResult Update(int employeeNumber, [FromBody] EmployeeProfile profile)
        {
            try
            {
                if (profile == null || profile.EmployeeNumber != employeeNumber)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid employee data" });

                _service.Update(profile);
                return ApiResponse(true, "Employee profile updated successfully", profile);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpDelete, Route("{employeeNumber:int}")]
        public IHttpActionResult Delete(int employeeNumber)
        {
            try
            {
                _service.Delete(employeeNumber);
                return ApiResponse(true, "Employee profile deleted successfully");
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }
    }
}

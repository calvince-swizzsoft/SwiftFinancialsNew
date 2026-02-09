using SwiftFinancials.Web.Areas.Payroll.models;
using System;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/employee-deductions")]
    public class EmployeeDeductionsController : ApiController
    {
        private readonly EmployeeDeductionService _service = new EmployeeDeductionService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var deductions = _service.GetAll();
                return ApiResponse(true, "Employee deductions retrieved successfully", deductions);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("{id:int}")]
        public IHttpActionResult Get(int id)
        {
            try
            {
                var deduction = _service.GetById(id);
                if (deduction == null)
                    return Content(System.Net.HttpStatusCode.NotFound, new { success = false, message = "Employee deduction not found" });

                return ApiResponse(true, "Employee deduction retrieved successfully", deduction);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] EmployeeDeduction deduction)
        {
            try
            {
                if (deduction == null)
                    return Content(System.Net.HttpStatusCode.BadRequest, new { success = false, message = "Invalid deduction data" });

                _service.Add(deduction);
                return Content(System.Net.HttpStatusCode.Created, new { success = true, message = "Employee deduction created successfully", data = deduction });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        [HttpPut, Route("{id:int}")]
        public IHttpActionResult Update(int id, [FromBody] EmployeeDeduction deduction)
        {
            try
            {
                if (deduction == null || deduction.Id != id)
                    return Content(System.Net.HttpStatusCode.BadRequest, new { success = false, message = "Invalid deduction data" });

                _service.Update(deduction);
                return ApiResponse(true, "Employee deduction updated successfully", deduction);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete, Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            try
            {
                _service.Delete(id);
                return ApiResponse(true, "Employee deduction deleted successfully");
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }
    }
}

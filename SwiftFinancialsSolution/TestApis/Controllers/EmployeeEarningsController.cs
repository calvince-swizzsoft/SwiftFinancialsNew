using SwiftFinancials.Web.Areas.Payroll.models;
using System;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/employee-earnings")]
    public class EmployeeEarningsController : ApiController
    {
        private readonly EmployeeEarningService _service = new EmployeeEarningService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var earnings = _service.GetAll();
                return ApiResponse(true, "Employee earnings retrieved successfully", earnings);
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
                var earning = _service.GetById(id);
                if (earning == null)
                    return Content(System.Net.HttpStatusCode.NotFound, new { success = false, message = "Employee earning not found" });

                return ApiResponse(true, "Employee earning retrieved successfully", earning);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] EmployeeEarning earning)
        {
            try
            {
                if (earning == null)
                    return Content(System.Net.HttpStatusCode.BadRequest, new { success = false, message = "Invalid earning data" });

                _service.Add(earning);
                return Content(System.Net.HttpStatusCode.Created, new { success = true, message = "Employee earning created successfully", data = earning });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        [HttpPut, Route("{id:int}")]
        public IHttpActionResult Update(int id, [FromBody] EmployeeEarning earning)
        {
            try
            {
                if (earning == null || earning.Id != id)
                    return Content(System.Net.HttpStatusCode.BadRequest, new { success = false, message = "Invalid earning data" });

                _service.Update(earning);
                return ApiResponse(true, "Employee earning updated successfully", earning);
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
                return ApiResponse(true, "Employee earning deleted successfully");
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }
    }
}

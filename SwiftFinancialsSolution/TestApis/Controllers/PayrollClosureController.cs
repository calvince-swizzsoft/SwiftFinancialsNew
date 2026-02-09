using System;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/payroll-closures")]
    public class PayrollClosureController : ApiController
    {
        private readonly PayrollClosureService _service = new PayrollClosureService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var closures = _service.GetAll();
                return ApiResponse(true, "Payroll closures retrieved successfully", closures);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("{id:int}")]
        public IHttpActionResult Get(int id)
        {
            try
            {
                var closure = _service.GetById(id);
                if (closure == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                        new { success = false, message = "Payroll closure not found" });

                return ApiResponse(true, "Payroll closure retrieved successfully", closure);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] PayrollClosure closure)
        {
            try
            {
                if (closure == null)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                        new { success = false, message = "Invalid payroll closure data" });

                _service.Add(closure);
                return Content(System.Net.HttpStatusCode.Created,
                    new { success = true, message = "Payroll closure created successfully", data = closure });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        [HttpPut, Route("{id:int}")]
        public IHttpActionResult Update(int id, [FromBody] PayrollClosure closure)
        {
            try
            {
                if (closure == null || closure.Id != id)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                        new { success = false, message = "Invalid payroll closure data" });

                _service.Update(closure);
                return ApiResponse(true, "Payroll closure updated successfully", closure);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        [HttpDelete, Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            try
            {
                _service.Delete(id);
                return ApiResponse(true, "Payroll closure deleted successfully");
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }
    }
}

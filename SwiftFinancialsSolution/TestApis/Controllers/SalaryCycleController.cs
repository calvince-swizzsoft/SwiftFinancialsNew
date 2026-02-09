using System;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/salary-cycles")]
    public class SalaryCycleController : ApiController
    {
        private readonly SalaryCycleService _service = new SalaryCycleService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var cycles = _service.GetAll();
                return ApiResponse(true, "Salary cycles retrieved successfully", cycles);
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
                var cycle = _service.GetById(id);
                if (cycle == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                        new { success = false, message = "Salary cycle not found" });

                return ApiResponse(true, "Salary cycle retrieved successfully", cycle);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] SalaryCycle cycle)
        {
            try
            {
                if (cycle == null)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                        new { success = false, message = "Invalid salary cycle data" });

                _service.Add(cycle);
                return Content(System.Net.HttpStatusCode.Created,
                    new { success = true, message = "Salary cycle created successfully", data = cycle });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        [HttpPut, Route("{id:int}")]
        public IHttpActionResult Update(int id, [FromBody] SalaryCycle cycle)
        {
            try
            {
                if (cycle == null || cycle.Id != id)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                        new { success = false, message = "Invalid salary cycle data" });

                _service.Update(cycle);
                return ApiResponse(true, "Salary cycle updated successfully", cycle);
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
                return ApiResponse(true, "Salary cycle deleted successfully");
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }
    }
}

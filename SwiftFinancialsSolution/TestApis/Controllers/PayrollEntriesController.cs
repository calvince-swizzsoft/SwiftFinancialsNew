
using SwiftFinancials.Web.Areas.Payroll.models;
using System;
using System.Web.Http;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/payroll-entries")]
    public class PayrollEntriesController : ApiController
    {
        private readonly PayrollEntryService _service = new PayrollEntryService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var entries = _service.GetAll();
                return ApiResponse(true, "Payroll entries retrieved successfully", entries);
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
                var entry = _service.GetById(id);
                if (entry == null)
                    return Content(System.Net.HttpStatusCode.NotFound, new { success = false, message = "Payroll entry not found" });

                return ApiResponse(true, "Payroll entry retrieved successfully", entry);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] PayrollEntry entry)
        {
            try
            {
                if (entry == null)
                    return Content(System.Net.HttpStatusCode.BadRequest, new { success = false, message = "Invalid payroll entry data" });

                _service.Add(entry);
                return Content(System.Net.HttpStatusCode.Created,
                    new { success = true, message = "Payroll entry created successfully", data = entry });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        [HttpPut, Route("{id:int}")]
        public IHttpActionResult Update(int id, [FromBody] PayrollEntry entry)
        {
            try
            {
                if (entry == null || entry.Id != id)
                    return Content(System.Net.HttpStatusCode.BadRequest, new { success = false, message = "Invalid payroll entry data" });

                _service.Update(entry);
                return ApiResponse(true, "Payroll entry updated successfully", entry);
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
                return ApiResponse(true, "Payroll entry deleted successfully");
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }
    }
}

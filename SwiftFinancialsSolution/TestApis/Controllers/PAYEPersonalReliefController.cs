using SwiftFinancials.Web.Areas.Payroll.models;
using System;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/paye-reliefs")]
    public class PAYEPersonalReliefController : ApiController
    {
        private readonly PAYEPersonalReliefService _service = new PAYEPersonalReliefService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try { return ApiResponse(true, "Reliefs retrieved successfully", _service.GetAll()); }
            catch (Exception ex) { return Content(System.Net.HttpStatusCode.InternalServerError, new { success = false, message = ex.Message }); }
        }

        [HttpGet, Route("{id:int}")]
        public IHttpActionResult Get(int id)
        {
            try
            {
                var relief = _service.GetById(id);
                if (relief == null) return Content(System.Net.HttpStatusCode.NotFound, new { success = false, message = "Relief not found" });
                return ApiResponse(true, "Relief retrieved successfully", relief);
            }
            catch (Exception ex) { return Content(System.Net.HttpStatusCode.InternalServerError, new { success = false, message = ex.Message }); }
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] PAYEPersonalRelief relief)
        {
            try
            {
                if (relief == null) return Content(System.Net.HttpStatusCode.BadRequest, new { success = false, message = "Invalid relief data" });
                _service.Add(relief);
                return Content(System.Net.HttpStatusCode.Created, new { success = true, message = "Relief created successfully", data = relief });
            }
            catch (Exception ex) { return Content(System.Net.HttpStatusCode.InternalServerError, new { success = false, message = ex.Message }); }
        }

        [HttpPut, Route("{id:int}")]
        public IHttpActionResult Update(int id, [FromBody] PAYEPersonalRelief relief)
        {
            try
            {
                if (relief == null || relief.Id != id) return Content(System.Net.HttpStatusCode.BadRequest, new { success = false, message = "Invalid relief data" });
                _service.Update(relief);
                return ApiResponse(true, "Relief updated successfully", relief);
            }
            catch (Exception ex) { return Content(System.Net.HttpStatusCode.InternalServerError, new { success = false, message = ex.Message }); }
        }

        [HttpDelete, Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            try { _service.Delete(id); return ApiResponse(true, "Relief deleted successfully"); }
            catch (Exception ex) { return Content(System.Net.HttpStatusCode.InternalServerError, new { success = false, message = ex.Message }); }
        }
    }
}

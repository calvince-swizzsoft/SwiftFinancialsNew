using SwiftFinancials.Web.Areas.Payroll.models;
using System;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/paye-taxbands")]
    public class PAYETaxBandsController : ApiController
    {
        private readonly PAYETaxBandService _service = new PAYETaxBandService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try { return ApiResponse(true, "Tax bands retrieved successfully", _service.GetAll()); }
            catch (Exception ex) { return Content(System.Net.HttpStatusCode.InternalServerError, new { success = false, message = ex.Message }); }
        }

        [HttpGet, Route("{id:int}")]
        public IHttpActionResult Get(int id)
        {
            try
            {
                var band = _service.GetById(id);
                if (band == null) return Content(System.Net.HttpStatusCode.NotFound, new { success = false, message = "Tax band not found" });
                return ApiResponse(true, "Tax band retrieved successfully", band);
            }
            catch (Exception ex) { return Content(System.Net.HttpStatusCode.InternalServerError, new { success = false, message = ex.Message }); }
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] PAYETaxBand band)
        {
            try
            {
                if (band == null) return Content(System.Net.HttpStatusCode.BadRequest, new { success = false, message = "Invalid tax band data" });
                _service.Add(band);
                return Content(System.Net.HttpStatusCode.Created, new { success = true, message = "Tax band created successfully", data = band });
            }
            catch (Exception ex) { return Content(System.Net.HttpStatusCode.InternalServerError, new { success = false, message = ex.Message }); }
        }

        [HttpPut, Route("{id:int}")]
        public IHttpActionResult Update(int id, [FromBody] PAYETaxBand band)
        {
            try
            {
                if (band == null || band.Id != id) return Content(System.Net.HttpStatusCode.BadRequest, new { success = false, message = "Invalid tax band data" });
                _service.Update(band);
                return ApiResponse(true, "Tax band updated successfully", band);
            }
            catch (Exception ex) { return Content(System.Net.HttpStatusCode.InternalServerError, new { success = false, message = ex.Message }); }
        }

        [HttpDelete, Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            try { _service.Delete(id); return ApiResponse(true, "Tax band deleted successfully"); }
            catch (Exception ex) { return Content(System.Net.HttpStatusCode.InternalServerError, new { success = false, message = ex.Message }); }
        }
    }
}

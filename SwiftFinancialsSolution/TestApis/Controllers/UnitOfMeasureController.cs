using System;
using System.Net;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/unit-of-measure")]
    public class UnitOfMeasureController : ApiController
    {
        private readonly UnitOfMeasureService _service = new UnitOfMeasureService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        // GET api/unit-of-measure
        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var units = _service.GetAll();
                return ApiResponse(true, "Units of Measure retrieved successfully", units);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // GET api/unit-of-measure/{id}
        [HttpGet, Route("{id:guid}")]
        public IHttpActionResult GetById(Guid id)
        {
            try
            {
                var unit = _service.GetById(id);
                if (unit == null)
                    return ApiResponse(false, "Unit of Measure not found");

                return ApiResponse(true, "Unit of Measure retrieved successfully", unit);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // POST api/unit-of-measure
        [HttpPost, Route("")]
        public IHttpActionResult Create(UnitOfMeasure unit)
        {
            try
            {
                unit.Id = Guid.NewGuid();
                unit.CreatedDate = DateTime.Now;

                _service.Add(unit);
                return ApiResponse(true, "Unit of Measure created successfully", unit);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // PUT api/unit-of-measure/{id}
        [HttpPut, Route("{id:guid}")]
        public IHttpActionResult Update(Guid id, UnitOfMeasure unit)
        {
            try
            {
                unit.Id = id; // make sure we update the right record
                _service.Update(unit);
                return ApiResponse(true, "Unit of Measure updated successfully", unit);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // DELETE api/unit-of-measure/{id}
        [HttpDelete, Route("{id:guid}")]
        public IHttpActionResult Delete(Guid id)
        {
            try
            {
                _service.Delete(id);
                return ApiResponse(true, "Unit of Measure deleted successfully");
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }
    }
}

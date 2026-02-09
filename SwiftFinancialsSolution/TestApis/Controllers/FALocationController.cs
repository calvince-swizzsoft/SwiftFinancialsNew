using System;
using System.Net;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/falocation")]
    public class FALocationController : ApiController
    {
        private readonly FALocationService _service = new FALocationService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        // GET api/falocation
        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var list = _service.GetAll();
                return ApiResponse(true, "FA Locations retrieved successfully", list);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // GET api/falocation/{id}
        [HttpGet, Route("{id:guid}")]
        public IHttpActionResult GetById(Guid id)
        {
            try
            {
                var location = _service.GetById(id);
                if (location == null)
                    return ApiResponse(false, "FA Location not found");

                return ApiResponse(true, "FA Location retrieved successfully", location);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // POST api/falocation
        [HttpPost, Route("")]
        public IHttpActionResult Create(FALocation location)
        {
            try
            {
                location.Id = Guid.NewGuid();
                location.CreatedDate = DateTime.Now;

                _service.Add(location);
                return ApiResponse(true, "FA Location created successfully", location);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // PUT api/falocation/{id}
        [HttpPut, Route("{id:guid}")]
        public IHttpActionResult Update(Guid id, FALocation location)
        {
            try
            {
                location.Id = id;
                var updated = _service.Update(location);

                if (updated)
                    return ApiResponse(true, "FA Location updated successfully", location);

                return ApiResponse(false, "FA Location not found or update failed");
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // DELETE api/falocation/{id}
        [HttpDelete, Route("{id:guid}")]
        public IHttpActionResult Delete(Guid id)
        {
            try
            {
                var deleted = _service.Delete(id);
                if (deleted)
                    return ApiResponse(true, "FA Location deleted successfully");

                return ApiResponse(false, "FA Location not found or delete failed");
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }
    }
}

using System;
using System.Net;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/locations")]
    public class LocationController : ApiController
    {
        private readonly LocationService _service = new LocationService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        // GET api/locations
        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var locations = _service.GetAll();
                return ApiResponse(true, "Locations retrieved successfully", locations);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // GET api/locations/{id}
        [HttpGet, Route("{id:guid}")]
        public IHttpActionResult GetById(Guid id)
        {
            try
            {
                var location = _service.GetById(id);
                if (location == null)
                    return ApiResponse(false, "Location not found");

                return ApiResponse(true, "Location retrieved successfully", location);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // POST api/locations
        [HttpPost, Route("")]
        public IHttpActionResult Create(Location location)
        {
            try
            {
                location.Id = Guid.NewGuid();
                location.CreatedDate = DateTime.Now;

                _service.Add(location);
                return ApiResponse(true, "Location created successfully", location);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // PUT api/locations/{id}
        [HttpPut, Route("{id:guid}")]
        public IHttpActionResult Update(Guid id, Location location)
        {
            try
            {
                location.Id = id; // enforce ID consistency
                _service.Update(location);
                return ApiResponse(true, "Location updated successfully", location);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // DELETE api/locations/{id}
        [HttpDelete, Route("{id:guid}")]
        public IHttpActionResult Delete(Guid id)
        {
            try
            {
                _service.Delete(id);
                return ApiResponse(true, "Location deleted successfully");
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }
    }
}

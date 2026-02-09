using Application.MainBoundedContext.DTO.RegistryModule;
using System;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/stations")]
    public class StationsController : ApiController
    {
        private readonly StationService _service = new StationService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var stations = _service.GetAll();
                return ApiResponse(true, "Stations retrieved successfully", stations);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("{id:guid}")]
        public IHttpActionResult Get(Guid id)
        {
            try
            {
                var station = _service.GetById(id);
                if (station == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Station not found" });

                return ApiResponse(true, "Station retrieved successfully", station);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("by-zone/{zoneId:guid}")]
        public IHttpActionResult GetByZone(Guid zoneId)
        {
            try
            {
                var stations = _service.GetByZoneId(zoneId);
                return ApiResponse(true, "Stations retrieved successfully", stations);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("by-name/{name}")]
        public IHttpActionResult GetByName(string name)
        {
            try
            {
                var stations = _service.GetByName(name);
                return ApiResponse(true, "Stations retrieved successfully", stations);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] StationDTO station)
        {
            try
            {
                if (station == null)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid station data" });

                // Validate ZoneId exists (only required foreign key that exists in the table)
                if (!_service.ZoneExists(station.ZoneId))
                {
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Zone does not exist" });
                }

                var createdStation = _service.Create(station);

                return Content(System.Net.HttpStatusCode.Created,
                               new
                               {
                                   success = true,
                                   message = "Station created successfully",
                                   data = createdStation
                               });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPut, Route("{id:guid}")]
        public IHttpActionResult Update(Guid id, [FromBody] StationDTO station)
        {
            try
            {
                if (station == null || station.Id != id)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid station data" });

                // Validate ZoneId exists (only required foreign key that exists in the table)
                if (!_service.ZoneExists(station.ZoneId))
                {
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Zone does not exist" });
                }

                _service.Update(station);
                return ApiResponse(true, "Station updated successfully", station);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpDelete, Route("{id:guid}")]
        public IHttpActionResult Delete(Guid id)
        {
            try
            {
                _service.Delete(id);
                return ApiResponse(true, "Station deleted successfully");
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        // Note: Removed GetWithDivisionEmployer endpoint since those columns don't exist in the database table
        // The table swiftFin_Stations only has ZoneId, not ZoneDivisionId or ZoneDivisionEmployerId
    }
}
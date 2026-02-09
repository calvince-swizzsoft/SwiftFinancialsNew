using Application.MainBoundedContext.DTO.RegistryModule;
using System;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/divisions")]
    public class DivisionsController : ApiController
    {
        private readonly DivisionService _service = new DivisionService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var divisions = _service.GetAll();
                return ApiResponse(true, "Divisions retrieved successfully", divisions);
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
                var division = _service.GetById(id);
                if (division == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Division not found" });

                return ApiResponse(true, "Division retrieved successfully", division);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("by-employer/{employerId:guid}")]
        public IHttpActionResult GetByEmployer(Guid employerId)
        {
            try
            {
                var divisions = _service.GetByEmployerId(employerId);
                return ApiResponse(true, "Divisions retrieved successfully", divisions);
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
                var divisions = _service.GetByName(name);
                return ApiResponse(true, "Divisions retrieved successfully", divisions);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("{id:guid}/with-zones")]
        public IHttpActionResult GetWithZones(Guid id)
        {
            try
            {
                var division = _service.GetWithZones(id);
                if (division == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Division not found" });

                return ApiResponse(true, "Division with zones retrieved successfully", division);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] DivisionDTO division)
        {
            try
            {
                if (division == null)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid division data" });

                // Validate EmployerId exists
                if (!_service.EmployerExists(division.EmployerId))
                {
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Employer does not exist" });
                }

                var createdDivision = _service.Create(division);

                return Content(System.Net.HttpStatusCode.Created,
                               new
                               {
                                   success = true,
                                   message = "Division created successfully",
                                   data = createdDivision
                               });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPut, Route("{id:guid}")]
        public IHttpActionResult Update(Guid id, [FromBody] DivisionDTO division)
        {
            try
            {
                if (division == null || division.Id != id)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid division data" });

                // Validate EmployerId exists if being changed
                if (!_service.EmployerExists(division.EmployerId))
                {
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Employer does not exist" });
                }

                _service.Update(division);
                return ApiResponse(true, "Division updated successfully", division);
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
                // Check if division has zones before deleting
                if (_service.HasZones(id))
                {
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Cannot delete division that has zones assigned" });
                }

                _service.Delete(id);
                return ApiResponse(true, "Division deleted successfully");
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }
    }
}
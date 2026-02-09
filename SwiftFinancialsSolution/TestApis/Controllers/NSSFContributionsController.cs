using System;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/nssf-contributions")]
    public class NSSFContributionsController : ApiController
    {
        private readonly NSSFContributionService _service = new NSSFContributionService();

        // ✅ Standard response wrapper
        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new
            {
                success,
                message,
                data
            });
        }

        // GET api/nssf-contributions
        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var contributions = _service.GetAll();
                return ApiResponse(true, "NSSF Contributions retrieved successfully", contributions);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // GET api/nssf-contributions/{id}
        [HttpGet, Route("{id:int}")]
        public IHttpActionResult Get(int id)
        {
            try
            {
                var contribution = _service.GetById(id);
                if (contribution == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                        new { success = false, message = "NSSF Contribution not found" });

                return ApiResponse(true, "NSSF Contribution retrieved successfully", contribution);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // POST api/nssf-contributions
        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] NSSFContribution contribution)
        {
            try
            {
                if (contribution == null)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                        new { success = false, message = "Invalid contribution data" });

                _service.Add(contribution);
                return Content(System.Net.HttpStatusCode.Created,
                    new { success = true, message = "NSSF Contribution created successfully", data = contribution });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // PUT api/nssf-contributions/{id}
        [HttpPut, Route("{id:int}")]
        public IHttpActionResult Update(int id, [FromBody] NSSFContribution contribution)
        {
            try
            {
                if (contribution == null || contribution.Id != id)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                        new { success = false, message = "Invalid contribution data" });

                _service.Update(contribution);
                return ApiResponse(true, "NSSF Contribution updated successfully", contribution);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // DELETE api/nssf-contributions/{id}
        [HttpDelete, Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            try
            {
                _service.Delete(id);
                return ApiResponse(true, "NSSF Contribution deleted successfully");
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }
    }
}

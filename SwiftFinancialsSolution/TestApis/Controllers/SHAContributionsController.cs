using System;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/sha-contributions")]
    public class SHAContributionsController : ApiController
    {
        private readonly SHAContributionService _service = new SHAContributionService();

        // Standard response wrapper
        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new
            {
                success,
                message,
                data
            });
        }

        // GET api/sha-contributions
        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var contributions = _service.GetAll();
                return ApiResponse(true, "SHA Contributions retrieved successfully", contributions);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // GET api/sha-contributions/5
        [HttpGet, Route("{id:int}")]
        public IHttpActionResult Get(int id)
        {
            try
            {
                var contribution = _service.GetById(id);
                if (contribution == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                        new { success = false, message = "SHA Contribution not found" });

                return ApiResponse(true, "SHA Contribution retrieved successfully", contribution);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // POST api/sha-contributions
        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] SHAContributions contribution)
        {
            try
            {
                if (contribution == null)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                        new { success = false, message = "Invalid contribution data" });

                _service.Add(contribution);
                return Content(System.Net.HttpStatusCode.Created,
                    new { success = true, message = "SHA Contribution created successfully", data = contribution });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // PUT api/sha-contributions/5
        [HttpPut, Route("{id:int}")]
        public IHttpActionResult Update(int id, [FromBody] SHAContributions contribution)
        {
            try
            {
                if (contribution == null || contribution.Id != id)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                        new { success = false, message = "Invalid contribution data" });

                _service.Update(contribution);
                return ApiResponse(true, "SHA Contribution updated successfully", contribution);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // DELETE api/sha-contributions/5
        [HttpDelete, Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            try
            {
                _service.Delete(id);
                return ApiResponse(true, "SHA Contribution deleted successfully");
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }
    }
}

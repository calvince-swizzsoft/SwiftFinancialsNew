using System;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/housing-levy-contributions")]
    public class HousingLevyContributionsController : ApiController
    {
        private readonly HousingLevyContributionService _service = new HousingLevyContributionService();

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

        // GET api/housing-levy-contributions
        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var contributions = _service.GetAll();
                return ApiResponse(true, "Housing levy contributions retrieved successfully", contributions);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // GET api/housing-levy-contributions/5
        [HttpGet, Route("{id:int}")]
        public IHttpActionResult Get(int id)
        {
            try
            {
                var contribution = _service.GetById(id);
                if (contribution == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                        new { success = false, message = "Housing levy contribution not found" });

                return ApiResponse(true, "Housing levy contribution retrieved successfully", contribution);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // POST api/housing-levy-contributions
        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] HousingLevyContribution contribution)
        {
            try
            {
                if (contribution == null)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                        new { success = false, message = "Invalid contribution data" });

                contribution.Total = contribution.EmployeeAmount + contribution.EmployerAmount;
                _service.Add(contribution);

                return Content(System.Net.HttpStatusCode.Created,
                    new { success = true, message = "Housing levy contribution created successfully", data = contribution });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // PUT api/housing-levy-contributions/5
        [HttpPut, Route("{id:int}")]
        public IHttpActionResult Update(int id, [FromBody] HousingLevyContribution contribution)
        {
            try
            {
                if (contribution == null || contribution.Id != id)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                        new { success = false, message = "Invalid contribution data" });

                contribution.Total = contribution.EmployeeAmount + contribution.EmployerAmount;
                _service.Update(contribution);

                return ApiResponse(true, "Housing levy contribution updated successfully", contribution);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // DELETE api/housing-levy-contributions/5
        [HttpDelete, Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            try
            {
                var existing = _service.GetById(id);
                if (existing == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                        new { success = false, message = "Housing levy contribution not found" });

                _service.Delete(id);
                return ApiResponse(true, "Housing levy contribution deleted successfully");
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }
    }
}

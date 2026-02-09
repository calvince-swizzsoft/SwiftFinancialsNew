using System;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/employee-branches")]
    public class EmployeeBranchesController : ApiController
    {
        private readonly EmployeeBranchService _service = new EmployeeBranchService();

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

        // GET api/employee-branches
        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var branches = _service.GetAll();
                return ApiResponse(true, "Branches retrieved successfully", branches);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // GET api/employee-branches/5
        [HttpGet, Route("{id:int}")]
        public IHttpActionResult Get(int id)
        {
            try
            {
                var branch = _service.GetById(id);
                if (branch == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                        new { success = false, message = "Branch not found" });

                return ApiResponse(true, "Branch retrieved successfully", branch);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // POST api/employee-branches
        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] EmployeeBranch branch)
        {
            try
            {
                if (branch == null)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                        new { success = false, message = "Invalid branch data" });

                _service.Add(branch);
                return Content(System.Net.HttpStatusCode.Created,
                    new { success = true, message = "Branch created successfully", data = branch });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // PUT api/employee-branches/5
        [HttpPut, Route("{id:int}")]
        public IHttpActionResult Update(int id, [FromBody] EmployeeBranch branch)
        {
            try
            {
                if (branch == null || branch.Code != id)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                        new { success = false, message = "Invalid branch data" });

                _service.Update(branch);
                return ApiResponse(true, "Branch updated successfully", branch);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // DELETE api/employee-branches/5
        [HttpDelete, Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            try
            {
                _service.Delete(id);
                return ApiResponse(true, "Branch deleted successfully");
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }
    }
}

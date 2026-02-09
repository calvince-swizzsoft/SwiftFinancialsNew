using System;
using System;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/employee-banks")]
    public class EmployeeBanksController : ApiController
    {
        private readonly EmployeeBankService _service = new EmployeeBankService();

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

        // GET api/employee-banks
        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var banks = _service.GetAll();
                return ApiResponse(true, "Banks retrieved successfully", banks);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // GET api/employee-banks/5
        [HttpGet, Route("{id:int}")]
        public IHttpActionResult Get(int id)
        {
            try
            {
                var bank = _service.GetById(id);
                if (bank == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                        new { success = false, message = "Bank not found" });

                return ApiResponse(true, "Bank retrieved successfully", bank);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // POST api/employee-banks
        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] EmployeeBank bank)
        {
            try
            {
                if (bank == null)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                        new { success = false, message = "Invalid bank data" });

                _service.Add(bank);
                return Content(System.Net.HttpStatusCode.Created,
                    new { success = true, message = "Bank created successfully", data = bank });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // PUT api/employee-banks/5
        [HttpPut, Route("{id:int}")]
        public IHttpActionResult Update(int id, [FromBody] EmployeeBank bank)
        {
            try
            {
                if (bank == null || bank.Code != id)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                        new { success = false, message = "Invalid bank data" });

                _service.Update(bank);
                return ApiResponse(true, "Bank updated successfully", bank);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // DELETE api/employee-banks/5
        [HttpDelete, Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            try
            {
                _service.Delete(id);
                return ApiResponse(true, "Bank deleted successfully");
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }
    }
}

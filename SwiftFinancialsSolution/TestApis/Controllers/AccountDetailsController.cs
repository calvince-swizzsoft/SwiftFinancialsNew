using System;
using System.Net;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/account-details")]
    public class AccountDetailsController : ApiController
    {
        private readonly AccountDetailsService _service = new AccountDetailsService();

        // Standard response wrapper
        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        // GET api/account-details
        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var accounts = _service.GetAll();
                return ApiResponse(true, "Account details retrieved successfully", accounts);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // GET api/account-details/{id}
        [HttpGet, Route("{id:int}")]
        public IHttpActionResult Get(int id)
        {
            try
            {
                var account = _service.GetById(id);
                if (account == null)
                    return Content(HttpStatusCode.NotFound,
                        new { success = false, message = "Account detail not found" });

                return ApiResponse(true, "Account detail retrieved successfully", account);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // POST api/account-details
        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] AccountDetails account)
        {
            try
            {
                if (account == null)
                    return Content(HttpStatusCode.BadRequest,
                        new { success = false, message = "Invalid account data" });

                _service.Add(account);
                return Content(HttpStatusCode.Created,
                    new { success = true, message = "Account detail created successfully", data = account });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // PUT api/account-details/{id}
        [HttpPut, Route("{id:int}")]
        public IHttpActionResult Update(int id, [FromBody] AccountDetails account)
        {
            try
            {
                if (account == null || account.Code != id)
                    return Content(HttpStatusCode.BadRequest,
                        new { success = false, message = "Invalid account data" });

                _service.Update(account);
                return ApiResponse(true, "Account detail updated successfully", account);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // DELETE api/account-details/{id}
        [HttpDelete, Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            try
            {
                _service.Delete(id);
                return ApiResponse(true, "Account detail deleted successfully");
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }
    }
}

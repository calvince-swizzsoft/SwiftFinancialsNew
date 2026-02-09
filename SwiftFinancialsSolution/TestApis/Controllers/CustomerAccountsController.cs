using Application.MainBoundedContext.DTO.AccountsModule;
using System;
using System.Web.Http;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/customer-accounts")]
    public class CustomerAccountsController : ApiController
    {
        private readonly CustomerAccountService _service = new CustomerAccountService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var accounts = _service.GetAll();
                return ApiResponse(true, "Customer accounts retrieved successfully", accounts);
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
                var account = _service.GetById(id);
                if (account == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Customer account not found" });

                return ApiResponse(true, "Customer account retrieved successfully", account);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        //[HttpPost, Route("")]
        //public IHttpActionResult Create([FromBody] CustomerAccountDTO account)
        //{
        //    try
        //    {
        //        if (account == null)
        //            return Content(System.Net.HttpStatusCode.BadRequest,
        //                           new { success = false, message = "Invalid customer account data" });

        //        // Generate new ID if not provided
        //        if (account.Id == Guid.Empty)
        //            account.Id = Guid.NewGuid();

        //        // Set creation timestamp
        //        if (account.CreatedDate == default(DateTime))
        //            account.CreatedDate = DateTime.Now;

        //        _service.Add(account);
        //        return Content(System.Net.HttpStatusCode.Created,
        //                       new { success = true, message = "Customer account created successfully", data = account });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Content(System.Net.HttpStatusCode.InternalServerError,
        //                       new { success = false, message = ex.Message });
        //    }
        //}

        //[HttpPut, Route("{id:guid}")]
        //public IHttpActionResult Update(Guid id, [FromBody] CustomerAccountDTO account)
        //{
        //    try
        //    {
        //        if (account == null || account.Id != id)
        //            return Content(System.Net.HttpStatusCode.BadRequest,
        //                           new { success = false, message = "Invalid customer account data" });

        //        // Update modification timestamp
        //        account.ModifiedDate = DateTime.Now;

        //        _service.Update(account);
        //        return ApiResponse(true, "Customer account updated successfully", account);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Content(System.Net.HttpStatusCode.InternalServerError,
        //                       new { success = false, message = ex.Message });
        //    }
        //}

        //[HttpDelete, Route("{id:guid}")]
        //public IHttpActionResult Delete(Guid id)
        //{
        //    try
        //    {
        //        _service.Delete(id);
        //        return ApiResponse(true, "Customer account deleted successfully");
        //    }
        //    catch (Exception ex)
        //    {
        //        return Content(System.Net.HttpStatusCode.InternalServerError,
        //                       new { success = false, message = ex.Message });
        //    }
        //}

        [HttpGet, Route("{id:guid}/accounts")]
        public IHttpActionResult GetCustomerAccounts(Guid id)
        {
            try
            {
                var customerAccountService = new CustomerAccountService();
                var accounts = customerAccountService.GetByCustomerId(id);

                return ApiResponse(true, "Customer accounts retrieved successfully", accounts);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        // Optional: Additional endpoints for specific queries
        [HttpGet, Route("customer/{customerId:guid}")]
        public IHttpActionResult GetByCustomerId(Guid customerId)
        {
            try
            {
                var accounts = _service.GetAll();
                // Filter by customerId - you might want to implement a specific service method for this
                return ApiResponse(true, "Customer accounts retrieved successfully", accounts);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("account-number/{accountNumber}")]
        public IHttpActionResult GetByAccountNumber(string accountNumber)
        {
            try
            {
                // You'll need to implement logic to parse account number and query accordingly
                return ApiResponse(true, "Endpoint not fully implemented", null);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }
    }
}
using System;
using System.Net;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/inventorypostingaccounts")]
    public class InventoryPostingAccountController : ApiController
    {
        private readonly InventoryPostingAccountService _service = new InventoryPostingAccountService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var list = _service.GetAll();
                return ApiResponse(true, "Inventory Posting Accounts retrieved successfully", list);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("{id:guid}")]
        public IHttpActionResult GetById(Guid id)
        {
            try
            {
                var account = _service.GetById(id);
                if (account == null)
                    return ApiResponse(false, "Inventory Posting Account not found");

                return ApiResponse(true, "Inventory Posting Account retrieved successfully", account);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create(InventoryPostingAccount account)
        {
            try
            {
                account.Id = Guid.NewGuid();
                account.CreatedDate = DateTime.Now;

                _service.Add(account);
                return ApiResponse(true, "Inventory Posting Account created successfully", account);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        [HttpPut, Route("{id:guid}")]
        public IHttpActionResult Update(Guid id, InventoryPostingAccount account)
        {
            try
            {
                account.Id = id;
                _service.Update(account);
                return ApiResponse(true, "Inventory Posting Account updated successfully", account);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete, Route("{id:guid}")]
        public IHttpActionResult Delete(Guid id)
        {
            try
            {
                _service.Delete(id);
                return ApiResponse(true, "Inventory Posting Account deleted successfully");
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }
    }
}

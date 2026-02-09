using System;
using System.Net;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/inventory-transactions")]
    public class InventoryTransactionController : ApiController
    {
        private readonly InventoryTransactionService _service = new InventoryTransactionService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        // GET api/inventory-transactions
        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var txns = _service.GetAll();
                return ApiResponse(true, "Transactions retrieved successfully", txns);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // GET api/inventory-transactions/{id}
        [HttpGet, Route("{id:guid}")]
        public IHttpActionResult GetById(Guid id)
        {
            try
            {
                var txn = _service.GetById(id);
                if (txn == null)
                    return ApiResponse(false, "Transaction not found");

                return ApiResponse(true, "Transaction retrieved successfully", txn);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // POST api/inventory-transactions
        [HttpPost, Route("")]
        public IHttpActionResult Create(InventoryTransaction txn)
        {
            try
            {
                txn.Id = Guid.NewGuid();
                txn.CreatedDate = DateTime.Now;

                _service.Add(txn);
                return ApiResponse(true, "Transaction created successfully", txn);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // PUT api/inventory-transactions/{id}
        [HttpPut, Route("{id:guid}")]
        public IHttpActionResult Update(Guid id, InventoryTransaction txn)
        {
            try
            {
                txn.Id = id;
                var updated = _service.Update(txn);

                if (updated)
                    return ApiResponse(true, "Transaction updated successfully", txn);

                return ApiResponse(false, "Transaction not found or update failed");
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // DELETE api/inventory-transactions/{id}
        [HttpDelete, Route("{id:guid}")]
        public IHttpActionResult Delete(Guid id)
        {
            try
            {
                var deleted = _service.Delete(id);
                if (deleted)
                    return ApiResponse(true, "Transaction deleted successfully");

                return ApiResponse(false, "Transaction not found or delete failed");
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }
    }
}

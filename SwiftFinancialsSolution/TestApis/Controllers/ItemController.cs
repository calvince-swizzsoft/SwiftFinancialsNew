using System;
using System.Net;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/items")]
    public class ItemController : ApiController
    {
        private readonly ItemService _service = new ItemService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        // GET api/items
        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var items = _service.GetAll();
                return ApiResponse(true, "Items retrieved successfully", items);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // GET api/items/{id}
        [HttpGet, Route("{id:guid}")]
        public IHttpActionResult GetById(Guid id)
        {
            try
            {
                var item = _service.GetById(id);
                if (item == null)
                    return ApiResponse(false, "Item not found");

                return ApiResponse(true, "Item retrieved successfully", item);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // POST api/items
        [HttpPost, Route("")]
        public IHttpActionResult Create(Item item)
        {
            try
            {
                item.Id = Guid.NewGuid();
                item.CreatedDate = DateTime.Now;

                _service.Add(item);
                return ApiResponse(true, "Item created successfully", item);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }



        // PUT api/items/{id}/balance
        [HttpPut, Route("{id:guid}/balance")]
        public IHttpActionResult UpdateInventoryBalance(Guid id, Item dto)
        {
            try
            {
                var updated = _service.UpdateInventoryBalance(id, dto.InventoryBalance);

                if (updated)
                    return ApiResponse(true, "Inventory balance updated successfully", new { Id = id, InventoryBalance = dto.InventoryBalance });

                return ApiResponse(false, "Item not found or update failed");
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // PUT api/items/{id}/reduce
        [HttpPut, Route("{id:guid}/reduce")]
        public IHttpActionResult ReduceInventoryBalance(Guid id, Item dto)
        {
            try
            {
                var updated = _service.ReduceInventoryBalance(id, dto.InventoryBalance);

                if (updated)
                    return ApiResponse(true, "Inventory balance reduced successfully",
                        new { Id = id, ReducedBy = dto.InventoryBalance });

                return ApiResponse(false, "Item not found or not enough stock to reduce");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message); 
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }



        // PUT api/items/{id}
        [HttpPut, Route("{id:guid}")]
        public IHttpActionResult Update(Guid id, Item item)
        {
            try
            {
                item.Id = id;
                var updated = _service.Update(item);

                if (updated)
                    return ApiResponse(true, "Item updated successfully", item);

                return ApiResponse(false, "Item not found or update failed");
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // DELETE api/items/{id}
        [HttpDelete, Route("{id:guid}")]
        public IHttpActionResult Delete(Guid id)
        {
            try
            {
                var deleted = _service.Delete(id);
                if (deleted)
                    return ApiResponse(true, "Item deleted successfully");

                return ApiResponse(false, "Item not found or delete failed");
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }


        // GET api/items/filter?asAtDate=2025-09-11&locationId=guid
        [HttpGet, Route("filter")]
        public IHttpActionResult GetFiltered([FromUri] DateTime? asAtDate = null, [FromUri] Guid? locationId = null)
        {
            try
            {
                var items = _service.GetFiltered(asAtDate, locationId);
                return ApiResponse(true, "Filtered items retrieved successfully", items);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }


        [HttpGet]
        [Route("{id:guid}/stockjournal")]
        public IHttpActionResult GetStockJournal(Guid id)

        {
            try
            {
                var journals = _service.GetStockJournalByItem(id);

                if (journals == null || journals.Count == 0)
                    return ApiResponse(false, "No stock journal entries found for this item");

                return ApiResponse(true, "Stock journal entries retrieved successfully", journals);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }



    }
}

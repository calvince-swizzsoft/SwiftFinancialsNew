using System;
using System.Net;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/itemjournals")]
    public class ItemJournalController : ApiController
    {
        private readonly ItemJournalService _service = new ItemJournalService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        // GET api/itemjournals
        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var journals = _service.GetAll();
                return ApiResponse(true, "Item journals retrieved successfully", journals);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // GET api/itemjournals/{id}
        [HttpGet, Route("{id:guid}")]
        public IHttpActionResult GetById(Guid id)
        {
            try
            {
                var journal = _service.GetById(id);
                if (journal == null)
                    return ApiResponse(false, "Item journal not found");

                return ApiResponse(true, "Item journal retrieved successfully", journal);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // POST api/itemjournals
        [HttpPost, Route("")]
        public IHttpActionResult Create(ItemJournal journal)
        {
            try
            {
                journal.Id = Guid.NewGuid();
                journal.CreatedDate = DateTime.Now;

                _service.Add(journal);
                return ApiResponse(true, "Item journal created successfully", journal);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // PUT api/itemjournals/{id}
        [HttpPut, Route("{id:guid}")]
        public IHttpActionResult Update(Guid id, ItemJournal journal)
        {
            try
            {
                journal.Id = id;
                var updated = _service.Update(journal);

                if (updated)
                    return ApiResponse(true, "Item journal updated successfully", journal);

                return ApiResponse(false, "Item journal not found or update failed");
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // DELETE api/itemjournals/{id}
        [HttpDelete, Route("{id:guid}")]
        public IHttpActionResult Delete(Guid id)
        {
            try
            {
                var deleted = _service.Delete(id);
                if (deleted)
                    return ApiResponse(true, "Item journal deleted successfully");

                return ApiResponse(false, "Item journal not found or delete failed");
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // POST api/itemjournals/{id}/post
        [HttpPost, Route("{id:guid}/post")]
        public IHttpActionResult PostJournal(Guid id)
        {
            try
            {
                var result = _service.PostJournal(id);

                if (result)
                    return ApiResponse(true, "Journal posted successfully");

                return ApiResponse(false, "Failed to post journal");
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("{id:guid}/cancel")]
        public IHttpActionResult Cancel(Guid id)
        {
            try
            {
                var success = _service.CancelJournal(id);

                if (!success)
                    return NotFound();

                return Ok(new
                {
                    success = true,
                    message = "Item journal cancelled successfully",
                    journalId = id
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

    }
}

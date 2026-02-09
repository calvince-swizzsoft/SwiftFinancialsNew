using Application.MainBoundedContext.DTO.AccountsModule;
using System;
using System.Collections.Generic;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/customer-receipts")]
    public class CustomerReceiptController : ApiController
    {
        private readonly CustomerReceiptService _service = new CustomerReceiptService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var receipts = _service.GetAll();
                return ApiResponse(true, "Customer receipts retrieved successfully", receipts);
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
                var receipt = _service.GetById(id);
                if (receipt == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Customer receipt not found" });

                return ApiResponse(true, "Customer receipt retrieved successfully", receipt);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("by-customer/{customerId:guid}")]
        public IHttpActionResult GetByCustomerId(Guid customerId)
        {
            try
            {
                var receipts = _service.GetByCustomerId(customerId);
                return ApiResponse(true, "Customer receipts retrieved successfully", receipts);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("by-customer-reference/{reference}")]
        public IHttpActionResult GetByCustomerReference(string reference)
        {
            try
            {
                if (string.IsNullOrEmpty(reference))
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Customer reference is required" });

                var receipts = _service.GetByCustomerReference(reference);
                return ApiResponse(true, "Customer receipts retrieved successfully", receipts);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("by-date-range")]
        public IHttpActionResult GetByDateRange([FromUri] DateTime startDate, [FromUri] DateTime endDate)
        {
            try
            {
                if (startDate > endDate)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Start date cannot be after end date" });

                var receipts = _service.GetByDateRange(startDate, endDate);
                return ApiResponse(true, "Customer receipts retrieved successfully", receipts);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("by-branch/{branchId:guid}")]
        public IHttpActionResult GetByBranchId(Guid branchId)
        {
            try
            {
                var receipts = _service.GetByBranchId(branchId);
                return ApiResponse(true, "Customer receipts retrieved successfully", receipts);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("search")]
        public IHttpActionResult Search([FromUri] string query = null, [FromUri] DateTime? startDate = null,
                                        [FromUri] DateTime? endDate = null, [FromUri] Guid? branchId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(query) && !startDate.HasValue && !endDate.HasValue && !branchId.HasValue)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "At least one search criteria is required" });

                var receipts = _service.Search(query, startDate, endDate, branchId);
                return ApiResponse(true, "Customer receipts retrieved successfully", receipts);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("summary")]
        public IHttpActionResult GetSummary([FromUri] Guid? branchId = null, [FromUri] DateTime? startDate = null,
                                           [FromUri] DateTime? endDate = null)
        {
            try
            {
                var summary = _service.GetReceiptSummary(branchId, startDate, endDate);
                return ApiResponse(true, "Receipt summary retrieved successfully", summary);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("paged")]
        public IHttpActionResult GetPaged([FromUri] int page = 1, [FromUri] int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100; // Limit page size

                var receipts = _service.GetAllWithPagination(page, pageSize);
                var totalCount = _service.GetTotalCount();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                return ApiResponse(true, "Customer receipts retrieved successfully", new
                {
                    receipts,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalCount,
                        totalPages,
                        hasPrevious = page > 1,
                        hasNext = page < totalPages
                    }
                });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] JournalDTO receipt)
        {
            try
            {
                if (receipt == null)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid receipt data" });

                // Validate required fields
                if (receipt.BranchId == Guid.Empty)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Branch ID is required" });

                if (receipt.PostingPeriodId == Guid.Empty)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Posting period ID is required" });

                if (receipt.TotalValue <= 0)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Total value must be greater than zero" });

                var createdReceipt = _service.Create(receipt);
                return Content(System.Net.HttpStatusCode.Created,
                               new { success = true, message = "Customer receipt created successfully", data = createdReceipt });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPut, Route("{id:guid}")]
        public IHttpActionResult Update(Guid id, [FromBody] JournalDTO receipt)
        {
            try
            {
                if (receipt == null || receipt.Id != id)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid receipt data" });

                _service.Update(receipt);
                return ApiResponse(true, "Customer receipt updated successfully", receipt);
            }
            catch (InvalidOperationException ex)
            {
                return Content(System.Net.HttpStatusCode.BadRequest,
                               new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpDelete, Route("{id:guid}")]
        public IHttpActionResult Delete(Guid id)
        {
            try
            {
                _service.Delete(id);
                return ApiResponse(true, "Customer receipt deleted successfully");
            }
            catch (InvalidOperationException ex)
            {
                return Content(System.Net.HttpStatusCode.BadRequest,
                               new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPost, Route("{id:guid}/lock")]
        public IHttpActionResult LockReceipt(Guid id)
        {
            try
            {
                var receipt = _service.GetById(id);
                if (receipt == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Customer receipt not found" });

                if (receipt.IsLocked)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Receipt is already locked" });

                _service.LockReceipt(id);
                return ApiResponse(true, "Customer receipt locked successfully");
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPost, Route("{id:guid}/unlock")]
        public IHttpActionResult UnlockReceipt(Guid id)
        {
            try
            {
                var receipt = _service.GetById(id);
                if (receipt == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Customer receipt not found" });

                if (!receipt.IsLocked)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Receipt is not locked" });

                _service.UnlockReceipt(id);
                return ApiResponse(true, "Customer receipt unlocked successfully");
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("{id:guid}/with-details")]
        public IHttpActionResult GetWithDetails(Guid id)
        {
            try
            {
                var receipt = _service.GetById(id);
                if (receipt == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Customer receipt not found" });

                // Get journal entries for this receipt using the SIMPLE version
                var journalEntryService = new JournalEntryService();

                // Try the simple version first
                var entries = journalEntryService.GetByJournalIdSimple(id);
                var entrySummary = journalEntryService.GetEntrySummary(id);

                return ApiResponse(true, "Customer receipt with details retrieved successfully", new
                {
                    receipt = receipt,
                    entries = entries,
                    summary = entrySummary,
                    entryCount = ((List<JournalEntryDTO>)entries).Count
                });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        // Add a new endpoint specifically for journal entries
        [HttpGet, Route("{id:guid}/entries")]
        public IHttpActionResult GetJournalEntries(Guid id)
        {
            try
            {
                // Verify the journal exists
                var receipt = _service.GetById(id);
                if (receipt == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Customer receipt not found" });

                // Get journal entries for this receipt
                var journalEntryService = new JournalEntryService();
                var entries = journalEntryService.GetByJournalId(id);
                var entrySummary = journalEntryService.GetEntrySummary(id);

                return ApiResponse(true, "Journal entries retrieved successfully", new
                {
                    entries = entries,
                    summary = entrySummary,
                    count = ((List<JournalEntryDTO>)entries).Count
                });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }
    }
}
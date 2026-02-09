using Application.MainBoundedContext.DTO.RegistryModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/nextofkin")]
    public class NextOfKinController : ApiController
    {
        private readonly NextOfKinService _service = new NextOfKinService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var nextOfKins = _service.GetAll();
                return ApiResponse(true, "Next of kins retrieved successfully", nextOfKins);
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
                var nextOfKin = _service.GetById(id);
                if (nextOfKin == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Next of kin not found" });

                return ApiResponse(true, "Next of kin retrieved successfully", nextOfKin);
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
                var nextOfKins = _service.GetByCustomerId(customerId);
                return ApiResponse(true, "Next of kins retrieved successfully", nextOfKins);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("percentage-summary/{customerId:guid}")]
        public IHttpActionResult GetPercentageSummary(Guid customerId)
        {
            try
            {
                var summary = _service.GetPercentageSummary(customerId);
                return ApiResponse(true, "Percentage summary retrieved successfully", summary);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] NextOfKinDTO nextOfKin)
        {
            try
            {
                if (nextOfKin == null)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid next of kin data" });

                // Validate required fields
                if (nextOfKin.CustomerId == Guid.Empty)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Customer ID is required" });

                if (string.IsNullOrEmpty(nextOfKin.FirstName))
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "First name is required" });

                if (nextOfKin.NominatedPercentage <= 0 || nextOfKin.NominatedPercentage > 100)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Nominated percentage must be between 0 and 100" });

                // Get current total percentage BEFORE creating
                var currentTotal = _service.GetTotalPercentageForCustomer(nextOfKin.CustomerId);
                var newTotal = currentTotal + nextOfKin.NominatedPercentage;

                if (newTotal > 100)
                {
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new
                                   {
                                       success = false,
                                       message = $"Cannot add next of kin. Total percentage would be {newTotal:0.##}%. " +
                                                 $"Current total: {currentTotal:0.##}%, Remaining: {100 - currentTotal:0.##}%",
                                       currentTotal,
                                       remaining = 100 - currentTotal
                                   });
                }

                var createdNextOfKin = _service.Create(nextOfKin);
                var summary = _service.GetPercentageSummary(nextOfKin.CustomerId);

                return Content(System.Net.HttpStatusCode.Created,
                               new
                               {
                                   success = true,
                                   message = "Next of kin created successfully",
                                   data = createdNextOfKin,
                                   percentageSummary = summary
                               });
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

        [HttpPost, Route("bulk")]
        public IHttpActionResult CreateBulk([FromBody] NextOfKinBulkCreateDTO bulkCreate)
        {
            try
            {
                if (bulkCreate == null || bulkCreate.NextOfKins == null || bulkCreate.NextOfKins.Count == 0)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid next of kin data" });

                if (bulkCreate.CustomerId == Guid.Empty)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Customer ID is required" });

                // Validate each next of kin
                for (int i = 0; i < bulkCreate.NextOfKins.Count; i++)
                {
                    var nok = bulkCreate.NextOfKins[i];
                    if (string.IsNullOrEmpty(nok.FirstName))
                        return Content(System.Net.HttpStatusCode.BadRequest,
                                       new { success = false, message = $"Next of kin at index {i}: First name is required" });

                    if (nok.NominatedPercentage <= 0 || nok.NominatedPercentage > 100)
                        return Content(System.Net.HttpStatusCode.BadRequest,
                                       new { success = false, message = $"Next of kin at index {i}: Nominated percentage must be between 0 and 100" });
                }

                // Get current total percentage for customer
                var currentTotal = _service.GetTotalPercentageForCustomer(bulkCreate.CustomerId);

                double bulkTotalPercentage = 0;
                foreach (var nextOfKin in bulkCreate.NextOfKins)
                {
                    nextOfKin.CustomerId = bulkCreate.CustomerId;
                    bulkTotalPercentage += nextOfKin.NominatedPercentage;
                }

                double newTotal = currentTotal + bulkTotalPercentage;

                if (newTotal > 100)
                {
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new
                                   {
                                       success = false,
                                       message = $"Cannot add next of kins. Total percentage would be {newTotal:0.##}%. " +
                                                 $"Current total: {currentTotal:0.##}%, " +
                                                 $"Bulk total: {bulkTotalPercentage:0.##}%, " +
                                                 $"Remaining: {100 - currentTotal:0.##}%",
                                       currentTotal,
                                       bulkTotal = bulkTotalPercentage,
                                       remaining = 100 - currentTotal
                                   });
                }

                var createdNextOfKins = new System.Collections.Generic.List<NextOfKinDTO>();
                var errors = new System.Collections.Generic.List<string>();

                // Create each next of kin
                foreach (var nextOfKin in bulkCreate.NextOfKins)
                {
                    try
                    {
                        var created = _service.Create(nextOfKin);
                        createdNextOfKins.Add(created);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error creating next of kin {nextOfKin.FirstName} {nextOfKin.LastName}: {ex.Message}");
                    }
                }

                var summary = _service.GetPercentageSummary(bulkCreate.CustomerId);

                return Content(System.Net.HttpStatusCode.Created,
                               new
                               {
                                   success = true,
                                   message = $"{createdNextOfKins.Count} next of kins created successfully",
                                   createdCount = createdNextOfKins.Count,
                                   errorCount = errors.Count,
                                   data = createdNextOfKins,
                                   errors = errors,
                                   percentageSummary = summary
                               });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPut, Route("{id}")]
        public IHttpActionResult Update(Guid id, [FromBody] NextOfKinDTO nextOfKin)
        {
            try
            {
                if (nextOfKin == null)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid next of kin data" });

                if (id != nextOfKin.Id)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "ID mismatch" });

                // Validate required fields
                if (nextOfKin.CustomerId == Guid.Empty)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Customer ID is required" });

                if (string.IsNullOrEmpty(nextOfKin.FirstName))
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "First name is required" });

                if (nextOfKin.NominatedPercentage <= 0 || nextOfKin.NominatedPercentage > 100)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Nominated percentage must be between 1 and 100" });

                // Check for data inconsistencies BEFORE attempting update
                var dataIssues = _service.ValidateCustomerPercentages();
                if (dataIssues.ContainsKey(nextOfKin.CustomerId))
                {
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new
                                   {
                                       success = false,
                                       message = "Data inconsistency detected",
                                       details = $"Customer already has {dataIssues[nextOfKin.CustomerId]:0.##}% allocated",
                                       fixRequired = true,
                                       customerId = nextOfKin.CustomerId
                                   });
                }

                // Check if exists
                var existing = _service.GetById(id);
                if (existing == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Next of kin not found" });

                _service.Update(nextOfKin);
                var summary = _service.GetPercentageSummary(nextOfKin.CustomerId);

                return Content(System.Net.HttpStatusCode.OK,
                               new
                               {
                                   success = true,
                                   message = "Next of kin updated successfully",
                                   data = _service.GetById(id),
                                   percentageSummary = summary
                               });
            }
            catch (InvalidOperationException ex)
            {
                return Content(System.Net.HttpStatusCode.BadRequest,
                               new { success = false, message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return Content(System.Net.HttpStatusCode.NotFound,
                               new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        // Add these new endpoints for diagnostics and fixing data
        [HttpGet, Route("diagnostics/{customerId}")]
        public IHttpActionResult GetDiagnostics(Guid customerId)
        {
            try
            {
                var total = _service.GetTotalPercentageForCustomer(customerId);
                var noks = _service.GetByCustomerId(customerId).ToList();

                return Ok(new
                {
                    customerId,
                    totalAllocated = total,
                    overAllocated = total > 100,
                    nextOfKinsCount = noks.Count,
                    nextOfKins = noks.Select(n => new
                    {
                        n.Id,
                        n.FirstName,
                        n.LastName,
                        n.NominatedPercentage,
                        n.CreatedDate
                    }).OrderByDescending(n => n.CreatedDate),
                    summary = _service.GetPercentageSummary(customerId),
                    dataIntegrityIssue = total > 100
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost, Route("fix-percentages/{customerId}")]
        public IHttpActionResult FixCustomerPercentages(Guid customerId)
        {
            try
            {
                var totalBefore = _service.GetTotalPercentageForCustomer(customerId);

                if (totalBefore <= 100)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "No fix needed - percentages are within valid range",
                        totalBefore,
                        totalAfter = totalBefore
                    });
                }

                _service.FixCustomerPercentage(customerId);
                var totalAfter = _service.GetTotalPercentageForCustomer(customerId);

                return Ok(new
                {
                    success = true,
                    message = "Percentages have been normalized",
                    totalBefore = Math.Round(totalBefore, 2),
                    totalAfter = Math.Round(totalAfter, 2),
                    summary = _service.GetPercentageSummary(customerId)
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpDelete, Route("{id:guid}")]
        public IHttpActionResult Delete(Guid id)
        {
            try
            {
                var nextOfKin = _service.GetById(id);
                if (nextOfKin == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Next of kin not found" });

                _service.Delete(id);
                var summary = _service.GetPercentageSummary(nextOfKin.CustomerId);

                return ApiResponse(true, "Next of kin deleted successfully", new
                {
                    percentageSummary = summary
                });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpDelete, Route("by-customer/{customerId:guid}")]
        public IHttpActionResult DeleteByCustomerId(Guid customerId)
        {
            try
            {
                _service.DeleteByCustomerId(customerId);
                return ApiResponse(true, "All next of kins for customer deleted successfully");
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }
    }

    public class NextOfKinBulkCreateDTO
    {
        public Guid CustomerId { get; set; }
        public System.Collections.Generic.List<NextOfKinDTO> NextOfKins { get; set; }
    }
}
using Application.MainBoundedContext.DTO.AccountsModule;
using System;
using System.Collections.Generic;
using System.Web.Http;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/costcenters")]
    public class CostCentersController : ApiController
    {
        private readonly CostCenterService _service = new CostCenterService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var costCenters = _service.GetAll();
                return ApiResponse(true, "Cost centers retrieved successfully", costCenters);
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
                var costCenter = _service.GetById(id);
                if (costCenter == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Cost center not found" });

                return ApiResponse(true, "Cost center retrieved successfully", costCenter);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("by-name/{name}")]
        public IHttpActionResult GetByName(string name)
        {
            try
            {
                var costCenter = _service.GetByName(name);
                if (costCenter == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Cost center not found" });

                return ApiResponse(true, "Cost center retrieved successfully", costCenter);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("search")]
        public IHttpActionResult Search([FromUri] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Search query is required" });

                var costCenters = _service.Search(query);
                return ApiResponse(true, "Cost centers retrieved successfully", costCenters);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("{id:guid}/usage")]
        public IHttpActionResult GetUsage(Guid id)
        {
            try
            {
                var costCenter = _service.GetById(id);
                if (costCenter == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Cost center not found" });

                var usageCount = _service.GetUsageCount(id);
                var isInUse = _service.IsCostCenterInUse(id);

                return ApiResponse(true, "Usage information retrieved successfully", new
                {
                    CostCenter = costCenter,
                    UsageCount = usageCount,
                    IsInUse = isInUse,
                    CanDelete = usageCount == 0 && !costCenter.IsLocked
                });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] CostCenterDTO costCenter)
        {
            try
            {
                if (costCenter == null)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid cost center data" });

                // Validate required fields
                if (string.IsNullOrWhiteSpace(costCenter.Description))
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Cost center name is required" });

                // Trim and validate description
                costCenter.Description = costCenter.Description.Trim();
                if (costCenter.Description.Length < 2)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Cost center name must be at least 2 characters" });

                var createdCostCenter = _service.Create(costCenter);

                return Content(System.Net.HttpStatusCode.Created,
                               new
                               {
                                   success = true,
                                   message = "Cost center created successfully",
                                   data = createdCostCenter
                               });
            }
            catch (InvalidOperationException ex)
            {
                // Handle duplicate name or other business rule violations
                return Content(System.Net.HttpStatusCode.BadRequest,
                               new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPut, Route("{id:guid}")]
        public IHttpActionResult Update(Guid id, [FromBody] CostCenterDTO costCenter)
        {
            try
            {
                if (costCenter == null || costCenter.Id != id)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid cost center data" });

                // Check if cost center exists
                var existing = _service.GetById(id);
                if (existing == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Cost center not found" });

                // Validate required fields
                if (string.IsNullOrWhiteSpace(costCenter.Description))
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Cost center name is required" });

                // Trim and validate description
                costCenter.Description = costCenter.Description.Trim();
                if (costCenter.Description.Length < 2)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Cost center name must be at least 2 characters" });

                _service.Update(costCenter);
                return ApiResponse(true, "Cost center updated successfully", costCenter);
            }
            catch (InvalidOperationException ex)
            {
                // Handle duplicate name or other business rule violations
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
                // Check if cost center exists
                var costCenter = _service.GetById(id);
                if (costCenter == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Cost center not found" });

                // Check if cost center is locked
                if (costCenter.IsLocked)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Cannot delete a locked cost center" });

                _service.Delete(id);
                return ApiResponse(true, "Cost center deleted successfully");
            }
            catch (InvalidOperationException ex)
            {
                // Handle business rule violations (e.g., cost center in use)
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
        public IHttpActionResult Lock(Guid id)
        {
            try
            {
                var costCenter = _service.GetById(id);
                if (costCenter == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Cost center not found" });

                costCenter.IsLocked = true;
                _service.Update(costCenter);

                return ApiResponse(true, "Cost center locked successfully", costCenter);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPost, Route("{id:guid}/unlock")]
        public IHttpActionResult Unlock(Guid id)
        {
            try
            {
                var costCenter = _service.GetById(id);
                if (costCenter == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Cost center not found" });

                costCenter.IsLocked = false;
                _service.Update(costCenter);

                return ApiResponse(true, "Cost center unlocked successfully", costCenter);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("usage-summary")]
        public IHttpActionResult GetUsageSummary()
        {
            try
            {
                var costCenters = _service.GetAll();
                var usageSummary = new List<CostCenterUsageDTO>();

                foreach (var costCenter in costCenters)
                {
                    var usageCount = _service.GetUsageCount(costCenter.Id);
                    usageSummary.Add(new CostCenterUsageDTO
                    {
                        CostCenterId = costCenter.Id,
                        CostCenterName = costCenter.Description,
                        UsageCount = usageCount,
                        IsLocked = costCenter.IsLocked
                    });
                }

                return ApiResponse(true, "Usage summary retrieved successfully", usageSummary);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }
    }
}
using Application.MainBoundedContext.DTO.RegistryModule;
using System;
using System.Linq;
using System.Web.Http;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/customers-comprehensive")]
    public class CustomersComprehensiveController : ApiController
    {
        private readonly CustomerComprehensiveService _service = new CustomerComprehensiveService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        [HttpGet, Route("details/{customerId:guid}")]
        public IHttpActionResult GetCustomerFullDetails(Guid customerId)
        {
            try
            {
                var customerDetails = _service.GetCustomerFullDetails(customerId);
                if (customerDetails == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Customer not found" });

                return ApiResponse(true, "Customer details retrieved successfully", customerDetails);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("all")]
        public IHttpActionResult GetAllCustomersWithDetails([FromUri] int page = 1, [FromUri] int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var customers = _service.GetAllCustomersWithDetails(page, pageSize);
                return ApiResponse(true, "Customers with details retrieved successfully", new
                {
                    page,
                    pageSize,
                    customers = customers
                });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("search")]
        public IHttpActionResult SearchCustomersWithDetails([FromUri] string query, [FromUri] int page = 1, [FromUri] int pageSize = 20)
        {
            try
            {
                if (string.IsNullOrEmpty(query))
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Search query is required" });

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var customers = _service.SearchCustomersWithDetails(query, page, pageSize);
                return ApiResponse(true, "Customers search results retrieved successfully", new
                {
                    query,
                    page,
                    pageSize,
                    customers = customers
                });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("by-type/{type:int}")]
        public IHttpActionResult GetCustomersByTypeWithDetails(int type, [FromUri] int page = 1, [FromUri] int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var customers = _service.GetCustomersByTypeWithDetails(type, page, pageSize);
                return ApiResponse(true, $"Customers of type {type} retrieved successfully", new
                {
                    customerType = type,
                    page,
                    pageSize,
                    customers = customers
                });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("by-station/{stationId:guid}")]
        public IHttpActionResult GetCustomersByStationWithDetails(Guid stationId, [FromUri] int page = 1, [FromUri] int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var customers = _service.GetCustomersByStationWithDetails(stationId, page, pageSize);
                return ApiResponse(true, "Customers by station retrieved successfully", new
                {
                    stationId,
                    page,
                    pageSize,
                    customers = customers
                });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("by-name/{name}")]
        public IHttpActionResult GetCustomersByNameWithDetails(string name, [FromUri] int page = 1, [FromUri] int pageSize = 20)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Name is required" });

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var customers = _service.GetCustomersByNameWithDetails(name, page, pageSize);
                return ApiResponse(true, "Customers by name retrieved successfully", new
                {
                    name,
                    page,
                    pageSize,
                    customers = customers
                });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("dashboard-summary")]
        public IHttpActionResult GetDashboardSummary()
        {
            try
            {
                var summary = _service.GetDashboardSummary();
                return ApiResponse(true, "Dashboard summary retrieved successfully", summary);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("without-nextofkin")]
        public IHttpActionResult GetCustomersWithoutNextOfKin([FromUri] int page = 1, [FromUri] int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                // This is a simplified implementation - you might want to create a specific method for this
                var allCustomers = _service.GetAllCustomersWithDetails(page, pageSize * 10); // Get more to filter
                var customersWithoutNextOfKin = new System.Collections.Generic.List<CustomerComprehensiveService.CustomerFullDetailsDTO>();

                foreach (var customer in allCustomers)
                {
                    if (customer.NextOfKins == null || customer.NextOfKins.Count == 0)
                    {
                        customersWithoutNextOfKin.Add(customer);
                    }
                }

                // Apply pagination after filtering
                var pagedResult = customersWithoutNextOfKin.Skip((page - 1) * pageSize).Take(pageSize);

                return ApiResponse(true, "Customers without next of kin retrieved successfully", new
                {
                    page,
                    pageSize,
                    totalCount = customersWithoutNextOfKin.Count,
                    customers = pagedResult
                });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("without-accounts")]
        public IHttpActionResult GetCustomersWithoutAccounts([FromUri] int page = 1, [FromUri] int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                // This is a simplified implementation
                var allCustomers = _service.GetAllCustomersWithDetails(page, pageSize * 10); // Get more to filter
                var customersWithoutAccounts = new System.Collections.Generic.List<CustomerComprehensiveService.CustomerFullDetailsDTO>();

                foreach (var customer in allCustomers)
                {
                    if (customer.Accounts == null || customer.Accounts.Count == 0)
                    {
                        customersWithoutAccounts.Add(customer);
                    }
                }

                // Apply pagination after filtering
                var pagedResult = customersWithoutAccounts.Skip((page - 1) * pageSize).Take(pageSize);

                return ApiResponse(true, "Customers without accounts retrieved successfully", new
                {
                    page,
                    pageSize,
                    totalCount = customersWithoutAccounts.Count,
                    customers = pagedResult
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
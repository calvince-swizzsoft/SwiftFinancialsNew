using Application.MainBoundedContext.DTO.AccountsModule;
using System;
using System.Collections.Generic;
using System.Web.Http;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/loanproducts")]
    public class LoanProductsController : ApiController
    {
        private readonly LoanProductService _service = new LoanProductService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var products = _service.GetAll();
                return ApiResponse(true, "Loan products retrieved successfully", products);
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
                var product = _service.GetById(id);
                if (product == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Loan product not found" });

                return ApiResponse(true, "Loan product retrieved successfully", product);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("by-code/{code:int}")]
        public IHttpActionResult GetByCode(int code)
        {
            try
            {
                var product = _service.GetByCode(code);
                if (product == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Loan product not found" });

                return ApiResponse(true, "Loan product retrieved successfully", product);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("by-category/{category:int}")]
        public IHttpActionResult GetByCategory(int category)
        {
            try
            {
                var products = _service.GetByCategory(category);
                return ApiResponse(true, "Loan products retrieved successfully", products);
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

                var products = _service.Search(query);
                return ApiResponse(true, "Loan products retrieved successfully", products);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("generate-code")]
        public IHttpActionResult GenerateCode()
        {
            try
            {
                int code = _service.GenerateCode();
                return ApiResponse(true, "Code generated successfully", new { Code = code });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("validate-code/{code:int}")]
        public IHttpActionResult ValidateCode(int code)
        {
            try
            {
                var existing = _service.GetByCode(code);
                if (existing != null)
                {
                    return ApiResponse(false, $"Loan product code {code} already exists", new
                    {
                        IsAvailable = false,
                        ExistingProduct = existing.Description
                    });
                }

                return ApiResponse(true, $"Loan product code {code} is available", new { IsAvailable = true });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("{id:guid}/in-use")]
        public IHttpActionResult CheckInUse(Guid id)
        {
            try
            {
                var product = _service.GetById(id);
                if (product == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Loan product not found" });

                bool isInUse = _service.IsProductInUse(id);
                return ApiResponse(true, "Product usage check completed", new
                {
                    IsInUse = isInUse,
                    CanDelete = !isInUse && !product.IsLocked
                });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] LoanProductDTO product)
        {
            try
            {
                if (product == null)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid loan product data" });

                // Validate required fields
                if (string.IsNullOrWhiteSpace(product.Description))
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Product name is required" });

                if (product.LoanInterestAnnualPercentageRate <= 0)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Annual percentage rate must be greater than 0" });

                if (product.LoanRegistrationTermInMonths <= 0)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Term in months must be greater than 0" });

                if (product.LoanRegistrationMinimumAmount <= 0)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Minimum amount must be greater than 0" });

                if (product.LoanRegistrationMaximumAmount <= 0)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Maximum amount must be greater than 0" });

                if (product.LoanRegistrationMaximumAmount < product.LoanRegistrationMinimumAmount)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Maximum amount must be greater than or equal to minimum amount" });

                // Validate GL accounts
                if (product.ChartOfAccountId == Guid.Empty)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Principal GL account is required" });

                if (product.InterestReceivedChartOfAccountId == Guid.Empty)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Interest received GL account is required" });

                if (product.InterestReceivableChartOfAccountId == Guid.Empty)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Interest receivable GL account is required" });

                if (product.InterestChargedChartOfAccountId == Guid.Empty)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Interest charged GL account is required" });

                var createdProduct = _service.Create(product);

                return Content(System.Net.HttpStatusCode.Created,
                               new
                               {
                                   success = true,
                                   message = "Loan product created successfully",
                                   data = createdProduct
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

        [HttpPut, Route("{id:guid}")]
        public IHttpActionResult Update(Guid id, [FromBody] LoanProductDTO product)
        {
            try
            {
                if (product == null || product.Id != id)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid loan product data" });

                // Check if product exists
                var existing = _service.GetById(id);
                if (existing == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Loan product not found" });

                // Check if product is locked
                if (existing.IsLocked)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Cannot update a locked loan product" });

                // Validate required fields
                if (string.IsNullOrWhiteSpace(product.Description))
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Product name is required" });

                if (product.LoanInterestAnnualPercentageRate <= 0)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Annual percentage rate must be greater than 0" });

                if (product.LoanRegistrationTermInMonths <= 0)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Term in months must be greater than 0" });

                if (product.LoanRegistrationMinimumAmount <= 0)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Minimum amount must be greater than 0" });

                if (product.LoanRegistrationMaximumAmount <= 0)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Maximum amount must be greater than 0" });

                if (product.LoanRegistrationMaximumAmount < product.LoanRegistrationMinimumAmount)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Maximum amount must be greater than or equal to minimum amount" });

                // Validate GL accounts
                if (product.ChartOfAccountId == Guid.Empty)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Principal GL account is required" });

                if (product.InterestReceivedChartOfAccountId == Guid.Empty)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Interest received GL account is required" });

                if (product.InterestReceivableChartOfAccountId == Guid.Empty)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Interest receivable GL account is required" });

                if (product.InterestChargedChartOfAccountId == Guid.Empty)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Interest charged GL account is required" });

                _service.Update(product);
                return ApiResponse(true, "Loan product updated successfully", product);
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

        [HttpDelete, Route("{id:guid}")]
        public IHttpActionResult Delete(Guid id)
        {
            try
            {
                // Check if product exists
                var product = _service.GetById(id);
                if (product == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Loan product not found" });

                _service.Delete(id);
                return ApiResponse(true, "Loan product deleted successfully");
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

        [HttpPost, Route("{id:guid}/lock")]
        public IHttpActionResult Lock(Guid id)
        {
            try
            {
                var product = _service.GetById(id);
                if (product == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Loan product not found" });

                product.IsLocked = true;
                _service.Update(product);

                return ApiResponse(true, "Loan product locked successfully", product);
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
                var product = _service.GetById(id);
                if (product == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Loan product not found" });

                product.IsLocked = false;
                _service.Update(product);

                return ApiResponse(true, "Loan product unlocked successfully", product);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("active")]
        public IHttpActionResult GetActiveProducts()
        {
            try
            {
                var products = _service.GetAll();
                // Filter out locked products for active listing
                var activeProducts = new List<LoanProductDTO>();
                foreach (var product in products)
                {
                    if (!product.IsLocked)
                    {
                        activeProducts.Add(product);
                    }
                }
                return ApiResponse(true, "Active loan products retrieved successfully", activeProducts);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }
    }
}
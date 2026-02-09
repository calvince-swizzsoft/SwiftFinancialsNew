using Application.MainBoundedContext.DTO.AccountsModule;
using System;
using System.Web.Http;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/savings-products")]
    public class SavingsProductsController : ApiController
    {
        private readonly SavingsProductService _service = new SavingsProductService();

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
                return ApiResponse(true, "Savings products retrieved successfully", products);
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
                                   new { success = false, message = "Savings product not found" });

                return ApiResponse(true, "Savings product retrieved successfully", product);
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
                                   new { success = false, message = "Savings product not found" });

                return ApiResponse(true, "Savings product retrieved successfully", product);
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
                var products = _service.GetActiveProducts();
                return ApiResponse(true, "Active savings products retrieved successfully", products);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("default")]
        public IHttpActionResult GetDefaultProducts()
        {
            try
            {
                var products = _service.GetDefaultProducts();
                return ApiResponse(true, "Default savings products retrieved successfully", products);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("mandatory")]
        public IHttpActionResult GetMandatoryProducts()
        {
            try
            {
                var products = _service.GetMandatoryProducts();
                return ApiResponse(true, "Mandatory savings products retrieved successfully", products);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] SavingsProductDTO product)
        {
            try
            {
                if (product == null)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid product data" });

                // Validate required fields
                if (string.IsNullOrEmpty(product.Description))
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Product description is required" });

                if (product.ChartOfAccountId == Guid.Empty)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Chart of Account is required" });

                var createdProduct = _service.Create(product);

                return Content(System.Net.HttpStatusCode.Created,
                               new
                               {
                                   success = true,
                                   message = "Savings product created successfully",
                                   data = createdProduct
                               });
            }
            catch (ArgumentException ex)
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

        [HttpPut, Route("{id:guid}")]
        public IHttpActionResult Update(Guid id, [FromBody] SavingsProductDTO product)
        {
            try
            {
                if (product == null || product.Id != id)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid product data" });

                // Validate required fields
                if (string.IsNullOrEmpty(product.Description))
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Product description is required" });

                if (product.ChartOfAccountId == Guid.Empty)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Chart of Account is required" });

                _service.Update(product);
                return ApiResponse(true, "Savings product updated successfully", product);
            }
            catch (ArgumentException ex)
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
                return ApiResponse(true, "Savings product deleted successfully");
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

        [HttpPut, Route("{id:guid}/lock")]
        public IHttpActionResult Lock(Guid id)
        {
            try
            {
                _service.Lock(id);
                return ApiResponse(true, "Savings product locked successfully");
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPut, Route("{id:guid}/unlock")]
        public IHttpActionResult Unlock(Guid id)
        {
            try
            {
                _service.Unlock(id);
                return ApiResponse(true, "Savings product unlocked successfully");
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPut, Route("{id:guid}/set-default")]
        public IHttpActionResult SetAsDefault(Guid id)
        {
            try
            {
                _service.SetAsDefault(id);
                return ApiResponse(true, "Savings product set as default successfully");
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }
    }
}
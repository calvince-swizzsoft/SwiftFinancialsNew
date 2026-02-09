using Application.MainBoundedContext.DTO.AdministrationModule;
using System;
using System.Collections.Generic;
using System.Web.Http;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/company-attached-products")]
    public class CompanyAttachedProductsController : ApiController
    {
        private readonly CompanyAttachedProductService _service = new CompanyAttachedProductService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var attachedProducts = _service.GetAll();
                return ApiResponse(true, "Company attached products retrieved successfully", attachedProducts);
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
                var attachedProduct = _service.GetById(id);
                if (attachedProduct == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Company attached product not found" });

                return ApiResponse(true, "Company attached product retrieved successfully", attachedProduct);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("by-company/{companyId:guid}")]
        public IHttpActionResult GetByCompany(Guid companyId)
        {
            try
            {
                var attachedProducts = _service.GetByCompanyId(companyId);
                return ApiResponse(true, "Company attached products retrieved successfully", attachedProducts);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("by-product/{productId:guid}")]
        public IHttpActionResult GetByProduct(Guid productId)
        {
            try
            {
                var attachedProducts = _service.GetByProductId(productId);
                return ApiResponse(true, "Company attached products retrieved successfully", attachedProducts);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] CompanyAttachedProductDTO attachedProduct)
        {
            try
            {
                if (attachedProduct == null)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid attached product data" });

                if (attachedProduct.CompanyId == Guid.Empty)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Company ID is required" });

                if (attachedProduct.TargetProductId == Guid.Empty)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Target Product ID is required" });

                var createdAttachedProduct = _service.Create(attachedProduct);

                return Content(System.Net.HttpStatusCode.Created,
                               new
                               {
                                   success = true,
                                   message = "Company attached product created successfully",
                                   data = createdAttachedProduct
                               });
            }
            catch (ArgumentException ex)
            {
                return Content(System.Net.HttpStatusCode.BadRequest,
                               new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Content(System.Net.HttpStatusCode.Conflict,
                               new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPost, Route("attach-multiple")]
        public IHttpActionResult AttachMultiple([FromBody] AttachMultipleRequest request)
        {
            try
            {
                if (request == null || request.CompanyId == Guid.Empty)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Company ID is required" });

                if (request.ProductIds == null || request.ProductIds.Count == 0)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "At least one product ID is required" });

                _service.CreateMultipleForCompany(request.CompanyId, request.ProductIds);

                return ApiResponse(true, "Products attached to company successfully");
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

        [HttpPost, Route("attach-mandatory/{companyId:guid}")]
        public IHttpActionResult AttachMandatoryProducts(Guid companyId)
        {
            try
            {
                _service.AttachMandatoryProductsToCompany(companyId);
                return ApiResponse(true, "Mandatory products attached to company successfully");
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
                return ApiResponse(true, "Company attached product deleted successfully");
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpDelete, Route("by-company-product")]
        public IHttpActionResult DeleteByCompanyAndProduct([FromUri] Guid companyId, [FromUri] Guid productId)
        {
            try
            {
                _service.DeleteByCompanyAndProduct(companyId, productId);
                return ApiResponse(true, "Company attached product deleted successfully");
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpDelete, Route("by-company/{companyId:guid}")]
        public IHttpActionResult DeleteAllByCompany(Guid companyId)
        {
            try
            {
                _service.DeleteAllByCompany(companyId);
                return ApiResponse(true, "All company attached products deleted successfully");
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }
    }

    // Request model for attaching multiple products
    public class AttachMultipleRequest
    {
        public Guid CompanyId { get; set; }
        public List<Guid> ProductIds { get; set; }
    }
}
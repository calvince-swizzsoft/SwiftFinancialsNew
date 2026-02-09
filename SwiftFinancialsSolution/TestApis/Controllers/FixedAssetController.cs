using System;
using System.Net;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/fixedasset")]
    public class FixedAssetController : ApiController
    {
        private readonly FixedAssetService _service = new FixedAssetService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        // GET api/fixedasset
        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var list = _service.GetAll();
                return ApiResponse(true, "Fixed Assets retrieved successfully", list);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // GET api/fixedasset/{id}
        [HttpGet, Route("{id:guid}")]
        public IHttpActionResult GetById(Guid id)
        {
            try
            {
                var asset = _service.GetById(id);
                if (asset == null)
                    return ApiResponse(false, "Fixed Asset not found");

                return ApiResponse(true, "Fixed Asset retrieved successfully", asset);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // POST api/fixedasset
        [HttpPost, Route("")]
        public IHttpActionResult Create(FixedAsset asset)
        {
            try
            {
                asset.Id = Guid.NewGuid();
                asset.CreatedDate = DateTime.Now;

                _service.Add(asset);
                return ApiResponse(true, "Fixed Asset created successfully", asset);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // PUT api/fixedasset/{id}
        [HttpPut, Route("{id:guid}")]
        public IHttpActionResult Update(Guid id, FixedAsset asset)
        {
            try
            {
                asset.Id = id;
                var updated = _service.Update(asset);

                if (updated)
                    return ApiResponse(true, "Fixed Asset updated successfully", asset);

                return ApiResponse(false, "Fixed Asset not found or update failed");
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // DELETE api/fixedasset/{id}
        [HttpDelete, Route("{id:guid}")]
        public IHttpActionResult Delete(Guid id)
        {
            try
            {
                var deleted = _service.Delete(id);
                if (deleted)
                    return ApiResponse(true, "Fixed Asset deleted successfully");

                return ApiResponse(false, "Fixed Asset not found or delete failed");
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }
    }
}

using System;
using System.Net;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/faclass")]
    public class FAClassController : ApiController
    {
        private readonly FAClassService _service = new FAClassService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        // GET api/faclass
        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var list = _service.GetAll();
                return ApiResponse(true, "FA Classes retrieved successfully", list);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // GET api/faclass/{id}
        [HttpGet, Route("{id:guid}")]
        public IHttpActionResult GetById(Guid id)
        {
            try
            {
                var faClass = _service.GetById(id);
                if (faClass == null)
                    return ApiResponse(false, "FA Class not found");

                return ApiResponse(true, "FA Class retrieved successfully", faClass);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // POST api/faclass
        [HttpPost, Route("")]
        public IHttpActionResult Create(FAClass faClass)
        {
            try
            {
                faClass.Id = Guid.NewGuid();
                faClass.CreatedDate = DateTime.Now;

                _service.Add(faClass);
                return ApiResponse(true, "FA Class created successfully", faClass);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // PUT api/faclass/{id}
        [HttpPut, Route("{id:guid}")]
        public IHttpActionResult Update(Guid id, FAClass faClass)
        {
            try
            {
                faClass.Id = id;
                var updated = _service.Update(faClass);

                if (updated)
                    return ApiResponse(true, "FA Class updated successfully", faClass);

                return ApiResponse(false, "FA Class not found or update failed");
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // DELETE api/faclass/{id}
        [HttpDelete, Route("{id:guid}")]
        public IHttpActionResult Delete(Guid id)
        {
            try
            {
                var deleted = _service.Delete(id);
                if (deleted)
                    return ApiResponse(true, "FA Class deleted successfully");

                return ApiResponse(false, "FA Class not found or delete failed");
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }
    }
}

using System;
using System.Net;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/fasubclass")]
    public class FASubClassController : ApiController
    {
        private readonly FASubClassService _service = new FASubClassService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        // GET api/fasubclass
        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var list = _service.GetAll();
                return ApiResponse(true, "FA SubClasses retrieved successfully", list);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // GET api/fasubclass/{id}
        [HttpGet, Route("{id:guid}")]
        public IHttpActionResult GetById(Guid id)
        {
            try
            {
                var subClass = _service.GetById(id);
                if (subClass == null)
                    return ApiResponse(false, "FA SubClass not found");

                return ApiResponse(true, "FA SubClass retrieved successfully", subClass);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // POST api/fasubclass
        [HttpPost, Route("")]
        public IHttpActionResult Create(FASubClass subClass)
        {
            try
            {
                subClass.Id = Guid.NewGuid();
                subClass.CreatedDate = DateTime.Now;

                _service.Add(subClass);
                return ApiResponse(true, "FA SubClass created successfully", subClass);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // PUT api/fasubclass/{id}
        [HttpPut, Route("{id:guid}")]
        public IHttpActionResult Update(Guid id, FASubClass subClass)
        {
            try
            {
                subClass.Id = id;
                var updated = _service.Update(subClass);

                if (updated)
                    return ApiResponse(true, "FA SubClass updated successfully", subClass);

                return ApiResponse(false, "FA SubClass not found or update failed");
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // DELETE api/fasubclass/{id}
        [HttpDelete, Route("{id:guid}")]
        public IHttpActionResult Delete(Guid id)
        {
            try
            {
                var deleted = _service.Delete(id);
                if (deleted)
                    return ApiResponse(true, "FA SubClass deleted successfully");

                return ApiResponse(false, "FA SubClass not found or delete failed");
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }
    }
}

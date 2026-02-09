using System;
using System.Net;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/categories")]
    public class CategoryController : ApiController
    {
        private readonly CategoryService _service = new CategoryService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        // GET api/categories
        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var categories = _service.GetAll();
                return ApiResponse(true, "Categories retrieved successfully", categories);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // GET api/categories/{id}
        [HttpGet, Route("{id:guid}")]
        public IHttpActionResult GetById(Guid id)
        {
            try
            {
                var category = _service.GetById(id);
                if (category == null)
                    return ApiResponse(false, "Category not found");

                return ApiResponse(true, "Category retrieved successfully", category);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // POST api/categories
        [HttpPost, Route("")]
        public IHttpActionResult Create(Category category)
        {
            try
            {
                category.Id = Guid.NewGuid();
                category.CreatedDate = DateTime.Now;

                _service.Add(category);
                return ApiResponse(true, "Category created successfully", category);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // PUT api/categories/{id}
        [HttpPut, Route("{id:guid}")]
        public IHttpActionResult Update(Guid id, Category category)
        {
            try
            {
                category.Id = id; // Ensure correct id
                _service.Update(category);

                return ApiResponse(true, "Category updated successfully", category);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }


        // DELETE api/categories/{id}
        [HttpDelete, Route("{id:guid}")]
        public IHttpActionResult Delete(Guid id)
        {
            try
            {
                _service.Delete(id);
                return ApiResponse(true, "Category deleted successfully");
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

    }
}

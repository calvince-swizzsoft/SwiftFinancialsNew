using Application.MainBoundedContext.DTO.AdministrationModule;
using System;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/companies")]
    public class CompaniesController : ApiController
    {
        private readonly CompanyService _service = new CompanyService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var companies = _service.GetAll();
                return ApiResponse(true, "Companies retrieved successfully", companies);
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
                var company = _service.GetById(id);
                if (company == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Company not found" });

                return ApiResponse(true, "Company retrieved successfully", company);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("by-registration/{registrationNumber}")]
        public IHttpActionResult GetByRegistrationNumber(string registrationNumber)
        {
            try
            {
                var company = _service.GetByRegistrationNumber(registrationNumber);
                if (company == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Company not found" });

                return ApiResponse(true, "Company retrieved successfully", company);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] CompanyDTO company)
        {
            try
            {
                if (company == null)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid company data" });

                var createdCompany = _service.Create(company);

                return Content(System.Net.HttpStatusCode.Created,
                               new
                               {
                                   success = true,
                                   message = "Company created successfully",
                                   data = createdCompany
                               });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPut, Route("{id:guid}")]
        public IHttpActionResult Update(Guid id, [FromBody] CompanyDTO company)
        {
            try
            {
                if (company == null || company.Id != id)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid company data" });

                _service.Update(company);
                return ApiResponse(true, "Company updated successfully", company);
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
                return ApiResponse(true, "Company deleted successfully");
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPatch, Route("{id:guid}/toggle-lock")]
        public IHttpActionResult ToggleLock(Guid id)
        {
            try
            {
                var company = _service.GetById(id);
                if (company == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Company not found" });

                company.IsLocked = !company.IsLocked;
                _service.Update(company);

                return ApiResponse(true, $"Company {(company.IsLocked ? "locked" : "unlocked")} successfully", company);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }
    }
}
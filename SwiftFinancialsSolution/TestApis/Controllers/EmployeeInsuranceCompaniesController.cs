using System;
using System.Net;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/employee-insurance-companies")]
    public class EmployeeInsuranceCompaniesController : ApiController
    {
        private readonly EmployeeInsuranceCompanyService _service = new EmployeeInsuranceCompanyService();

        // Standard response wrapper
        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new
            {
                success,
                message,
                data
            });
        }


        // GET api/employee-insurance-companies
        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var companies = _service.GetAll();
                return ApiResponse(true, "Insurance companies retrieved successfully", companies);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // GET api/employee-insurance-companies/5
        [HttpGet, Route("{id:int}")]
        public IHttpActionResult Get(int id)
        {
            try
            {
                var company = _service.GetById(id);
                if (company == null)
                    return Content(HttpStatusCode.NotFound,
                        new { success = false, message = "Insurance company not found" });

                return ApiResponse(true, "Insurance company retrieved successfully", company);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // POST api/employee-insurance-companies
        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] EmployeeInsuranceCompany company)
        {
            try
            {
                if (company == null)
                    return Content(HttpStatusCode.BadRequest,
                        new { success = false, message = "Invalid insurance company data" });

                _service.Add(company);
                return Content(HttpStatusCode.Created,
                    new { success = true, message = "Insurance company created successfully", data = company });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // PUT api/employee-insurance-companies/5
        [HttpPut, Route("{id:int}")]
        public IHttpActionResult Update(int id, [FromBody] EmployeeInsuranceCompany company)
        {
            try
            {
                if (company == null || company.Code != id)
                    return Content(HttpStatusCode.BadRequest,
                        new { success = false, message = "Invalid insurance company data" });

                _service.Update(company);
                return ApiResponse(true, "Insurance company updated successfully", company);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // DELETE api/employee-insurance-companies/5
        [HttpDelete, Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            try
            {
                _service.Delete(id);
                return ApiResponse(true, "Insurance company deleted successfully");
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }
    }
}

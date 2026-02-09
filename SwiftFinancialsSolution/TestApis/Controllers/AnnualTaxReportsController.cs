using SwiftFinancials.Web.Areas.Payroll.models;
using System;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/annual-tax-reports")]
    public class AnnualTaxReportsController : ApiController
    {
        private readonly AnnualTaxReportService _service = new AnnualTaxReportService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var reports = _service.GetAll();
                return ApiResponse(true, "Annual tax reports retrieved successfully", reports);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("{employeeId:int}/{taxYear:int}")]
        public IHttpActionResult GetByEmployeeYear(int employeeId, int taxYear)
        {
            try
            {
                var report = _service.GetByEmployeeYear(employeeId, taxYear);
                if (report == null)
                    return Content(System.Net.HttpStatusCode.NotFound, new { success = false, message = "Report not found" });

                return ApiResponse(true, "Annual tax report retrieved successfully", report);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] AnnualTaxReport report)
        {
            try
            {
                if (report == null)
                    return Content(System.Net.HttpStatusCode.BadRequest, new { success = false, message = "Invalid report data" });

                _service.Add(report);
                return Content(System.Net.HttpStatusCode.Created,
                    new { success = true, message = "Annual tax report created successfully", data = report });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }
    }
}

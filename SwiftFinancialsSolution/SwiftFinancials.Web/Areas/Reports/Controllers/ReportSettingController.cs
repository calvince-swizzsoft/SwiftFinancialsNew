using Application.MainBoundedContext.DTO.AdministrationModule;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using OfficeOpenXml;
using SwiftFinancials.Web.Areas.Admin.Models;
using SwiftFinancials.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Reports.Controllers
{
    public class ReportSettingController : MasterController
    {
        string connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        public async Task<ActionResult> Index(ReportDTO reportDTO)
        {
            await ServeNavigationMenus();

            var findReports = await _channelService.FindReportsAsync(GetServiceHeader());

            var ReportsCount = findReports.Count;

            ViewBag.ReportsCount = ReportsCount;
            ViewBag.Reports = findReports;
            return View();
        }


        public JsonResult GetReportParameters(string reportQuery)
        {
            TempData["sp"] = reportQuery;
            var parameters = new List<StoredProcedureParameter>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(reportQuery, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    try
                    {
                        conn.Open();
                        SqlCommandBuilder.DeriveParameters(cmd);

                        foreach (SqlParameter param in cmd.Parameters)
                        {
                            if (param.Direction == ParameterDirection.Input || param.Direction == ParameterDirection.InputOutput)
                            {
                                parameters.Add(new StoredProcedureParameter
                                {
                                    Name = param.ParameterName,
                                    DataType = param.SqlDbType.ToString(),
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
                    }
                }
            }

            return Json(parameters, JsonRequestBehavior.AllowGet);
        }





        // Generate Report
        [HttpPost]
        public ActionResult GenerateReport(string reportQuery, string format, Dictionary<string, string> paramValues)
        {
            var sp = TempData["sp"];

            reportQuery = sp.ToString();
            try
            {
                if (string.IsNullOrEmpty(format))
                {
                    TempData["Error"] = "Report type is not specified.";
                    return RedirectToAction("Index");
                }

                switch (format.ToLower())
                {
                    case "pdf":
                        return GeneratePDFReport(reportQuery, paramValues);

                    case "excel":
                        return GenerateExcelReport(reportQuery, paramValues);

                    default:
                        TempData["Error"] = "Invalid report type specified.";
                        return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error generating report: {ex.Message}";
                return RedirectToAction("Index");
            }
        }


        private DataTable FetchReportData(string reportQuery, Dictionary<string, string> paramValues)
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(reportQuery, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    foreach (var param in paramValues)
                    {
                        cmd.Parameters.AddWithValue(param.Key, param.Value);
                    }

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }

            return dt;
        }


        public ActionResult GeneratePDFReport(string reportQuery, Dictionary<string, string> paramValues)
        {
            DataTable reportData = FetchReportData(reportQuery, paramValues);

            using (MemoryStream stream = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4, 25, 25, 30, 30);
                PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();

                // Add a logo image in the header
                string logoPath = Server.MapPath("~/Images/MIA.JPG");
                iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(logoPath);
                logo.ScaleToFit(300f, 300f);
                logo.Alignment = Element.ALIGN_CENTER;
                pdfDoc.Add(logo);

                // Add report data to the PDF
                PdfPTable table = new PdfPTable(reportData.Columns.Count);

                // Add table header
                foreach (DataColumn column in reportData.Columns)
                {
                    PdfPCell header = new PdfPCell(new Phrase(column.ColumnName));
                    header.HorizontalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(header);
                }

                // Add table rows
                foreach (DataRow row in reportData.Rows)
                {
                    foreach (DataColumn column in reportData.Columns)
                    {
                        table.AddCell(row[column].ToString());
                    }
                }

                pdfDoc.Add(table);
                pdfDoc.Close();

                // Return the PDF as a downloadable file
                return File(stream.ToArray(), "application/pdf", "Report.pdf");
            }
        }

        // Action to generate the Excel report
        public ActionResult GenerateExcelReport(string reportQuery, Dictionary<string, string> paramValues)
        {
            DataTable reportData = FetchReportData(reportQuery, paramValues);

            using (XLWorkbook workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Report");

                // Add report data to the Excel sheet
                for (int i = 0; i < reportData.Columns.Count; i++)
                {
                    worksheet.Cell(1, i + 1).Value = reportData.Columns[i].ColumnName;
                }

                for (int i = 0; i < reportData.Rows.Count; i++)
                {
                    for (int j = 0; j < reportData.Columns.Count; j++)
                    {
                        worksheet.Cell(i + 2, j + 1).Value = reportData.Rows[i][j].ToString();
                    }
                }

                // Format Excel sheet (autofit columns, bold headers, etc.)
                worksheet.Columns().AdjustToContents();
                worksheet.Row(1).Style.Font.Bold = true;

                // Return the Excel as a downloadable file
                using (MemoryStream stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Report.xlsx");
                }
            }
        }
    }
}
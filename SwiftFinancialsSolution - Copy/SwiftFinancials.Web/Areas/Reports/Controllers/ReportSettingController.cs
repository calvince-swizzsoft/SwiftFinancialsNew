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
using ClosedXML;

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



        // Action to generate the Excel report
        public ActionResult GenerateExcelReport(string reportQuery, Dictionary<string, string> paramValues)
        {
            DataTable reportData = FetchReportData(reportQuery, paramValues);

            using (XLWorkbook workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Report");

                string imagePath = Server.MapPath("~/Images/MIA.JPG"); 
                var picture = worksheet.AddPicture(imagePath)
                                       .MoveTo(worksheet.Cell(1, 1)); 

                int startRow = 13;
                int startColumn = 1; 

                
                for (int i = 0; i < reportData.Columns.Count; i++)
                {
                    var headerCell = worksheet.Cell(startRow, startColumn + i);
                    headerCell.Value = reportData.Columns[i].ColumnName;
                    headerCell.Style.Font.Bold = true;
                    headerCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0, 51, 102); 
                    headerCell.Style.Font.FontColor = XLColor.White;
                    headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    headerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }

                
                for (int i = 0; i < reportData.Rows.Count; i++)
                {
                    for (int j = 0; j < reportData.Columns.Count; j++)
                    {
                        var cell = worksheet.Cell(startRow + 1 + i, startColumn + j); 
                        cell.Value = reportData.Rows[i][j].ToString();
                        cell.Style.Fill.BackgroundColor = (i % 2 == 0) ? XLColor.FromArgb(242, 242, 242) : XLColor.White; 
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    }
                }

                int tableWidth = reportData.Columns.Count;
                int logoColumnStart = (tableWidth / 2) - 1; 
                if (logoColumnStart < 1) logoColumnStart = 1; 
                picture.MoveTo(worksheet.Cell(1, logoColumnStart)); 

                worksheet.Columns().AdjustToContents();
                worksheet.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                worksheet.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                using (MemoryStream stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Report.xlsx");
                }
            }
        }





        public ActionResult GeneratePDFReport(string reportQuery, Dictionary<string, string> paramValues)
        {
            DataTable reportData = FetchReportData(reportQuery, paramValues);

            using (MemoryStream stream = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4, 25, 25, 30, 50);
                PdfWriter writer = PdfWriter.GetInstance(pdfDoc, stream);
                writer.PageEvent = new PDFPageEventHelper(); 

                pdfDoc.Open();

                
                string logoPath = Server.MapPath("~/Images/MIA.JPG");
                iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(logoPath);
                logo.ScaleToFit(200f, 80f);  
                logo.Alignment = Element.ALIGN_CENTER;
                pdfDoc.Add(logo);

                
                pdfDoc.Add(new Paragraph("\n"));

                
                PdfPTable table = new PdfPTable(reportData.Columns.Count)
                {
                    WidthPercentage = 100,  
                    SpacingBefore = 10f,
                    SpacingAfter = 10f,
                    HorizontalAlignment = Element.ALIGN_CENTER
                };

                BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                Font headerFont = new Font(bf, 12, Font.BOLD, BaseColor.WHITE);
                Font cellFont = new Font(bf, 10, Font.NORMAL, BaseColor.BLACK);

                foreach (DataColumn column in reportData.Columns)
                {
                    PdfPCell header = new PdfPCell(new Phrase(column.ColumnName, headerFont))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        BackgroundColor = new BaseColor(0, 51, 102),
                        Padding = 5
                    };
                    table.AddCell(header);
                }

                foreach (DataRow row in reportData.Rows)
                {
                    foreach (DataColumn column in reportData.Columns)
                    {
                        PdfPCell cell = new PdfPCell(new Phrase(row[column].ToString(), cellFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            Padding = 5,
                            BorderColor = new BaseColor(200, 200, 200)
                        };
                        table.AddCell(cell);
                    }
                }

                pdfDoc.Add(table);
                pdfDoc.Close();

                return File(stream.ToArray(), "application/pdf", "Report.pdf");
            }
        }
    }


    public class PDFPageEventHelper : PdfPageEventHelper
    {
        private readonly Font _footerFont = new Font(Font.FontFamily.HELVETICA, 10, Font.NORMAL, BaseColor.GRAY);

        public override void OnEndPage(PdfWriter writer, Document document)
        {
            PdfContentByte cb = writer.DirectContent;
            Rectangle pageSize = document.PageSize;

            BaseColor faintGray = new BaseColor(200, 200, 200); 

            cb.SetLineWidth(0.5f); 
            cb.SetColorStroke(faintGray); 
            cb.MoveTo(pageSize.GetLeft(40), pageSize.GetBottom(40));
            cb.LineTo(pageSize.GetRight(40), pageSize.GetBottom(40));
            cb.Stroke();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED), 10);
            cb.SetTextMatrix(pageSize.GetRight(60), pageSize.GetBottom(30));
            cb.ShowText($"Page {writer.PageNumber}");
            cb.EndText();
        }
    }
}
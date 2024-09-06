//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Configuration;
//using System.Data;
//using System.Data.SqlClient;
//using System.Drawing;
//using System.Globalization;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Web;
//using System.Web.Mvc;
//using Application.MainBoundedContext.DTO;
//using Application.MainBoundedContext.DTO.AccountsModule;
//using Application.MainBoundedContext.DTO.BackOfficeModule;
//using Application.MainBoundedContext.DTO.RegistryModule;
//using Dapper;
//using Infrastructure.Crosscutting.Framework.Utils;
//using iTextSharp.text;
//using iTextSharp.text.pdf;
//using Microsoft.AspNet.Identity;
//using OfficeOpenXml;
//using SwiftFinancials.Web.Areas.Reports.Models;
//using SwiftFinancials.Web.Controllers;
//using SwiftFinancials.Web.Helpers;
//using SwiftFinancials.Web.PDF;
//using Image = iTextSharp.text.Image;

//namespace SwiftFinancials.Web.Areas.Reports.Controllers
//{
//    public class DynamicReportingController : Controller
//    {
//        private readonly string _connectionString = "Data Source = (local); Initial Catalog = SwiftFinancialsDB_Live; Persist Security Info=true; User ID = sa; Password=pass123; Pooling=True";
//        private SqlConnection Connection => new SqlConnection(_connectionString);

//        public ActionResult Index()
//        {
//            using (var db = Connection)
//            {
//                string sql = "SELECT Name, StoredProcedureName FROM swiftFin_DynamicReports";
//                var reports = db.Query<DynamicReport>(sql);
//                return View(reports.ToList());
//            }
//        }


//        public async Task<ActionResult> Excel(string storedProcedureName)
//        {
//            var data = await ExecuteStoredProcedureAsync(storedProcedureName);

//            var excelFile = GenerateExcelFile(data);

//            return File(excelFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{storedProcedureName}.xlsx");
//        }

//        public async Task<ActionResult> PDF(string storedProcedureName)
//        {
//            var data = await ExecuteStoredProcedureAsync(storedProcedureName);

//            var pdfFile = GeneratePdfFile(data);

//            return File(pdfFile, "application/pdf", $"{storedProcedureName}.pdf");
//        }


//        private async Task<DataTable> ExecuteStoredProcedureAsync(string storedProcedureName)
//        {
//            var dataTable = new DataTable();

//            // Loans
//            DateTime startDate = new DateTime(2024, 01, 01);
//            DateTime endDate = new DateTime(2024, 12, 31);
//            int statusCode = 48829; // Example status code


//            // Membership By Age
//            int Age1 = 18;
//            int Age2 = 100;

//            // Customers By Gender
//            int gender = 1;
//            DateTime CustomersEndDate = new DateTime(2024, 12, 31);

//            using (var connection = Connection)
//            {
//                using (var command = new SqlCommand(storedProcedureName, connection))
//                {
//                    command.CommandType = CommandType.StoredProcedure;

//                    if (storedProcedureName == "sp_LoansByStatus")
//                    {
//                        command.Parameters.AddWithValue("@StartDate", startDate);
//                        command.Parameters.AddWithValue("@EndDate", endDate);
//                        command.Parameters.AddWithValue("@Status", statusCode);
//                    }   
//                    else if(storedProcedureName== "Sp_MembersListingByAge")
//                    {
//                        command.Parameters.AddWithValue("@Age1", Age1);
//                        command.Parameters.AddWithValue("@Age2", Age2);
//                    }

//                    else if(storedProcedureName== "sp_CustomerByGender")
//                    {
//                        command.Parameters.AddWithValue("@pGender", gender);
//                        command.Parameters.AddWithValue("@EndDate", CustomersEndDate);
//                    }

//                    await connection.OpenAsync();
//                    using (var reader = await command.ExecuteReaderAsync())
//                    {
//                        dataTable.Load(reader);
//                    }
//                }
//            }

//            return dataTable;
//        }

        
//        private byte[] GenerateExcelFile(DataTable data)
//        {
//            using (var package = new ExcelPackage())
//            {
//                var worksheet = package.Workbook.Worksheets.Add("Report");

//                worksheet.Cells["A1"].LoadFromDataTable(data, true);

//                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

//                return package.GetAsByteArray(); 
//            }
//        }

//        private byte[] GeneratePdfFile(DataTable data)
//        {
//            using (var memoryStream = new MemoryStream())
//            {
//                var document = new Document(PageSize.A0, 10, 10, 10, 10);
//                PdfWriter.GetInstance(document, memoryStream);
//                document.Open();

//                var pdfTable = new PdfPTable(data.Columns.Count)
//                {
//                    WidthPercentage = 100
//                };

//                float[] columnWidths = new float[data.Columns.Count];
//                for (int i = 0; i < columnWidths.Length; i++)
//                {
//                    columnWidths[i] = 5f; 
//                }
//                pdfTable.SetWidths(columnWidths);

                
//                foreach (DataColumn column in data.Columns)
//                {
//                    var cell = new PdfPCell(new Phrase(column.ColumnName))
//                    {
//                        BackgroundColor = BaseColor.LIGHT_GRAY,
//                        HorizontalAlignment = Element.ALIGN_CENTER,
//                        NoWrap = false, 
//                        Padding = 5
//                    };
//                    pdfTable.AddCell(cell);
//                }

//                foreach (DataRow row in data.Rows)
//                {
//                    foreach (var item in row.ItemArray)
//                    {
//                        var cell = new PdfPCell(new Phrase(item.ToString()))
//                        {
//                            NoWrap = false, 
//                            Padding = 5
//                        };
//                        pdfTable.AddCell(cell);
//                    }
//                }

//                document.Add(pdfTable);
//                document.Close();

//                return memoryStream.ToArray();
//            }
//        }

//    }










//    //[HttpGet]
//    //public JsonResult GetReports()
//    //{
//    //    using (var db = Connection)
//    //    {
//    //        string sql = "SELECT Name, StoredProcedureName FROM swiftFin_DynamicReports";
//    //        var reports = db.Query<DynamicReport>(sql);
//    //        return Json(reports, JsonRequestBehavior.AllowGet);
//    //    }
//    //}



//    //private async Task<List<LoanCaseDTO>> GetLoansByStatusAsync(DateTime startDate, DateTime endDate, int statusCode)
//    //{
//    //    var loanCases = new List<LoanCaseDTO>();

//    //    string connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"]?.ConnectionString;

//    //    if (string.IsNullOrEmpty(connectionString))
//    //    {
//    //        throw new InvalidOperationException("Connection string 'SwiftFin_Dev' is not configured properly.");
//    //    }

//    //    try
//    //    {
//    //        using (SqlConnection connection = new SqlConnection(connectionString))
//    //        {
//    //            using (SqlCommand command = new SqlCommand("sp_LoansByStatus", connection))
//    //            {
//    //                command.CommandType = CommandType.StoredProcedure;

//    //                // Set the parameters
//    //                command.Parameters.AddWithValue("@StartDate", startDate);
//    //                command.Parameters.AddWithValue("@EndDate", endDate);
//    //                command.Parameters.AddWithValue("@Status", statusCode);

//    //                await connection.OpenAsync();

//    //                using (SqlDataReader reader = await command.ExecuteReaderAsync())
//    //                {
//    //                    while (await reader.ReadAsync())
//    //                    {
//    //                        var loanCase = new LoanCaseDTO
//    //                        {
//    //                            CaseNumber = reader.IsDBNull(reader.GetOrdinal("CaseNumber")) ? 0 : reader.GetInt32(reader.GetOrdinal("CaseNumber")),
//    //                            CustomerIndividualFirstName = reader.IsDBNull(reader.GetOrdinal("FullName")) ? string.Empty : reader.GetString(reader.GetOrdinal("FullName")),
//    //                            AmountApplied = reader.IsDBNull(reader.GetOrdinal("AmountApplied")) ? 0 : reader.GetDecimal(reader.GetOrdinal("AmountApplied")),
//    //                            CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? string.Empty : reader.GetString(reader.GetOrdinal("CreatedBy")),
//    //                            CreatedDate = reader.IsDBNull(reader.GetOrdinal("CreatedDate")) ? (DateTime)(DateTime?)null : reader.GetDateTime(reader.GetOrdinal("CreatedDate"))

//    //                            // Map other properties as necessary
//    //                        };

//    //                        loanCases.Add(loanCase);
//    //                    }
//    //                }

//    //                return loanCases;
//    //            }
//    //        }
//    //    }
//    //    catch (SqlException ex)
//    //    {
//    //        // Log the exception or handle it as needed
//    //        // Example: log error (assuming you have a logging framework)
//    //        // _logger.LogError(ex, "An error occurred while retrieving loan cases.");
//    //        throw; // Rethrow the exception if you want it to propagate
//    //    }
//    //    catch (Exception ex)
//    //    {
//    //        // Handle other potential exceptions
//    //        // Example: log error
//    //        // _logger.LogError(ex, "An unexpected error occurred.");
//    //        throw; // Rethrow the exception if you want it to propagate
//    //    }

//    //    return loanCases;
//    //}

//}





//    // Create PDF Class
//    //public class CreatePdf
//    //{
//    //    public bool WritePdf(string fpath, string address, List<LoanCaseDTO> trans)
//    //    {
//    //        string imageFilePath = HttpContext.Current.Server.MapPath(".") + "/Images/MIA.JPG";
//    //        string image3 = HttpContext.Current.Server.MapPath(".") + "/Images/MIA.JPG";

//    //        string datetime = DateTime.Now.ToString("yyyyMMddHHmmss");

//    //        var filename = "CustomerReport" + datetime;

//    //        string f_name = filename + ".pdf";

//    //        var appRootDir = address + f_name;
//    //        try
//    //        {
//    //            //if (!Directory.Exists(directory))
//    //            //{
//    //            //    Directory.CreateDirectory(directory);
//    //            //}
//    //            if (System.IO.File.Exists(appRootDir))
//    //                System.IO.File.Delete(appRootDir);

//    //            //Font Definition 
//    //            var headerfont = FontFactory.GetFont(FontFactory.TIMES_ROMAN, 8);
//    //            var normalFont = FontFactory.GetFont(FontFactory.TIMES_ROMAN, 5);
//    //            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 6);
//    //            var underLine = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 6, iTextSharp.text.Font.UNDERLINE);

//    //            //Fetching Needed Data
//    //            using (var fs = new FileStream(appRootDir, FileMode.Create, FileAccess.Write, FileShare.None))
//    //            // Creating iTextSharp.text.Document object
//    //            using (var doc = new Document())
//    //            {
//    //                doc.SetMargins(doc.LeftMargin, doc.RightMargin, doc.TopMargin + 20, doc.BottomMargin + 40);
//    //                // Creating iTextSharp.text.pdf.PdfWriter object
//    //                // It helps to write the Document to the Specified FileStream
//    //                using (var writer = PdfWriter.GetInstance(doc, fs))
//    //                {
//    //                    //Set Header & Footer Using Page Events with the above class
//    //                    writer.PageEvent = new PdfHeaderFooter(image3);
//    //                    writer.AddViewerPreference(PdfName.HIDEMENUBAR, new PdfBoolean(true));
//    //                    writer.ViewerPreferences = PdfWriter.HideMenubar;
//    //                    // Openning the Document
//    //                    doc.Open();
//    //                    // Adding a paragraph
//    //                    // NOTE: When we want to insert text, then we've to do it through creating paragraph
//    //                    //doc.NewPage();
//    //                    DateTime datet = DateTime.Now;

//    //                    string date = datet.ToString();
//    //                    var prSpace = new Paragraph("\n");
//    //                    //var prSpace2 = new Paragraph("SMS REOSRTS");
//    //                    //var prSpace3 = new Paragraph("\n");
//    //                    var pr1 = new Paragraph("Customer Statement", headerfont);
//    //                    var pr2 = new Paragraph("Date : " + date, headerfont);
//    //                    var pr3 = new Paragraph("", headerfont);
//    //                    var pr33 = new Paragraph("", headerfont);
//    //                    var pr4 = new Paragraph("", headerfont);
//    //                    var glnodesTable = new PdfPTable(2) { WidthPercentage = 100 };
//    //                    var widths = new float[] { 10f, 20f };
//    //                    glnodesTable.SetWidths(widths);

//    //                    var cell1 = new PdfPCell(new Phrase("E-mail", boldFont))
//    //                    {
//    //                        BackgroundColor = BaseColor.LIGHT_GRAY,
//    //                        Colspan = 1
//    //                    };
//    //                    glnodesTable.AddCell(cell1);

//    //                    var cell2 = new PdfPCell(new Phrase("Branch", boldFont))
//    //                    {
//    //                        BackgroundColor = BaseColor.LIGHT_GRAY,
//    //                        Colspan = 1
//    //                    };
//    //                    glnodesTable.AddCell(cell2);
//    //                    foreach (var n in trans)
//    //                    {
//    //                        //decimal paidin = Math.Round(Convert.ToDecimal(n.PaidIn), 2);

//    //                        //decimal withdrawn = Math.Round(Convert.ToDecimal(n.Widthrawn), 2);

//    //                        //decimal balanc = Math.Round(Convert.ToDecimal(n.Balance), 2);

//    //                        var cell10 = new PdfPCell(new Phrase(n.BranchAddressEmail.ToString(), normalFont))
//    //                        {
//    //                            Colspan = 1
//    //                        };
//    //                        glnodesTable.AddCell(cell10);

//    //                        var cell11 = new PdfPCell(new Phrase(n.BranchDescription.ToString(), normalFont))
//    //                        {
//    //                            Colspan = 1
//    //                        };
//    //                        glnodesTable.AddCell(cell11);
//    //                    }

//    //                    //Letter Head 

//    //                    doc.Add(pr1);
//    //                    doc.Add(pr2);
//    //                    doc.Add(pr3);
//    //                    doc.Add(pr33);
//    //                    doc.Add(pr1);
//    //                    doc.Add(pr2);
//    //                    doc.Add(pr3);
//    //                    doc.Add(pr33);
//    //                    doc.Add(pr1);
//    //                    doc.Add(pr2);
//    //                    doc.Add(pr3);
//    //                    doc.Add(pr33);
//    //                    doc.Add(pr1);
//    //                    doc.Add(pr2);
//    //                    doc.Add(pr3);
//    //                    doc.Add(pr33);
//    //                    doc.Add(pr3);
//    //                    doc.Add(pr33);
//    //                    doc.Add(pr3);
//    //                    doc.Add(pr33);
//    //                    //doc.Add(pr3);
//    //                    if (trans.Count > 0)
//    //                    {
//    //                        doc.Add(pr4);
//    //                        doc.Add(prSpace);
//    //                        doc.Add(glnodesTable);
//    //                        doc.Add(prSpace);
//    //                    }

//    //                    var outroTable = new PdfPTable(1) { WidthPercentage = 100 };
//    //                    PdfPCell cell;
//    //                    cell = new PdfPCell(new Paragraph("Thank You", normalFont));
//    //                    cell.HorizontalAlignment = Element.ALIGN_LEFT;
//    //                    cell.Border = PdfPCell.NO_BORDER;
//    //                    outroTable.AddCell(cell);
//    //                    doc.Close();
//    //                }
//    //            }


//    //        }
//    //        // Catching iTextSharp.text.DocumentException if any
//    //        catch (DocumentException de)
//    //        {
//    //        }
//    //        // Catching System.IO.IOException if any
//    //        catch (IOException ioe)
//    //        {
//    //        }
//    //        catch (Exception ex)
//    //        {
//    //            string exe = "";
//    //        }
//    //        return false;
//    //    }
//    //}









//    //// PDF Header 
//    //public class PdfHeaderFooter : PdfPageEventHelper
//    //{
//    //    public iTextSharp.text.Image _headerImage { get; set; }
//    //    public iTextSharp.text.Image ImageFooter { get; set; }

//    //    private readonly float _imageHeight;

//    //    public PdfHeaderFooter(string headerImagePath)
//    //    {
//    //        _headerImage = Image.GetInstance(headerImagePath);
//    //        _headerImage.ScaleToFit(100f, 100f);
//    //        _imageHeight = _headerImage.ScaledHeight;
//    //    }

//    //    public override void OnStartPage(PdfWriter writer, Document document)
//    //    {
//    //        if (writer.PageNumber == 1)
//    //        {
//    //            PdfContentByte canvas = writer.DirectContent;

//    //            // Set the position of the header image
//    //            float headerX = document.LeftMargin;
//    //            float headerY = document.PageSize.Height - _headerImage.ScaledHeight - document.TopMargin;

//    //            _headerImage.SetAbsolutePosition(headerX, headerY);
//    //            canvas.AddImage(_headerImage);

//    //            document.SetMargins(document.LeftMargin, document.RightMargin, document.TopMargin + _headerImage.ScaledHeight, document.BottomMargin);
//    //        }
//    //    }
//    //}
////}

using SwiftFinancials.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO.AdministrationModule;
using System.Data;

namespace SwiftFinancials.Web.Areas.Admin.Controllers
{
    public class SSRSReportSettingController : MasterController
    {
        string connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            var categories = await GetCategoriesAsync();
            ViewBag.Categories = categories;
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> UploadReport(HttpPostedFileBase reportFile, int categoryId, string reportname)
        {
            await ServeNavigationMenus();

            if (reportFile == null || categoryId == 0 || string.IsNullOrWhiteSpace(reportname))
            {
                TempData["ReportError"] = "Missing values";
                return RedirectToAction("Create");
            }

            if (reportFile == null || Path.GetExtension(reportFile.FileName).ToLower() != ".rdl")
            {
                TempData["InvalidFile"] = "Please upload a valid file. The correct report file format MUST be \".rdl\" file extension.";
                ViewBag.Categories = await GetCategoriesAsync();
                return RedirectToAction("Create");
            }

            byte[] fileData;
            using (var binaryReader = new BinaryReader(reportFile.InputStream))
            {
                fileData = binaryReader.ReadBytes(reportFile.ContentLength);
            }

            await SaveReportToDatabaseAsync(reportFile.FileName, fileData, categoryId, reportname);

            if (TempData["Error"] != null)
            {
                ViewBag.Categories = await GetCategoriesAsync();
                return RedirectToAction("Create");
            }

            TempData["Success"] = "Report saved successfully!";
            ViewBag.Categories = await GetCategoriesAsync();
            return View("Create");
        }


        private async Task SaveReportToDatabaseAsync(string fileName, byte[] fileContent, int categoryId, string reportname)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileContent == null || categoryId <= 0 || string.IsNullOrWhiteSpace(reportname))
            {
                TempData["ReportError"] = "Missing values";
                return;
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                var checkQuery = "SELECT COUNT(*) FROM SSRSReports WHERE FileName = @FileName AND CategoryID = @CategoryID AND ReportName = @reportname";
                using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@FileName", fileName);
                    checkCmd.Parameters.AddWithValue("@CategoryID", categoryId);
                    checkCmd.Parameters.AddWithValue("@reportname", reportname);

                    int reportExists = (int)await checkCmd.ExecuteScalarAsync();

                    if (reportExists > 0)
                    {
                        TempData["Error"] = "A report with the same name and category already exists.";
                        return;
                    }
                }

                var insertQuery = "INSERT INTO SSRSReports (FileName, FileContent, CategoryID, ReportName, CreatedDate) VALUES (@FileName, @FileContent, @CategoryID, @reportname, @CreatedDate)";
                using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@FileName", fileName);
                    cmd.Parameters.AddWithValue("@FileContent", fileContent);
                    cmd.Parameters.AddWithValue("@CategoryID", categoryId);
                    cmd.Parameters.AddWithValue("@reportname", reportname);
                    cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }


        private async Task<List<SelectListItem>> GetCategoriesAsync()
        {
            await ServeNavigationMenus();

            var categories = new List<SelectListItem>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                var query = "SELECT CategoryID, CategoryName FROM ReportCategories Order By CategoryName ASC";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            categories.Add(new SelectListItem
                            {
                                Value = reader["CategoryID"].ToString(),
                                Text = reader["CategoryName"].ToString()
                            });
                        }
                    }
                }
            }

            return categories;
        }


        [HttpPost]
        public async Task<ActionResult> CreateCategory(string reportCategory)
        {
            if (string.IsNullOrWhiteSpace(reportCategory))
            {
                TempData["CategoryError"] = "Report Name is required.";
                return RedirectToAction("Create");
            }

            await AddCategoryToDatabaseAsync(reportCategory);

            if (TempData["ErrorCat"] != null)
            {
                ViewBag.Categories = await GetCategoriesAsync();
                return RedirectToAction("Create");
            }

            TempData["CategorySuccess"] = "Report category created successfully!";
            return RedirectToAction("Create");
        }

        private async Task AddCategoryToDatabaseAsync(string reportCategory)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                var checkQuery = "SELECT COUNT(*) FROM ReportCategories WHERE CategoryName = @CategoryName";
                using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@CategoryName", reportCategory);

                    int reportExists = (int)await checkCmd.ExecuteScalarAsync();

                    if (reportExists > 0)
                    {
                        TempData["ErrorCat"] = "Category already exists.";
                        return;
                    }
                }


                var query = "INSERT INTO ReportCategories (CategoryName) VALUES (@CategoryName)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CategoryName", reportCategory.ToUpper());
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }



        public async Task<ActionResult> GetReports()
        {
            var reports = new List<object>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                var query = @"
            SELECT 
                r.ReportID, 
                r.FileName, 
                r.ReportName, 
                c.CategoryName 
            FROM 
                SSRSReports r
            INNER JOIN 
                ReportCategories c ON r.CategoryID = c.CategoryID
            ORDER BY 
                r.CreatedDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            reports.Add(new
                            {
                                Id = (int)reader["ReportID"],
                                FileName = reader["FileName"].ToString(),
                                ReportName = reader["ReportName"].ToString(),
                                CategoryName = reader["CategoryName"].ToString() 
                            });
                        }
                    }
                }
            }

            return Json(reports, JsonRequestBehavior.AllowGet);
        }




        [HttpPost]
        public async Task<ActionResult> DeleteReport(int id)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                var query = "DELETE FROM SSRSReports WHERE Id = @Id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            return new HttpStatusCodeResult(200);
        }



        public ActionResult DownloadReport(int id)
        {
            byte[] fileBytes = null;
            string fileName = null;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string sqlQuery = "SELECT FileContent, FileName FROM SSRSReports WHERE ReportID = @Id";

                using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });

                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            fileBytes = reader["FileContent"] as byte[];
                            fileName = reader["FileName"] as string;
                        }
                    }
                }
            }

            if (fileBytes == null || fileName == null)
            {
                return HttpNotFound("Report not found.");
            }

            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }


        public async Task<ActionResult> EditReport(int id)
        {
            dynamic report = new System.Dynamic.ExpandoObject();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                var query = "SELECT ReportID, FileName, ReportName, CategoryID FROM SSRSReports WHERE ReportID = @Id";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            report.ReportID = (int)reader["ReportID"];
                            report.FileName = reader["FileName"].ToString();
                            report.ReportName = reader["ReportName"].ToString();
                            report.CategoryID = (int)reader["CategoryID"];
                        }
                        else
                        {
                            return HttpNotFound("Report not found.");
                        }
                    }
                }
            }

            ViewBag.Report = report;
            ViewBag.Categories = await GetCategoriesAsync();
            return View();
        }



        [HttpPost]
        public async Task<ActionResult> UpdateReport(int reportID, string reportName, int categoryId, HttpPostedFileBase reportFile)
        {
            if (string.IsNullOrWhiteSpace(reportName) || categoryId <= 0)
            {
                TempData["ErrorReportName"] = "Report Name and Category are required.";

                return RedirectToAction("EditReport", new { id = reportID });
            }

            byte[] fileData = null;
            if (reportFile != null && Path.GetExtension(reportFile.FileName).ToLower() == ".rdl")
            {
                using (var binaryReader = new BinaryReader(reportFile.InputStream))
                {
                    fileData = binaryReader.ReadBytes(reportFile.ContentLength);
                }
            }
            else if (reportFile != null && Path.GetExtension(reportFile.FileName).ToLower() != ".rdl")
            {
                TempData["InvalidFileEdit"] = "Please upload a valid file. The correct report file format MUST be \".rdl\" file extension.";
                ViewBag.Categories = await GetCategoriesAsync();
                return RedirectToAction("EditReport", new { id = reportID });
            }


            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                var updateQuery = "UPDATE SSRSReports SET ReportName = @ReportName, FileName = @FileName, CategoryID = @CategoryID" +
                                  (fileData != null ? ", FileContent = @FileContent" : "") +
                                  " WHERE ReportID = @ReportID";

                using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@ReportName", reportName);
                    cmd.Parameters.AddWithValue("@CategoryID", categoryId);
                    if (fileData != null)
                    {
                        cmd.Parameters.AddWithValue("@FileContent", fileData);
                    }
                    cmd.Parameters.AddWithValue("@ReportID", reportID);
                    cmd.Parameters.AddWithValue("@FileName", reportFile.FileName);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            TempData["EditSuccess"] = "Report updated successfully!";
            return RedirectToAction("Create");
        }

    }
}

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

            TempData["Success"] = "Report uploaded successfully!";
            ViewBag.Categories = await GetCategoriesAsync();
            return View("Create");
        }


        private async Task SaveReportToDatabaseAsync(string fileName, byte[] fileContent, int categoryId, string reportname)
        {
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
    }
}

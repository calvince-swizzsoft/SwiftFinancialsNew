using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Mvc;
using SwiftFinancials.Web.Areas.Reports.Models;
using SwiftFinancials.Web.Controllers;
using System.Configuration;
using System.Threading.Tasks;

namespace SwiftFinancials.Web.Areas.Reports.Controllers
{
    public class SSRSReportsController : MasterController
    {
        private readonly string _connectionString;

        public SSRSReportsController()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            var reportsByCategory = new List<ReportByCategoryViewModel>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open(); 

                string sql = @"
                SELECT 
                    rc.CategoryName, 
                    r.ReportID, 
                    r.FileName, 
                    r.ReportName, 
                    r.CreatedDate
                FROM 
                    SSRSReports r
                INNER JOIN 
                    ReportCategories rc ON r.CategoryID = rc.CategoryID
                ORDER BY 
                    rc.CategoryName, r.CreatedDate DESC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync()) 
                    {
                        while (reader.Read())
                        {
                            reportsByCategory.Add(new ReportByCategoryViewModel
                            {
                                CategoryName = reader["CategoryName"].ToString(),
                                ReportID = (int)reader["ReportID"],
                                FileName = reader["FileName"].ToString(),
                                ReportName = reader["ReportName"].ToString(),
                                CreatedDate = (DateTime)reader["CreatedDate"]
                            });
                        }
                    }
                }
            }

            return View(reportsByCategory);
        }
    }
}

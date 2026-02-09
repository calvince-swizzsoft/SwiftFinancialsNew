using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace TestApis.Controllers
{
    [RoutePrefix("api/BudgetManagement")]
    public class BudgetManagementController : ApiController
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        private readonly string baseUrl = "https://4a68aa2818b9.ngrok-free.app/api/items";


        #region MODELS

        public class Project
        {
            public int ProjectId { get; set; }
            public string ProjectCode { get; set; }
            public string ProjectName { get; set; }
            public string ProjectManager { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public decimal? BudgetAmount { get; set; }
            public string Department { get; set; }
            public string Status { get; set; }
            public string Description { get; set; }
            public List<Budget> Budgets { get; set; } = new List<Budget>();
        }

        public class Budget
        {
            public int BudgetId { get; set; }
            public int ProjectId { get; set; }
            public string BudgetCode { get; set; }
            public string BudgetTitle { get; set; }
            public int FiscalYear { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal? ApprovedAmount { get; set; }
            public string ApprovalStatus { get; set; }
            public string CreatedBy { get; set; }
            public DateTime CreatedDate { get; set; }
            public List<BudgetLine> BudgetLines { get; set; } = new List<BudgetLine>();
        }

        public class BudgetLine
        {
            public int BudgetLineId { get; set; }
            public int BudgetId { get; set; }
            public string AccountCode { get; set; }
            public string Description { get; set; }
            public decimal AllocatedAmount { get; set; }
            public decimal SpentAmount { get; set; }
            public decimal RemainingAmount { get; set; }
            public string Department { get; set; }
            public string Category { get; set; }
            public string Status { get; set; }
        }

        #endregion

        #region CREATE ENDPOINTS

        [HttpPost]
        [Route("CreateProject")]
        public async Task<IHttpActionResult> CreateProject([FromBody] Project project)
        {
            if (project == null) return BadRequest("Invalid project data.");

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    INSERT INTO Projects 
                    (ProjectCode, ProjectName, ProjectManager, StartDate, EndDate, BudgetAmount, Department, Status, Description, CreatedBy, CreatedDate)
                    OUTPUT INSERTED.ProjectId
                    VALUES 
                    (@ProjectCode, @ProjectName, @ProjectManager, @StartDate, @EndDate, @BudgetAmount, @Department, @Status, @Description, @CreatedBy, GETDATE());
                ", conn);

                cmd.Parameters.AddWithValue("@ProjectCode", project.ProjectCode);
                cmd.Parameters.AddWithValue("@ProjectName", project.ProjectName);
                cmd.Parameters.AddWithValue("@ProjectManager", (object)project.ProjectManager ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@StartDate", (object)project.StartDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EndDate", (object)project.EndDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BudgetAmount", (object)project.BudgetAmount ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Department", (object)project.Department ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Status", (object)project.Status ?? "Active");
                cmd.Parameters.AddWithValue("@Description", (object)project.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedBy", "System");

                project.ProjectId = (int)cmd.ExecuteScalar();
            }

            return Ok(project);
        }

        [HttpPost]
        [Route("CreateBudget")]
        public async Task<IHttpActionResult> CreateBudget([FromBody] Budget budget)
        {
            if (budget == null) return BadRequest("Invalid budget data.");

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    INSERT INTO Budgets 
                    (ProjectId, BudgetCode, BudgetTitle, FiscalYear, TotalAmount, ApprovedAmount, ApprovalStatus, CreatedBy, CreatedDate)
                    OUTPUT INSERTED.BudgetId
                    VALUES 
                    (@ProjectId, @BudgetCode, @BudgetTitle, @FiscalYear, @TotalAmount, @ApprovedAmount, @ApprovalStatus, @CreatedBy, GETDATE());
                ", conn);

                cmd.Parameters.AddWithValue("@ProjectId", budget.ProjectId);
                cmd.Parameters.AddWithValue("@BudgetCode", budget.BudgetCode);
                cmd.Parameters.AddWithValue("@BudgetTitle", budget.BudgetTitle);
                cmd.Parameters.AddWithValue("@FiscalYear", budget.FiscalYear);
                cmd.Parameters.AddWithValue("@TotalAmount", budget.TotalAmount);
                cmd.Parameters.AddWithValue("@ApprovedAmount", (object)budget.ApprovedAmount ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ApprovalStatus", (object)budget.ApprovalStatus ?? "Pending");
                cmd.Parameters.AddWithValue("@CreatedBy", (object)budget.CreatedBy ?? "System");

                budget.BudgetId = (int)cmd.ExecuteScalar();
            }

            return Ok(budget);
        }

        [HttpPost]
        [Route("CreateBudgetLine")]
        public async Task<IHttpActionResult> CreateBudgetLine([FromBody] BudgetLine line)
        {
            if (line == null) return BadRequest("Invalid budget line data.");

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    INSERT INTO BudgetLines 
                    (BudgetId, AccountCode, Description, AllocatedAmount, SpentAmount, Department, Category, Status, CreatedBy, CreatedDate)
                    OUTPUT INSERTED.BudgetLineId
                    VALUES 
                    (@BudgetId, @AccountCode, @Description, @AllocatedAmount, @SpentAmount, @Department, @Category, @Status, 'System', GETDATE());
                ", conn);

                cmd.Parameters.AddWithValue("@BudgetId", line.BudgetId);
                cmd.Parameters.AddWithValue("@AccountCode", line.AccountCode);
                cmd.Parameters.AddWithValue("@Description", line.Description);
                cmd.Parameters.AddWithValue("@AllocatedAmount", line.AllocatedAmount);
                cmd.Parameters.AddWithValue("@SpentAmount", line.SpentAmount);
                cmd.Parameters.AddWithValue("@Department", (object)line.Department ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Category", (object)line.Category ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Status", (object)line.Status ?? "Active");

                line.BudgetLineId = (int)cmd.ExecuteScalar();
            }

            return Ok(line);
        }

        #endregion

        #region GET ENDPOINTS

        [HttpGet]
        [Route("GetProjects")]
        public async Task<IHttpActionResult> GetProjects()
        {
            var list = new List<Project>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT * FROM Projects ORDER BY id DESC", conn);
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    list.Add(new Project
                    {
                        ProjectId = (int)rdr["ProjectId"],
                        ProjectCode = rdr["ProjectCode"].ToString(),
                        ProjectName = rdr["ProjectName"].ToString(),
                        ProjectManager = rdr["ProjectManager"].ToString(),
                        Department = rdr["Department"].ToString(),
                        Status = rdr["Status"].ToString(),
                        BudgetAmount = rdr["BudgetAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["BudgetAmount"]),
                        StartDate = rdr["StartDate"] == DBNull.Value ? null : (DateTime?)rdr["StartDate"],
                        EndDate = rdr["EndDate"] == DBNull.Value ? null : (DateTime?)rdr["EndDate"]
                    });
                }
            }

            return Ok(list);
        }


        [HttpGet]
        [Route("GetProjectswithbudgets")]
        public async Task<IHttpActionResult> GetProjectswithbudgets()
        {
            var projects = new List<Project>();

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                // 1️⃣ Fetch all projects
                using (var cmd = new SqlCommand("SELECT * FROM Projects ORDER BY CreatedDate DESC", conn))
                using (var rdr = await cmd.ExecuteReaderAsync())
                {
                    while (await rdr.ReadAsync())
                    {
                        projects.Add(new Project
                        {
                            ProjectId = (int)rdr["ProjectId"],
                            ProjectCode = rdr["ProjectCode"].ToString(),
                            ProjectName = rdr["ProjectName"].ToString(),
                            ProjectManager = rdr["ProjectManager"].ToString(),
                            Department = rdr["Department"].ToString(),
                            Status = rdr["Status"].ToString(),
                            BudgetAmount = rdr["BudgetAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["BudgetAmount"]),
                            StartDate = rdr["StartDate"] == DBNull.Value ? null : (DateTime?)rdr["StartDate"],
                            EndDate = rdr["EndDate"] == DBNull.Value ? null : (DateTime?)rdr["EndDate"],
                            Budgets = new List<Budget>()
                        });
                    }
                }

                // 2️⃣ For each project, get budgets where ProjectId matches
                foreach (var project in projects)
                {
                    using (var cmdBudgets = new SqlCommand("SELECT * FROM Budgets WHERE ProjectId = @ProjectId", conn))
                    {
                        cmdBudgets.Parameters.AddWithValue("@ProjectId", project.ProjectId);

                        using (var rdrBudgets = await cmdBudgets.ExecuteReaderAsync())
                        {
                            while (await rdrBudgets.ReadAsync())
                            {
                                project.Budgets.Add(new Budget
                                {
                                    BudgetId = (int)rdrBudgets["BudgetId"],
                                    ProjectId = (int)rdrBudgets["ProjectId"],
                                    BudgetCode = rdrBudgets["BudgetCode"].ToString(),
                                    BudgetTitle = rdrBudgets["BudgetTitle"].ToString(),
                                    FiscalYear = (int)rdrBudgets["FiscalYear"],
                                    TotalAmount = Convert.ToDecimal(rdrBudgets["TotalAmount"]),
                                    ApprovedAmount = rdrBudgets["ApprovedAmount"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(rdrBudgets["ApprovedAmount"]),
                                    ApprovalStatus = rdrBudgets["ApprovalStatus"].ToString(),
                                    CreatedBy = rdrBudgets["CreatedBy"].ToString(),
                                    CreatedDate = Convert.ToDateTime(rdrBudgets["CreatedDate"])
                                });
                            }
                        }
                    }
                }
            }

            return Ok(projects);
        }


        [HttpGet]
        [Route("GetBudgetsByProject/{projectId}")]
        public async Task<IHttpActionResult> GetBudgetsByProject(int projectId)
        {
            var list = new List<Budget>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT * FROM Budgets WHERE ProjectId=@ProjectId", conn);
                cmd.Parameters.AddWithValue("@ProjectId", projectId);
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    list.Add(new Budget
                    {
                        BudgetId = (int)rdr["BudgetId"],
                        ProjectId = (int)rdr["ProjectId"],
                        BudgetCode = rdr["BudgetCode"].ToString(),
                        BudgetTitle = rdr["BudgetTitle"].ToString(),
                        FiscalYear = (int)rdr["FiscalYear"],
                        TotalAmount = Convert.ToDecimal(rdr["TotalAmount"]),
                        ApprovedAmount = rdr["ApprovedAmount"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(rdr["ApprovedAmount"]),
                        ApprovalStatus = rdr["ApprovalStatus"].ToString(),
                        CreatedBy = rdr["CreatedBy"].ToString(),
                        CreatedDate = Convert.ToDateTime(rdr["CreatedDate"])
                    });
                }
            }

            return Ok(list);
        }



        [HttpGet]
        [Route("GetAllProjectWithBudgetsAndLines")]
        public async Task<IHttpActionResult> GetAllProjectWithBudgetsAndLines()
        {
            var projects = new List<Project>();

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                // 1️⃣ Fetch all projects
                var projectCmd = new SqlCommand("SELECT * FROM Projects", conn);
                using (var rdr = await projectCmd.ExecuteReaderAsync())
                {
                    while (await rdr.ReadAsync())
                    {
                        projects.Add(new Project
                        {
                            ProjectId = (int)rdr["ProjectId"],
                            ProjectCode = rdr["ProjectCode"].ToString(),
                            ProjectName = rdr["ProjectName"].ToString(),
                            ProjectManager = rdr["ProjectManager"]?.ToString(),
                            StartDate = rdr["StartDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(rdr["StartDate"]),
                            EndDate = rdr["EndDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(rdr["EndDate"]),
                            BudgetAmount = rdr["BudgetAmount"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(rdr["BudgetAmount"]),
                            Department = rdr["Department"]?.ToString(),
                            Budgets = new List<Budget>()
                        });
                    }
                }

                if (!projects.Any())
                    return Ok(new List<Project>()); // Return empty list instead of NotFound

                // 2️⃣ Fetch all budgets across all projects
                var projectIds = string.Join(",", projects.Select(p => p.ProjectId));
                var budgetQuery = $"SELECT * FROM Budgets WHERE ProjectId IN ({projectIds})";
                var budgets = new List<Budget>();

                using (var rdr = await new SqlCommand(budgetQuery, conn).ExecuteReaderAsync())
                {
                    while (await rdr.ReadAsync())
                    {
                        budgets.Add(new Budget
                        {
                            BudgetId = (int)rdr["BudgetId"],
                            ProjectId = (int)rdr["ProjectId"],
                            BudgetCode = rdr["BudgetCode"].ToString(),
                            BudgetTitle = rdr["BudgetTitle"].ToString(),
                            FiscalYear = (int)rdr["FiscalYear"],
                            TotalAmount = Convert.ToDecimal(rdr["TotalAmount"]),
                            ApprovedAmount = rdr["ApprovedAmount"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(rdr["ApprovedAmount"]),
                            ApprovalStatus = rdr["ApprovalStatus"].ToString(),
                            CreatedBy = rdr["CreatedBy"].ToString(),
                            CreatedDate = Convert.ToDateTime(rdr["CreatedDate"]),
                            BudgetLines = new List<BudgetLine>()
                        });
                    }
                }

                // 3️⃣ Fetch all budget lines for these budgets
                if (budgets.Any())
                {
                    var budgetIds = string.Join(",", budgets.Select(b => b.BudgetId));
                    var linesQuery = $"SELECT * FROM BudgetLines WHERE BudgetId IN ({budgetIds})";

                    using (var rdr = await new SqlCommand(linesQuery, conn).ExecuteReaderAsync())
                    {
                        while (await rdr.ReadAsync())
                        {
                            var line = new BudgetLine
                            {
                                BudgetLineId = (int)rdr["BudgetLineId"],
                                BudgetId = (int)rdr["BudgetId"],
                                AccountCode = rdr["AccountCode"].ToString(),
                                Description = rdr["Description"].ToString(),
                                AllocatedAmount = Convert.ToDecimal(rdr["AllocatedAmount"]),
                                SpentAmount = Convert.ToDecimal(rdr["SpentAmount"]),
                                RemainingAmount = Convert.ToDecimal(rdr["RemainingAmount"]),
                                Department = rdr["Department"].ToString(),
                                Category = rdr["Category"].ToString(),
                                Status = rdr["Status"].ToString()
                            };

                            var budget = budgets.FirstOrDefault(b => b.BudgetId == line.BudgetId);
                            if (budget != null)
                                budget.BudgetLines.Add(line);
                        }
                    }
                }

                // 4️⃣ Attach budgets to their corresponding projects
                foreach (var project in projects)
                {
                    project.Budgets = budgets.Where(b => b.ProjectId == project.ProjectId).ToList();
                }
            }

            return Ok(projects);
        }




        [HttpPost]
        [Route("CreateProjectWithBudgetsAndLines")]
        public async Task<IHttpActionResult> CreateProjectWithBudgetsAndLines([FromBody] Project project)
        {
            if (project == null)
                return BadRequest("Project data is required.");

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // 1️⃣ Insert Project
                        var insertProjectSql = @"
                    INSERT INTO Projects (ProjectCode, ProjectName, ProjectManager, StartDate, EndDate, BudgetAmount, Department)
                    OUTPUT INSERTED.ProjectId
                    VALUES (@ProjectCode, @ProjectName, @ProjectManager, @StartDate, @EndDate, @BudgetAmount, @Department)";

                        var projectCmd = new SqlCommand(insertProjectSql, conn, tran);
                        projectCmd.Parameters.AddWithValue("@ProjectCode", project.ProjectCode);
                        projectCmd.Parameters.AddWithValue("@ProjectName", project.ProjectName);
                        projectCmd.Parameters.AddWithValue("@ProjectManager", (object)project.ProjectManager ?? DBNull.Value);
                        projectCmd.Parameters.AddWithValue("@StartDate", (object)project.StartDate ?? DBNull.Value);
                        projectCmd.Parameters.AddWithValue("@EndDate", (object)project.EndDate ?? DBNull.Value);
                        projectCmd.Parameters.AddWithValue("@BudgetAmount", (object)project.BudgetAmount ?? DBNull.Value);
                        projectCmd.Parameters.AddWithValue("@Department", (object)project.Department ?? DBNull.Value);

                        int projectId = Convert.ToInt32(await projectCmd.ExecuteScalarAsync());
                        project.ProjectId = projectId;

                        // 2️⃣ Insert Budgets
                        if (project.Budgets != null && project.Budgets.Any())
                        {
                            foreach (var budget in project.Budgets)
                            {
                                var insertBudgetSql = @"
                            INSERT INTO Budgets (ProjectId, BudgetCode, BudgetTitle, FiscalYear, TotalAmount, ApprovedAmount, ApprovalStatus, CreatedBy, CreatedDate)
                            OUTPUT INSERTED.BudgetId
                            VALUES (@ProjectId, @BudgetCode, @BudgetTitle, @FiscalYear, @TotalAmount, @ApprovedAmount, @ApprovalStatus, @CreatedBy, @CreatedDate)";

                                var budgetCmd = new SqlCommand(insertBudgetSql, conn, tran);
                                budgetCmd.Parameters.AddWithValue("@ProjectId", projectId);
                                budgetCmd.Parameters.AddWithValue("@BudgetCode", budget.BudgetCode);
                                budgetCmd.Parameters.AddWithValue("@BudgetTitle", budget.BudgetTitle);
                                budgetCmd.Parameters.AddWithValue("@FiscalYear", budget.FiscalYear);
                                budgetCmd.Parameters.AddWithValue("@TotalAmount", budget.TotalAmount);
                                budgetCmd.Parameters.AddWithValue("@ApprovedAmount", (object)budget.ApprovedAmount ?? DBNull.Value);
                                budgetCmd.Parameters.AddWithValue("@ApprovalStatus", budget.ApprovalStatus);
                                budgetCmd.Parameters.AddWithValue("@CreatedBy", budget.CreatedBy);
                                budgetCmd.Parameters.AddWithValue("@CreatedDate", budget.CreatedDate);

                                int budgetId = Convert.ToInt32(await budgetCmd.ExecuteScalarAsync());
                                budget.BudgetId = budgetId;

                                // 3️⃣ Insert Budget Lines
                                if (budget.BudgetLines != null && budget.BudgetLines.Any())
                                {
                                    foreach (var line in budget.BudgetLines)
                                    {
                                        var insertLineSql = @"
                                    INSERT INTO BudgetLines (BudgetId, AccountCode, Description, AllocatedAmount, SpentAmount,  Department, Category, Status)
                                    VALUES (@BudgetId, @AccountCode, @Description, @AllocatedAmount, @SpentAmount,  @Department, @Category, @Status)";

                                        var lineCmd = new SqlCommand(insertLineSql, conn, tran);
                                        lineCmd.Parameters.AddWithValue("@BudgetId", budgetId);
                                        lineCmd.Parameters.AddWithValue("@AccountCode", line.AccountCode);
                                        lineCmd.Parameters.AddWithValue("@Description", line.Description);
                                        lineCmd.Parameters.AddWithValue("@AllocatedAmount", line.AllocatedAmount);
                                        lineCmd.Parameters.AddWithValue("@SpentAmount", line.SpentAmount);
                                        lineCmd.Parameters.AddWithValue("@Department", line.Department);
                                        lineCmd.Parameters.AddWithValue("@Category", line.Category);
                                        lineCmd.Parameters.AddWithValue("@Status", line.Status);

                                        await lineCmd.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }

                        tran.Commit();
                        return Ok(new { Message = "Project with budgets and lines created successfully.", ProjectId = projectId });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return InternalServerError(ex);
                    }
                }
            }
        }


        [HttpGet]
        [Route("GetProjectWithBudgetsAndLines/{projectId}")]
        public async Task<IHttpActionResult> GetProjectWithBudgetsAndLines(int projectId)
        {
            Project project = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                // 1️⃣ Fetch Project
                var projectCmd = new SqlCommand("SELECT * FROM Projects WHERE ProjectId=@ProjectId", conn);
                projectCmd.Parameters.AddWithValue("@ProjectId", projectId);

                using (var rdr = await projectCmd.ExecuteReaderAsync())
                {
                    if (await rdr.ReadAsync())
                    {
                        project = new Project
                        {
                            ProjectId = (int)rdr["ProjectId"],
                            ProjectCode = rdr["ProjectCode"].ToString(),
                            ProjectName = rdr["ProjectName"].ToString(),
                            ProjectManager = rdr["ProjectManager"]?.ToString(),
                            StartDate = rdr["StartDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(rdr["StartDate"]),
                            EndDate = rdr["EndDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(rdr["EndDate"]),
                            BudgetAmount = rdr["BudgetAmount"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(rdr["BudgetAmount"]),
                            Department = rdr["Department"]?.ToString(),
                            Budgets = new List<Budget>()
                        };
                    }
                }

                if (project == null)
                    return NotFound();

                // 2️⃣ Fetch Budgets
                var budgetCmd = new SqlCommand("SELECT * FROM Budgets WHERE ProjectId=@ProjectId", conn);
                budgetCmd.Parameters.AddWithValue("@ProjectId", projectId);

                var budgetList = new List<Budget>();
                using (var rdr = await budgetCmd.ExecuteReaderAsync())
                {
                    while (await rdr.ReadAsync())
                    {
                        budgetList.Add(new Budget
                        {
                            BudgetId = (int)rdr["BudgetId"],
                            ProjectId = (int)rdr["ProjectId"],
                            BudgetCode = rdr["BudgetCode"].ToString(),
                            BudgetTitle = rdr["BudgetTitle"].ToString(),
                            FiscalYear = (int)rdr["FiscalYear"],
                            TotalAmount = Convert.ToDecimal(rdr["TotalAmount"]),
                            ApprovedAmount = rdr["ApprovedAmount"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(rdr["ApprovedAmount"]),
                            ApprovalStatus = rdr["ApprovalStatus"].ToString(),
                            CreatedBy = rdr["CreatedBy"].ToString(),
                            CreatedDate = Convert.ToDateTime(rdr["CreatedDate"]),
                            BudgetLines = new List<BudgetLine>()
                        });
                    }
                }

                if (budgetList.Any())
                {
                    var budgetIds = string.Join(",", budgetList.Select(b => b.BudgetId));
                    var linesQuery = $"SELECT * FROM BudgetLines WHERE BudgetId IN ({budgetIds})";

                    var linesCmd = new SqlCommand(linesQuery, conn);
                    using (var rdr = await linesCmd.ExecuteReaderAsync())
                    {
                        while (await rdr.ReadAsync())
                        {
                            var line = new BudgetLine
                            {
                                BudgetLineId = (int)rdr["BudgetLineId"],
                                BudgetId = (int)rdr["BudgetId"],
                                AccountCode = rdr["AccountCode"].ToString(),
                                Description = rdr["Description"].ToString(),
                                AllocatedAmount = Convert.ToDecimal(rdr["AllocatedAmount"]),
                                SpentAmount = Convert.ToDecimal(rdr["SpentAmount"]),
                                RemainingAmount = Convert.ToDecimal(rdr["RemainingAmount"]),
                                Department = rdr["Department"].ToString(),
                                Category = rdr["Category"].ToString(),
                                Status = rdr["Status"].ToString(),

                            };


                            var budget = budgetList.FirstOrDefault(b => b.BudgetId == line.BudgetId);
                            if (budget != null)
                                budget.BudgetLines.Add(line);
                        }
                    }
                }

                // Attach budgets to project
                project.Budgets = budgetList;
            }

            return Ok(project);
        }


        [HttpGet]
        [Route("GetBudgetLinesByBudget/{budgetId}")]
        public async Task<IHttpActionResult> GetBudgetLinesByBudget(int budgetId)
        {
            var lines = new List<BudgetLine>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT * FROM BudgetLines WHERE BudgetId=@BudgetId", conn);
                cmd.Parameters.AddWithValue("@BudgetId", budgetId);
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    lines.Add(new BudgetLine
                    {
                        BudgetLineId = (int)rdr["BudgetLineId"],
                        BudgetId = (int)rdr["BudgetId"],
                        AccountCode = rdr["AccountCode"].ToString(),
                        Description = rdr["Description"].ToString(),
                        AllocatedAmount = Convert.ToDecimal(rdr["AllocatedAmount"]),
                        SpentAmount = Convert.ToDecimal(rdr["SpentAmount"]),
                        RemainingAmount = Convert.ToDecimal(rdr["AllocatedAmount"]) - Convert.ToDecimal(rdr["SpentAmount"]),
                        Department = rdr["Department"].ToString(),
                        Category = rdr["Category"].ToString(),
                        Status = rdr["Status"].ToString()
                    });
                }
            }

            return Ok(lines);
        }
        [HttpPost]
        [Route("CreateBudgetWithLines")]
        public async Task<IHttpActionResult> CreateBudgetWithLines([FromBody] Budget budget)
        {
            if (budget == null || budget.BudgetLines == null || budget.BudgetLines.Count == 0)
                return BadRequest("Invalid budget or budget lines.");

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // 1️⃣ Create the Budget
                        var cmdBudget = new SqlCommand(@"
                            INSERT INTO Budgets 
                            (ProjectId, BudgetCode, BudgetTitle, FiscalYear, TotalAmount, ApprovedAmount, ApprovalStatus, CreatedBy, CreatedDate)
                            OUTPUT INSERTED.BudgetId
                            VALUES 
                            (@ProjectId, @BudgetCode, @BudgetTitle, @FiscalYear, @TotalAmount, @ApprovedAmount, @ApprovalStatus, @CreatedBy, GETDATE());
                        ", conn, tran);

                        cmdBudget.Parameters.AddWithValue("@ProjectId", budget.ProjectId);
                        cmdBudget.Parameters.AddWithValue("@BudgetCode", budget.BudgetCode);
                        cmdBudget.Parameters.AddWithValue("@BudgetTitle", budget.BudgetTitle);
                        cmdBudget.Parameters.AddWithValue("@FiscalYear", budget.FiscalYear);
                        cmdBudget.Parameters.AddWithValue("@TotalAmount", budget.TotalAmount);
                        cmdBudget.Parameters.AddWithValue("@ApprovedAmount", (object)budget.ApprovedAmount ?? DBNull.Value);
                        cmdBudget.Parameters.AddWithValue("@ApprovalStatus", (object)budget.ApprovalStatus ?? "Pending");
                        cmdBudget.Parameters.AddWithValue("@CreatedBy", (object)budget.CreatedBy ?? "System");

                        int budgetId = (int)cmdBudget.ExecuteScalar();
                        budget.BudgetId = budgetId;

                        // 2️⃣ Insert each BudgetLine
                        foreach (var line in budget.BudgetLines)
                        {
                            var cmdLine = new SqlCommand(@"
                                INSERT INTO BudgetLines 
                                (BudgetId, AccountCode, Description, AllocatedAmount, SpentAmount, Department, Category, Status, CreatedBy, CreatedDate)
                                OUTPUT INSERTED.BudgetLineId
                                VALUES 
                                (@BudgetId, @AccountCode, @Description, @AllocatedAmount, @SpentAmount, @Department, @Category, @Status, 'System', GETDATE());
                            ", conn, tran);

                            cmdLine.Parameters.AddWithValue("@BudgetId", budgetId);
                            cmdLine.Parameters.AddWithValue("@AccountCode", line.AccountCode);
                            cmdLine.Parameters.AddWithValue("@Description", line.Description);
                            cmdLine.Parameters.AddWithValue("@AllocatedAmount", line.AllocatedAmount);
                            cmdLine.Parameters.AddWithValue("@SpentAmount", line.SpentAmount);
                            cmdLine.Parameters.AddWithValue("@Department", (object)line.Department ?? DBNull.Value);
                            cmdLine.Parameters.AddWithValue("@Category", (object)line.Category ?? DBNull.Value);
                            cmdLine.Parameters.AddWithValue("@Status", (object)line.Status ?? "Active");

                            line.BudgetLineId = (int)cmdLine.ExecuteScalar();
                            line.BudgetId = budgetId;
                        }

                        tran.Commit();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return InternalServerError(ex);
                    }
                }
            }

            return Ok(budget);
        }

        [HttpGet]
        [Route("GetBudgetsWithLines/{projectId}")]
        public async Task<IHttpActionResult> GetBudgetsWithLines(int projectId)
        {
            var budgets = new List<Budget>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                var cmdBudgets = new SqlCommand("SELECT * FROM Budgets WHERE ProjectId=@ProjectId", conn);
                cmdBudgets.Parameters.AddWithValue("@ProjectId", projectId);
                var rdrBudgets = cmdBudgets.ExecuteReader();

                while (rdrBudgets.Read())
                {
                    budgets.Add(new Budget
                    {
                        BudgetId = (int)rdrBudgets["BudgetId"],
                        ProjectId = projectId,
                        BudgetCode = rdrBudgets["BudgetCode"].ToString(),
                        BudgetTitle = rdrBudgets["BudgetTitle"].ToString(),
                        FiscalYear = (int)rdrBudgets["FiscalYear"],
                        TotalAmount = Convert.ToDecimal(rdrBudgets["TotalAmount"])
                    });
                }
                rdrBudgets.Close();

                foreach (var budget in budgets)
                {
                    var cmdLines = new SqlCommand("SELECT * FROM BudgetLines WHERE BudgetId=@BudgetId", conn);
                    cmdLines.Parameters.AddWithValue("@BudgetId", budget.BudgetId);
                    var rdrLines = cmdLines.ExecuteReader();
                    while (rdrLines.Read())
                    {
                        budget.BudgetLines.Add(new BudgetLine
                        {
                            BudgetLineId = (int)rdrLines["BudgetLineId"],
                            AccountCode = rdrLines["AccountCode"].ToString(),
                            Description = rdrLines["Description"].ToString(),
                            AllocatedAmount = Convert.ToDecimal(rdrLines["AllocatedAmount"]),
                            SpentAmount = Convert.ToDecimal(rdrLines["SpentAmount"]),
                            RemainingAmount = Convert.ToDecimal(rdrLines["AllocatedAmount"]) - Convert.ToDecimal(rdrLines["SpentAmount"]),
                            Department = rdrLines["Department"].ToString(),
                            Category = rdrLines["Category"].ToString(),
                            Status = rdrLines["Status"].ToString()
                        });
                    }
                    rdrLines.Close();
                }
            }

            return Ok(budgets);
        }

        #endregion
    }
}

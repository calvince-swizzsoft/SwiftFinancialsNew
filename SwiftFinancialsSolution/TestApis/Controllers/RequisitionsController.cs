using Procurement.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using TestApis.Controllers;

namespace Procurement.Controllers.Api
{
    [RoutePrefix("api/requisitions")]
    public class RequisitionsController : ApiController
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        private readonly MasterController master;

        public RequisitionsController()
        {
            master = new MasterController();
        }

        private IHttpActionResult JsonResponse(bool success, string message, object data = null)
        {
            return Json(new { success, message, data });
        }

        // ===========================
        // GET: api/requisitions
        // ===========================
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetAll()
        {
            var requisitions = new List<PurchaseRequisitionDto>();

            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    await con.OpenAsync();

                    var headerSql = @"
                SELECT 
                    pr.RequisitionId,
                    pr.RequisitionNumber,
                    pr.ProjectDescription,
                    pr.ProjectCode,
                    pr.ProjectId,
                    pr.Purpose,
                    pr.RequestedBy,
                    pr.DepartmentId,
                    pr.RequestDate,
                    pr.Status,
                    pr.TotalAmount,
                    e.Id AS EmployeeId,
                    e.CustomerId AS EmployeeCustomerId,
                    c.Individual_FirstName,
                    c.Individual_LastName,
                    d.Description AS DepartmentName
                FROM dbo.PurchaseRequisitions pr
                INNER JOIN SwiftFinancialsDB_Live.dbo.swiftFin_Employees e ON pr.RequestedBy = e.Id
                INNER JOIN SwiftFinancialsDB_Live.dbo.swiftFin_Customers c ON e.CustomerId = c.Id
                INNER JOIN SwiftFinancialsDB_Live.dbo.swiftFin_Departments d ON e.DepartmentId = d.Id
                ORDER BY pr.RequisitionId DESC;";

                    var requisitionLookup = new Dictionary<long, PurchaseRequisitionDto>();

                    // Load headers
                    using (var cmd = new SqlCommand(headerSql, con))
                    using (var rdr = await cmd.ExecuteReaderAsync())
                    {
                        while (await rdr.ReadAsync())
                        {
                            var requisition = new PurchaseRequisitionDto
                            {
                                RequisitionId = rdr["RequisitionId"] != DBNull.Value ? Convert.ToInt64(rdr["RequisitionId"]) : 0,
                                RequisitionNumber = rdr["RequisitionNumber"]?.ToString(),
                                Projectcode = rdr["ProjectCode"]?.ToString(),
                                ProjectId = rdr["ProjectId"] != DBNull.Value ? Convert.ToInt32(rdr["ProjectId"]) : 0,
                                ProjectDescription = rdr["ProjectDescription"]?.ToString(),
                                Purpose = rdr["Purpose"]?.ToString(),
                                RequestedBy = rdr["RequestedBy"] != DBNull.Value ? Guid.Parse(rdr["RequestedBy"].ToString()) : Guid.Empty,
                                RequestedByFullname = $"{(rdr["Individual_FirstName"] ?? "").ToString()} {(rdr["Individual_LastName"] ?? "").ToString()}".Trim(),
                                DepartmentId = rdr["DepartmentId"] != DBNull.Value ? Guid.Parse(rdr["DepartmentId"].ToString()) : Guid.Empty,
                                DepartmentName = rdr["DepartmentName"]?.ToString(),
                                RequestDate = (DateTime)(rdr["RequestDate"] != DBNull.Value ? Convert.ToDateTime(rdr["RequestDate"]) : (DateTime?)null),
                                Status = rdr["Status"]?.ToString(),
                                TotalAmount = rdr["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(rdr["TotalAmount"]) : 0,
                                Lines = new List<PurchaseRequisitionLineDto>()
                            };

                            requisitionLookup[requisition.RequisitionId] = requisition;
                        }
                    }

                    // Load line items
                    var linesSql = @"
                SELECT 
                    RequisitionId, 
                    LineNumber, 
                    ItemId, 
                    ItemDescription, 
                    Quantity, 
                    UnitPrice, 
                    AccountCode, 
                    BudgetLine, 
                    BudgetDescription
                FROM dbo.PurchaseRequisitionLines
                ORDER BY RequisitionId, LineNumber;";

                    using (var cmd = new SqlCommand(linesSql, con))
                    using (var rdr = await cmd.ExecuteReaderAsync())
                    {
                        while (await rdr.ReadAsync())
                        {
                            var requisitionId = rdr["RequisitionId"] != DBNull.Value ? Convert.ToInt64(rdr["RequisitionId"]) : 0;
                            if (requisitionLookup.TryGetValue(requisitionId, out var requisition))
                            {
                                requisition.Lines.Add(new PurchaseRequisitionLineDto
                                {
                                    LineNumber = rdr["LineNumber"] != DBNull.Value ? Convert.ToInt32(rdr["LineNumber"]) : 0,
                                    ItemId = rdr["ItemId"] != DBNull.Value ? Guid.Parse(rdr["ItemId"].ToString()) : Guid.Empty,
                                    ItemDescription = rdr["ItemDescription"]?.ToString(),
                                    Quantity = rdr["Quantity"] != DBNull.Value ? Convert.ToDecimal(rdr["Quantity"]) : 0,
                                    UnitPrice = rdr["UnitPrice"] != DBNull.Value ? Convert.ToDecimal(rdr["UnitPrice"]) : 0,
                                    AccountCode = rdr["AccountCode"]?.ToString(),
                                    BudgetLine = rdr["BudgetLine"] != DBNull.Value ? Convert.ToInt32(rdr["BudgetLine"]) : 0,
                                    Budgetdescription = rdr["BudgetDescription"]?.ToString()
                                });
                            }
                        }
                    }

                    requisitions = requisitionLookup.Values.ToList();
                }

                return JsonResponse(true, "Requisitions retrieved successfully", requisitions);
            }
            catch (Exception ex)
            {
                return JsonResponse(false, $"Error retrieving requisitions: {ex.Message}");
            }
        }


        [HttpGet]
        [Route("getAllByStatus")]
        public async Task<IHttpActionResult> GetAllByStatus(string status)
        {
            var requisitions = new List<PurchaseRequisitionDto>();

            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    await con.OpenAsync();

                    // Dynamically add WHERE clause if status is provided
                    var headerSql = @"
                SELECT 
                    pr.RequisitionId,
                    pr.RequisitionNumber,
                    pr.Purpose,
                    pr.RequestedBy,
                    pr.DepartmentId,
                    pr.RequestDate,
                    pr.Status,
                    pr.TotalAmount,
                    e.Id AS EmployeeId,
                    e.CustomerId AS EmployeeCustomerId,
                    c.Individual_FirstName,
                    c.Individual_LastName,
                    d.Description AS DepartmentName
                FROM dbo.PurchaseRequisitions pr
                INNER JOIN SwiftFinancialsDB_Live.dbo.swiftFin_Employees e ON pr.RequestedBy = e.Id
                INNER JOIN SwiftFinancialsDB_Live.dbo.swiftFin_Customers c ON e.CustomerId = c.Id
                INNER JOIN SwiftFinancialsDB_Live.dbo.swiftFin_Departments d ON e.DepartmentId = d.Id
                WHERE (@Status IS NULL OR pr.Status = @Status)
                ORDER BY pr.RequisitionId DESC;";

                    var requisitionLookup = new Dictionary<long, PurchaseRequisitionDto>();

                    // Load headers
                    using (var cmd = new SqlCommand(headerSql, con))
                    {
                        cmd.Parameters.AddWithValue("@Status", (object)status ?? DBNull.Value);

                        using (var rdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await rdr.ReadAsync())
                            {
                                var requisition = new PurchaseRequisitionDto
                                {
                                    RequisitionId = rdr.GetInt64(rdr.GetOrdinal("RequisitionId")),
                                    RequisitionNumber = rdr["RequisitionNumber"]?.ToString(),
                                    Purpose = rdr["Purpose"]?.ToString(),
                                    RequestedBy = rdr["RequestedBy"] != DBNull.Value ? Guid.Parse(rdr["RequestedBy"].ToString()) : Guid.Empty,
                                    RequestedByFullname = $"{rdr["Individual_FirstName"]} {rdr["Individual_LastName"]}".Trim(),
                                    DepartmentId = rdr["DepartmentId"] != DBNull.Value ? Guid.Parse(rdr["DepartmentId"].ToString()) : Guid.Empty,
                                    DepartmentName = rdr["DepartmentName"]?.ToString(),
                                    RequestDate = rdr["RequestDate"] != DBNull.Value ? Convert.ToDateTime(rdr["RequestDate"]) : DateTime.MinValue,
                                    Status = rdr["Status"]?.ToString(),
                                    TotalAmount = rdr["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(rdr["TotalAmount"]) : 0,
                                    Lines = new List<PurchaseRequisitionLineDto>()
                                };

                                requisitionLookup[requisition.RequisitionId] = requisition;
                            }
                        }
                    }

                    // Load line items
                    var linesSql = @"
                        SELECT RequisitionId, LineNumber, ItemId, ItemDescription, Quantity, UnitPrice, AccountCode,BudgetLine,Budgetdescription
                        FROM dbo.PurchaseRequisitionLines
                        ORDER BY RequisitionId, LineNumber;";

                    using (var cmd = new SqlCommand(linesSql, con))
                    using (var rdr = await cmd.ExecuteReaderAsync())
                    {
                        while (await rdr.ReadAsync())
                        {
                            var requisitionId = rdr.GetInt64(rdr.GetOrdinal("RequisitionId"));
                            if (requisitionLookup.TryGetValue(requisitionId, out var requisition))
                            {
                                requisition.Lines.Add(new PurchaseRequisitionLineDto
                                {
                                    LineNumber = rdr["LineNumber"] != DBNull.Value ? Convert.ToInt32(rdr["LineNumber"]) : 0,
                                    ItemId = rdr["ItemId"] != DBNull.Value ? Guid.Parse(rdr["ItemId"].ToString()) : Guid.Empty,
                                    ItemDescription = rdr["ItemDescription"]?.ToString(),
                                    Quantity = rdr["Quantity"] != DBNull.Value ? Convert.ToDecimal(rdr["Quantity"]) : 0,
                                    UnitPrice = rdr["UnitPrice"] != DBNull.Value ? Convert.ToDecimal(rdr["UnitPrice"]) : 0,
                                    AccountCode = rdr["AccountCode"]?.ToString(),
                                    BudgetLine = rdr["BudgetLine"] != DBNull.Value ? Convert.ToInt32(rdr["BudgetLine"]) : 0,
                                    Budgetdescription = rdr["Budgetdescription"]?.ToString(),

                                });
                            }
                        }
                    }

                    requisitions = requisitionLookup.Values.ToList();
                }

                return JsonResponse(true, "Requisitions retrieved successfully", requisitions);
            }
            catch (Exception ex)
            {
                return JsonResponse(false, $"Error retrieving requisitions: {ex.Message}");
            }
        }


        // ===========================
        // POST: api/requisitions
        // ===========================
        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> Create([FromBody] PurchaseRequisitionDto model)
        {
            if (model == null || model.Lines == null || !model.Lines.Any())
            {
                return JsonResponse(false, "Requisition must contain at least one line.");
            }

            const string insertHeader = @"
        INSERT INTO dbo.PurchaseRequisitions 
            (RequisitionNumber,Projectcode,ProjectId,ProjectDescription, RequestedBy, DepartmentId, Purpose, RequestDate, Status, TotalAmount, CreatedAt)
        VALUES 
            (@RequisitionNumber,@Projectcode,@ProjectId,@ProjectDescription, @RequestedBy, @DepartmentId, @Purpose, @RequestDate, @Status, @TotalAmount, SYSUTCDATETIME());
        SELECT CAST(SCOPE_IDENTITY() AS bigint);";

            const string insertLine = @"
        INSERT INTO dbo.PurchaseRequisitionLines 
            (RequisitionId, LineNumber, ItemId, ItemDescription, Quantity, UnitPrice, AccountCode, BudgetLine, Budgetdescription)
        VALUES 
            (@RequisitionId, @LineNumber, @ItemId, @ItemDescription, @Quantity, @UnitPrice, @AccountCode, @BudgetLine, @Budgetdescription);";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            long reqId;
                            var reqNumber = "PR-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

                            // --- INSERT HEADER ---
                            using (var hdr = new SqlCommand(insertHeader, conn, tx))
                            {
                                hdr.Parameters.AddWithValue("@RequisitionNumber", reqNumber);
                                hdr.Parameters.AddWithValue("@Projectcode", model.Projectcode ?? string.Empty);
                                hdr.Parameters.AddWithValue("@ProjectId", model.ProjectId);
                                hdr.Parameters.AddWithValue("@ProjectDescription", model.ProjectDescription ?? string.Empty);
                                hdr.Parameters.AddWithValue("@RequestedBy", model.RequestedBy == Guid.Empty ? Guid.NewGuid() : model.RequestedBy);
                                hdr.Parameters.AddWithValue("@DepartmentId", model.DepartmentId == Guid.Empty ? (object)DBNull.Value : model.DepartmentId);
                                hdr.Parameters.AddWithValue("@Purpose", model.Purpose ?? string.Empty);
                                hdr.Parameters.AddWithValue("@RequestDate", model.RequestDate == default ? DateTime.UtcNow : model.RequestDate);
                                hdr.Parameters.AddWithValue("@Status", model.Status ?? "Draft");
                                hdr.Parameters.AddWithValue("@TotalAmount", model.Lines.Sum(x => x.Quantity * x.UnitPrice));

                                reqId = Convert.ToInt64(await hdr.ExecuteScalarAsync());
                            }

                            // --- INSERT LINES ---
                            using (var line = new SqlCommand(insertLine, conn, tx))
                            {
                                line.Parameters.Add("@RequisitionId", SqlDbType.BigInt);
                                line.Parameters.Add("@LineNumber", SqlDbType.Int);
                                line.Parameters.Add("@ItemId", SqlDbType.UniqueIdentifier);
                                line.Parameters.Add("@ItemDescription", SqlDbType.NVarChar, 1000);
                                line.Parameters.Add("@Quantity", SqlDbType.Decimal);
                                line.Parameters.Add("@UnitPrice", SqlDbType.Decimal);
                                line.Parameters.Add("@AccountCode", SqlDbType.NVarChar, 50);
                                line.Parameters.Add("@BudgetLine", SqlDbType.Int);
                                line.Parameters.Add("@Budgetdescription", SqlDbType.NVarChar, 500);

                                int lineNumber = 1;
                                foreach (var l in model.Lines)
                                {
                                    line.Parameters["@RequisitionId"].Value = reqId;
                                    line.Parameters["@LineNumber"].Value = lineNumber++;
                                    line.Parameters["@ItemId"].Value = l.ItemId == Guid.Empty ? (object)DBNull.Value : l.ItemId;
                                    line.Parameters["@ItemDescription"].Value = l.ItemDescription ?? string.Empty;
                                    line.Parameters["@Quantity"].Value = l.Quantity;
                                    line.Parameters["@UnitPrice"].Value = l.UnitPrice;
                                    line.Parameters["@AccountCode"].Value = l.AccountCode ?? string.Empty;
                                    line.Parameters["@BudgetLine"].Value = l.BudgetLine;
                                    line.Parameters["@Budgetdescription"].Value = l.Budgetdescription ?? string.Empty;

                                    await line.ExecuteNonQueryAsync();
                                }
                            }

                            tx.Commit();
                            return JsonResponse(true, "Requisition created successfully", new { requisitionId = reqId, requisitionNumber = reqNumber });
                        }
                        catch (Exception ex)
                        {
                            tx.Rollback();
                            return JsonResponse(false, $"Error creating requisition: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return JsonResponse(false, $"Error creating requisition: {ex.Message}");
            }
        }



        [HttpGet]
        [Route("employees")]
        public async Task<IHttpActionResult> GetEmployeesAsync()
        {
            var serviceHeader = master.GetServiceHeader();

            var employeesDTOs = await master._channelService.FindEmployeesAsync(serviceHeader);

            if (employeesDTOs == null || !employeesDTOs.Any())
            {
                return NotFound(); // Returns HTTP 404 if no employees are found
            }

            return Ok(employeesDTOs); // Returns HTTP 200 with the list of employees
        }


        [HttpPost]
        [Route("InternalRequisition")]
        public async Task<IHttpActionResult> InternalRequisition([FromBody] PurchaseRequisitionDto model)
        {
            if (model == null || model.Lines == null || !model.Lines.Any())
            {
                return JsonResponse(false, "Requisition must contain at least one line.");
            }

            const string insertHeader = @"
        INSERT INTO dbo.PurchaseRequisitions 
            (RequisitionNumber, RequestedBy, DepartmentId, Purpose, RequestDate, Status, TotalAmount, CreatedAt, RequisitionType)
        VALUES 
            (@RequisitionNumber, @RequestedBy, @DepartmentId, @Purpose, @RequestDate, @Status, @TotalAmount, SYSUTCDATETIME(), @RequisitionType);
        SELECT CAST(SCOPE_IDENTITY() AS bigint);";

            const string insertLine = @"
        INSERT INTO dbo.PurchaseRequisitionLines 
            (RequisitionId, LineNumber, ItemId, ItemDescription, Quantity, UnitPrice, AccountCode)
        VALUES 
            (@RequisitionId, @LineNumber, @ItemId, @ItemDescription, @Quantity, @UnitPrice, @AccountCode);";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            long reqId;
                            string reqNumber = "PR-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

                            // Insert Requisition Header
                            using (var hdr = new SqlCommand(insertHeader, conn, tx))
                            {
                                hdr.Parameters.AddWithValue("@RequisitionNumber", reqNumber);
                                hdr.Parameters.AddWithValue("@RequestedBy", model.RequestedBy == Guid.Empty ? Guid.NewGuid() : model.RequestedBy);
                                hdr.Parameters.AddWithValue("@DepartmentId", model.DepartmentId);
                                hdr.Parameters.AddWithValue("@Purpose", model.Purpose ?? string.Empty);
                                hdr.Parameters.AddWithValue("@RequestDate", model.RequestDate == default ? DateTime.UtcNow : model.RequestDate);
                                hdr.Parameters.AddWithValue("@Status", model.Status ?? "Draft");
                                hdr.Parameters.AddWithValue("@TotalAmount", model.Lines.Sum(x => x.Quantity * x.UnitPrice));
                                hdr.Parameters.AddWithValue("@RequisitionType", "Internal"); // explicitly mark as internal

                                reqId = Convert.ToInt64(await hdr.ExecuteScalarAsync());
                            }

                            // Insert Requisition Lines
                            using (var line = new SqlCommand(insertLine, conn, tx))
                            {
                                line.Parameters.Add("@RequisitionId", SqlDbType.BigInt).Value = reqId;
                                line.Parameters.Add("@LineNumber", SqlDbType.Int);
                                line.Parameters.Add("@ItemId", SqlDbType.BigInt);
                                line.Parameters.Add("@ItemDescription", SqlDbType.NVarChar, 1000);
                                line.Parameters.Add("@Quantity", SqlDbType.Decimal);
                                line.Parameters.Add("@UnitPrice", SqlDbType.Decimal);
                                line.Parameters.Add("@AccountCode", SqlDbType.NVarChar, 50);

                                int lineNumber = 1;
                                foreach (var l in model.Lines)
                                {
                                    line.Parameters["@LineNumber"].Value = lineNumber++;
                                    line.Parameters["@ItemId"].Value = (object)l.ItemId ?? DBNull.Value;
                                    line.Parameters["@ItemDescription"].Value = l.ItemDescription ?? string.Empty;
                                    line.Parameters["@Quantity"].Value = l.Quantity;
                                    line.Parameters["@UnitPrice"].Value = l.UnitPrice;
                                    line.Parameters["@AccountCode"].Value = l.AccountCode ?? string.Empty;

                                    await line.ExecuteNonQueryAsync();
                                }
                            }

                            // Commit transaction
                            tx.Commit();

                            return JsonResponse(true, "Requisition created successfully", new
                            {
                                requisitionId = reqId,
                                requisitionNumber = reqNumber
                            });
                        }
                        catch (Exception ex)
                        {
                            // Rollback on failure
                            tx.Rollback();
                            return JsonResponse(false, $"Error creating requisition: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return JsonResponse(false, $"Error creating requisition: {ex.Message}");
            }
        }


        // ===========================
        // PUT: api/requisitions/{id}
        // ===========================
        [HttpPut]
        [Route("{id:long}")]
        public async Task<IHttpActionResult> Update(long id, [FromBody] PurchaseRequisitionDto model)
        {
            if (!ModelState.IsValid)
                return JsonResponse(false, "Invalid requisition data", ModelState);

            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    await con.OpenAsync();

                    using (var tran = con.BeginTransaction())
                    {
                        try
                        {
                            var hdr = new SqlCommand(@"
                                UPDATE PurchaseRequisitions
                                SET Purpose = @Purpose,
                                    DepartmentId = @DepartmentId,
                                    Status = @Status,
                                    TotalAmount = @TotalAmount
                                WHERE RequisitionId = @RequisitionId", con, tran);

                            hdr.Parameters.AddWithValue("@Purpose", model.Purpose ?? string.Empty);
                            hdr.Parameters.AddWithValue("@DepartmentId", model.DepartmentId);
                            hdr.Parameters.AddWithValue("@Status", model.Status ?? "Draft");
                            hdr.Parameters.AddWithValue("@TotalAmount", model.Lines.Sum(x => x.Quantity * x.UnitPrice));
                            hdr.Parameters.AddWithValue("@RequisitionId", id);

                            await hdr.ExecuteNonQueryAsync();

                            // Delete existing lines
                            var deleteCmd = new SqlCommand("DELETE FROM PurchaseRequisitionLines WHERE RequisitionId = @RequisitionId", con, tran);
                            deleteCmd.Parameters.AddWithValue("@RequisitionId", id);
                            await deleteCmd.ExecuteNonQueryAsync();

                            // Insert new lines
                            int lineNumber = 1;
                            foreach (var l in model.Lines)
                            {
                                var lineCmd = new SqlCommand(@"
                                    INSERT INTO PurchaseRequisitionLines
                                    (RequisitionId, LineNumber, ItemId, ItemDescription, Quantity, UnitPrice, AccountCode)
                                    VALUES (@RequisitionId, @LineNumber, @ItemId, @ItemDescription, @Quantity, @UnitPrice, @AccountCode)", con, tran);

                                lineCmd.Parameters.AddWithValue("@RequisitionId", id);
                                lineCmd.Parameters.AddWithValue("@LineNumber", lineNumber++);
                                lineCmd.Parameters.AddWithValue("@ItemId", (object)l.ItemId ?? DBNull.Value);
                                lineCmd.Parameters.AddWithValue("@ItemDescription", l.ItemDescription ?? string.Empty);
                                lineCmd.Parameters.AddWithValue("@Quantity", l.Quantity);
                                lineCmd.Parameters.AddWithValue("@UnitPrice", l.UnitPrice);
                                lineCmd.Parameters.AddWithValue("@AccountCode", l.AccountCode ?? string.Empty);

                                await lineCmd.ExecuteNonQueryAsync();
                            }

                            tran.Commit();
                            return JsonResponse(true, "Requisition updated successfully", new { requisitionId = id });
                        }
                        catch (Exception ex)
                        {
                            tran.Rollback();
                            return JsonResponse(false, $"Error updating requisition: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return JsonResponse(false, $"Error updating requisition: {ex.Message}");
            }
        }

        // ===========================
        // POST: api/requisitions/{id}/submit
        // ===========================
        [HttpPost]
        [Route("{id:long}/submit/{status}")]
        public async Task<IHttpActionResult> Submit(long id, string status)

        {
            const string checkSql = "SELECT Status FROM dbo.PurchaseRequisitions WHERE RequisitionId = @RequisitionId;";
            const string updateSql = "UPDATE dbo.PurchaseRequisitions SET Status = @Status WHERE RequisitionId = @RequisitionId;";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    using (var check = new SqlCommand(checkSql, conn))
                    {
                        check.Parameters.AddWithValue("@RequisitionId", id);

                        var currentStatus = (await check.ExecuteScalarAsync()) as string;

                        if (currentStatus == null)
                            return JsonResponse(false, "Requisition not found.");

                        //if (!string.Equals(currentStatus, "Draft", StringComparison.OrdinalIgnoreCase))
                        //    return JsonResponse(false, "Only requisitions in Draft status can be submitted.");

                        using (var upd = new SqlCommand(updateSql, conn))
                        {
                            upd.Parameters.AddWithValue("@Status", status ?? "Submitted");
                            upd.Parameters.AddWithValue("@RequisitionId", id);
                            await upd.ExecuteNonQueryAsync();
                        }

                        return JsonResponse(true, "Requisition submitted successfully", new { requisitionId = id, status = status });
                    }
                }
            }
            catch (Exception ex)
            {
                return JsonResponse(false, $"Error submitting requisition: {ex.Message}");
            }
        }





        [HttpGet]
        [Route("Editrequisitions/{id}")]
        public async Task<IHttpActionResult> Edit(long id)
        {
            var model = new PurchaseRequisitionDto();

            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    await con.OpenAsync();

                    // ----------------------------
                    // Load requisition header
                    // ----------------------------
                    using (var cmd = new SqlCommand("SELECT * FROM PurchaseRequisitions WHERE RequisitionId = @Id", con))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        using (var rdr = await cmd.ExecuteReaderAsync())
                        {
                            if (await rdr.ReadAsync())
                            {
                                model.RequisitionId = (long)rdr["RequisitionId"];
                                model.RequisitionNumber = rdr["RequisitionNumber"].ToString();
                                model.Purpose = rdr["Purpose"].ToString();
                                model.RequestedBy = rdr["RequestedBy"] != DBNull.Value
                                    ? (Guid)rdr["RequestedBy"]
                                    : Guid.Empty;

                                model.DepartmentId = rdr["DepartmentId"] != DBNull.Value
                                    ? Guid.Parse(rdr["DepartmentId"].ToString())
                                    : Guid.Empty;

                                model.RequestDate = (DateTime)rdr["RequestDate"];
                                model.Status = rdr["Status"].ToString();
                                model.TotalAmount = rdr["TotalAmount"] != DBNull.Value
                                    ? Convert.ToDecimal(rdr["TotalAmount"])
                                    : 0;
                            }
                            else
                            {
                                return Json(new
                                {
                                    success = false,
                                    message = "Requisition not found."
                                });
                            }
                        }
                    }

                    // ----------------------------
                    // Load line items
                    // ----------------------------
                    model.Lines = new List<PurchaseRequisitionLineDto>();
                    using (var cmd = new SqlCommand("SELECT * FROM PurchaseRequisitionLines WHERE RequisitionId = @Id", con))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        using (var rdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await rdr.ReadAsync())
                            {
                                model.Lines.Add(new PurchaseRequisitionLineDto
                                {
                                    LineNumber = Convert.ToInt32(rdr["LineNumber"]),
                                    ItemId = rdr["ItemId"] != DBNull.Value ? Guid.Parse(rdr["ItemId"].ToString()) : Guid.Empty,
                                    ItemDescription = rdr["ItemDescription"].ToString(),
                                    Quantity = Convert.ToDecimal(rdr["Quantity"]),
                                    UnitPrice = Convert.ToDecimal(rdr["UnitPrice"]),
                                    AccountCode = rdr["AccountCode"].ToString()
                                });
                            }
                        }
                    }
                }

                // ----------------------------
                // Return JSON response
                // ----------------------------
                return Json(new
                {
                    success = true,
                    message = "Requisition loaded successfully.",
                    data = model
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "An error occurred while loading the requisition.",
                    error = ex.Message
                });
            }
        }

    }
}

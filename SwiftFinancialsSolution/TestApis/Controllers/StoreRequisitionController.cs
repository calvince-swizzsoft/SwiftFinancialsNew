using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Web.Controllers.Api
{
    [RoutePrefix("api/storerequisition")]
    public class StoreRequisitionController : ApiController
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        private readonly string baseUrl = "https://localhost:44327/api/items";

        // GET: api/store-requisition
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetAll()
        {
            try
            {
                var requisitions = await GetAllRequisitions();
                return Ok(requisitions);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error retrieving requisitions.", ex));
            }
        }

        // GET: api/store-requisition/{id}
        [HttpGet]
        [Route("{id:guid}")]
        public async Task<IHttpActionResult> GetById(Guid id)
        {
            try
            {
                var requisitions = await GetAllRequisitions();
                var requisition = requisitions.FirstOrDefault(r => r.StoreRequisitionID == id);

                if (requisition == null)
                    return NotFound();

                return Ok(requisition);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception($"Error retrieving requisition ID {id}.", ex));
            }
        }

        // POST: api/store-requisition
        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> Create([FromBody] StoreRequisitionDTO requisition)
        {
            if (requisition == null)
                return BadRequest("Requisition data is required.");

            if (requisition.Lines == null || requisition.Lines.Count == 0)
                return BadRequest("At least one line item is required.");

            try
            {
                Guid requisitionId = Guid.NewGuid();

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            string insertRequisition = @"
                                INSERT INTO StoreRequisition 
                                (StoreRequisitionID, RequisitionNumber, RequesterID, DepartmentID, RequestDate, RequiredDate, Status, Remarks, CreatedBy)
                                VALUES (@StoreRequisitionID, @RequisitionNumber, @RequesterID, @DepartmentID, @RequestDate, @RequiredDate, @Status, @Remarks, @CreatedBy)";

                            SqlCommand cmd = new SqlCommand(insertRequisition, conn, transaction);
                            cmd.Parameters.AddWithValue("@StoreRequisitionID", requisitionId);
                            cmd.Parameters.AddWithValue("@RequisitionNumber", requisition.RequisitionNumber);
                            cmd.Parameters.AddWithValue("@RequesterID", requisition.RequesterID);
                            cmd.Parameters.AddWithValue("@DepartmentID", requisition.DepartmentID);
                            cmd.Parameters.AddWithValue("@RequestDate", requisition.RequestDate);
                            cmd.Parameters.AddWithValue("@RequiredDate", requisition.RequiredDate);
                            cmd.Parameters.AddWithValue("@Status", requisition.Status);
                            cmd.Parameters.AddWithValue("@Remarks", (object)requisition.Remarks ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@CreatedBy", requisition.CreatedBy);

                            await cmd.ExecuteNonQueryAsync();

                            foreach (var line in requisition.Lines)
                            {
                                string insertLine = @"
                                    INSERT INTO StoreRequisitionLines 
                                    (LineID, StoreRequisitionID, ItemID, ItemDescription, QuantityRequested, QuantityIssued, UnitPrice, Remarks)
                                    VALUES (@LineID, @StoreRequisitionID, @ItemID, @ItemDescription, @QuantityRequested, @QuantityIssued, @UnitPrice, @Remarks)";

                                SqlCommand lineCmd = new SqlCommand(insertLine, conn, transaction);
                                lineCmd.Parameters.AddWithValue("@LineID", Guid.NewGuid());
                                lineCmd.Parameters.AddWithValue("@StoreRequisitionID", requisitionId);
                                lineCmd.Parameters.AddWithValue("@ItemID", line.ItemID);
                                lineCmd.Parameters.AddWithValue("@ItemDescription", (object)line.ItemDescription ?? DBNull.Value);
                                lineCmd.Parameters.AddWithValue("@QuantityRequested", line.QuantityRequested);
                                lineCmd.Parameters.AddWithValue("@QuantityIssued", line.QuantityIssued);
                                lineCmd.Parameters.AddWithValue("@UnitPrice", (object)line.UnitPrice ?? DBNull.Value);
                                lineCmd.Parameters.AddWithValue("@Remarks", (object)line.Remarks ?? DBNull.Value);

                                await lineCmd.ExecuteNonQueryAsync();
                            }

                            transaction.Commit();
                            requisition.StoreRequisitionID = requisitionId;

                            return Created(new Uri(Request.RequestUri + "/" + requisitionId), requisition);
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error creating requisition.", ex));
            }
        }

        // PUT: api/store-requisition/{id}/approve?approvedBy=GUID
        [HttpPut]
        [Route("{id:guid}/approve")]
        public async Task<IHttpActionResult> Approve(Guid id, [FromUri] Guid approvedBy)
        {
            if (approvedBy == Guid.Empty)
                return BadRequest("Invalid approver ID.");

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Mark the requisition as approved
                        string query = @"
                    UPDATE StoreRequisition
                    SET Status = 'Approved',
                        ApprovedBy = @ApprovedBy,
                        ApprovedDate = GETDATE()
                    WHERE StoreRequisitionID = @RequisitionID";

                        SqlCommand cmd = new SqlCommand(query, conn, transaction);
                        cmd.Parameters.AddWithValue("@ApprovedBy", approvedBy);
                        cmd.Parameters.AddWithValue("@RequisitionID", id);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                            return NotFound();

                        // 2. Get requisition lines to update inventory
                        string linesQuery = @"
                    SELECT ItemID, QuantityRequested 
                    FROM StoreRequisitionLines
                    WHERE StoreRequisitionID = @RequisitionID";

                        SqlCommand linesCmd = new SqlCommand(linesQuery, conn, transaction);
                        linesCmd.Parameters.AddWithValue("@RequisitionID", id);

                        var lines = new List<(Guid ItemID, decimal QuantityRequested)>();
                        using (var reader = await linesCmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                lines.Add(((Guid)reader["ItemID"], (decimal)reader["QuantityRequested"]));
                            }
                        }

                        // 3. Update inventory via external API
                        using (var httpClient = new HttpClient())
                        {
                            httpClient.Timeout = TimeSpan.FromSeconds(30); // Optional but good practice

                            foreach (var line in lines)
                            {
                                var jsonContent = new
                                {
                                    ItemID = line.ItemID,
                                    InventoryBalance = line.QuantityRequested
                                };

                                // Manually serialize to JSON
                                var json = JsonConvert.SerializeObject(jsonContent);
                                var content = new StringContent(json, Encoding.UTF8, "application/json");

                                try
                                {
                                    var response = await httpClient.PutAsync($"{baseUrl}/{line.ItemID}/reduce", content);

                                    if (!response.IsSuccessStatusCode)
                                    {
                                        Console.WriteLine($" Failed to update inventory for ItemID {line.ItemID}. Status: {response.StatusCode}");
                                        // You can choose to continue or throw
                                        // continue;
                                        throw new HttpRequestException($"Failed to update inventory for ItemID {line.ItemID}: {response.ReasonPhrase}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"❌ Error updating ItemID {line.ItemID}: {ex.Message}");
                                    // Optionally rethrow if you want to stop the process
                                    throw new HttpRequestException($"Failed to update inventory for ItemID {line.ItemID}");
                                }
                            }
                        }
                        transaction.Commit();
                        return Ok(new { message = "Requisition approved and inventory updated successfully." });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return InternalServerError(new Exception("Error approving requisition and updating inventory.", ex));
                    }
                }
            }
        }



        [HttpPut]
        [Route("{id:guid}/updateStatus")]
        public async Task<IHttpActionResult> UpdateStatus(Guid id, [FromUri] string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return BadRequest("Status must be provided.");

            status = status.Trim();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Update requisition status first
                        string query = @"
                    UPDATE StoreRequisition
                    SET Status = @Status,
                        ApprovedDate = CASE WHEN @Status = 'Approved' THEN GETDATE() ELSE NULL END
                    WHERE StoreRequisitionID = @RequisitionID";

                        SqlCommand cmd = new SqlCommand(query, conn, transaction);
                        cmd.Parameters.AddWithValue("@Status", status);
                        cmd.Parameters.AddWithValue("@RequisitionID", id);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                            return NotFound();

                        // If status is Approved, continue with inventory update
                        if (status.Equals("Post", StringComparison.OrdinalIgnoreCase))
                        {
                            // Get requisition lines
                            string linesQuery = @"
                        SELECT ItemID, QuantityRequested 
                        FROM StoreRequisitionLines
                        WHERE StoreRequisitionID = @RequisitionID";

                            SqlCommand linesCmd = new SqlCommand(linesQuery, conn, transaction);
                            linesCmd.Parameters.AddWithValue("@RequisitionID", id);

                            var lines = new List<(Guid ItemID, decimal QuantityRequested)>();
                            using (var reader = await linesCmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    lines.Add(((Guid)reader["ItemID"], (decimal)reader["QuantityRequested"]));
                                }
                            }

                            // Update inventory via external API
                            using (var httpClient = new System.Net.Http.HttpClient())
                            {
                                foreach (var line in lines)
                                {
                                    var jsonContent = new
                                    {
                                        ItemID = line.ItemID,
                                        InventoryBalance = line.QuantityRequested
                                    };

                                    var response = await httpClient.PutAsJsonAsync($"{baseUrl}/{line.ItemID}/reduce", jsonContent);
                                    if (!response.IsSuccessStatusCode)
                                        throw new Exception($"Failed to update inventory for ItemID {line.ItemID}");
                                }
                            }
                        }

                        transaction.Commit();
                        return Ok(new { message = $"Requisition {status} successfully." });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return InternalServerError(new Exception("Error updating requisition status.", ex));
                    }
                }
            }
        }




        // GET: api/store-requisition/status?status=Pending
        [HttpGet]
        [Route("status")]
        public async Task<IHttpActionResult> GetByStatus([FromUri] string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return BadRequest("Status is required.");

            try
            {
                var requisitions = await GetAllRequisitions();
                var filtered = requisitions
                    .Where(r => r.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                return Ok(filtered);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception($"Error retrieving requisitions with status '{status}'.", ex));
            }
        }


        #region Helper Methods

        private async Task<List<StoreRequisitionDTO>> GetAllRequisitions()
        {
            var result = new List<StoreRequisitionDTO>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT r.StoreRequisitionID, r.RequisitionNumber, r.RequesterID, r.DepartmentID,
                   r.RequestDate, r.RequiredDate, r.Status, r.Remarks,
                   r.ApprovedBy, r.ApprovedDate, r.CreatedBy, r.CreatedDate,
                   r.ModifiedBy, r.ModifiedDate,
                   l.LineID, l.ItemID, l.ItemDescription, l.QuantityRequested,
                   l.QuantityIssued, l.UnitPrice, l.Remarks AS LineRemarks,
                   d.Description,
                   ind.Individual_LastName + ' ' + ind.Individual_LastName AS RequestedByName
            FROM StoreRequisition r
            LEFT JOIN StoreRequisitionLines l ON r.StoreRequisitionID = l.StoreRequisitionID
            INNER JOIN swiftFin_Departments d ON r.DepartmentID = d.Id
            INNER JOIN swiftFin_Employees e ON r.RequesterID = e.id
            INNER JOIN swiftFin_Customers ind ON e.CustomerID = ind.Id
            ORDER BY r.StoreRequisitionID DESC";

                SqlCommand cmd = new SqlCommand(query, conn);
                await conn.OpenAsync();
                SqlDataReader reader = await cmd.ExecuteReaderAsync();

                var requisitions = new Dictionary<Guid, StoreRequisitionDTO>();

                while (await reader.ReadAsync())
                {
                    Guid reqId = (Guid)reader["StoreRequisitionID"];

                    if (!requisitions.ContainsKey(reqId))
                    {
                        requisitions[reqId] = new StoreRequisitionDTO
                        {
                            StoreRequisitionID = reqId,
                            RequisitionNumber = reader["RequisitionNumber"].ToString(),
                            RequesterName = reader["RequestedByName"].ToString(), // New property for full name
                            DepartmentName = reader["Description"].ToString(), // New property for department
                            RequesterID = (Guid)reader["RequesterID"],
                            DepartmentID = (Guid)reader["DepartmentID"],
                            RequestDate = (DateTime)reader["RequestDate"],
                            RequiredDate = (DateTime)reader["RequiredDate"],
                            Status = reader["Status"].ToString(),
                            Remarks = reader["Remarks"].ToString(),
                            ApprovedBy = reader["ApprovedBy"] as Guid?,
                            ApprovedDate = reader["ApprovedDate"] as DateTime?,
                            CreatedBy = (Guid)reader["CreatedBy"],
                            CreatedDate = (DateTime)reader["CreatedDate"],
                            ModifiedBy = reader["ModifiedBy"] as Guid?,
                            ModifiedDate = reader["ModifiedDate"] as DateTime?,
                            Lines = new List<StoreRequisitionLineDTO>()
                        };
                    }

                    if (reader["LineID"] != DBNull.Value)
                    {
                        requisitions[reqId].Lines.Add(new StoreRequisitionLineDTO
                        {
                            LineID = (Guid)reader["LineID"],
                            StoreRequisitionID = reqId,
                            ItemID = (Guid)reader["ItemID"],
                            ItemDescription = reader["ItemDescription"].ToString(),
                            QuantityRequested = (decimal)reader["QuantityRequested"],
                            QuantityIssued = (decimal)reader["QuantityIssued"],
                            UnitPrice = reader["UnitPrice"] as decimal?,
                            Remarks = reader["LineRemarks"].ToString()
                        });
                    }
                }

                result = requisitions.Values.ToList();
            }

            return result;
        }

        #endregion
    }

    #region DTOs

    public class StoreRequisitionDTO
    {
        public Guid StoreRequisitionID { get; set; }
        public string RequisitionNumber { get; set; }
        public Guid RequesterID { get; set; }
        public Guid DepartmentID { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime RequiredDate { get; set; }
        public string Status { get; set; }
        public string Remarks { get; set; }
        public Guid? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string RequesterName { get; set; }
        public string DepartmentName { get; set; }
        public string FileBase64 { get; set; }


        public List<StoreRequisitionLineDTO> Lines { get; set; } = new List<StoreRequisitionLineDTO>();
    }

    public class StoreRequisitionLineDTO
    {
        public Guid LineID { get; set; }
        public Guid StoreRequisitionID { get; set; }
        public Guid ItemID { get; set; }
        public string ItemDescription { get; set; }
        public decimal QuantityRequested { get; set; }
        public decimal QuantityIssued { get; set; }
        public decimal? UnitPrice { get; set; }
        public string Remarks { get; set; }
    }

    #endregion
}

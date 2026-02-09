using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Http;
using TestApis.Models;

namespace TestApis.Controllers
{
    [RoutePrefix("api/Document")]
    public class DocumentController : ApiController
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        // Upload document
        [HttpPost]
        [Route("Upload")]
        public IHttpActionResult UploadDocument(DocumentModel model)
        {
            if (model == null) return BadRequest("Document data is required.");

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = @"INSERT INTO Documents
                                    (FileName, FileType, FileBase64, Uploadedby, UploadedByID, UploadedByRole, UploadedForID, VisibilityLevel, Status, Remarks, UploadedDate)
                                     VALUES
                                    (@FileName, @FileType, @FileBase64, @Uploadedby, @UploadedByID, @UploadedByRole, @UploadedForID, @VisibilityLevel, @Status, @Remarks, @UploadedDate)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@FileName", model.FileName);
                        cmd.Parameters.AddWithValue("@FileType", model.FileType);
                        cmd.Parameters.AddWithValue("@FileBase64", model.FileBase64);
                        cmd.Parameters.AddWithValue("@Uploadedby", model.Uploadedby ?? string.Empty);
                        cmd.Parameters.AddWithValue("@UploadedByID", model.UploadedByID);
                        cmd.Parameters.AddWithValue("@UploadedByRole", model.UploadedByRole);
                        cmd.Parameters.AddWithValue("@UploadedForID", (object)model.UploadedForID ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@VisibilityLevel", model.VisibilityLevel);
                        cmd.Parameters.AddWithValue("@Status", model.Status);
                        cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? string.Empty);
                        cmd.Parameters.AddWithValue("@UploadedDate", model.UploadedDate);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok("Document uploaded successfully.");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // Get all documents (with optional filtering)
        [HttpGet]
        [Route("GetAll")]
        public IHttpActionResult GetAllDocuments(string uploadedByRole = null, Guid? uploadedByID = null, string visibilityLevel = null)
        {
            List<DocumentModel> documents = new List<DocumentModel>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = @"SELECT * FROM Documents WHERE 1=1";

                    if (!string.IsNullOrEmpty(uploadedByRole)) query += " AND UploadedByRole=@UploadedByRole";
                    if (uploadedByID.HasValue) query += " AND UploadedByID=@UploadedByID";
                    if (!string.IsNullOrEmpty(visibilityLevel)) query += " AND VisibilityLevel=@VisibilityLevel";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        if (!string.IsNullOrEmpty(uploadedByRole)) cmd.Parameters.AddWithValue("@UploadedByRole", uploadedByRole);
                        if (uploadedByID.HasValue) cmd.Parameters.AddWithValue("@UploadedByID", uploadedByID.Value);
                        if (!string.IsNullOrEmpty(visibilityLevel)) cmd.Parameters.AddWithValue("@VisibilityLevel", visibilityLevel);

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                documents.Add(new DocumentModel
                                {
                                    DocumentID = Convert.ToInt32(reader["DocumentID"]),
                                    FileName = reader["FileName"].ToString(),
                                    FileType = reader["FileType"].ToString(),
                                    FileBase64 = reader["FileBase64"].ToString(),
                                    Uploadedby = reader["Uploadedby"].ToString(),
                                    UploadedByID = Guid.Parse(reader["UploadedByID"].ToString()),
                                    UploadedByRole = reader["UploadedByRole"].ToString(),
                                    UploadedForID = reader["UploadedForID"] != DBNull.Value ? Guid.Parse(reader["UploadedForID"].ToString()) : (Guid?)null,
                                    VisibilityLevel = reader["VisibilityLevel"].ToString(),
                                    Status = reader["Status"].ToString(),
                                    Remarks = reader["Remarks"].ToString(),
                                    UploadedDate = Convert.ToDateTime(reader["UploadedDate"])
                                });
                            }
                        }
                    }
                }

                return Ok(documents);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // Update document status or remarks
        [HttpPut]
        [Route("Update/{id}")]
        public IHttpActionResult UpdateDocument(int id, [FromBody] DocumentModel model)
        {
            if (model == null) return BadRequest("Document data is required.");

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = @"UPDATE Documents 
                                     SET Status=@Status, Remarks=@Remarks, Uploadedby=@Uploadedby
                                     WHERE DocumentID=@DocumentID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Status", model.Status ?? string.Empty);
                        cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? string.Empty);
                        cmd.Parameters.AddWithValue("@Uploadedby", model.Uploadedby ?? string.Empty);
                        cmd.Parameters.AddWithValue("@DocumentID", id);

                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();
                        if (rows == 0) return NotFound();
                    }
                }

                return Ok("Document updated successfully.");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // Delete document
        [HttpDelete]
        [Route("Delete/{id}")]
        public IHttpActionResult DeleteDocument(int id)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = "DELETE FROM Documents WHERE DocumentID=@DocumentID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@DocumentID", id);
                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();
                        if (rows == 0) return NotFound();
                    }
                }

                return Ok("Document deleted successfully.");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}

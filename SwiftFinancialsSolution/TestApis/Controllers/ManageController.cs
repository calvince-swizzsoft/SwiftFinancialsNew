using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web.Http;

namespace SwiftFinancials.API.Controllers
{
    [Authorize]
    [RoutePrefix("api/manage")]
    public class ManageController : ApiController
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFinancialsDB_Live"].ConnectionString;

        // GET api/manage/user/{id}
        [HttpGet]
        [Route("user/{id}")]
        public async Task<IHttpActionResult> GetUserById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("User ID required.");

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("SELECT Id, UserName, Email, PhoneNumber, LastPasswordChangedDate FROM AspNetUsers WHERE Id = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (!reader.HasRows)
                        return NotFound();

                    await reader.ReadAsync();
                    var user = new
                    {
                        Id = reader["Id"],
                        UserName = reader["UserName"],
                        Email = reader["Email"],
                        PhoneNumber = reader["PhoneNumber"],
                        LastPasswordChangedDate = reader["LastPasswordChangedDate"]
                    };
                    return Ok(user);
                }
            }
        }

        // PUT api/manage/change-password
        [HttpPut]
        [Route("change-password")]
        public async Task<IHttpActionResult> ChangePassword([FromBody] ChangePasswordRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.UserId) || string.IsNullOrWhiteSpace(model.NewPassword))
                return BadRequest("Invalid data.");

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("UPDATE AspNetUsers SET PasswordHash = @Password, LastPasswordChangedDate = @Date WHERE Id = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Password", HashPassword(model.NewPassword));
                cmd.Parameters.AddWithValue("@Date", DateTime.Now);
                cmd.Parameters.AddWithValue("@Id", model.UserId);

                await conn.OpenAsync();
                int affected = await cmd.ExecuteNonQueryAsync();

                if (affected > 0)
                    return Ok(new { message = "Password changed successfully." });

                return BadRequest("Failed to update password.");
            }
        }

        // PUT api/manage/add-phone
        [HttpPut]
        [Route("add-phone")]
        public async Task<IHttpActionResult> AddPhoneNumber([FromBody] AddPhoneRequest model)
        {
            if (model == null || string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.PhoneNumber))
                return BadRequest("Missing required fields.");

            string verificationCode = GenerateCode();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("UPDATE AspNetUsers SET PhoneNumber = @Phone WHERE Id = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Phone", model.PhoneNumber);
                cmd.Parameters.AddWithValue("@Id", model.UserId);

                await conn.OpenAsync();
                int affected = await cmd.ExecuteNonQueryAsync();

                if (affected > 0)
                {
                    // Optionally send verification SMS (placeholder)
                    return Ok(new
                    {
                        message = "Phone number added successfully.",
                        verificationCode = verificationCode // TODO: Send via SMS gateway
                    });
                }

                return BadRequest("Failed to add phone number.");
            }
        }

        // POST api/manage/verify-phone
        [HttpPost]
        [Route("verify-phone")]
        public async Task<IHttpActionResult> VerifyPhone([FromBody] VerifyPhoneRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.UserId) || string.IsNullOrWhiteSpace(model.Code))
                return BadRequest("Invalid request.");

            // Here you'd normally check code validity (from DB or cache)
            bool isValidCode = model.Code == "123456"; // placeholder

            if (!isValidCode)
                return BadRequest("Invalid verification code.");

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("UPDATE AspNetUsers SET PhoneNumberConfirmed = 1 WHERE Id = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", model.UserId);
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }

            return Ok(new { message = "Phone number verified successfully." });
        }

        // Helper Methods
        private string HashPassword(string password)
        {
            // Use a secure password hashing algorithm instead of plain text.
            return password /*BCrypt.Net.BCrypt.HashPassword(password)*/;
        }

        private string GenerateCode()
        {
            return new Random().Next(100000, 999999).ToString();
        }
    }

    // DTOs for requests
    public class ChangePasswordRequest
    {
        public string UserId { get; set; }
        public string NewPassword { get; set; }
    }

    public class AddPhoneRequest
    {
        public string UserId { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class VerifyPhoneRequest
    {
        public string UserId { get; set; }
        public string Code { get; set; }
    }
}

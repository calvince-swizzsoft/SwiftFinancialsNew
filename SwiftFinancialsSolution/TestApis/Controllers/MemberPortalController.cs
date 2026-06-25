using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Extensions;
using Infrastructure.Crosscutting.Framework.Utils;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Newtonsoft.Json;
using SwiftFinancials.Presentation.Infrastructure.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using TestApis.Helpers;
using TestApis.Models;
using TestApis.Services;
using static TestApis.Controllers.ValuesController;

namespace TestApis.Controllers
{
 
    [RoutePrefix("api/MemberPortal")]
    [AllowAnonymous]
    
    public class MemberPortalController : ApiController
    {
        private readonly MasterController master;

        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        public MemberPortalController()
        {
            master = new MasterController();
        }

        #region DTO
        private readonly string _cs = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        private readonly CustomerStatementService _statementService = new CustomerStatementService();
        private readonly CustomerService _customerService = new CustomerService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }
       
        public class CustomerStatementRequestDto
        {
            public string IdentityCardNo { get; set; }

            public DateTime startDate { get; set; }

            public DateTime endDate { get; set; }

            public bool IncludeProductBreakdown { get; set; } = true;
        }
        public class Registration
        {
            public string idNumber { get; set; }
        }

        // Request Model
        public class ChangePinRequest
        {
            public string identifier { get; set; } 
            public string currentPin { get; set; }   
            public string newPin { get; set; }       
        }

        public class Login
        {
            public string identifier { get; set; }
            public string pin { get; set; }
        }

        public class LoginRequest
        {
            public string identifier { get; set; }  
            public string pin { get; set; }          
        }

        public class ConfirmOtpRequest
        {
            public string memberNo { get; set; }
            public string otp { get; set; }
        }

        public class OtpRequest
        {
            public string identifier { get; set; } // Payroll Number or ID Number
        }

        public class UserDetails
        {
            public string FullNames { get; set; }
            public string IDNumber { get; set; }
            public string PhoneNumber { get; set; }
            public string EmailAddress { get; set; }
        }

        public class ForgotPINRequest
        {
            public string idNumber { get; set; }
        }


        public class VerifyResetOtpRequest
        {
            public string idNumber { get; set; }
            public string otp { get; set; }
        }

        public class ResetChangePinRequest
        {
            public string idNumber { get; set; }
            public string currentPin { get; set; } // system-generated PIN from SMS
            public string newPin { get; set; } // member's preferred PIN
        }

        public class CustomerAccountDto
        {
            public Guid CustomerId { get; set; }
            public string Reference2 { get; set; }
            public string FullName { get; set; }
            public Guid CustomerAccountId { get; set; }
            public int ProductCode { get; set; }
            public string AccountType { get; set; }
            public string ProductDescription { get; set; }
            public string AccountStatus { get; set; }
            public decimal AccountBalance { get; set; }
            public DateTime? LastTransactionDate { get; set; }
        }
        #endregion

        [HttpPost]
        [Route("Register")]
        public async Task<IHttpActionResult> Register([FromBody] Registration registration)
        {
            using (var conn = new SqlConnection(_cs))
            {
                conn.Open();

                // 1. Fetch customer using ID Number
                var cmdCustomer = new SqlCommand(@"
            SELECT TOP 1
                Reference2,
                Individual_FirstName,
                Individual_LastName,
                Individual_IdentityCardNumber,
                Individual_BirthDate,
                Individual_Gender,
                Address_MobileLine,
                Address_Email,
                SequentialId
            FROM swiftFin_Customers
            WHERE Individual_IdentityCardNumber = @idNumber", conn);
                cmdCustomer.Parameters.AddWithValue("@idNumber", registration.idNumber);

                using (var rd = cmdCustomer.ExecuteReader())
                {
                    if (!rd.Read())
                        return Json(new { success = false, message = "Invalid ID number" });

                    // ✅ Read ALL values while reader is still open
                    var memberNo = rd["Reference2"].ToString();
                    var idNumber = rd["Individual_IdentityCardNumber"].ToString();
                    var firstName = rd["Individual_FirstName"].ToString();
                    var lastName = rd["Individual_LastName"].ToString();
                    var phoneNumber = rd["Address_MobileLine"].ToString();
                    var email = rd["Address_Email"].ToString();
                    var gender = rd["Individual_Gender"].ToString();
                    var dob = rd["Individual_BirthDate"] == DBNull.Value
                                        ? (object)DBNull.Value
                                        : rd["Individual_BirthDate"];
                    var fullNames = firstName + " " + lastName;

                    // ✅ Close reader BEFORE running any more SQL commands
                    rd.Close();

                    // 2. ✅ Block registration if no phone number
                    if (string.IsNullOrWhiteSpace(phoneNumber) ||
                        phoneNumber == "0" ||
                        phoneNumber == "N/A" ||
                        phoneNumber.Length < 10)
                        return Json(new
                        {
                            success = false,
                            message = "Your account has no registered phone number. Please contact Rubani Sacco to update your details."
                        });

                    // 3. Check if already registered
                    var checkCmd = new SqlCommand(
                        "SELECT COUNT(1) FROM Registration WHERE IDNumber = @id", conn);
                    checkCmd.Parameters.AddWithValue("@id", idNumber);
                    var existingCount = (int)checkCmd.ExecuteScalar();
                    bool isReRegistration = existingCount > 0;

                    // 4. Generate new PIN
                    var pin = new Random().Next(1000, 9999).ToString();
                    PinSecurity.Create(pin, out var hash, out var salt);

                    // 5. Send SMS BEFORE saving — abort if SMS fails
                    string smsMessage = isReRegistration
                        ? $"Dear {fullNames}, your Alternative Channels account has been re-registered. " +
                          $"Your new PIN is: {pin}. Do not share this PIN with anyone."
                        : $"Dear {fullNames}, thank you for registering for Alternative Channels. " +
                          $"Your new PIN is: {pin}. You can change it to your preferred one. " +
                          "Do not share this PIN with anyone.";

                    try
                    {
                        await SmsHelper.SendPin(pin, smsMessage, phoneNumber);
                    }
                    catch (Exception)
                    {
                        return Json(new { success = false, message = "Registration failed. Could not send PIN via SMS. Please try again later." });
                    }

                    if (isReRegistration)
                    {
                        // ✅ Overwrite existing registration
                        var updateCmd = new SqlCommand(@"
                    UPDATE Registration SET
                        FullNames    = @names,
                        PhoneNumber  = @phone,
                        EmailAddress = @email,
                        DateOfBirth  = @dob,
                        Gender       = @gender,
                        MemberNo     = @memberNo,
                        PIN          = @pin,
                        IMSI         = @salt,
                        FirstLogin   = 1,
                        Approved     = 1,
                        Status       = 'Active',
                        Trials       = 0,
                        CreatedAt    = GETUTCDATE(),
                        CreatedBy    = 'SYSTEM'
                    WHERE IDNumber = @id", conn);
                        updateCmd.Parameters.AddWithValue("@names", fullNames);
                        updateCmd.Parameters.AddWithValue("@phone", phoneNumber);
                        updateCmd.Parameters.AddWithValue("@email", email);
                        updateCmd.Parameters.AddWithValue("@dob", dob);
                        updateCmd.Parameters.AddWithValue("@gender", gender);
                        updateCmd.Parameters.AddWithValue("@memberNo", memberNo);
                        updateCmd.Parameters.AddWithValue("@pin", Convert.ToBase64String(hash));
                        updateCmd.Parameters.AddWithValue("@salt", Convert.ToBase64String(salt));
                        updateCmd.Parameters.AddWithValue("@id", idNumber); // ✅ included
                        updateCmd.ExecuteNonQuery();

                        return Json(new
                        {
                            success = true,
                            memberNo,
                            message = "Re-registration successful. Your new PIN has been sent to your mobile number."
                        });
                    }
                    else
                    {
                        // ✅ Fresh registration
                        var insertCmd = new SqlCommand(@"
                    INSERT INTO Registration
                    (
                        FullNames, PhoneNumber, EmailAddress, IDNumber,
                        DateOfBirth, Gender, MemberNo,
                        PIN, IMSI, FirstLogin, Approved, Status,
                        Trials, CreatedAt, CreatedBy
                    )
                    VALUES
                    (
                        @names, @phone, @email, @id,
                        @dob, @gender, @memberNo,
                        @pin, @salt, 1, 1, 'Active',
                        0, GETUTCDATE(), 'SYSTEM'
                    )", conn);
                        insertCmd.Parameters.AddWithValue("@names", fullNames);
                        insertCmd.Parameters.AddWithValue("@phone", phoneNumber);
                        insertCmd.Parameters.AddWithValue("@email", email);
                        insertCmd.Parameters.AddWithValue("@id", idNumber); // ✅ was missing
                        insertCmd.Parameters.AddWithValue("@dob", dob);
                        insertCmd.Parameters.AddWithValue("@gender", gender);
                        insertCmd.Parameters.AddWithValue("@memberNo", memberNo);
                        insertCmd.Parameters.AddWithValue("@pin", Convert.ToBase64String(hash));
                        insertCmd.Parameters.AddWithValue("@salt", Convert.ToBase64String(salt));
                        insertCmd.ExecuteNonQuery();

                        return Json(new
                        {
                            success = true,
                            memberNo,
                            message = "Registration successful. Your PIN has been sent to your mobile number."
                        });
                    }
                }
            }
        }
        [HttpPost]
        [Route("ChangePin")]
        public async Task<IHttpActionResult> ChangePin([FromBody] ChangePinRequest request)
        {
            string ipAddress = GetClientIp();
            string userAgent = Request.Headers.UserAgent?.ToString() ?? "Unknown";

            using (var conn = new SqlConnection(_cs))
            {
                conn.Open();

                // 1. Validate user exists and is active
                var validateCmd = new SqlCommand(@"
            SELECT PIN, IMSI, Trials, MemberNo, IDNumber, PhoneNumber, FullNames
            FROM Registration
            WHERE (MemberNo = @identifier OR IDNumber = @identifier)
              AND Status = 'Active'", conn);
                validateCmd.Parameters.AddWithValue("@identifier", request.identifier);

                using (var rd = validateCmd.ExecuteReader())
                {
                    if (!rd.Read())
                        return Json(new { success = false, message = "User not found or inactive" });

                    var memberNo = rd["MemberNo"].ToString();
                    var trials = (int)rd["Trials"];
                    var phoneNumber = rd["PhoneNumber"].ToString();
                    var fullNames = rd["FullNames"].ToString();

                    if (trials >= 5)
                        return Json(new { success = false, message = "Account locked. Please contact support." });

                    var currentHash = Convert.FromBase64String(rd["PIN"].ToString());
                    var currentSalt = Convert.FromBase64String(rd["IMSI"].ToString());

                    // 2. Verify current PIN
                    if (!PinSecurity.Verify(request.currentPin, currentHash, currentSalt))
                    {
                        rd.Close();
                        IncrementTrials(conn, memberNo);
                        return Json(new { success = false, message = "Invalid current PIN" });
                    }

                    // 3. Validate new PIN requirements
                    var pinValidation = ValidateNewPin(request.newPin);
                    if (!pinValidation.IsValid)
                    {
                        rd.Close();
                        return Json(new { success = false, message = pinValidation.Message });
                    }

                    // 4. Generate new PIN hash and salt
                    PinSecurity.Create(request.newPin, out var newHash, out var newSalt);

                    // 5. Update PIN in database and reset trials
                    var updateCmd = new SqlCommand(@"
                UPDATE Registration 
                SET PIN = @newPin,
                    IMSI = @newSalt,
                    Trials = 0,
                    LastPinChangeDate = GETUTCDATE(),
                    LastPinChangeIP = @ip,
                    LastPinChangeUserAgent = @userAgent
                WHERE MemberNo = @memberNo", conn);

                    updateCmd.Parameters.AddWithValue("@newPin", Convert.ToBase64String(newHash));
                    updateCmd.Parameters.AddWithValue("@newSalt", Convert.ToBase64String(newSalt));
                    updateCmd.Parameters.AddWithValue("@memberNo", memberNo);
                    updateCmd.Parameters.AddWithValue("@ip", ipAddress);
                    updateCmd.Parameters.AddWithValue("@userAgent", userAgent);
                    updateCmd.ExecuteNonQuery();

                    // 6. Send confirmation SMS
                    string message =
                        $"Dear {fullNames}, your PIN has been updated successfully. " +
                        $"You can now log in to the Web Portal and Mobile App using your new preferred PIN. " +
                        $"Do not share your PIN with anyone.";

                    await SmsHelper.SendPin(request.newPin, message, phoneNumber);

                    rd.Close();

                    return Json(new
                    {
                        success = true,
                        message = "PIN changed successfully",
                        memberNo
                    });
                }
            }
        }

        // Helper method to validate new PIN
        private PinValidationResult ValidateNewPin(string newPin)
        {
            if (string.IsNullOrWhiteSpace(newPin))
                return new PinValidationResult(false, "PIN cannot be empty");

            if (newPin.Length < 6)
                return new PinValidationResult(false, "PIN must be at least 6 characters long");

            if (newPin.Length > 20)
                return new PinValidationResult(false, "PIN cannot exceed 20 characters");

            // Check for at least:
            // - 1 uppercase letter
            // - 1 lowercase letter  
            // - 1 number
            // - 1 special character (optional - you can adjust requirements)

            bool hasUpper = newPin.Any(char.IsUpper);
            bool hasLower = newPin.Any(char.IsLower);
            bool hasDigit = newPin.Any(char.IsDigit);
            bool hasSpecial = newPin.Any(ch => !char.IsLetterOrDigit(ch));

            if (!hasUpper)
                return new PinValidationResult(false, "PIN must contain at least one uppercase letter");

            if (!hasLower)
                return new PinValidationResult(false, "PIN must contain at least one lowercase letter");

            if (!hasDigit)
                return new PinValidationResult(false, "PIN must contain at least one number");

            // Optional: Require special characters
            // if (!hasSpecial)
            //     return new PinValidationResult(false, "PIN must contain at least one special character (!@#$%^&*)");

            // Check for common patterns (optional)
            string[] commonPatterns = { "123456", "password", "qwerty", "abc123", "111111", "000000" };
            if (commonPatterns.Any(pattern => newPin.ToLower().Contains(pattern)))
                return new PinValidationResult(false, "PIN contains common pattern. Please choose a stronger PIN");

            // Check for sequential numbers (optional)
            if (IsSequentialNumbers(newPin))
                return new PinValidationResult(false, "PIN contains sequential numbers. Please choose a stronger PIN");

            return new PinValidationResult(true, "PIN is valid");
        }

        private bool IsSequentialNumbers(string pin)
        {
            // Check for sequences like 123, 234, 345 etc.
            for (int i = 0; i < pin.Length - 2; i++)
            {
                if (char.IsDigit(pin[i]) && char.IsDigit(pin[i + 1]) && char.IsDigit(pin[i + 2]))
                {
                    int num1 = int.Parse(pin[i].ToString());
                    int num2 = int.Parse(pin[i + 1].ToString());
                    int num3 = int.Parse(pin[i + 2].ToString());

                    if (num2 == num1 + 1 && num3 == num2 + 1)
                        return true;
                }
            }
            return false;
        }

        // PIN Validation Result class
        public class PinValidationResult
        {
            public bool IsValid { get; set; }
            public string Message { get; set; }

            public PinValidationResult(bool isValid, string message)
            {
                IsValid = isValid;
                Message = message;
            }
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IHttpActionResult> login([FromBody] LoginRequest request)
        {
            string ipAddress = GetClientIp();
            string userAgent = Request.Headers.UserAgent?.ToString() ?? "Unknown";
            using (var conn = new SqlConnection(_cs))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
    SELECT r.PIN, r.IMSI, r.Trials, r.MemberNo, r.IDNumber, r.PhoneNumber, r.FullNames
    FROM Registration r
    INNER JOIN swiftFin_Customers c ON r.IDNumber = c.Individual_IdentityCardNumber
    WHERE (c.Individual_PayrollNumbers = @identifier OR c.Individual_IdentityCardNumber = @identifier)
      AND r.Status = 'Active'", conn);
                cmd.Parameters.AddWithValue("@identifier", request.identifier);

                using (var rd = cmd.ExecuteReader())
                {
                    // ✅ Specific message: identifier not found
                    if (!rd.Read())
                        return Json(new { success = false, message = "Invalid username. Please check your Staff Number or ID Number." });

                    var trials = (int)rd["Trials"];
                    if (trials >= 5)
                        return Json(new { success = false, message = "Account locked. Please contact support." });

                    var hash = Convert.FromBase64String(rd["PIN"].ToString());
                    var salt = Convert.FromBase64String(rd["IMSI"].ToString());

                    // ✅ Save MemberNo before closing reader (fixes the bug)
                    var memberNo = rd["MemberNo"].ToString();
                    var phoneNumber = rd["PhoneNumber"].ToString();
                    var fullNames = rd["FullNames"].ToString();

                    // ✅ Specific message: PIN is wrong
                    if (!PinSecurity.Verify(request.pin, hash, salt))
                    {
                        rd.Close();
                        IncrementTrials(conn, memberNo);

                        var newTrials = trials + 1;

                        if (newTrials >= 5)
                        {
                            return Json(new
                            {
                                success = false,
                                message = "Invalid PIN. Your account has now been locked. Please contact support."
                            });
                        }

                        return Json(new
                        {
                            success = false,
                            message = $"Invalid PIN. You have {5 - newTrials} attempt(s) remaining before your account is locked."
                        });
                    }

                    // PIN verified - now generate and send OTP
                    var otp = new Random().Next(1000, 9999).ToString();
                    PinSecurity.Create(otp, out var otpHash, out var otpSalt);
                    rd.Close();

                    var storeOtpCmd = new SqlCommand(@"
        UPDATE Registration 
        SET LoginOTP = @otp,
            LoginOTPSalt = @salt,
            LoginOTPExpiry = DATEADD(MINUTE, 5, GETUTCDATE()),
            LoginOTPAttempts = 0,
            PendingLoginIdentifier = @identifier,
            PendingLoginIP = @ip,
            PendingLoginUserAgent = @userAgent
        WHERE MemberNo = @memberNo", conn);
                    storeOtpCmd.Parameters.AddWithValue("@otp", Convert.ToBase64String(otpHash));
                    storeOtpCmd.Parameters.AddWithValue("@salt", Convert.ToBase64String(otpSalt));
                    storeOtpCmd.Parameters.AddWithValue("@memberNo", memberNo);
                    storeOtpCmd.Parameters.AddWithValue("@identifier", request.identifier);
                    storeOtpCmd.Parameters.AddWithValue("@ip", ipAddress);
                    storeOtpCmd.Parameters.AddWithValue("@userAgent", userAgent);
                    storeOtpCmd.ExecuteNonQuery();

                    string message = $"Dear {fullNames}, your login OTP is: {otp}. Valid for 5 minutes. Do not share this code.";
                    await SmsHelper.SendPin(otp, message, phoneNumber);

                    return Json(new
                    {
                        success = true,
                        message = "PIN verified. OTP sent to your registered mobile number",
                        memberNo,
                        requiresOtp = true,
                        otpExpiryMinutes = 5
                    });
                }
            }
        }

        [HttpPost]
        [Route("SendOtp")]
        public async Task<IHttpActionResult> SendOtp([FromBody] OtpRequest request)
        {
            using (var conn = new SqlConnection(_cs))
            {
                conn.Open();

                // Query to find user by identifier (Payroll Numbers or ID Number)
                var cmd = new SqlCommand(@"
            SELECT r.PIN, r.IMSI, r.Trials, r.MemberNo, r.IDNumber, r.PhoneNumber, r.FullNames
            FROM Registration r
            INNER JOIN swiftFin_Customers c ON r.IDNumber = c.Individual_IdentityCardNumber
            WHERE (c.Individual_PayrollNumbers = @identifier OR c.Individual_IdentityCardNumber = @identifier)
              AND r.Status = 'Active'", conn);

                cmd.Parameters.AddWithValue("@identifier", request.identifier);

                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read())
                        return Json(new { success = false, message = "User not found or inactive" });

                    var memberNo = rd["MemberNo"].ToString();
                    var idNumber = rd["IDNumber"].ToString();
                    var phoneNumber = rd["PhoneNumber"].ToString();
                    var fullNames = rd["FullNames"].ToString();
                    var trials = (int)rd["Trials"];

                    if (trials >= 5)
                        return Json(new { success = false, message = "Account locked. Please contact support." });

                    // Generate 4-digit OTP
                    var otp = new Random().Next(1000, 9999).ToString();

                    // Hash OTP for storage
                    PinSecurity.Create(otp, out var hash, out var salt);

                    rd.Close();

                    // Store OTP in database with expiration time (5 minutes)
                    var storeOtpCmd = new SqlCommand(@"
                UPDATE Registration 
                SET LoginOTP = @otp,
                    LoginOTPSalt = @salt,
                    LoginOTPExpiry = DATEADD(MINUTE, 5, GETUTCDATE()),
                    LoginOTPAttempts = 0
                WHERE MemberNo = @memberNo", conn);

                    storeOtpCmd.Parameters.AddWithValue("@otp", Convert.ToBase64String(hash));
                    storeOtpCmd.Parameters.AddWithValue("@salt", Convert.ToBase64String(salt));
                    storeOtpCmd.Parameters.AddWithValue("@memberNo", memberNo);
                    storeOtpCmd.ExecuteNonQuery();

                    // Send OTP via SMS
                    string message = $"Dear {fullNames}, your login OTP is: {otp}. This OTP is valid for 5 minutes. Do not share this code with anyone.";

                    // Send SMS
                    await SmsHelper.SendPin(otp, message, phoneNumber);

                    // Optional: Log OTP request
                    LogOtpRequest(conn, memberNo, request.identifier, ipAddress: GetClientIp());

                    return Json(new
                    {
                        success = true,
                        message = "OTP sent successfully to your registered mobile number",
                        memberNo,
                        otpExpiryMinutes = 5
                    });
                }
            }
        }

        [HttpPost]
        [Route("ConfirmOtp")]
        public IHttpActionResult ConfirmOtp([FromBody] ConfirmOtpRequest request)
        {
            string ipAddress = GetClientIp();
            string userAgent = Request.Headers.UserAgent?.ToString() ?? "Unknown";

            using (var conn = new SqlConnection(_cs))
            {
                conn.Open();

                // Get user and OTP details — include FullNames, IDNumber, PhoneNumber directly
                var cmd = new SqlCommand(@"
            SELECT MemberNo, PIN, IMSI, Trials, FullNames, IDNumber, PhoneNumber,
                   LoginOTP, LoginOTPSalt, LoginOTPExpiry, LoginOTPAttempts
            FROM Registration
            WHERE MemberNo = @memberNo
              AND Status = 'Active'", conn);

                cmd.Parameters.AddWithValue("@memberNo", request.memberNo);

                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read())
                        return Json(new { success = false, message = "User not found or inactive" });

                    var memberNo = rd["MemberNo"].ToString();
                    var trials = (int)rd["Trials"];
                    var fullNames = rd["FullNames"].ToString();
                    var idNumber = rd["IDNumber"].ToString();
                    var phoneNumber = rd["PhoneNumber"].ToString();

                    if (trials >= 5)
                        return Json(new { success = false, message = "Account locked. Please contact support." });

                    // Check if OTP exists
                    if (rd["LoginOTP"] == DBNull.Value || rd["LoginOTPSalt"] == DBNull.Value)
                        return Json(new { success = false, message = "No OTP request found. Please request OTP first." });

                    var otpHash = Convert.FromBase64String(rd["LoginOTP"].ToString());
                    var otpSalt = Convert.FromBase64String(rd["LoginOTPSalt"].ToString());
                    var otpExpiry = (DateTime)rd["LoginOTPExpiry"];
                    var otpAttempts = (int)rd["LoginOTPAttempts"];

                    // Check OTP expiry
                    if (DateTime.UtcNow > otpExpiry)
                    {
                        rd.Close();
                        ClearOtp(conn, memberNo);
                        return Json(new { success = false, message = "OTP has expired. Please request a new OTP." });
                    }

                    // Check OTP attempts (max 3 attempts)
                    if (otpAttempts >= 3)
                    {
                        rd.Close();
                        ClearOtp(conn, memberNo);
                        return Json(new { success = false, message = "Too many invalid OTP attempts. Please request a new OTP." });
                    }

                    // Verify OTP
                    if (!PinSecurity.Verify(request.otp, otpHash, otpSalt))
                    {
                        rd.Close();
                        IncrementOtpAttempts(conn, memberNo);
                        return Json(new { success = false, message = "Invalid OTP" });
                    }

                    rd.Close();

                    // OTP verified — complete login
                    UpdateLoginTracking(conn, memberNo, ipAddress, userAgent);
                    ClearOtp(conn, memberNo);
                    LogSuccessfulLogin(conn, memberNo, ipAddress, userAgent);

                    // Return same user details captured from SendOtp fields
                    return Json(new
                    {
                        success = true,
                        message = "Login successful",
                        memberNo,
                        fullNames,
                        idNumber,
                        phoneNumber
                    });
                }
            }
        }


        private void ClearOtp(SqlConnection conn, string memberNo)
        {
            var cmd = new SqlCommand(@"
        UPDATE Registration 
        SET LoginOTP = NULL,
            LoginOTPSalt = NULL,
            LoginOTPExpiry = NULL,
            LoginOTPAttempts = 0
        WHERE MemberNo = @memberNo", conn);

            cmd.Parameters.AddWithValue("@memberNo", memberNo);
            cmd.ExecuteNonQuery();
        }

        private void IncrementOtpAttempts(SqlConnection conn, string memberNo)
        {
            var cmd = new SqlCommand(@"
        UPDATE Registration 
        SET LoginOTPAttempts = ISNULL(LoginOTPAttempts, 0) + 1
        WHERE MemberNo = @memberNo", conn);

            cmd.Parameters.AddWithValue("@memberNo", memberNo);
            cmd.ExecuteNonQuery();
        }

        private void LogOtpRequest(SqlConnection conn, string memberNo, string identifier, string ipAddress)
        {
            var cmd = new SqlCommand(@"
        INSERT INTO OtpLog (MemberNo, Identifier, RequestIP, RequestDate)
        VALUES (@memberNo, @identifier, @ip, GETUTCDATE())", conn);

            cmd.Parameters.AddWithValue("@memberNo", memberNo);
            cmd.Parameters.AddWithValue("@identifier", identifier);
            cmd.Parameters.AddWithValue("@ip", ipAddress);
            cmd.ExecuteNonQuery();
        }

        private void LogSuccessfulLogin(SqlConnection conn, string memberNo, string ipAddress, string userAgent)
        {
            var cmd = new SqlCommand(@"
        UPDATE Registration 
        SET LoginCount       = ISNULL(LoginCount, 0) + 1,
            LastLoginAt      = GETUTCDATE(),
            LastLoginIP      = @ip,
            LastUserAgent    = @userAgent
        WHERE MemberNo = @memberNo", conn);

            cmd.Parameters.AddWithValue("@memberNo", memberNo);
            cmd.Parameters.AddWithValue("@ip", ipAddress);
            cmd.Parameters.AddWithValue("@userAgent", userAgent);
            cmd.ExecuteNonQuery();
        }

        private UserDetails GetUserDetails(SqlConnection conn, string memberNo)
        {
            var cmd = new SqlCommand(@"
        SELECT FullNames, IDNumber, PhoneNumber, EmailAddress
        FROM Registration
        WHERE MemberNo = @memberNo", conn);

            cmd.Parameters.AddWithValue("@memberNo", memberNo);

            using (var rd = cmd.ExecuteReader())
            {
                if (rd.Read())
                {
                    return new UserDetails
                    {
                        FullNames = rd["FullNames"].ToString(),
                        IDNumber = rd["IDNumber"].ToString(),
                        PhoneNumber = rd["PhoneNumber"].ToString(),
                        EmailAddress = rd["EmailAddress"].ToString()
                    };
                }
            }

            return null;
        }

        [HttpPost]
        [Route("ForgotPIN")]
        public async Task<IHttpActionResult> ForgotPIN([FromBody] ForgotPINRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.idNumber))
                return Json(new { success = false, message = "ID Number is required" });

            using (var conn = new SqlConnection(_cs))
            {
                conn.Open();

                // 1. Look up member by ID Number
                var cmd = new SqlCommand(@"
            SELECT MemberNo, FullNames, PhoneNumber, Status
            FROM Registration
            WHERE IDNumber = @idNumber", conn);

                cmd.Parameters.AddWithValue("@idNumber", request.idNumber);

                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read())
                        return Json(new { success = false, message = "ID Number not found" });

                    var memberNo = rd["MemberNo"].ToString();
                    var fullNames = rd["FullNames"].ToString();
                    var phone = rd["PhoneNumber"].ToString();
                    var status = rd["Status"].ToString();

                    rd.Close();

                    // 2. Block locked/inactive accounts
                    if (status != "Active")
                        return Json(new { success = false, message = "Account is not active. Please contact support." });

                    // 3. Generate OTP
                    var otp = new Random().Next(1000, 9999).ToString();

                    // Hash OTP for storage
                    PinSecurity.Create(otp, out var otpHash, out var otpSalt);

                    // 4. Store OTP in database with expiration (5 minutes)
                    var storeOtpCmd = new SqlCommand(@"
                UPDATE Registration 
                SET ResetOTP = @otp,
                    ResetOTPSalt = @salt,
                    ResetOTPExpiry = DATEADD(MINUTE, 5, GETUTCDATE()),
                    ResetOTPAttempts = 0
                WHERE IDNumber = @idNumber", conn);

                    storeOtpCmd.Parameters.AddWithValue("@otp", Convert.ToBase64String(otpHash));
                    storeOtpCmd.Parameters.AddWithValue("@salt", Convert.ToBase64String(otpSalt));
                    storeOtpCmd.Parameters.AddWithValue("@idNumber", request.idNumber);
                    storeOtpCmd.ExecuteNonQuery();

                    // 5. Send OTP via SMS
                    string message = $"Dear {fullNames}, your PIN reset OTP is: {otp}. This OTP is valid for 5 minutes. Do not share this code with anyone.";

                    await SmsHelper.SendPin(otp, message, phone);

                    return Json(new
                    {
                        success = true,
                        message = "OTP sent to your registered mobile number",
                        requiresOtp = true,
                        memberNo = memberNo,
                        otpExpiryMinutes = 5
                    });
                }
            }
        }

        [HttpPost]
        [Route("VerifyResetOtp")]
        public async Task<IHttpActionResult> VerifyResetOtp([FromBody] VerifyResetOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.idNumber))
                return Json(new { success = false, message = "ID Number is required" });

            if (string.IsNullOrWhiteSpace(request?.otp))
                return Json(new { success = false, message = "OTP is required" });

            using (var conn = new SqlConnection(_cs))
            {
                conn.Open();

                // 1. Look up member and OTP details
                var cmd = new SqlCommand(@"
            SELECT MemberNo, FullNames, PhoneNumber, Status,
                   ResetOTP, ResetOTPSalt, ResetOTPExpiry, ResetOTPAttempts
            FROM Registration
            WHERE IDNumber = @idNumber", conn);

                cmd.Parameters.AddWithValue("@idNumber", request.idNumber);

                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read())
                        return Json(new { success = false, message = "ID Number not found" });

                    var memberNo = rd["MemberNo"].ToString();
                    var fullNames = rd["FullNames"].ToString();
                    var phone = rd["PhoneNumber"].ToString();
                    var status = rd["Status"].ToString();

                    // Check if OTP exists
                    if (rd["ResetOTP"] == DBNull.Value || rd["ResetOTPSalt"] == DBNull.Value)
                        return Json(new { success = false, message = "No OTP request found. Please request a new OTP." });

                    var otpHash = Convert.FromBase64String(rd["ResetOTP"].ToString());
                    var otpSalt = Convert.FromBase64String(rd["ResetOTPSalt"].ToString());
                    var otpExpiry = (DateTime)rd["ResetOTPExpiry"];
                    var otpAttempts = (int)rd["ResetOTPAttempts"];

                    rd.Close();

                    // 2. Check OTP expiry
                    if (DateTime.UtcNow > otpExpiry)
                    {
                        ClearResetOtp(conn, request.idNumber);
                        return Json(new { success = false, message = "OTP has expired. Please request a new OTP." });
                    }

                    // 3. Check OTP attempts (max 3 attempts)
                    if (otpAttempts >= 3)
                    {
                        ClearResetOtp(conn, request.idNumber);
                        return Json(new { success = false, message = "Too many invalid OTP attempts. Please request a new OTP." });
                    }

                    // 4. Verify OTP
                    if (!PinSecurity.Verify(request.otp, otpHash, otpSalt))
                    {
                        IncrementResetOtpAttempts(conn, request.idNumber);
                        return Json(new { success = false, message = "Invalid OTP", attemptsRemaining = 3 - (otpAttempts + 1) });
                    }

                    // 5. OTP verified - generate new PIN
                    var newPin = new Random().Next(1000, 9999).ToString();
                    PinSecurity.Create(newPin, out var pinHash, out var pinSalt);

                    // 6. Update Registration with new PIN
                    var updateCmd = new SqlCommand(@"
                UPDATE Registration
                SET PIN = @pin,
                    IMSI = @salt,
                    Trials = 0,
                    UpdatedAt = GETUTCDATE(),
                    UpdatedBy = 'SYSTEM',
                    ResetOTP = NULL,
                    ResetOTPSalt = NULL,
                    ResetOTPExpiry = NULL,
                    ResetOTPAttempts = 0
                WHERE IDNumber = @idNumber", conn);

                    updateCmd.Parameters.AddWithValue("@pin", Convert.ToBase64String(pinHash));
                    updateCmd.Parameters.AddWithValue("@salt", Convert.ToBase64String(pinSalt));
                    updateCmd.Parameters.AddWithValue("@idNumber", request.idNumber);
                    updateCmd.ExecuteNonQuery();

                    // 7. Send new PIN via SMS
                    string message = $"Dear {fullNames}, your PIN has been reset. Your new PIN is: {newPin}. Please login and change your PIN immediately.";

                    await SmsHelper.SendPin(newPin, message, phone);

                    return Json(new
                    {
                        success = true,
                        message = "PIN reset successful! A new PIN has been sent to your registered phone number.",
                        memberNo = memberNo
                    });
                }
            }
        }

        private void ClearResetOtp(SqlConnection conn, string idNumber)
        {
            var cmd = new SqlCommand(@"
        UPDATE Registration 
        SET ResetOTP = NULL,
            ResetOTPSalt = NULL,
            ResetOTPExpiry = NULL,
            ResetOTPAttempts = 0
        WHERE IDNumber = @idNumber", conn);

            cmd.Parameters.AddWithValue("@idNumber", idNumber);
            cmd.ExecuteNonQuery();
        }

        private void IncrementResetOtpAttempts(SqlConnection conn, string idNumber)
        {
            var cmd = new SqlCommand(@"
        UPDATE Registration 
        SET ResetOTPAttempts = ISNULL(ResetOTPAttempts, 0) + 1
        WHERE IDNumber = @idNumber", conn);

            cmd.Parameters.AddWithValue("@idNumber", idNumber);
            cmd.ExecuteNonQuery();
        }

        [HttpPost]
        [Route("ResetChangePin")]
        public async Task<IHttpActionResult> ResetChangePin([FromBody] ResetChangePinRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.idNumber))
                return Json(new { success = false, message = "ID Number is required" });

            if (string.IsNullOrWhiteSpace(request?.currentPin))
                return Json(new { success = false, message = "Current PIN is required" });

            if (string.IsNullOrWhiteSpace(request?.newPin))
                return Json(new { success = false, message = "New PIN is required" });

            using (var conn = new SqlConnection(_cs))
            {
                conn.Open();

                // 1. Fetch member by ID Number
                var cmd = new SqlCommand(@"
            SELECT MemberNo, FullNames, PhoneNumber, PIN, IMSI, Trials, Status
            FROM Registration
            WHERE IDNumber = @idNumber", conn);

                cmd.Parameters.AddWithValue("@idNumber", request.idNumber);

                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read())
                        return Json(new { success = false, message = "ID Number not found" });

                    var memberNo = rd["MemberNo"].ToString();
                    var fullNames = rd["FullNames"].ToString();
                    var phoneNumber = rd["PhoneNumber"].ToString();
                    var status = rd["Status"].ToString();
                    var trials = (int)rd["Trials"];

                    if (status != "Active")
                        return Json(new { success = false, message = "Account is not active. Please contact support." });

                    if (trials >= 5)
                        return Json(new { success = false, message = "Account locked. Please contact support." });

                    var currentHash = Convert.FromBase64String(rd["PIN"].ToString());
                    var currentSalt = Convert.FromBase64String(rd["IMSI"].ToString());

                    rd.Close();

                    // 2. Verify system-generated PIN (the one sent via SMS after reset)
                    if (!PinSecurity.Verify(request.currentPin, currentHash, currentSalt))
                    {
                        IncrementTrials(conn, memberNo);
                        return Json(new { success = false, message = "Invalid current PIN" });
                    }

                    // 3. Validate new PIN requirements
                    var pinValidation = ValidateNewPin(request.newPin);
                    if (!pinValidation.IsValid)
                        return Json(new { success = false, message = pinValidation.Message });

                    // 4. Hash new PIN
                    PinSecurity.Create(request.newPin, out var newHash, out var newSalt);

                    // 5. Update PIN in database
                    var updateCmd = new SqlCommand(@"
                UPDATE Registration
                SET PIN                  = @newPin,
                    IMSI                 = @newSalt,
                    Trials               = 0,
                    FirstLogin           = 0,
                    LastPinChangeDate    = GETUTCDATE(),
                    UpdatedAt            = GETUTCDATE(),
                    UpdatedBy            = 'MEMBER'
                WHERE IDNumber = @idNumber", conn);

                    updateCmd.Parameters.AddWithValue("@newPin", Convert.ToBase64String(newHash));
                    updateCmd.Parameters.AddWithValue("@newSalt", Convert.ToBase64String(newSalt));
                    updateCmd.Parameters.AddWithValue("@idNumber", request.idNumber);
                    updateCmd.ExecuteNonQuery();

                    // 6. Send confirmation SMS
                    string message =
                        $"Dear {fullNames}, your PIN has been updated successfully. " +
                        $"You can now log in to the Web Portal and Mobile App using your new preferred PIN. " +
                        $"Do not share your PIN with anyone.";

                    await SmsHelper.SendPin(request.newPin, message, phoneNumber);

                    return Json(new
                    {
                        success = true,
                        message = "PIN changed successfully. You can now login with your new PIN.",
                        memberNo
                    });
                }
            }
        }

        [HttpGet, Route("GetStatementByMemberNo")]
        public IHttpActionResult GetStatementByMemberNo([FromUri] string MemberNo, [FromUri] DateTime startDate, [FromUri] DateTime endDate, [FromUri] bool IncludeProductBreakdown = true)
        {
            try
            {
                if (startDate > endDate)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Start date cannot be after end date" });

                // Get customer details
                var customer = _customerService.GetByMemberNo(MemberNo);


                if (customer == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Customer not found" });
                if (startDate == null)
                {
                    startDate = (DateTime)customer.RegistrationDate;
                }
                if (endDate == null)
                {
                    endDate = DateTime.Now;
                }

                // Get statement
                var statement = _statementService.GetCustomerStatementByCustomerId(customer.Id, startDate, endDate);
                var statementList = (System.Collections.Generic.List<CustomerStatementDTO>)statement;

                // Calculate opening balance
                var openingBalance = _statementService.GetCustomerBalanceAsOfDate(customer.Id, startDate.AddSeconds(-1));

                // Calculate running totals including opening balance
                decimal runningTotal = openingBalance;
                foreach (var transaction in statementList)
                {
                    runningTotal += transaction.Credit - transaction.Debit;
                    transaction.RunningTotal = runningTotal;
                }

                // Calculate summary
                var summary = new CustomerStatementSummaryDTO
                {
                    CustomerName = customer.FullName,
                    SerialNumber = customer.SerialNumber.ToString(),
                    FirstTransactionDate = startDate,
                    LastTransactionDate = endDate,
                    OpeningBalance = openingBalance,
                    ClosingBalance = runningTotal
                };

                // Calculate totals
                decimal totalDebit = 0, totalCredit = 0;
                foreach (var transaction in statementList)
                {
                    totalDebit += transaction.Debit;
                    totalCredit += transaction.Credit;
                }

                summary.TotalTransactions = statementList.Count;
                summary.TotalDebit = totalDebit;
                summary.TotalCredit = totalCredit;
                summary.NetBalance = totalCredit - totalDebit;

                // Get product breakdown if requested
                if (IncludeProductBreakdown)
                {
                    summary.ProductBreakdown = new System.Collections.Generic.List<CustomerProductStatementDTO>(
                        _statementService.GetStatementByProduct(customer.Id, startDate, endDate)
                    );
                }

                return ApiResponse(true, "Customer statement retrieved successfully", new
                {
                    customer = new
                    {
                        customer.FullName,
                        customer.SerialNumber,
                        customer.Reference1,
                        customer.Reference2,
                        customer.Reference3,
                        customer.AddressAddressLine1,
                        customer.AddressAddressLine2,
                        customer.AddressMobileLine,
                        customer.IndividualIdentityCardNumber
                    },
                    statement = statement,
                    summary = summary,
                    openingBalance = openingBalance,
                    closingBalance = runningTotal
                });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("GetMemberWithDetails/by-reference2/{reference2}")]
        public async Task<IHttpActionResult> GetCustomersWithNextOfKinByReference2(string reference2)
        {
            try
            {
                var customers = new List<object>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    // Get customer details
                    string customerSql = @"
SELECT 
    c.Id,
    c.Individual_FirstName + ' ' + c.Individual_LastName AS FullName,
    c.SerialNumber,
    c.Type,
    c.Individual_Type AS IndividualType,
    c.Individual_FirstName AS IndividualFirstName,
    c.Individual_LastName AS IndividualLastName,
    c.Individual_IdentityCardNumber AS IndividualIdentityCardNumber,
    c.Address_MobileLine AS AddressMobileLine,
    c.Address_Email AS AddressEmail,
    c.PersonalIdentificationNumber,
    c.Reference1,
    c.Reference2,
    c.Reference3,
    c.RegistrationDate,
    c.RecordStatus,
    c.IsDefaulter,
    c.IsLocked,
    c.NonIndividual_DateEstablished AS NonIndividualDateEstablished,
    c.Individual_BirthDate AS IndividualBirthDate,
    c.Individual_Gender AS Gender,
    c.Individual_Nationality AS Nationality,
    c.Address_AddressLine1 AS AddressLine1,
    c.Address_City AS City,
    c.Address_PostalCode AS PostalCode
FROM swiftFin_Customers c
WHERE c.Reference2 = @reference2";

                    using (var cmd = new SqlCommand(customerSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@reference2", reference2);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var customerId = reader["Id"] != DBNull.Value ? (Guid)reader["Id"] : Guid.Empty;

                                var customerObj = new
                                {
                                    Id = customerId,
                                    FullName = reader["FullName"]?.ToString(),
                                    SerialNumber = reader["SerialNumber"] != DBNull.Value ? Convert.ToInt32(reader["SerialNumber"]) : 0,
                                    Type = reader["Type"] != DBNull.Value ? Convert.ToInt32(reader["Type"]) : 0,
                                    IndividualType = reader["IndividualType"] != DBNull.Value ? Convert.ToInt32(reader["IndividualType"]) : 0,
                                    IndividualFirstName = reader["IndividualFirstName"]?.ToString(),
                                    IndividualLastName = reader["IndividualLastName"]?.ToString(),
                                    IndividualIdentityCardNumber = reader["IndividualIdentityCardNumber"]?.ToString(),
                                    AddressMobileLine = reader["AddressMobileLine"]?.ToString(),
                                    AddressEmail = reader["AddressEmail"]?.ToString(),
                                    PersonalIdentificationNumber = reader["PersonalIdentificationNumber"]?.ToString(),
                                    Reference1 = reader["Reference1"]?.ToString(),
                                    Reference2 = reader["Reference2"]?.ToString(),
                                    Reference3 = reader["Reference3"]?.ToString(),
                                    RegistrationDate = reader["RegistrationDate"] != DBNull.Value ? (DateTime?)reader["RegistrationDate"] : null,
                                    RecordStatus = reader["RecordStatus"] != DBNull.Value ? Convert.ToInt32(reader["RecordStatus"]) : 0,
                                    IsDefaulter = reader["IsDefaulter"] != DBNull.Value && (bool)reader["IsDefaulter"],
                                    IsLocked = reader["IsLocked"] != DBNull.Value && (bool)reader["IsLocked"],
                                    NonIndividualDateEstablished = reader["NonIndividualDateEstablished"] != DBNull.Value ? (DateTime?)reader["NonIndividualDateEstablished"] : null,
                                    IndividualBirthDate = reader["IndividualBirthDate"] != DBNull.Value ? (DateTime?)reader["IndividualBirthDate"] : null,
                                    Gender = reader["Gender"] != DBNull.Value ? Convert.ToInt32(reader["Gender"]) : (int?)null,
                                    Nationality = reader["Nationality"]?.ToString(),
                                    AddressLine1 = reader["AddressLine1"]?.ToString(),
                                    City = reader["City"]?.ToString(),
                                    PostalCode = reader["PostalCode"]?.ToString(),
                                    NextOfKin = new List<object>() // Will be populated after reader is closed
                                };

                                customers.Add(customerObj);
                            }
                        }
                    }

                    if (!customers.Any())
                    {
                        return Json(new ApiResponse<object>
                        {
                            Success = false,
                            Message = $"No customers found with Reference2 '{reference2}'.",
                            Data = null
                        });
                    }

                    // Get next of kin for all customers found
                    var customerIds = customers.Select(c => (Guid)c.GetType().GetProperty("Id").GetValue(c)).ToList();

                    if (customerIds.Any())
                    {
                        string nokSql = @"
SELECT 
    Id,
    CustomerId,
    FirstName,
    LastName,
    FirstName + ' ' + LastName AS FullName,
    Relationship,
    Salutation,
    Gender,
    IdentityCardNumber,
    IdentityCardType,
    Address_AddressLine1 AS AddressLine1,
    Address_AddressLine2 AS AddressLine2,
    Address_Street AS Street,
    Address_PostalCode AS PostalCode,
    Address_City AS City,
    Address_Email AS Email,
    Address_LandLine AS LandLine,
    Address_MobileLine AS MobileLine,
    NominatedPercentage,
    Remarks,
    CreatedDate
FROM swiftFin_NextOfKin
WHERE CustomerId IN ({0})";

                        // Create parameterized IN clause
                        var parameters = new List<string>();
                        for (int i = 0; i < customerIds.Count; i++)
                        {
                            parameters.Add($"@customerId{i}");
                        }

                        nokSql = string.Format(nokSql, string.Join(",", parameters));

                        using (var cmd = new SqlCommand(nokSql, conn))
                        {
                            // Add parameters
                            for (int i = 0; i < customerIds.Count; i++)
                            {
                                cmd.Parameters.AddWithValue($"@customerId{i}", customerIds[i]);
                            }

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                var nextOfKinsByCustomer = new Dictionary<Guid, List<object>>();

                                while (await reader.ReadAsync())
                                {
                                    var customerId = reader["CustomerId"] != DBNull.Value ? (Guid)reader["CustomerId"] : Guid.Empty;

                                    var nokObj = new
                                    {
                                        Id = reader["Id"] != DBNull.Value ? (Guid)reader["Id"] : Guid.Empty,
                                        CustomerId = customerId,
                                        FullName = reader["FullName"]?.ToString(),
                                        FirstName = reader["FirstName"]?.ToString(),
                                        LastName = reader["LastName"]?.ToString(),
                                        Relationship = reader["Relationship"] != DBNull.Value ? Convert.ToInt32(reader["Relationship"]) : 0,
                                        Salutation = reader["Salutation"] != DBNull.Value ? Convert.ToInt32(reader["Salutation"]) : (int?)null,
                                        Gender = reader["Gender"] != DBNull.Value ? Convert.ToInt32(reader["Gender"]) : (int?)null,
                                        IdentityCardNumber = reader["IdentityCardNumber"]?.ToString(),
                                        IdentityCardType = reader["IdentityCardType"] != DBNull.Value ? Convert.ToInt32(reader["IdentityCardType"]) : (int?)null,
                                        AddressLine1 = reader["AddressLine1"]?.ToString(),
                                        AddressLine2 = reader["AddressLine2"]?.ToString(),
                                        Street = reader["Street"]?.ToString(),
                                        PostalCode = reader["PostalCode"]?.ToString(),
                                        City = reader["City"]?.ToString(),
                                        Email = reader["Email"]?.ToString(),
                                        LandLine = reader["LandLine"]?.ToString(),
                                        MobileLine = reader["MobileLine"]?.ToString(),
                                        NominatedPercentage = reader["NominatedPercentage"] != DBNull.Value ? Convert.ToDecimal(reader["NominatedPercentage"]) : 0,
                                        Remarks = reader["Remarks"]?.ToString(),
                                        CreatedDate = reader["CreatedDate"] != DBNull.Value ? (DateTime?)reader["CreatedDate"] : null
                                    };

                                    if (!nextOfKinsByCustomer.ContainsKey(customerId))
                                    {
                                        nextOfKinsByCustomer[customerId] = new List<object>();
                                    }
                                    nextOfKinsByCustomer[customerId].Add(nokObj);
                                }

                                // Update each customer with their next of kin
                                var updatedCustomers = new List<object>();
                                foreach (var customer in customers)
                                {
                                    var customerId = (Guid)customer.GetType().GetProperty("Id").GetValue(customer);

                                    // Create a new anonymous object with the next of kin included
                                    var customerWithNok = new
                                    {
                                        ((dynamic)customer).Id,
                                        ((dynamic)customer).FullName,
                                        ((dynamic)customer).SerialNumber,
                                        ((dynamic)customer).Type,
                                        ((dynamic)customer).IndividualType,
                                        ((dynamic)customer).IndividualFirstName,
                                        ((dynamic)customer).IndividualLastName,
                                        ((dynamic)customer).IndividualIdentityCardNumber,
                                        ((dynamic)customer).AddressMobileLine,
                                        ((dynamic)customer).AddressEmail,
                                        ((dynamic)customer).PersonalIdentificationNumber,
                                        ((dynamic)customer).Reference1,
                                        ((dynamic)customer).Reference2,
                                        ((dynamic)customer).Reference3,
                                        ((dynamic)customer).RegistrationDate,
                                        ((dynamic)customer).RecordStatus,
                                        ((dynamic)customer).IsDefaulter,
                                        ((dynamic)customer).IsLocked,
                                        ((dynamic)customer).NonIndividualDateEstablished,
                                        ((dynamic)customer).IndividualBirthDate,
                                        ((dynamic)customer).Gender,
                                        ((dynamic)customer).Nationality,
                                        ((dynamic)customer).AddressLine1,
                                        ((dynamic)customer).City,
                                        ((dynamic)customer).PostalCode,
                                        NextOfKin = nextOfKinsByCustomer.ContainsKey(customerId) ? nextOfKinsByCustomer[customerId] : new List<object>()
                                    };

                                    updatedCustomers.Add(customerWithNok);
                                }

                                customers = updatedCustomers;
                            }
                        }
                    }
                }

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = customers.Count > 1 ? "Customers retrieved successfully." : "Customer retrieved successfully.",
                    Data = new
                    {
                        Customers = customers,
                        Summary = new
                        {
                            TotalCustomers = customers.Count,
                            TotalNextOfKin = customers.Sum(c => ((List<object>)((dynamic)c).NextOfKin).Count)
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Error in GetCustomersWithNextOfKinByReference2: {ex.Message}");
                System.Diagnostics.Trace.TraceError($"Stack Trace: {ex.StackTrace}");

                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving customer details.",
                    Data = new { Error = ex.Message, InnerError = ex.InnerException?.Message }
                });
            }
        }




        [HttpGet]
        [Route("customerAccounts/{reference2}")]
        public async Task<IHttpActionResult> GetCustomerAccountsWithBalances(string reference2)
        {
            try
            {
                var result = new List<object>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    // First, get the customer and their accounts
                    string sql = @"
SELECT 
    c.Id AS CustomerId,
    c.Reference2,
    c.Individual_FirstName + ' ' + c.Individual_LastName AS FullName,
    ca.Id AS CustomerAccountId,
    ca.CustomerAccountType_ProductCode AS ProductCode,
    ca.CustomerAccountType_TargetProductId AS TargetProductId,
    ca.CustomerAccountType_TargetProductCode AS TargetProductCode,
    ca.Status,
    ca.CreatedDate,
    -- Product details
    COALESCE(lp.Description, sp.Description) AS ProductDescription,
    lp.ChartOfAccountId AS LoanChartOfAccountId,
    sp.ChartOfAccountId AS SavingsChartOfAccountId,
    CASE 
        WHEN lp.Id IS NOT NULL THEN 'Loan'
        WHEN sp.Id IS NOT NULL THEN 'Savings'
        ELSE 'Unknown'
    END AS AccountType,
    -- Format account number
    CAST(c.SerialNumber AS VARCHAR) + '-' + 
    CAST(ca.CustomerAccountType_ProductCode AS VARCHAR) + '-' + 
    CAST(ca.CustomerAccountType_TargetProductCode AS VARCHAR) AS AccountNumber
FROM swiftFin_Customers c
INNER JOIN swiftFin_CustomerAccounts ca ON ca.CustomerId = c.Id
LEFT JOIN swiftFin_LoanProducts lp ON lp.Id = ca.CustomerAccountType_TargetProductId
LEFT JOIN swiftFin_SavingsProducts sp ON sp.Id = ca.CustomerAccountType_TargetProductId
WHERE c.Reference2 = @reference2
ORDER BY ca.CreatedDate DESC";

                    var accounts = new List<dynamic>();
                    Guid? customerId = null;
                    string customerName = null;
                    string customerRef2 = null;

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@reference2", reference2);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                if (!customerId.HasValue)
                                {
                                    customerId = reader["CustomerId"] != DBNull.Value ? (Guid)reader["CustomerId"] : Guid.Empty;
                                    customerName = reader["FullName"]?.ToString();
                                    customerRef2 = reader["Reference2"]?.ToString();
                                }

                                var accountId = reader["CustomerAccountId"] != DBNull.Value ? (Guid)reader["CustomerAccountId"] : Guid.Empty;
                                var chartOfAccountId = reader["LoanChartOfAccountId"] ?? reader["SavingsChartOfAccountId"];
                                var status = reader["Status"] != DBNull.Value ? Convert.ToInt32(reader["Status"]) : 0;

                                accounts.Add(new
                                {
                                    AccountId = accountId,
                                    AccountNumber = reader["AccountNumber"]?.ToString(),
                                    ProductCode = reader["ProductCode"] != DBNull.Value ? Convert.ToInt32(reader["ProductCode"]) : 0,
                                    TargetProductCode = reader["TargetProductCode"] != DBNull.Value ? Convert.ToInt32(reader["TargetProductCode"]) : 0,
                                    TargetProductId = reader["TargetProductId"] != DBNull.Value ? (Guid)reader["TargetProductId"] : Guid.Empty,
                                    AccountType = reader["AccountType"]?.ToString(),
                                    ProductDescription = reader["ProductDescription"]?.ToString(),
                                    Status = status,
                                    StatusDescription = GetAccountStatusDescription(status),
                                    CreatedDate = reader["CreatedDate"] != DBNull.Value ? (DateTime?)reader["CreatedDate"] : null,
                                    ChartOfAccountId = chartOfAccountId != DBNull.Value ? (Guid?)chartOfAccountId : null
                                });
                            }
                        }
                    }

                    if (!accounts.Any())
                    {
                        return Ok(new ApiResponse<object>
                        {
                            Success = false,
                            Message = $"No accounts found for customer with Reference2 '{reference2}'.",
                            Data = null
                        });
                    }

                    // Get balances for each account
                    var accountIds = accounts.Select(a => (Guid)a.AccountId).ToList();
                    var accountBalances = await CalculateAccountBalancesAsync(conn, accountIds);

                    // Combine account details with balances
                    foreach (var account in accounts)
                    {
                        var accountId = (Guid)account.AccountId;
                        var balance = accountBalances.ContainsKey(accountId) ? accountBalances[accountId] : 0;

                        result.Add(new
                        {
                            CustomerInfo = new
                            {
                                CustomerId = customerId,
                                FullName = customerName,
                                Reference2 = customerRef2
                            },
                            AccountInfo = new
                            {
                                AccountId = account.AccountId,
                                AccountNumber = account.AccountNumber,
                                ProductCode = account.ProductCode,
                                TargetProductCode = account.TargetProductCode,
                                AccountType = account.AccountType,
                                ProductDescription = account.ProductDescription,
                                Status = account.Status,
                                StatusDescription = account.StatusDescription,
                                CreatedDate = account.CreatedDate,
                                CurrentBalance = balance,
                                FormattedBalance = balance.ToString("N2"),
                                BalanceStatus = balance >= 0 ? "Positive" : "Negative"
                            }
                        });
                    }

                    // Calculate summary statistics
                    var totalBalance = accountBalances.Sum(b => b.Value);
                    var positiveBalanceAccounts = accountBalances.Count(b => b.Value > 0);
                    var negativeBalanceAccounts = accountBalances.Count(b => b.Value < 0);
                    var zeroBalanceAccounts = accountBalances.Count(b => b.Value == 0);

                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Customer accounts with balances retrieved successfully.",
                        Data = new
                        {
                            CustomerSummary = new
                            {
                                CustomerId = customerId,
                                CustomerName = customerName,
                                Reference2 = customerRef2,
                                TotalAccounts = accounts.Count,
                                TotalBalance = totalBalance,
                                FormattedTotalBalance = totalBalance.ToString("N2")
                            },
                            Accounts = result,
                            BalanceSummary = new
                            {
                                TotalBalance = totalBalance,
                                FormattedTotalBalance = totalBalance.ToString("N2"),
                                PositiveBalanceAccounts = positiveBalanceAccounts,
                                NegativeBalanceAccounts = negativeBalanceAccounts,
                                ZeroBalanceAccounts = zeroBalanceAccounts,
                                AverageBalancePerAccount = accounts.Count > 0 ? totalBalance / accounts.Count : 0
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Error in GetCustomerAccountsWithBalances: {ex.Message}");
                System.Diagnostics.Trace.TraceError($"Stack Trace: {ex.StackTrace}");

                return Ok(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving customer accounts with balances.",
                    Data = new { Error = ex.Message, InnerError = ex.InnerException?.Message }
                });
            }
        }

        private async Task<Dictionary<Guid, decimal>> CalculateAccountBalancesAsync(SqlConnection connection, List<Guid> accountIds)
        {
            var balances = new Dictionary<Guid, decimal>();

            try
            {
                // Use a more efficient approach with table-valued parameter for multiple accounts
                // Create a DataTable to pass as parameter
                var accountIdsTable = new System.Data.DataTable();
                accountIdsTable.Columns.Add("Id", typeof(Guid));
                foreach (var id in accountIds)
                {
                    accountIdsTable.Rows.Add(id);
                }

                // Single combined SQL query using CTE for better performance
                string balanceSql = @"
-- Define withdrawal transaction types
WITH WithdrawalTypes AS (
    SELECT Description FROM (VALUES 
        ('Withdrawals'),
        ('Transfer'),
        ('Withdrawal'),
        ('Cash Withdrawal'),
        ('Bank Transfer'),
        ('EFT')
    ) AS wt(Description)
),
WithdrawalJournals AS (
    SELECT DISTINCT j.Id
    FROM swiftFin_Journals j
    INNER JOIN swiftFin_JournalEntries je ON j.Id = je.JournalId
    INNER JOIN WithdrawalTypes wt ON j.PrimaryDescription = wt.Description
    WHERE je.CustomerAccountId IN (SELECT Id FROM @AccountIds)
),
AccountBalances AS (
    SELECT 
        je.CustomerAccountId,
        ISNULL(SUM(
            CASE 
                -- Normal credits (deposits, interest, etc.) - positive contribution
                WHEN je.Amount < 0 AND wj.Id IS NULL THEN ABS(je.Amount)
                -- Withdrawal debits - negative contribution (reduces balance)
                WHEN je.Amount > 0 AND wj.Id IS NOT NULL THEN -je.Amount
                ELSE 0
            END
        ), 0) AS AccountBalance,
        -- Additional breakdown for verification
        ISNULL(SUM(CASE WHEN je.Amount < 0 AND wj.Id IS NULL THEN ABS(je.Amount) END), 0) AS TotalCredits,
        ISNULL(SUM(CASE WHEN je.Amount > 0 AND wj.Id IS NOT NULL THEN je.Amount END), 0) AS TotalWithdrawals
    FROM swiftFin_JournalEntries je
    LEFT JOIN WithdrawalJournals wj ON je.JournalId = wj.Id
    WHERE je.CustomerAccountId IN (SELECT Id FROM @AccountIds)
    GROUP BY je.CustomerAccountId
)
SELECT 
    a.Id AS CustomerAccountId,
    ISNULL(ab.AccountBalance, 0) AS AccountBalance,
    ISNULL(ab.TotalCredits, 0) AS TotalCredits,
    ISNULL(ab.TotalWithdrawals, 0) AS TotalWithdrawals
FROM @AccountIds a
LEFT JOIN AccountBalances ab ON a.Id = ab.CustomerAccountId";

                using (var cmd = new SqlCommand(balanceSql, connection))
                {
                    // Add table-valued parameter
                    var tvpParam = cmd.Parameters.AddWithValue("@AccountIds", accountIdsTable);
                    tvpParam.SqlDbType = System.Data.SqlDbType.Structured;
                    tvpParam.TypeName = "dbo.GuidList"; // You'll need to create this type in SQL Server

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var accountId = reader["CustomerAccountId"] != DBNull.Value ? (Guid)reader["CustomerAccountId"] : Guid.Empty;
                            var balance = reader["AccountBalance"] != DBNull.Value ? Convert.ToDecimal(reader["AccountBalance"]) : 0m;

                            balances[accountId] = Math.Abs(balance);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Error in CalculateAccountBalancesAsync: {ex.Message}");

                // Fallback to individual queries if TVP fails
                balances = await CalculateAccountBalancesFallbackAsync(connection, accountIds);
            }

            return balances;
        }

        private async Task<Dictionary<Guid, decimal>> CalculateAccountBalancesFallbackAsync(SqlConnection connection, List<Guid> accountIds)
        {
            var balances = new Dictionary<Guid, decimal>();

            // Process in batches to avoid SQL command length issues
            const int batchSize = 100;
            for (int i = 0; i < accountIds.Count; i += batchSize)
            {
                var batchIds = accountIds.Skip(i).Take(batchSize).ToList();
                var idList = string.Join(",", batchIds.Select(id => $"'{id}'"));

                string balanceSql = $@"
WITH WithdrawalTypes AS (
    SELECT Description FROM (VALUES 
        ('Withdrawals'), ('Transfer'), ('Withdrawal'), 
        ('Cash Withdrawal'), ('Bank Transfer'), ('EFT')
    ) AS wt(Description)
),
WithdrawalJournals AS (
    SELECT DISTINCT j.Id
    FROM swiftFin_Journals j
    INNER JOIN swiftFin_JournalEntries je ON j.Id = je.JournalId
    INNER JOIN WithdrawalTypes wt ON j.PrimaryDescription = wt.Description
    WHERE je.CustomerAccountId IN ({idList})
)
SELECT 
    je.CustomerAccountId,
    ISNULL(SUM(
        CASE 
            WHEN je.Amount < 0 AND wj.Id IS NULL THEN ABS(je.Amount)
            WHEN je.Amount > 0 AND wj.Id IS NOT NULL THEN -je.Amount
            ELSE 0
        END
    ), 0) AS AccountBalance
FROM swiftFin_JournalEntries je
LEFT JOIN WithdrawalJournals wj ON je.JournalId = wj.Id
WHERE je.CustomerAccountId IN ({idList})
GROUP BY je.CustomerAccountId";

                using (var cmd = new SqlCommand(balanceSql, connection))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var accountId = reader["CustomerAccountId"] != DBNull.Value ? (Guid)reader["CustomerAccountId"] : Guid.Empty;
                            var balance = reader["AccountBalance"] != DBNull.Value ? Convert.ToDecimal(reader["AccountBalance"]) : 0m;
                            balances[accountId] = Math.Abs(balance);
                        }
                    }
                }
            }

            // Ensure all accounts have a balance
            foreach (var accountId in accountIds)
            {
                if (!balances.ContainsKey(accountId))
                {
                    balances[accountId] = 0m;
                }
            }

            return balances;
        }

        private string GetAccountStatusDescription(int status)
        {
            // C# 7.3 compatible switch statement
            switch (status)
            {
                case 0: return "Active";
                case 1: return "Active";
                case 2: return "Dormant";
                case 3: return "Closed";
                case 4: return "Blocked";
                default: return "Unknown";
            }
        }

        [HttpGet]
        [Route("customerAccountsSimple/{reference2}")]
        public async Task<IHttpActionResult> GetCustomerAccountsSimple(string reference2)
        {
            try
            {
                var result = new List<CustomerAccountBalanceDto>();

                string sql = @"
WITH WithdrawalTypes AS (
    SELECT Description FROM (VALUES 
        ('Withdrawals'), ('Transfer'), ('Withdrawal'), 
        ('Cash Withdrawal'), ('Bank Transfer'), ('EFT')
    ) AS wt(Description)
),
WithdrawalJournals AS (
    SELECT DISTINCT j.Id
    FROM swiftFin_Journals j
    INNER JOIN WithdrawalTypes wt ON j.PrimaryDescription = wt.Description
),
AccountBalances AS (
    SELECT 
        je.CustomerAccountId,
        ISNULL(SUM(
            CASE 
                WHEN je.Amount < 0 AND wj.Id IS NULL THEN ABS(je.Amount)
                WHEN je.Amount > 0 AND wj.Id IS NOT NULL THEN -je.Amount
                ELSE 0
            END
        ), 0) AS CalculatedBalance
    FROM swiftFin_JournalEntries je
    LEFT JOIN WithdrawalJournals wj ON je.JournalId = wj.Id
    GROUP BY je.CustomerAccountId
)
SELECT 
    c.Id AS CustomerId,
    c.Reference2,
    c.Individual_FirstName + ' ' + c.Individual_LastName AS FullName,
    ca.Id AS AccountId,
    ca.CustomerAccountType_ProductCode AS ProductCode,
    COALESCE(lp.Description, sp.Description) AS ProductDescription,
    CASE 
        WHEN lp.Id IS NOT NULL THEN 'Loan'
        WHEN sp.Id IS NOT NULL THEN 'Savings'
        ELSE 'Unknown'
    END AS AccountType,
    ca.Status,
    ca.CreatedDate,
    ISNULL(ab.CalculatedBalance, 0) AS CurrentBalance
FROM swiftFin_Customers c
INNER JOIN swiftFin_CustomerAccounts ca ON ca.CustomerId = c.Id
LEFT JOIN swiftFin_LoanProducts lp ON lp.Id = ca.CustomerAccountType_TargetProductId
LEFT JOIN swiftFin_SavingsProducts sp ON sp.Id = ca.CustomerAccountType_TargetProductId
LEFT JOIN AccountBalances ab ON ab.CustomerAccountId = ca.Id
WHERE c.Reference2 = @reference2
ORDER BY ca.CreatedDate DESC";

                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@reference2", reference2);
                    await conn.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var status = reader["Status"] != DBNull.Value ? Convert.ToInt32(reader["Status"]) : 0;

                            var dto = new CustomerAccountBalanceDto
                            {
                                CustomerId = reader["CustomerId"] != DBNull.Value ? (Guid)reader["CustomerId"] : Guid.Empty,
                                Reference2 = reader["Reference2"]?.ToString(),
                                FullName = reader["FullName"]?.ToString(),
                                AccountId = reader["AccountId"] != DBNull.Value ? (Guid)reader["AccountId"] : Guid.Empty,
                                ProductCode = reader["ProductCode"] != DBNull.Value ? Convert.ToInt32(reader["ProductCode"]) : 0,
                                ProductDescription = reader["ProductDescription"]?.ToString(),
                                AccountType = reader["AccountType"]?.ToString(),
                                Status = status,
                                StatusDescription = GetAccountStatusDescription(status),
                                CreatedDate = reader["CreatedDate"] != DBNull.Value ? (DateTime?)reader["CreatedDate"] : null,
                                CurrentBalance = reader["CurrentBalance"] != DBNull.Value ? Math.Abs(Convert.ToDecimal(reader["CurrentBalance"])) : 0m
                            };
                            result.Add(dto);
                        }
                    }
                }

                return Ok(new ApiResponse<List<CustomerAccountBalanceDto>>
                {
                    Success = true,
                    Message = "Customer accounts retrieved successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Error in GetCustomerAccountsSimple: {ex.Message}");

                return Ok(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving customer accounts.",
                    Data = new { Error = ex.Message }
                });
            }
        }

        // DTO for the simplified version
        public class CustomerAccountBalanceDto
        {
            public Guid CustomerId { get; set; }
            public string Reference2 { get; set; }
            public string FullName { get; set; }
            public Guid AccountId { get; set; }
            public int ProductCode { get; set; }
            public string ProductDescription { get; set; }
            public string AccountType { get; set; }
            public int Status { get; set; }
            public string StatusDescription { get; set; }
            public DateTime? CreatedDate { get; set; }
            public decimal CurrentBalance { get; set; }
            public string FormattedBalance => CurrentBalance.ToString("N2");
        }

       
        [HttpGet]
        [Route("GetLoanProducts")]
        public async Task<IHttpActionResult> GetLoanProducts([FromUri] string search = null, [FromUri] int pageIndex = 0, [FromUri] int pageSize = 20)
        {
            if (pageIndex < 0 || pageSize <= 0)
                return BadRequest("Invalid paging parameters.");

            try
            {
                var serviceHeader = master.GetServiceHeader();

                var loanProducts = await master._channelService.FindLoanProductsByFilterInPageAsync(search, pageIndex, pageSize, serviceHeader);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = loanProducts.PageCollection != null && loanProducts.PageCollection.Any()
                        ? $"{loanProducts.ItemsCount} loan products retrieved."
                        : "No loan products found.",
                    Data = loanProducts.PageCollection
                });
            }
            catch (Exception ex)
            {
                // log ex here (Serilog / AppInsights / ELK — non-negotiable)
                return InternalServerError(new Exception("Failed to retrieve loan products." + ex));
            }
        }



        [HttpPost]
        [Route("LoanApplication")]
        public async Task<IHttpActionResult> Create([FromBody] LoanCaseDTO2 loanCaseDTO)
        {
            var serviceHeader = master.GetServiceHeader();

            try
            {
                // 1. Fetch loan product
                var loanProduct = await master._channelService.FindLoanProductAsync(loanCaseDTO.LoanProductId, serviceHeader);
                var branches = await master._channelService.FindBranchesAsync(serviceHeader);
                var branch = branches?.FirstOrDefault(c => c.Description != null && c.Description.StartsWith("Rubani", StringComparison.OrdinalIgnoreCase));
                if (branch != null)
                {
                    loanCaseDTO.BranchId = branch.Id;
                }
                if (loanProduct == null)
                    return BadRequest("Invalid loan product.");

                // 2. Parse collaterals
                var collateralGuidList = loanCaseDTO.collateralIds?
                    .Split(',')
                    .Where(x => Guid.TryParse(x, out _))
                    .Select(Guid.Parse)
                    .ToList() ?? new List<Guid>();

                var collateralDocuments = new List<CustomerDocumentDTO>();

                foreach (var id in collateralGuidList)
                {
                    var doc = await master._channelService.FindCustomerDocumentAsync(id, serviceHeader);
                    if (doc != null)
                        collateralDocuments.Add(doc);
                }


                // 3. Get guarantors from client payload (NOT Session)
                var guarantors = loanCaseDTO.Guarantors ?? new List<LoanGuarantorDTO>();

                // Apply required rules
                if (loanProduct.LoanRegistrationMinimumGuarantors > guarantors.Count)
                {

                    return BadRequest($"Loan product requires minimum {loanProduct.LoanRegistrationMinimumGuarantors} guarantors.");
                }

                else if (loanProduct.LoanRegistrationMinimumGuarantors < guarantors.Count)
                {
                    var guaranteedSum = guarantors.Sum(g => g.AmountGuaranteed);

                    if (guaranteedSum < loanCaseDTO.AmountApplied)
                    {
                        return BadRequest("Total amount guaranteed does not secure the applied amount.");
                    }
                }


                // 4. Membership period validation
                var customers = await master._channelService.FindCustomersAsync(serviceHeader);

                var customer = customers?.FirstOrDefault(c => c.Reference2 == loanCaseDTO.CustomerReference2);
                if (customer != null)
                {
                    loanCaseDTO.CustomerId = customer.Id;

                    var months = ((DateTime.Now.Year - customer.CreatedDate.Year) * 12) +
                                 (DateTime.Now.Month - customer.CreatedDate.Month);
                    if (months < loanProduct.LoanRegistrationMinimumMembershipPeriod)
                    {
                        return BadRequest("Member does not meet minimum membership period or does not exist.");
                    }
                }

                // 5. Merge loan product rules into loanCaseDTO
                MapLoanProductAttributes(loanCaseDTO, loanProduct);


                // 6. Create loan
                loanCaseDTO.CreatedBy = User.Identity.Name;
                loanCaseDTO.Status = (int)LoanCaseStatus.Deferred;

                var createResult = await master._channelService.AddLoanCaseAsync(loanCaseDTO.MapTo<LoanCaseDTO>(), serviceHeader);
                // 7. Attach sector classification


                if (createResult.ErrorMessageResult != null)
                    return Ok(new
                    {
                        success = false,
                        message = "Error Posting This Loan.",
                        loanCaseId = createResult.ErrorMessageResult
                    });

                // 7. Attach collaterals
                if (collateralDocuments.Any())
                {
                    await master._channelService.UpdateLoanCollateralsByLoanCaseIdAsync(createResult.Id, new ObservableCollection<CustomerDocumentDTO>(collateralDocuments), serviceHeader);
                }

                // 8. Attach guarantors
                if (guarantors.Any())
                {
                    await master._channelService.UpdateLoanGuarantorsByLoanCaseIdAsync(createResult.Id, new ObservableCollection<LoanGuarantorDTO>(guarantors), serviceHeader);
                }
     //           string message =
     //$"Dear {customer.IndividualFirstName} {customer.IndividualLastName}, " +
     //$"your loan application of KES {loanCaseDTO.AmountApplied:N0} has been successfully registered and is currently under review. " +
     //$"We will notify you once processing is complete.";
                //await SmsHelper.SendMessageAsync(customer.AddressMobileLine, message);

                return Ok(new
                {
                    success = true,
                    message = "Loan created successfully.",
                    loanCaseId = createResult.Id
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        [HttpGet]
        [Route("GetAllLoanByMemberNo")]
        public async Task<IHttpActionResult> GetAllLoanByMemberNo(string MemberNo)
        {
            var serviceHeader = master.GetServiceHeader();

            var pageInfo = await master._channelService.FindLoanCasesAsync(serviceHeader);
            var loanCase = pageInfo?
                .Where(c =>
                    c.CustomerReference2 == MemberNo &&
                    c.Status == (int)LoanCaseStatus.Deferred)
                .OrderByDescending(c => c.CreatedDate) // or ApprovedOn / Id
                .FirstOrDefault();
            if (loanCase == null)
                return BadRequest("Loans Not Found.");

            return Ok(loanCase);
        }
        [HttpPost]
        [Route("UpdateLoanCase")]
        public async Task<IHttpActionResult> UpdateLoanCase(LoanCaseDTO2 loanCaseDTO)
        {
            if (loanCaseDTO == null)
                return BadRequest("Invalid payload.");

            var serviceHeader = master.GetServiceHeader();

            // Fetch core entities
            var loanProduct = await master._channelService
                .FindLoanProductAsync(loanCaseDTO.LoanProductId, serviceHeader);

            if (loanProduct == null)
                return BadRequest("Loan product not found.");

            var loanCase = await master._channelService
                .FindLoanCaseAsync(loanCaseDTO.Id, serviceHeader);

            if (loanCase == null)
                return BadRequest("Loan case not found.");

            // Normalize guarantors
            var guarantors = loanCaseDTO.Guarantors ?? new List<LoanGuarantorDTO>();

            // ===== Business Rules =====

            if (guarantors.Count < loanProduct.LoanRegistrationMinimumGuarantors)
                return BadRequest(
                    $"Loan product requires at least {loanProduct.LoanRegistrationMinimumGuarantors} guarantors."
                );

            var guaranteedSum = guarantors.Sum(g => g.AmountGuaranteed);

            if (guaranteedSum < loanCaseDTO.AmountApplied)
                return BadRequest("Total guaranteed amount is insufficient to cover the applied loan amount.");

            // ===== State Mutation =====

            loanCase.AmountApplied = loanCaseDTO.AmountApplied;
            loanCase.LoanRegistrationTermInMonths = loanCaseDTO.LoanRegistrationTermInMonths;
            loanCase.Status = (int)LoanCaseStatus.Registered;

            // ===== Persistence =====

            var loanUpdated = await master._channelService
                .UpdateLoanCaseAsync(loanCase, serviceHeader);

            if (!loanUpdated)
                return BadRequest("Loan case update failed.");

            var guarantorsUpdated = await master._channelService
                .UpdateLoanGuarantorsByLoanCaseIdAsync(
                    loanCase.Id,
                    new ObservableCollection<LoanGuarantorDTO>(guarantors),
                    serviceHeader
                );

            if (!guarantorsUpdated)
                return BadRequest("Guarantor update failed.");

            // ===== Response =====

            return Ok(new
            {
                success = true,
                loanCaseReference = loanCase.Reference
            });
        }


        [HttpGet]
        [Route("GetCustomerShareStatement/{customerAccountId}")]
        public async Task<HttpResponseMessage> GetCustomerShareStatement(Guid customerAccountId, DateTime? startDate = null, DateTime? endDate = null, bool downloadPdf = false)
        {
            try
            {
                // Create a simple model for SQL results
                var customerData = new
                {
                    FirstName = "",
                    LastName = "",
                    Mobile = "",
                    Email = "",
                    Reference2 = "",
                    Reference3 = "",
                    BranchCode = 0,
                    CustomerSerialNumber = 0,
                    ProductCode = 0,
                    TargetProductCode = 0
                };

                var statementRows = new List<CustomerShareStatementRow>();
                decimal totalContribution = 0;

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Get customer and account information - FIXED query
                    string customerQuery = @"
               SELECT TOP 1 
                   c.Individual_FirstName,
                   c.Individual_LastName,
                   c.Address_MobileLine,
                   c.Address_Email,
                   c.Reference2,
                   c.Reference3,
                   b.Code as BranchCode,
                   c.SerialNumber as CustomerSerialNumber,
                   ca.CustomerAccountType_ProductCode,
                   ca.CustomerAccountType_TargetProductCode
               FROM [SwiftFinancialsDB_Live].[dbo].[swiftFin_CustomerAccounts] ca
               INNER JOIN [SwiftFinancialsDB_Live].[dbo].[swiftFin_Customers] c 
                   ON ca.CustomerId = c.Id
               INNER JOIN [SwiftFinancialsDB_Live].[dbo].[swiftFin_Branches] b
                   ON ca.BranchId = b.Id
               WHERE ca.Id = @CustomerAccountId";

                    using (var cmd = new SqlCommand(customerQuery, connection))
                    {
                        cmd.Parameters.Add("@CustomerAccountId", SqlDbType.UniqueIdentifier).Value = customerAccountId;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                customerData = new
                                {
                                    FirstName = reader["Individual_FirstName"]?.ToString() ?? "",
                                    LastName = reader["Individual_LastName"]?.ToString() ?? "",
                                    Mobile = reader["Address_MobileLine"]?.ToString() ?? "",
                                    Email = reader["Address_Email"]?.ToString() ?? "",
                                    Reference2 = reader["Reference2"]?.ToString() ?? "",
                                    Reference3 = reader["Reference3"]?.ToString() ?? "",
                                    BranchCode = Convert.ToInt32(reader["BranchCode"]),
                                    CustomerSerialNumber = Convert.ToInt32(reader["CustomerSerialNumber"]),
                                    ProductCode = Convert.ToInt32(reader["CustomerAccountType_ProductCode"]),
                                    TargetProductCode = Convert.ToInt32(reader["CustomerAccountType_TargetProductCode"])
                                };
                            }
                            else
                            {
                                // Customer not found
                                var response = Request.CreateResponse(HttpStatusCode.NotFound);
                                response.Content = new StringContent(
                                    JsonConvert.SerializeObject(new ApiResponse<object>
                                    {
                                        Success = false,
                                        Message = "Customer account not found.",
                                        Data = null
                                    }),
                                    Encoding.UTF8,
                                    "application/json");
                                return response;
                            }
                        }
                    }

                    // Now get share statement
                    using (var command = new SqlCommand("usp_GetCustomerShareStatement", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Add parameters
                        command.Parameters.Add("@CustomerAccountId", SqlDbType.UniqueIdentifier).Value = customerAccountId;
                        command.Parameters.Add("@StartDate", SqlDbType.Date).Value = (object)startDate ?? DBNull.Value;
                        command.Parameters.Add("@EndDate", SqlDbType.Date).Value = (object)endDate ?? DBNull.Value;

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            // First result set: Statement rows
                            while (await reader.ReadAsync())
                            {
                                var row = new CustomerShareStatementRow
                                {
                                    Date = reader["Date"].ToString(),
                                    ShareContribution = Convert.ToDecimal(reader["Share Contribution"]),
                                    Cumulative = Convert.ToDecimal(reader["Cumulative"]),
                                    Description = reader["Description"].ToString()
                                };
                                statementRows.Add(row);
                            }

                            // Move to second result set: Total contribution
                            if (await reader.NextResultAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    totalContribution = reader["TotalContribution"] != DBNull.Value ?
                                        Convert.ToDecimal(reader["TotalContribution"]) : 0;
                                }
                            }
                        }
                    }
                }

                // Build the full account number using the same logic as in the DTO
                string fullAccountNumber = string.Format("{0}-{1}-{2}-{3}",
                    customerData.BranchCode.ToString().PadLeft(3, '0'),
                    customerData.CustomerSerialNumber.ToString().PadLeft(7, '0'),
                    customerData.ProductCode.ToString().PadLeft(3, '0'),
                    customerData.TargetProductCode.ToString().PadLeft(3, '0'));

                // Create the result object with customer info
                var shareStatementResult = new
                {
                    Customer = new
                    {
                        FullName = $"{customerData.FirstName} {customerData.LastName}".Trim(),
                        AccountNumber = fullAccountNumber,
                        StaffNo = customerData.Reference2,
                        PFNumber = customerData.Reference3,
                        Mobile = customerData.Mobile,
                        Email = customerData.Email
                    },
                    Statement = statementRows,
                    TotalContribution = totalContribution
                };

                // If PDF download is requested
                if (downloadPdf)
                {
                    byte[] pdfBytes = GenerateShareStatementPdf(customerData, fullAccountNumber, statementRows, totalContribution, startDate, endDate);

                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(pdfBytes)
                    };

                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

                    string customerName = $"{customerData.FirstName}_{customerData.LastName}".Replace(" ", "_");
                    response.Content.Headers.ContentDisposition =
                        new ContentDispositionHeaderValue("attachment")
                        {
                            FileName = $"ShareStatement_{customerName}_{DateTime.Now:yyyyMMdd}.pdf"
                        };

                    return response;
                }
                else
                {
                    // Return JSON response
                    var response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new StringContent(
                        JsonConvert.SerializeObject(new ApiResponse<object>
                        {
                            Success = true,
                            Message = statementRows.Count > 0 ?
                                $"Share statement retrieved successfully. Total: {totalContribution:C}" :
                                "No transactions found for the given period.",
                            Data = shareStatementResult
                        }),
                        Encoding.UTF8,
                        "application/json");
                    return response;
                }
            }
            catch (Exception ex)
            {
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent(
                    JsonConvert.SerializeObject(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "An error occurred while retrieving customer share statement.",
                        Data = ex.Message
                    }),
                    Encoding.UTF8,
                    "application/json");
                return response;
            }
        }
        #region GenerateShareStatementPdf
        private byte[] GenerateShareStatementPdf(dynamic customerData, string fullAccountNumber,
 List<CustomerShareStatementRow> statementRows, decimal totalContribution,
 DateTime? startDate = null, DateTime? endDate = null)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Create document with smaller margins for better space utilization
                Document document = new Document(PageSize.A4, 30, 30, 50, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);

                document.Open();

                // ===== RUBANI SACCO COLOR THEME =====
                BaseColor SkyBlue = new BaseColor(0, 174, 239); // #00AEEF
                BaseColor Red = new BaseColor(255, 0, 0);       // #FF0000
                BaseColor DarkGray = new BaseColor(26, 26, 26); // #1A1A1A
                BaseColor LightGray = new BaseColor(217, 217, 217); // #D9D9D9
                BaseColor White = BaseColor.WHITE;
                BaseColor TableHeaderBlue = new BaseColor(173, 216, 230); // Light blue for table headers

                // Fonts using Rubani Sacco theme
                Font titleFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, DarkGray));
                Font headerFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, DarkGray));
                Font subHeaderFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, White));
                Font normalFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA, 9));
                Font boldFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9));
                Font smallFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA, 8, DarkGray));
                Font amountFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, DarkGray));

                // ===== CUSTOM HEADER WITH RUBANI SACCO LOGO =====
                try
                {
                    // Create a table with 1 column for logo on top, then company info below
                    PdfPTable headerTable = new PdfPTable(1)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 10f
                    };

                    // Row 1: Logo centered at top
                    PdfPCell logoCell = new PdfPCell();
                    logoCell.Border = Rectangle.NO_BORDER;
                    logoCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    logoCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    logoCell.PaddingBottom = 5f;

                    // Try to load logo from local path
                    string logoPath = @"C:/Users/Karenju/Desktop/testapidebug/Assets/Images/rubani-logo.jpeg";
                    if (File.Exists(logoPath))
                    {
                        try
                        {
                            Image logo = Image.GetInstance(logoPath);
                            logo.ScaleToFit(100, 100); // Increased size for better visibility
                            logoCell.AddElement(logo);
                        }
                        catch (Exception)
                        {
                            // Fallback to text if image fails to load
                            logoCell.AddElement(new Paragraph("RUBANI SACCO",
                                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, SkyBlue))
                            {
                                Alignment = Element.ALIGN_CENTER
                            });
                        }
                    }
                    else
                    {
                        // Use text if no logo file
                        logoCell.AddElement(new Paragraph("RUBANI SACCO",
                            FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, SkyBlue))
                        {
                            Alignment = Element.ALIGN_CENTER
                        });
                    }

                    headerTable.AddCell(logoCell);

                    // Row 2: Company Info - LEFT ALIGNED BELOW LOGO
                    PdfPCell infoCell = new PdfPCell();
                    infoCell.Border = Rectangle.NO_BORDER;
                    infoCell.HorizontalAlignment = Element.ALIGN_LEFT;
                    infoCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    infoCell.PaddingTop = 5f;

                    // Company name - LEFT ALIGNED
                    var companyNamePara = new Paragraph("RUBANI SACCO",
                        FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, SkyBlue))
                    {
                        Alignment = Element.ALIGN_LEFT
                    };
                    infoCell.AddElement(companyNamePara);

                    // Address - LEFT ALIGNED
                    var address = new Paragraph("Rubani House, Off Airport North Embakasi",
                        FontFactory.GetFont(FontFactory.HELVETICA, 10))
                    {
                        Alignment = Element.ALIGN_LEFT
                    };
                    infoCell.AddElement(address);

                    // Email - LEFT ALIGNED
                    var email = new Paragraph("rubanisacco@gmail.com",
                        FontFactory.GetFont(FontFactory.HELVETICA, 10))
                    {
                        Alignment = Element.ALIGN_LEFT
                    };
                    infoCell.AddElement(email);

                    headerTable.AddCell(infoCell);

                    document.Add(headerTable);

                    // Add decorative line (Blue-Red-Blue)
                    var lineTable = new PdfPTable(3)
                    {
                        WidthPercentage = 100,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        SpacingAfter = 10f
                    };
                    lineTable.SetWidths(new float[] { 33, 34, 33 });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = SkyBlue,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = Red,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = SkyBlue,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    document.Add(lineTable);
                }
                catch (Exception)
                {
                    // Fallback header if anything goes wrong
                    var fallbackPara = new Paragraph("RUBANI SACCO\nRubani House, Off Airport North Embakasi\nrubanisacco@gmail.com",
                        FontFactory.GetFont(FontFactory.HELVETICA, 10))
                    {
                        Alignment = Element.ALIGN_LEFT,
                        SpacingAfter = 15f,
                        IndentationLeft = 0f
                    };
                    document.Add(fallbackPara);
                }

                // ===== STATEMENT TITLE =====
                document.Add(new Paragraph("SHARES STATEMENT", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 10f
                });

                // ===== MEMBER INFORMATION SECTION =====
                string fullName = $"{customerData.FirstName} {customerData.LastName}".Trim().ToUpper();
                string staffNo = customerData.Reference2;
                string pfNumber = customerData.Reference3;

                // Use Paragraphs for member info (like in loan statement)
                Paragraph memberInfo = new Paragraph();
                memberInfo.Alignment = Element.ALIGN_LEFT;

                // Add member info with proper formatting
                memberInfo.Add(new Chunk("Name: ", boldFont));
                memberInfo.Add(new Chunk(fullName, normalFont));
                memberInfo.Add(new Chunk("   MemberNo: ", boldFont));
                memberInfo.Add(new Chunk(staffNo ?? "N/A", normalFont));
                memberInfo.Add(Chunk.NEWLINE);

                memberInfo.Add(new Chunk("Staff No: ", boldFont));
                memberInfo.Add(new Chunk(pfNumber ?? "N/A", normalFont));
                memberInfo.Add(new Chunk("   Account No: ", boldFont));
                memberInfo.Add(new Chunk(fullAccountNumber, normalFont));

                memberInfo.SpacingAfter = 15f;
                document.Add(memberInfo);

                // ===== STATEMENT PERIOD SECTION =====
                if (startDate.HasValue || endDate.HasValue)
                {
                    string periodText = "Statement Period: ";
                    if (startDate.HasValue && endDate.HasValue)
                        periodText += $"{startDate.Value:dd/MM/yyyy} to {endDate.Value:dd/MM/yyyy}";
                    else if (startDate.HasValue)
                        periodText += $"From {startDate.Value:dd/MM/yyyy}";
                    else if (endDate.HasValue)
                        periodText += $"To {endDate.Value:dd/MM/yyyy}";

                    var periodPara = new Paragraph(periodText, boldFont)
                    {
                        Alignment = Element.ALIGN_LEFT,
                        SpacingAfter = 10f
                    };
                    document.Add(periodPara);
                }

                // ===== SUMMARY SECTION =====
                PdfPTable summaryTable = new PdfPTable(2)
                {
                    WidthPercentage = 100,
                    SpacingAfter = 15f
                };
                summaryTable.SetWidths(new float[] { 50, 50 });

                // Summary header
                var summaryHeader = new PdfPCell(new Phrase("SUMMARY", subHeaderFont))
                {
                    BackgroundColor = DarkGray,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 8,
                    Colspan = 2,
                    Border = Rectangle.NO_BORDER,
                    BorderWidthBottom = 2f,
                    BorderColorBottom = SkyBlue
                };
                summaryTable.AddCell(summaryHeader);

                // Total Contribution row
                summaryTable.AddCell(new PdfPCell(new Phrase("Total Share Contribution:", boldFont))
                {
                    Border = Rectangle.NO_BORDER,
                    Padding = 8,
                    BackgroundColor = TableHeaderBlue
                });

                summaryTable.AddCell(new PdfPCell(new Phrase(totalContribution.ToString("N2"), amountFont))
                {
                    Border = Rectangle.NO_BORDER,
                    Padding = 8,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    BackgroundColor = TableHeaderBlue
                });

                document.Add(summaryTable);

                // ===== TRANSACTIONS SECTION =====
                if (statementRows != null && statementRows.Count > 0)
                {
                    // Section header
                    var sectionHeaderPara = new Paragraph("TRANSACTION DETAILS",
                        FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, White))
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 5f
                    };

                    // Create a background for the header
                    PdfPTable headerBgTable = new PdfPTable(1)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 10f
                    };

                    PdfPCell headerCell = new PdfPCell(sectionHeaderPara)
                    {
                        BackgroundColor = DarkGray,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 8,
                        Border = Rectangle.NO_BORDER,
                        BorderWidthBottom = 2f,
                        BorderColorBottom = SkyBlue
                    };
                    headerBgTable.AddCell(headerCell);
                    document.Add(headerBgTable);

                    // Transactions table with 4 columns - NO BORDERS
                    PdfPTable transTable = new PdfPTable(4)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 5f
                    };
                    transTable.SetWidths(new float[] { 20, 40, 20, 20 });

                    // Table headers - NO BORDERS
                    string[] headers = { "Date", "Description", "Share Contribution", "Cumulative" };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        PdfPCell headerCellItem = new PdfPCell(new Phrase(headers[i], headerFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            Padding = 5,
                            // NO BORDERS - remove all border widths
                            BorderWidthTop = 0f,
                            BorderWidthBottom = 0f,
                            BorderWidthLeft = 0f,
                            BorderWidthRight = 0f
                        };
                        transTable.AddCell(headerCellItem);
                    }

                    // Add transactions with NO BORDERS between rows
                    for (int rowIndex = 0; rowIndex < statementRows.Count; rowIndex++)
                    {
                        var row = statementRows[rowIndex];

                        // Date cell - NO BORDERS
                        PdfPCell dateCell = new PdfPCell(new Phrase(row.Date, normalFont));
                        dateCell.HorizontalAlignment = Element.ALIGN_CENTER;
                        dateCell.Padding = 5f;
                        // NO BORDERS - remove all border widths
                        dateCell.BorderWidthTop = 0f;
                        dateCell.BorderWidthBottom = 0f;
                        dateCell.BorderWidthLeft = 0f;
                        dateCell.BorderWidthRight = 0f;
                        transTable.AddCell(dateCell);

                        // Description cell - NO BORDERS
                        PdfPCell descCell = new PdfPCell(new Phrase(row.Description ?? "", normalFont));
                        descCell.HorizontalAlignment = Element.ALIGN_LEFT;
                        descCell.Padding = 5f;
                        // NO BORDERS - remove all border widths
                        descCell.BorderWidthTop = 0f;
                        descCell.BorderWidthBottom = 0f;
                        descCell.BorderWidthLeft = 0f;
                        descCell.BorderWidthRight = 0f;
                        transTable.AddCell(descCell);

                        // Share Contribution cell - NO BORDERS
                        PdfPCell shareCell = new PdfPCell(new Phrase(row.ShareContribution.ToString("N2"), normalFont));
                        shareCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                        shareCell.Padding = 5f;
                        // NO BORDERS - remove all border widths
                        shareCell.BorderWidthTop = 0f;
                        shareCell.BorderWidthBottom = 0f;
                        shareCell.BorderWidthLeft = 0f;
                        shareCell.BorderWidthRight = 0f;
                        transTable.AddCell(shareCell);

                        // Cumulative cell - NO BORDERS
                        PdfPCell cumulativeCell = new PdfPCell(new Phrase(row.Cumulative.ToString("N2"), normalFont));
                        cumulativeCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                        cumulativeCell.Padding = 5f;
                        // NO BORDERS - remove all border widths
                        cumulativeCell.BorderWidthTop = 0f;
                        cumulativeCell.BorderWidthBottom = 0f;
                        cumulativeCell.BorderWidthLeft = 0f;
                        cumulativeCell.BorderWidthRight = 0f;
                        transTable.AddCell(cumulativeCell);
                    }

                    document.Add(transTable);
                }
                else
                {
                    var noTransPara = new Paragraph("No transactions found for the selected period.", normalFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingBefore = 20f,
                        SpacingAfter = 20f
                    };
                    document.Add(noTransPara);
                }

                // ===== GRAND TOTAL SECTION =====
                document.Add(new Paragraph("\n"));
                PdfPTable grandTotalTable = new PdfPTable(2)
                {
                    WidthPercentage = 100,
                    SpacingAfter = 20f
                };
                grandTotalTable.SetWidths(new float[] { 70, 30 });

                grandTotalTable.AddCell(new PdfPCell(new Phrase("GRAND TOTAL SHARE CONTRIBUTION:",
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, DarkGray)))
                {
                    Border = Rectangle.NO_BORDER,
                    Padding = 10,
                    BackgroundColor = TableHeaderBlue
                });

                grandTotalTable.AddCell(new PdfPCell(new Phrase(totalContribution.ToString("N2"),
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, Red)))
                {
                    Border = Rectangle.NO_BORDER,
                    Padding = 10,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    BackgroundColor = TableHeaderBlue
                });

                document.Add(grandTotalTable);

                // ===== CUSTOM FOOTER =====
                document.Add(new Paragraph("\n"));
                var footerPara = new Paragraph(
                    $"Statement Generated on: {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Page: 1",
                    smallFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 10f
                };
                document.Add(footerPara);

                // ===== FOOTER NOTES =====
                document.Add(new Paragraph("\n"));
                document.Add(new Paragraph("This is a system generated statement.", smallFont)
                {
                    Alignment = Element.ALIGN_CENTER
                });
                document.Add(new Paragraph("For any queries, contact: rubanisacco@gmail.com", smallFont)
                {
                    Alignment = Element.ALIGN_CENTER
                });

                document.Close();
                writer.Close();

                return ms.ToArray();
            }
        }

        // Updated helper method with NO borders at all
        private PdfPCell CreateShareStyledCell(string text, Font font, int alignment = Element.ALIGN_LEFT)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text ?? "", font));
            cell.HorizontalAlignment = alignment;
            cell.Padding = 5f;

            // REMOVE ALL BORDERS
            cell.BorderWidthLeft = 0f;
            cell.BorderWidthRight = 0f;
            cell.BorderWidthTop = 0f;
            cell.BorderWidthBottom = 0f;

            return cell;
        }

        public class SasraForm6Row
        {
            public string ReportSection { get; set; }
            public string LineItem { get; set; }
            public decimal? Amount { get; set; }
            public int DisplayOrder { get; set; }
        }

        public class SasraForm6Meta
        {
            public string SaccoName { get; set; }
            public DateTime FiscalStartDate { get; set; }
            public DateTime PeriodEndingDate { get; set; }
            public DateTime GeneratedDate { get; set; }
        }

        public class SasraReportRow
        {
            public string ReportSection { get; set; }
            public string LineItem { get; set; }
            public decimal? Amount { get; set; }
            public int DisplayOrder { get; set; }
        }

        public class SasraReportMeta
        {
            public string SaccoName { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public DateTime GeneratedDate { get; set; }
        }

        public class SasraForm5Row
        {
            public string RefNo { get; set; }
            public string Description { get; set; }
            public decimal Amount { get; set; }
        }

        public class SasraFormMeta
        {
            public string SaccoName { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public DateTime GeneratedDate { get; set; }
        }
        #endregion


        [HttpGet]
        [Route("GetMemberStatementByReference2/{reference2}")]
        public async Task<HttpResponseMessage> GetMemberStatementByReference2(
    string reference2,
    DateTime? startDate = null,
    DateTime? endDate = null,
    bool downloadPdf = false)
        {
            try
            {
                // Step 1: Get the customer ID from reference2
                var customerId = await GetCustomerIdByReference2(reference2);

                if (!customerId.HasValue)
                {
                    var notFoundResponse = Request.CreateResponse(HttpStatusCode.NotFound);
                    notFoundResponse.Content = new StringContent(
                        JsonConvert.SerializeObject(new ApiResponse<object>
                        {
                            Success = false,
                            Message = $"No customer found with Reference2/Member Number: '{reference2}'",
                            Data = null
                        }),
                        Encoding.UTF8,
                        "application/json");
                    return notFoundResponse;
                }

                // Step 2: Call the existing method with the found customer ID
                return await GetMemberStatement(customerId.Value, startDate, endDate, downloadPdf);
            }
            catch (Exception ex)
            {
                var errorResponse = Request.CreateResponse(HttpStatusCode.InternalServerError);
                errorResponse.Content = new StringContent(
                    JsonConvert.SerializeObject(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "An error occurred while retrieving member statement by reference2.",
                        Data = ex.Message + " | Inner: " + (ex.InnerException?.Message ?? "None")
                    }),
                    Encoding.UTF8,
                    "application/json");
                return errorResponse;
            }
        }

        [HttpGet]
        [Route("GetMemberStatement/{customerId}")]
        public async Task<HttpResponseMessage> GetMemberStatement(
     Guid customerId,
     DateTime? startDate = null,
     DateTime? endDate = null,
     bool downloadPdf = false)
        {
            try
            {
                var memberStatement = new MemberStatementResult
                {
                    CustomerId = customerId,
                    StartDate = startDate,
                    EndDate = endDate
                };

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // ===== GET LOANS INFORMATION =====
                    var allLoanStatements = new List<LoanStatementResult>();

                    // Only try to get loans if the SP might return data
                    using (var loanCommand = new SqlCommand("sp_GenerateMemberLoanStatement", connection))
                    {
                        loanCommand.CommandType = CommandType.StoredProcedure;
                        loanCommand.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;

                        if (startDate.HasValue)
                            loanCommand.Parameters.Add("@StartDate", SqlDbType.Date).Value = startDate.Value.Date;
                        else
                            loanCommand.Parameters.Add("@StartDate", SqlDbType.Date).Value = DBNull.Value;

                        if (endDate.HasValue)
                            loanCommand.Parameters.Add("@EndDate", SqlDbType.Date).Value = endDate.Value.Date;
                        else
                            loanCommand.Parameters.Add("@EndDate", SqlDbType.Date).Value = DBNull.Value;

                        using (var reader = await loanCommand.ExecuteReaderAsync())
                        {
                            // Check if there are any result sets at all
                            bool hasResults = false;

                            // Process each loan (each iteration through the outer while loop is one loan)
                            do
                            {
                                // Result Set 1: Loan Header for current loan
                                if (!await reader.ReadAsync())
                                {
                                    // No more loans or no loans at all
                                    break;
                                }

                                hasResults = true;

                                var loanHeader = new
                                {
                                    LoanNumber = reader["LoanNumber"]?.ToString() ?? "",
                                    LoanProductType = reader["LoanProductType"]?.ToString() ?? "",
                                    AppliedLoanAmount = reader["AppliedLoanAmount"] != DBNull.Value ? Convert.ToDecimal(reader["AppliedLoanAmount"]) : 0m,
                                    MonthlyRepayment = reader["MonthlyRepayment"] != DBNull.Value ? Convert.ToDecimal(reader["MonthlyRepayment"]) : 0m,
                                    CustomerAccountId = reader["CustomerAccountId"] != DBNull.Value ? (Guid)reader["CustomerAccountId"] : Guid.Empty,
                                    MemberNumber = reader["MemberNumber"]?.ToString() ?? "",
                                    DisbursedDate = reader["DisbursedDate"] != DBNull.Value ?
                                        Convert.ToDateTime(reader["DisbursedDate"]).ToString("yyyy-MM-dd") : ""
                                };

                                var statementRows = new List<LoanStatementRow>();
                                var summary = new LoanSummary();
                                DateTime? statementStartDate = null;
                                DateTime? statementEndDate = null;

                                // Result Set 2: Statement rows for this loan
                                if (await reader.NextResultAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        var row = new LoanStatementRow
                                        {
                                            TransDate = reader["TransDate"] != DBNull.Value ?
                                                Convert.ToDateTime(reader["TransDate"]).ToString("yyyy-MM-dd") : "",
                                            OpeningBalance = reader["OpeningBalance"] != DBNull.Value ?
                                                Convert.ToDecimal(reader["OpeningBalance"]) : 0m,
                                            Principle = reader["Principle"] != DBNull.Value ?
                                                Convert.ToDecimal(reader["Principle"]) : 0m,
                                            Interest = reader["Interest"] != DBNull.Value ?
                                                Convert.ToDecimal(reader["Interest"]) : 0m,
                                            Amount = reader["Amount"] != DBNull.Value ?
                                                Convert.ToDecimal(reader["Amount"]) : 0m,
                                            LoanBalance = reader["LoanBalance"] != DBNull.Value ?
                                                Convert.ToDecimal(reader["LoanBalance"]) : 0m,
                                            PostingDate = reader["TransDate"] != DBNull.Value ?
                                                Convert.ToDateTime(reader["TransDate"]).ToString("yyyy-MM-dd") : "",
                                            Balance = reader["LoanBalance"] != DBNull.Value ?
                                                Convert.ToDecimal(reader["LoanBalance"]) : 0m
                                        };
                                        statementRows.Add(row);
                                    }
                                }

                                // Result Set 3: Summary for this loan
                                if (await reader.NextResultAsync())
                                {
                                    if (await reader.ReadAsync())
                                    {
                                        summary = new LoanSummary
                                        {
                                            TotalDisbursed = reader["TotalDisbursed"] != DBNull.Value ? Convert.ToDecimal(reader["TotalDisbursed"]) : 0m,
                                            TotalPrincipalRepaid = reader["TotalPrincipalPaid"] != DBNull.Value ? Convert.ToDecimal(reader["TotalPrincipalPaid"]) : 0m,
                                            TotalInterestPaid = reader["TotalInterestPaid"] != DBNull.Value ? Convert.ToDecimal(reader["TotalInterestPaid"]) : 0m,
                                            TotalInterestAccrued = reader["TotalInterestCharged"] != DBNull.Value ? Convert.ToDecimal(reader["TotalInterestCharged"]) : 0m,
                                            OutstandingLoanAmount = reader["OutstandingPrincipal"] != DBNull.Value ? Convert.ToDecimal(reader["OutstandingPrincipal"]) : 0m,
                                            OutstandingLoanInterest = reader["OutstandingInterest"] != DBNull.Value ? Convert.ToDecimal(reader["OutstandingInterest"]) : 0m,
                                            TotalOutstandingBalance = reader["TotalOutstandingBalance"] != DBNull.Value ? Convert.ToDecimal(reader["TotalOutstandingBalance"]) : 0m,
                                            OpeningBalance = reader["OpeningBalance"] != DBNull.Value ? Convert.ToDecimal(reader["OpeningBalance"]) : 0m
                                        };

                                        if (reader["StartDate"] != DBNull.Value)
                                            statementStartDate = Convert.ToDateTime(reader["StartDate"]);
                                        if (reader["EndDate"] != DBNull.Value)
                                            statementEndDate = Convert.ToDateTime(reader["EndDate"]);
                                    }
                                }

                                // Get customer details for this loan
                                var customerData = await GetCustomerDetails(connection, loanHeader.CustomerAccountId, customerId);

                                // Build the full account number
                                string fullAccountNumber = string.Format("{0}-{1}-{2}-{3}",
                                    customerData.BranchCode.ToString().PadLeft(3, '0'),
                                    customerData.CustomerSerialNumber.ToString().PadLeft(7, '0'),
                                    customerData.ProductCode.ToString().PadLeft(3, '0'),
                                    customerData.TargetProductCode.ToString().PadLeft(3, '0'));

                                // Create the loan statement result
                                var loanStatementResult = new LoanStatementResult
                                {
                                    LoanNumber = loanHeader.LoanNumber,
                                    Customer = new CustomerInfo
                                    {
                                        FullName = $"{customerData.FirstName} {customerData.LastName}".Trim(),
                                        AccountNumber = fullAccountNumber,
                                        StaffNo = customerData.Reference2,
                                        PFNumber = customerData.Reference3,
                                        Mobile = customerData.Mobile,
                                        Email = customerData.Email
                                    },
                                    LoanDetails = new LoanDetails
                                    {
                                        LoanNumber = loanHeader.LoanNumber,
                                        LoanProductType = loanHeader.LoanProductType,
                                        AppliedAmount = loanHeader.AppliedLoanAmount,
                                        MonthlyRepayment = loanHeader.MonthlyRepayment,
                                        MemberNumber = loanHeader.MemberNumber,
                                        DisbursedDate = loanHeader.DisbursedDate
                                    },
                                    Statement = statementRows,
                                    Summary = summary,
                                    StartDate = statementStartDate,
                                    EndDate = statementEndDate
                                };

                                allLoanStatements.Add(loanStatementResult);

                                // Move to next loan's first result set (if any)
                            } while (await reader.NextResultAsync());

                            // If no loans were found, that's fine - we just continue with empty list
                        }
                    }

                    // ===== GET SHARES INFORMATION =====
                    var allSharesStatements = new List<SharesStatementResult>();

                    using (var sharesCommand = new SqlCommand("sp_GenerateAllSharesStatement", connection))
                    {
                        sharesCommand.CommandType = CommandType.StoredProcedure;
                        sharesCommand.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;

                        if (startDate.HasValue)
                            sharesCommand.Parameters.Add("@StartDate", SqlDbType.Date).Value = startDate.Value.Date;
                        else
                            sharesCommand.Parameters.Add("@StartDate", SqlDbType.Date).Value = DBNull.Value;

                        if (endDate.HasValue)
                            sharesCommand.Parameters.Add("@EndDate", SqlDbType.Date).Value = endDate.Value.Date;
                        else
                            sharesCommand.Parameters.Add("@EndDate", SqlDbType.Date).Value = DBNull.Value;

                        using (var reader = await sharesCommand.ExecuteReaderAsync())
                        {
                            // Dictionary to group transactions by account
                            var accountTransactions = new Dictionary<Guid, List<SharesTransaction>>();
                            var accountDetails = new Dictionary<Guid, (string ProductName, decimal TotalContribution)>();

                            // Check if there are any result sets
                            bool hasSharesData = false;

                            // OUTPUT 0: Account Header (first result set) - Skip it
                            if (await reader.NextResultAsync())
                            {
                                // First result set is now the Detailed Statement (OUTPUT 1)
                                while (await reader.ReadAsync())
                                {
                                    // Skip if it's a message result set
                                    if (reader.FieldCount == 1 && reader.GetName(0) == "Message")
                                        continue;

                                    hasSharesData = true;

                                    var customerAccountId = reader["CustomerAccountId"] != DBNull.Value ?
                                        (Guid)reader["CustomerAccountId"] : Guid.Empty;

                                    var transaction = new SharesTransaction
                                    {
                                        TransactionDate = reader["Date"]?.ToString() ?? "",
                                        Description = reader["Description"]?.ToString() ?? "",
                                        DepositAmount = reader["Share Contribution"] != DBNull.Value ?
                                            Convert.ToDecimal(reader["Share Contribution"]) : 0m,
                                        WithdrawalAmount = 0m,
                                        RunningBalance = reader["Cumulative"] != DBNull.Value ?
                                            Convert.ToDecimal(reader["Cumulative"]) : 0m
                                    };

                                    if (!accountTransactions.ContainsKey(customerAccountId))
                                        accountTransactions[customerAccountId] = new List<SharesTransaction>();

                                    accountTransactions[customerAccountId].Add(transaction);
                                }
                            }

                            // Move to Summary result set (OUTPUT 2)
                            if (hasSharesData && await reader.NextResultAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    // Skip if it's a message
                                    if (reader.FieldCount == 1 && reader.GetName(0) == "Message")
                                        continue;

                                    var customerAccountId = reader["CustomerAccountId"] != DBNull.Value ?
                                        (Guid)reader["CustomerAccountId"] : Guid.Empty;
                                    var productName = reader["ProductName"]?.ToString() ?? "";
                                    var totalContribution = reader["TotalContribution"] != DBNull.Value ?
                                        Convert.ToDecimal(reader["TotalContribution"]) : 0m;

                                    accountDetails[customerAccountId] = (productName, totalContribution);
                                }
                            }

                            // Skip the third result set (Summary Stats - OUTPUT 3) if it exists
                            if (hasSharesData)
                            {
                                await reader.NextResultAsync();
                            }

                            // Create shares statement results for each account
                            foreach (var account in accountDetails)
                            {
                                var transactions = accountTransactions.ContainsKey(account.Key)
                                    ? accountTransactions[account.Key]
                                    : new List<SharesTransaction>();

                                // Calculate summary values from transactions
                                decimal openingBalance = 0m;
                                decimal totalDeposits = transactions.Sum(t => t.DepositAmount);
                                decimal closingBalance = transactions.Any()
                                    ? transactions.Last().RunningBalance
                                    : 0m;

                                // Use the TotalContribution from the SP for the summary
                                decimal actualTotalContribution = account.Value.TotalContribution;

                                // Create shares statement result
                                var sharesStatementResult = new SharesStatementResult
                                {
                                    StatementType = "SHARES/SAVINGS STATEMENT",
                                    ProductName = account.Value.ProductName,
                                    AccountType = "Share Account",
                                    ProductCode = 0,
                                    Period = $"{(startDate.HasValue ? startDate.Value.ToString("dd/MM/yyyy") : "Beginning")} to {(endDate.HasValue ? endDate.Value.ToString("dd/MM/yyyy") : DateTime.Now.ToString("dd/MM/yyyy"))}",
                                    OpeningBalance = openingBalance,
                                    TotalDeposits = totalDeposits,
                                    TotalWithdrawals = 0m,
                                    ClosingBalance = closingBalance,
                                    Transactions = transactions,
                                    Summary = new SharesAccountSummary
                                    {
                                        AccountName = account.Value.ProductName,
                                        AccountType = "Share Account",
                                        OpeningBalance = openingBalance,
                                        TotalDeposits = actualTotalContribution,
                                        TotalWithdrawals = 0m,
                                        ClosingBalance = closingBalance,
                                        NetMovement = actualTotalContribution
                                    }
                                };

                                allSharesStatements.Add(sharesStatementResult);
                            }
                        }
                    }

                    // Get customer info from either loans or shares, or directly from DB
                    CustomerInfo customerInfo = null;

                    // Try to get from loans first
                    if (allLoanStatements.Count > 0)
                    {
                        customerInfo = allLoanStatements.First().Customer;
                    }
                    // Then try from shares
                    else if (allSharesStatements.Count > 0)
                    {
                        // For shares, we need to get customer info separately since shares SP doesn't return it
                        customerInfo = new CustomerInfo
                        {
                            FullName = await GetCustomerName(connection, customerId),
                            AccountNumber = "N/A", // We don't have account number from shares SP
                            StaffNo = await GetCustomerStaffNo(connection, customerId),
                            Mobile = await GetCustomerMobile(connection, customerId),
                            Email = await GetCustomerEmail(connection, customerId),
                            PFNumber = await GetCustomerPFNumber(connection, customerId)
                        };
                    }
                    // If no accounts at all, still get basic customer info
                    else
                    {
                        customerInfo = new CustomerInfo
                        {
                            FullName = await GetCustomerName(connection, customerId),
                            AccountNumber = "N/A",
                            StaffNo = await GetCustomerStaffNo(connection, customerId),
                            Mobile = await GetCustomerMobile(connection, customerId),
                            Email = await GetCustomerEmail(connection, customerId),
                            PFNumber = await GetCustomerPFNumber(connection, customerId)
                        };
                    }

                    // Populate member statement
                    memberStatement.Customer = customerInfo;
                    memberStatement.LoanStatements = allLoanStatements;
                    memberStatement.SharesStatements = allSharesStatements;

                    // Calculate totals
                    memberStatement.TotalLoanBalance = allLoanStatements.Sum(l => l.Summary?.TotalOutstandingBalance ?? 0);
                    memberStatement.TotalSharesBalance = allSharesStatements.Sum(s => s.ClosingBalance);
                    memberStatement.TotalAccounts = allLoanStatements.Count + allSharesStatements.Count;

                    // Check if we found any data - but don't return 404, just return empty lists with customer info
                    if (memberStatement.LoanStatements.Count == 0 && memberStatement.SharesStatements.Count == 0)
                    {
                        // Return success with empty data and appropriate message
                        var emptyResponse = Request.CreateResponse(HttpStatusCode.OK);
                        emptyResponse.Content = new StringContent(
                            JsonConvert.SerializeObject(new ApiResponse<object>
                            {
                                Success = true,
                                Message = "No active loan or shares accounts found for this customer. Customer information is provided.",
                                Data = memberStatement
                            }),
                            Encoding.UTF8,
                            "application/json");
                        return emptyResponse;
                    }

                    // If PDF download is requested
                    if (downloadPdf)
                    {
                        byte[] pdfBytes = GenerateMemberStatementPdf(memberStatement, startDate, endDate);

                        var response = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new ByteArrayContent(pdfBytes)
                        };

                        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

                        string customerName = memberStatement.Customer?.FullName?.Replace(" ", "_") ?? "Customer";
                        string dateRange = "";
                        if (startDate.HasValue && endDate.HasValue)
                            dateRange = $"{startDate.Value:yyyyMMdd}_{endDate.Value:yyyyMMdd}";
                        else if (startDate.HasValue)
                            dateRange = $"from_{startDate.Value:yyyyMMdd}";
                        else if (endDate.HasValue)
                            dateRange = $"to_{endDate.Value:yyyyMMdd}";

                        response.Content.Headers.ContentDisposition =
                            new ContentDispositionHeaderValue("attachment")
                            {
                                FileName = $"MemberStatement_{customerName}_{dateRange}_{DateTime.Now:yyyyMMdd}.pdf"
                            };

                        return response;
                    }
                    else
                    {
                        // Return JSON response
                        var response = Request.CreateResponse(HttpStatusCode.OK);
                        string message = $"Found {memberStatement.LoanStatements.Count} loan(s) and {memberStatement.SharesStatements.Count} shares/savings account(s).";

                        response.Content = new StringContent(
                            JsonConvert.SerializeObject(new ApiResponse<object>
                            {
                                Success = true,
                                Message = message,
                                Data = memberStatement
                            }),
                            Encoding.UTF8,
                            "application/json");
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent(
                    JsonConvert.SerializeObject(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "An error occurred while retrieving member statement.",
                        Data = ex.Message + " | Inner: " + (ex.InnerException?.Message ?? "None")
                    }),
                    Encoding.UTF8,
                    "application/json");
                return response;
            }
        }
        private async Task<CustomerData> GetCustomerDetails(SqlConnection connection, Guid customerAccountId, Guid customerId)
        {
            string customerQuery = @"
        SELECT TOP 1
            c.Individual_FirstName,
            c.Individual_LastName,
            c.Address_MobileLine,
            c.Address_Email,
            c.Reference2,
            c.Reference3,
            ISNULL(b.Code, 0) as BranchCode,
            ISNULL(c.SerialNumber, 0) as CustomerSerialNumber,
            ISNULL(ca.CustomerAccountType_ProductCode, 0) as ProductCode,
            ISNULL(ca.CustomerAccountType_TargetProductCode, 0) as TargetProductCode
        FROM [SwiftFinancialsDB_Live].[dbo].[swiftFin_CustomerAccounts] ca
        INNER JOIN [SwiftFinancialsDB_Live].[dbo].[swiftFin_Customers] c
            ON ca.CustomerId = c.Id
        LEFT JOIN [SwiftFinancialsDB_Live].[dbo].[swiftFin_Branches] b
            ON ca.BranchId = b.Id
        WHERE (ca.Id = @CustomerAccountId OR c.Id = @CustomerId)
        ORDER BY ca.CreatedDate DESC";

            using (var cmd = new SqlCommand(customerQuery, connection))
            {
                cmd.Parameters.Add("@CustomerAccountId", SqlDbType.UniqueIdentifier).Value = customerAccountId;
                cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new CustomerData
                        {
                            FirstName = reader["Individual_FirstName"]?.ToString() ?? "",
                            LastName = reader["Individual_LastName"]?.ToString() ?? "",
                            Mobile = reader["Address_MobileLine"]?.ToString() ?? "",
                            Email = reader["Address_Email"]?.ToString() ?? "",
                            Reference2 = reader["Reference2"]?.ToString() ?? "",
                            Reference3 = reader["Reference3"]?.ToString() ?? "",
                            BranchCode = reader["BranchCode"] != DBNull.Value ? Convert.ToInt32(reader["BranchCode"]) : 0,
                            CustomerSerialNumber = reader["CustomerSerialNumber"] != DBNull.Value ? Convert.ToInt32(reader["CustomerSerialNumber"]) : 0,
                            ProductCode = reader["ProductCode"] != DBNull.Value ? Convert.ToInt32(reader["ProductCode"]) : 0,
                            TargetProductCode = reader["TargetProductCode"] != DBNull.Value ? Convert.ToInt32(reader["TargetProductCode"]) : 0
                        };
                    }
                }
            }

            // Return default if no customer found
            return new CustomerData();
        }

        private async Task<string> GetCustomerName(SqlConnection connection, Guid customerId)
        {
            string query = @"
        SELECT 
            CASE 
                WHEN Type = 1
                THEN CONCAT(ISNULL(Individual_FirstName, ''), ' ', ISNULL(Individual_LastName, ''))
                ELSE ISNULL(NonIndividual_Description, '')
            END AS FullName
        FROM swiftFin_Customers
        WHERE Id = @CustomerId";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;

                var result = await cmd.ExecuteScalarAsync();
                return result?.ToString()?.Trim() ?? "Customer";
            }
        }

        private async Task<string> GetCustomerStaffNo(SqlConnection connection, Guid customerId)
        {
            string query = @"
        SELECT Reference2 
        FROM swiftFin_Customers 
        WHERE Id = @CustomerId";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;
                return (await cmd.ExecuteScalarAsync())?.ToString() ?? "N/A";
            }
        }

        private async Task<string> GetCustomerMobile(SqlConnection connection, Guid customerId)
        {
            string query = @"
        SELECT Address_MobileLine 
        FROM swiftFin_Customers 
        WHERE Id = @CustomerId";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;
                return (await cmd.ExecuteScalarAsync())?.ToString() ?? "N/A";
            }
        }

        private async Task<string> GetCustomerEmail(SqlConnection connection, Guid customerId)
        {
            string query = @"
        SELECT Address_Email 
        FROM swiftFin_Customers 
        WHERE Id = @CustomerId";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;
                return (await cmd.ExecuteScalarAsync())?.ToString() ?? "N/A";
            }
        }

        private async Task<string> GetCustomerPFNumber(SqlConnection connection, Guid customerId)
        {
            string query = @"
        SELECT Reference3 
        FROM swiftFin_Customers 
        WHERE Id = @CustomerId";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;
                return (await cmd.ExecuteScalarAsync())?.ToString() ?? "N/A";
            }
        }

        public class MemberStatementResult
        {
            public Guid CustomerId { get; set; }
            public CustomerInfo Customer { get; set; }
            public List<LoanStatementResult> LoanStatements { get; set; } = new List<LoanStatementResult>();
            public List<SharesStatementResult> SharesStatements { get; set; } = new List<SharesStatementResult>();
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public decimal TotalLoanBalance { get; set; }
            public decimal TotalSharesBalance { get; set; }
            public int TotalAccounts { get; set; }
        }

        public class SharesStatementResult
        {
            public string StatementType { get; set; }
            public string ProductName { get; set; }
            public string AccountType { get; set; }
            public int ProductCode { get; set; }
            public string Period { get; set; }
            public decimal OpeningBalance { get; set; }
            public decimal TotalDeposits { get; set; }
            public decimal TotalWithdrawals { get; set; }
            public decimal ClosingBalance { get; set; }
            public List<SharesTransaction> Transactions { get; set; } = new List<SharesTransaction>();
            public SharesAccountSummary Summary { get; set; }
        }

        public class SharesTransaction
        {
            public string TransactionDate { get; set; }
            public string Description { get; set; }
            public decimal DepositAmount { get; set; }
            public decimal WithdrawalAmount { get; set; }
            public decimal RunningBalance { get; set; }
        }

        public class SharesAccountSummary
        {
            public string AccountName { get; set; }
            public string AccountType { get; set; }
            public decimal OpeningBalance { get; set; }
            public decimal TotalDeposits { get; set; }
            public decimal TotalWithdrawals { get; set; }
            public decimal ClosingBalance { get; set; }
            public decimal NetMovement { get; set; }
        }

        private byte[] GenerateMemberStatementPdf(MemberStatementResult memberStatement, DateTime? startDate = null, DateTime? endDate = null)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Create document with same margins as individual statements
                Document document = new Document(PageSize.A4, 30, 30, 50, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);

                document.Open();

                // ===== RUBANI SACCO COLOR THEME =====
                BaseColor SkyBlue = new BaseColor(0, 174, 239); // #00AEEF
                BaseColor Red = new BaseColor(255, 0, 0);       // #FF0000
                BaseColor Green = new BaseColor(0, 150, 0);     // #009600
                BaseColor DarkGray = new BaseColor(26, 26, 26); // #1A1A1A
                BaseColor LightGray = new BaseColor(217, 217, 217); // #D9D9D9
                BaseColor MediumGray = new BaseColor(128, 128, 128); // #808080 - Added for section headers
                BaseColor White = BaseColor.WHITE;

                // ===== FONTS - BOOK ANTIQUA FONT WITH SIZE 11 =====
                // Load Book Antiqua font (make sure it's available on your system)
                // You may need to install Book Antiqua font or use a different serif font
                string bookAntiquaFontName = "Book Antiqua";

                // Try to create Book Antiqua fonts, fallback to Times if not available
                Font titleFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 16f, Font.BOLD, DarkGray);
                Font normalFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.NORMAL, DarkGray);
                Font boldFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.BOLD, DarkGray);
                Font smallFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 9f, Font.NORMAL, DarkGray);
                Font companyNameFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 14f, Font.BOLD, SkyBlue);
                Font companyInfoFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.NORMAL, DarkGray);
                Font sectionHeaderFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12f, Font.BOLD, SkyBlue);
                Font tableHeaderFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 9f, Font.BOLD, DarkGray);
                Font tableCellFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 10f, Font.NORMAL, DarkGray);

                // Fallback fonts if Book Antiqua is not available
                try
                {
                    // Test if Book Antiqua is available
                    var testFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f);
                }
                catch
                {
                    // Fallback to Times New Roman if Book Antiqua is not available
                    bookAntiquaFontName = "Times New Roman";
                    titleFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 16f, Font.BOLD, DarkGray);
                    normalFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.NORMAL, DarkGray);
                    boldFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.BOLD, DarkGray);
                    smallFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 9f, Font.NORMAL, DarkGray);
                    companyNameFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 14f, Font.BOLD, SkyBlue);
                    companyInfoFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.NORMAL, DarkGray);
                    sectionHeaderFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12f, Font.BOLD, SkyBlue);
                    tableHeaderFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 9f, Font.BOLD, DarkGray);
                    tableCellFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 10f, Font.NORMAL, DarkGray);
                }

                // ===== CUSTOM HEADER WITH RUBANI SACCO LOGO =====
                try
                {
                    // Create a table with 1 column for left-aligned content
                    PdfPTable headerTable = new PdfPTable(1)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 8f
                    };

                    // Row 1: Logo left-aligned at top
                    PdfPCell logoCell = new PdfPCell();
                    logoCell.Border = Rectangle.NO_BORDER;
                    logoCell.HorizontalAlignment = Element.ALIGN_LEFT;
                    logoCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    logoCell.PaddingBottom = 3f;

                    // Try to load logo from local path
                    string logoPath = @"C:\Users\ADMIN\source\repos\SwiftFinancialsNew\SwiftFinancialsSolution\TestApis\Assets\Images\rubani-logo.jpeg";
                    if (File.Exists(logoPath))
                    {
                        try
                        {
                            Image logo = Image.GetInstance(logoPath);
                            logo.ScaleToFit(100, 100);
                            logoCell.AddElement(logo);
                        }
                        catch (Exception)
                        {
                            logoCell.AddElement(new Paragraph("RUBANI SACCO", companyNameFont)
                            {
                                Alignment = Element.ALIGN_LEFT
                            });
                        }
                    }
                    else
                    {
                        logoCell.AddElement(new Paragraph("RUBANI SACCO", companyNameFont)
                        {
                            Alignment = Element.ALIGN_LEFT
                        });
                    }

                    headerTable.AddCell(logoCell);

                    // Row 2: Company Info - LEFT ALIGNED
                    PdfPCell infoCell = new PdfPCell();
                    infoCell.Border = Rectangle.NO_BORDER;
                    infoCell.HorizontalAlignment = Element.ALIGN_LEFT;
                    infoCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    infoCell.PaddingTop = 3f;

                    var companyNamePara = new Paragraph("RUBANI SACCO", companyNameFont)
                    {
                        Alignment = Element.ALIGN_LEFT
                    };
                    infoCell.AddElement(companyNamePara);

                    var address = new Paragraph("Rubani House, Off Airport North Embakasi", companyInfoFont)
                    {
                        Alignment = Element.ALIGN_LEFT
                    };
                    infoCell.AddElement(address);

                    var email = new Paragraph("rubanisacco@gmail.com", companyInfoFont)
                    {
                        Alignment = Element.ALIGN_LEFT
                    };
                    infoCell.AddElement(email);

                    headerTable.AddCell(infoCell);
                    document.Add(headerTable);

                    // Add decorative line (Blue-Red-Blue)
                    var lineTable = new PdfPTable(3)
                    {
                        WidthPercentage = 100,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        SpacingAfter = 8f
                    };
                    lineTable.SetWidths(new float[] { 33, 34, 33 });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = SkyBlue,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = Red,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = SkyBlue,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    document.Add(lineTable);
                }
                catch (Exception)
                {
                    var fallbackPara = new Paragraph("RUBANI SACCO\nRubani House, Off Airport North Embakasi\nrubanisacco@gmail.com",
                        companyInfoFont)
                    {
                        Alignment = Element.ALIGN_LEFT,
                        SpacingAfter = 10f
                    };
                    document.Add(fallbackPara);
                }

                // ===== MEMBER DETAILED STATEMENT TITLE =====
                string titleText = "MEMBER DETAILED STATEMENT";
                if (startDate.HasValue || endDate.HasValue)
                {
                    titleText = "MEMBER DETAILED STATEMENT";
                    string dateRangeText = "";

                    if (startDate.HasValue && endDate.HasValue)
                        dateRangeText = $"{startDate.Value:dd/MM/yyyy} to {endDate.Value:dd/MM/yyyy}";
                    else if (startDate.HasValue)
                        dateRangeText = $"From {startDate.Value:dd/MM/yyyy}";
                    else if (endDate.HasValue)
                        dateRangeText = $"To {endDate.Value:dd/MM/yyyy}";

                    if (!string.IsNullOrEmpty(dateRangeText))
                    {
                        document.Add(new Paragraph(titleText, titleFont)
                        {
                            Alignment = Element.ALIGN_CENTER,
                            SpacingAfter = 3f
                        });

                        document.Add(new Paragraph(dateRangeText,
                            FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12f, Font.BOLD, DarkGray))
                        {
                            Alignment = Element.ALIGN_CENTER,
                            SpacingAfter = 8f
                        });
                    }
                    else
                    {
                        document.Add(new Paragraph(titleText, titleFont)
                        {
                            Alignment = Element.ALIGN_CENTER,
                            SpacingAfter = 8f
                        });
                    }
                }
                else
                {
                    document.Add(new Paragraph(titleText, titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 8f
                    });
                }

                // ===== MEMBER INFORMATION SECTION =====
                if (memberStatement.Customer != null)
                {
                    // Create a 2-column table for better alignment
                    PdfPTable memberInfoTable = new PdfPTable(2)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 10f
                    };
                    memberInfoTable.SetWidths(new float[] { 40, 60 });

                    // Left column: Name, Staff No, Mobile
                    Paragraph leftColumn = new Paragraph();
                    leftColumn.Add(new Chunk("Name: ", boldFont));
                    leftColumn.Add(new Chunk(memberStatement.Customer.FullName, normalFont));
                    leftColumn.Add(Chunk.NEWLINE);
                    leftColumn.Add(new Chunk("Staff No: ", boldFont));
                    leftColumn.Add(new Chunk(memberStatement.Customer.PFNumber ?? "N/A", normalFont));
                    leftColumn.Add(Chunk.NEWLINE);
                    leftColumn.Add(new Chunk("Mobile: ", boldFont));
                    leftColumn.Add(new Chunk(memberStatement.Customer.Mobile ?? "N/A", normalFont));

                    PdfPCell leftCell = new PdfPCell(leftColumn)
                    {
                        Border = Rectangle.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_LEFT,
                        Padding = 3
                    };
                    memberInfoTable.AddCell(leftCell);

                    // Right column: MemberNo, Account No, Email
                    Paragraph rightColumn = new Paragraph();
                    rightColumn.Add(new Chunk("MemberNo: ", boldFont));
                    rightColumn.Add(new Chunk(memberStatement.Customer.StaffNo ?? "N/A", normalFont));
                    rightColumn.Add(Chunk.NEWLINE);
                    rightColumn.Add(new Chunk("Account No: ", boldFont));
                    rightColumn.Add(new Chunk(memberStatement.Customer.AccountNumber, normalFont));
                    rightColumn.Add(Chunk.NEWLINE);
                    rightColumn.Add(new Chunk("Email: ", boldFont));
                    rightColumn.Add(new Chunk(memberStatement.Customer.Email ?? "N/A", normalFont));

                    PdfPCell rightCell = new PdfPCell(rightColumn)
                    {
                        Border = Rectangle.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_LEFT,
                        Padding = 3
                    };
                    memberInfoTable.AddCell(rightCell);

                    document.Add(memberInfoTable);
                }

                // ===== STATEMENT PERIOD SECTION =====
                if (startDate.HasValue || endDate.HasValue)
                {
                    string periodText = "Statement Period: ";
                    if (startDate.HasValue && endDate.HasValue)
                        periodText += $"{startDate.Value:dd/MM/yyyy} to {endDate.Value:dd/MM/yyyy}";
                    else if (startDate.HasValue)
                        periodText += $"From {startDate.Value:dd/MM/yyyy}";
                    else if (endDate.HasValue)
                        periodText += $"To {endDate.Value:dd/MM/yyyy}";

                    var periodPara = new Paragraph(periodText, boldFont)
                    {
                        Alignment = Element.ALIGN_LEFT,
                        SpacingAfter = 8f
                    };
                    document.Add(periodPara);
                }

                // In the PDF generation method, update the shares section:

                // ===== SHARES/SAVINGS DETAILED SECTION =====
                if (memberStatement.SharesStatements.Count > 0)
                {
                    var sharesHeader = new Paragraph("SHARES/SAVINGS STATEMENT",
                        FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12f, Font.BOLD, White))
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 3f
                    };

                    PdfPTable sharesHeaderTable = new PdfPTable(1)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 10f
                    };

                    PdfPCell sharesHeaderCell = new PdfPCell(sharesHeader)
                    {
                        BackgroundColor = MediumGray,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 6,
                        Border = Rectangle.NO_BORDER,
                        BorderWidthBottom = 2f,
                        BorderColorBottom = SkyBlue
                    };
                    sharesHeaderTable.AddCell(sharesHeaderCell);
                    document.Add(sharesHeaderTable);

                    int sharesCounter = 1;
                    foreach (var shares in memberStatement.SharesStatements)
                    {
                        // Account Header - Show product name only
                        var accountHeaderPara = new Paragraph($"ACCOUNT #{sharesCounter}: {shares.ProductName}", sectionHeaderFont)
                        {
                            Alignment = Element.ALIGN_LEFT,
                            SpacingAfter = 8f
                        };
                        document.Add(accountHeaderPara);

                        // Transactions Section - Show ALL transactions (no limit)
                        if (shares.Transactions.Count > 0)
                        {
                            // Use all transactions, not just first 5
                            var allTransactions = shares.Transactions;

                            PdfPTable transTable = new PdfPTable(5)
                            {
                                WidthPercentage = 100,
                                SpacingAfter = 5f
                            };
                            transTable.SetWidths(new float[] { 20, 35, 15, 15, 15 });

                            // Table headers - using tableHeaderFont
                            PdfPCell dateHeaderCell = new PdfPCell(new Phrase("Date", tableHeaderFont))
                            {
                                BackgroundColor = LightGray,
                                HorizontalAlignment = Element.ALIGN_CENTER,
                                Padding = 4,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthTop = 1f,
                                BorderWidthBottom = 1f,
                                BorderWidthLeft = 1f,
                                BorderWidthRight = 0f,
                                BorderColorTop = DarkGray,
                                BorderColorBottom = DarkGray,
                                BorderColorLeft = DarkGray
                            };
                            transTable.AddCell(dateHeaderCell);

                            PdfPCell descHeaderCell = new PdfPCell(new Phrase("Description", tableHeaderFont))
                            {
                                BackgroundColor = LightGray,
                                HorizontalAlignment = Element.ALIGN_CENTER,
                                Padding = 4,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthTop = 1f,
                                BorderWidthBottom = 1f,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderColorTop = DarkGray,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(descHeaderCell);

                            PdfPCell depositHeaderCell = new PdfPCell(new Phrase("Deposit", tableHeaderFont))
                            {
                                BackgroundColor = LightGray,
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 4,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthTop = 1f,
                                BorderWidthBottom = 1f,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderColorTop = DarkGray,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(depositHeaderCell);

                            // Withdrawal header - kept for consistency but will be empty for shares
                            PdfPCell withdrawalHeaderCell = new PdfPCell(new Phrase("Withdrawal", tableHeaderFont))
                            {
                                BackgroundColor = LightGray,
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 4,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthTop = 1f,
                                BorderWidthBottom = 1f,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderColorTop = DarkGray,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(withdrawalHeaderCell);

                            PdfPCell balanceHeaderCell = new PdfPCell(new Phrase("Balance", tableHeaderFont))
                            {
                                BackgroundColor = LightGray,
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 4,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthTop = 1f,
                                BorderWidthBottom = 1f,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 1f,
                                BorderColorTop = DarkGray,
                                BorderColorBottom = DarkGray,
                                BorderColorRight = DarkGray
                            };
                            transTable.AddCell(balanceHeaderCell);

                            // Add ALL transactions - using tableCellFont
                            for (int i = 0; i < allTransactions.Count; i++)
                            {
                                var transaction = allTransactions[i];
                                bool isLastRow = (i == allTransactions.Count - 1);

                                PdfPCell dateCell = new PdfPCell(new Phrase(transaction.TransactionDate, tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_CENTER,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 1f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorLeft = DarkGray,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(dateCell);

                                PdfPCell descCell = new PdfPCell(new Phrase(transaction.Description ?? "", tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_LEFT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(descCell);

                                PdfPCell depositCell = new PdfPCell(new Phrase(transaction.DepositAmount.ToString("N2"), tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(depositCell);

                                // Withdrawal cell - always empty for shares
                                PdfPCell withdrawalCell = new PdfPCell(new Phrase("", tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(withdrawalCell);

                                PdfPCell balanceCell = new PdfPCell(new Phrase(transaction.RunningBalance.ToString("N2"), tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 1f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorRight = DarkGray,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(balanceCell);
                            }

                            document.Add(transTable);

                        }

                        // Account Summary - Show net movement only
                        if (shares.Summary != null)
                        {
                            var summaryPara = new Paragraph();
                            summaryPara.Alignment = Element.ALIGN_LEFT;

                            string accountTypeName = shares.ProductName ?? shares.AccountType;
                            string totalLabel = accountTypeName.Contains("Share", StringComparison.OrdinalIgnoreCase) ?
                                               "Total Share Capital" : $"Total {accountTypeName}";

                            summaryPara.Add(new Chunk($"{totalLabel}: ", boldFont));
                            summaryPara.Add(new Chunk(shares.Summary.NetMovement.ToString("N2"),
                                FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.BOLD, shares.Summary.NetMovement >= 0 ? Green : Red)));

                            summaryPara.SpacingAfter = 8f;
                            document.Add(summaryPara);
                        }

                        sharesCounter++;
                    }
                }
                // ===== LOANS DETAILED SECTION =====
                if (memberStatement.LoanStatements.Count > 0)
                {
                    var loansHeader = new Paragraph("LOANS STATEMENT",
                        FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12f, Font.BOLD, White))
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 3f
                    };

                    PdfPTable loansHeaderTable = new PdfPTable(1)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 10f
                    };

                    PdfPCell loansHeaderCell = new PdfPCell(loansHeader)
                    {
                        BackgroundColor = MediumGray,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 6,
                        Border = Rectangle.NO_BORDER,
                        BorderWidthBottom = 2f,
                        BorderColorBottom = SkyBlue
                    };
                    loansHeaderTable.AddCell(loansHeaderCell);
                    document.Add(loansHeaderTable);

                    int loanCounter = 1;
                    foreach (var loan in memberStatement.LoanStatements)
                    {
                        // Loan Header
                        var loanHeaderPara = new Paragraph($"LOAN #{loanCounter}: {loan.LoanNumber}", sectionHeaderFont)
                        {
                            Alignment = Element.ALIGN_LEFT,
                            SpacingAfter = 3f
                        };
                        document.Add(loanHeaderPara);

                        // Loan Details - 3 column layout with Disbursed Date centered
                        PdfPTable loanDetailsTable = new PdfPTable(3)
                        {
                            WidthPercentage = 100,
                            SpacingAfter = 3f  // Reduced from 5f
                        };
                        loanDetailsTable.SetWidths(new float[] { 33, 34, 33 });

                        // Format the disbursed date
                        string mainDisbursedDateDisplay = "N/A";
                        if (!string.IsNullOrEmpty(loan.LoanDetails.DisbursedDate))
                        {
                            DateTime disbursedDate;
                            if (DateTime.TryParse(loan.LoanDetails.DisbursedDate, out disbursedDate))
                            {
                                mainDisbursedDateDisplay = disbursedDate.ToString("dd/MM/yyyy");
                            }
                            else
                            {
                                mainDisbursedDateDisplay = loan.LoanDetails.DisbursedDate;
                            }
                        }

                        // Column 1: Loan Product - Left aligned
                        Paragraph col1Details = new Paragraph();
                        col1Details.Add(new Chunk("Loan Product: ", boldFont));
                        col1Details.Add(new Chunk(loan.LoanDetails.LoanProductType, normalFont));

                        PdfPCell col1Cell = new PdfPCell(col1Details)
                        {
                            Border = Rectangle.NO_BORDER,
                            Padding = 3,
                            HorizontalAlignment = Element.ALIGN_LEFT,
                            VerticalAlignment = Element.ALIGN_MIDDLE
                        };
                        loanDetailsTable.AddCell(col1Cell);

                        // Column 2: Disbursed Date - CENTERED
                        Paragraph col2Details = new Paragraph();
                        col2Details.Add(new Chunk("Disbursed Date: ", boldFont));
                        col2Details.Add(new Chunk(mainDisbursedDateDisplay, normalFont));

                        PdfPCell col2Cell = new PdfPCell(col2Details)
                        {
                            Border = Rectangle.NO_BORDER,
                            Padding = 3,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            VerticalAlignment = Element.ALIGN_MIDDLE
                        };
                        loanDetailsTable.AddCell(col2Cell);

                        // Column 3: Issued Amount - Right aligned
                        Paragraph col3Details = new Paragraph();
                        col3Details.Add(new Chunk("Issued Amount: ", boldFont));
                        col3Details.Add(new Chunk(loan.LoanDetails.AppliedAmount.ToString("N0"), normalFont));

                        PdfPCell col3Cell = new PdfPCell(col3Details)
                        {
                            Border = Rectangle.NO_BORDER,
                            Padding = 3,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            VerticalAlignment = Element.ALIGN_MIDDLE
                        };
                        loanDetailsTable.AddCell(col3Cell);

                        document.Add(loanDetailsTable);

                        // CURRENT OUTSTANDING - Single line, left and right aligned
                        if (loan.Summary != null)
                        {
                            PdfPTable outstandingTable = new PdfPTable(2)
                            {
                                WidthPercentage = 100,
                                SpacingAfter = 5f  // Reduced from 10f
                            };
                            outstandingTable.SetWidths(new float[] { 50, 50 });

                            // Left cell: "CURRENT OUTSTANDING:" label
                            PdfPCell labelCell = new PdfPCell(new Paragraph("CURRENT OUTSTANDING:",
                                FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.BOLD, DarkGray)))
                            {
                                Border = Rectangle.NO_BORDER,
                                HorizontalAlignment = Element.ALIGN_LEFT,
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 3,
                                PaddingTop = 0
                            };
                            outstandingTable.AddCell(labelCell);

                            // Right cell: Value
                            PdfPCell valueCell = new PdfPCell(new Paragraph(loan.Summary.TotalOutstandingBalance.ToString("N0"),
                                FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.BOLD, Red)))
                            {
                                Border = Rectangle.NO_BORDER,
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 3,
                                PaddingTop = 0
                            };
                            outstandingTable.AddCell(valueCell);

                            document.Add(outstandingTable);
                        }

                        // Transaction Table
                        PdfPTable transTable = new PdfPTable(6)
                        {
                            WidthPercentage = 100,
                            SpacingAfter = 5f  // Reduced from 10f
                        };
                        transTable.SetWidths(new float[] { 15, 18, 15, 15, 15, 22 });

                        // Table headers - using tableHeaderFont
                        PdfPCell dateHeaderCell = new PdfPCell(new Phrase("Date", tableHeaderFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            Padding = 4,
                            PaddingTop = 6,
                            PaddingBottom = 6,
                            BorderWidthTop = 1f,
                            BorderWidthBottom = 1f,
                            BorderWidthLeft = 1f,
                            BorderWidthRight = 0f,
                            BorderColorTop = DarkGray,
                            BorderColorBottom = DarkGray,
                            BorderColorLeft = DarkGray
                        };
                        transTable.AddCell(dateHeaderCell);

                        PdfPCell openingBalanceHeaderCell = new PdfPCell(new Phrase("Opening Balance", tableHeaderFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 4,
                            PaddingTop = 6,
                            PaddingBottom = 6,
                            BorderWidthTop = 1f,
                            BorderWidthBottom = 1f,
                            BorderWidthLeft = 0f,
                            BorderWidthRight = 0f,
                            BorderColorTop = DarkGray,
                            BorderColorBottom = DarkGray
                        };
                        transTable.AddCell(openingBalanceHeaderCell);

                        PdfPCell principleHeaderCell = new PdfPCell(new Phrase("Principle", tableHeaderFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 4,
                            PaddingTop = 6,
                            PaddingBottom = 6,
                            BorderWidthTop = 1f,
                            BorderWidthBottom = 1f,
                            BorderWidthLeft = 0f,
                            BorderWidthRight = 0f,
                            BorderColorTop = DarkGray,
                            BorderColorBottom = DarkGray
                        };
                        transTable.AddCell(principleHeaderCell);

                        PdfPCell interestHeaderCell = new PdfPCell(new Phrase("Interest", tableHeaderFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 4,
                            PaddingTop = 6,
                            PaddingBottom = 6,
                            BorderWidthTop = 1f,
                            BorderWidthBottom = 1f,
                            BorderWidthLeft = 0f,
                            BorderWidthRight = 0f,
                            BorderColorTop = DarkGray,
                            BorderColorBottom = DarkGray
                        };
                        transTable.AddCell(interestHeaderCell);

                        PdfPCell amountHeaderCell = new PdfPCell(new Phrase("Amount", tableHeaderFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 4,
                            PaddingTop = 6,
                            PaddingBottom = 6,
                            BorderWidthTop = 1f,
                            BorderWidthBottom = 1f,
                            BorderWidthLeft = 0f,
                            BorderWidthRight = 0f,
                            BorderColorTop = DarkGray,
                            BorderColorBottom = DarkGray
                        };
                        transTable.AddCell(amountHeaderCell);

                        PdfPCell loanBalanceHeaderCell = new PdfPCell(new Phrase("Loan Balance", tableHeaderFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 4,
                            PaddingTop = 6,
                            PaddingBottom = 6,
                            BorderWidthTop = 1f,
                            BorderWidthBottom = 1f,
                            BorderWidthLeft = 0f,
                            BorderWidthRight = 1f,
                            BorderColorTop = DarkGray,
                            BorderColorBottom = DarkGray,
                            BorderColorRight = DarkGray
                        };
                        transTable.AddCell(loanBalanceHeaderCell);

                        // Add transactions if they exist - using tableCellFont
                        if (loan.Statement != null && loan.Statement.Count > 0)
                        {
                            for (int i = 0; i < loan.Statement.Count; i++)
                            {
                                var row = loan.Statement[i];
                                bool isLastRow = (i == loan.Statement.Count - 1);

                                // Format date
                                string transDate = "";
                                if (!string.IsNullOrEmpty(row.TransDate))
                                {
                                    DateTime date;
                                    if (DateTime.TryParse(row.TransDate, out date))
                                        transDate = date.ToString("dd/MM/yyyy");
                                    else
                                        transDate = row.TransDate;
                                }

                                // Date cell
                                PdfPCell dateCell = new PdfPCell(new Phrase(transDate, tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_CENTER,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 1f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorLeft = DarkGray,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(dateCell);

                                // Opening Balance cell
                                PdfPCell openingBalanceCell = new PdfPCell(new Phrase(row.OpeningBalance.ToString("N0"), tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(openingBalanceCell);

                                // Principle cell
                                PdfPCell principleCell = new PdfPCell(new Phrase(row.Principle.ToString("N0"), tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(principleCell);

                                // Interest cell
                                PdfPCell interestCell = new PdfPCell(new Phrase(row.Interest.ToString("N0"), tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(interestCell);

                                // Amount cell - with color coding
                                Font amountCellFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 10f, Font.BOLD, row.Amount > 0 ? Green : DarkGray);
                                PdfPCell amountCell = new PdfPCell(new Phrase(row.Amount.ToString("N0"), amountCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(amountCell);

                                // Loan Balance cell
                                PdfPCell balanceCell = new PdfPCell(new Phrase(row.LoanBalance.ToString("N0"), tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 1f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorRight = DarkGray,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(balanceCell);
                            }

                            //if (loan.Statement.Count > 10)
                            //{
                            //    var moreTransPara = new Paragraph($"... and {loan.Statement.Count - 10} more transactions", smallFont)
                            //    {
                            //        Alignment = Element.ALIGN_CENTER,
                            //        SpacingBefore = 3f,
                            //        SpacingAfter = 3f  // Reduced from 8f
                            //    };
                            //    document.Add(moreTransPara);
                            //}
                        }
                        else
                        {
                            // No transactions - show initial disbursement
                            decimal issuedAmount = loan.LoanDetails.AppliedAmount;

                            // Use a different variable name for the "else" block
                            string noTransactionsDateDisplay = "N/A";
                            if (!string.IsNullOrEmpty(loan.LoanDetails.DisbursedDate))
                            {
                                DateTime noTransactionsDate;
                                if (DateTime.TryParse(loan.LoanDetails.DisbursedDate, out noTransactionsDate))
                                {
                                    noTransactionsDateDisplay = noTransactionsDate.ToString("dd/MM/yyyy");
                                }
                                else
                                {
                                    noTransactionsDateDisplay = loan.LoanDetails.DisbursedDate;
                                }
                            }

                            // Date cell
                            PdfPCell dateCell = new PdfPCell(new Phrase(noTransactionsDateDisplay, tableCellFont))
                            {
                                HorizontalAlignment = Element.ALIGN_CENTER,
                                Padding = 3,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthLeft = 1f,
                                BorderWidthRight = 0f,
                                BorderWidthTop = 0f,
                                BorderWidthBottom = 1f,
                                BorderColorLeft = DarkGray,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(dateCell);

                            // Opening Balance cell
                            PdfPCell openingBalanceCell = new PdfPCell(new Phrase(issuedAmount.ToString("N0"), tableCellFont))
                            {
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 3,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderWidthTop = 0f,
                                BorderWidthBottom = 1f,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(openingBalanceCell);

                            // Principle cell
                            PdfPCell principleCell = new PdfPCell(new Phrase("0", tableCellFont))
                            {
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 3,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderWidthTop = 0f,
                                BorderWidthBottom = 1f,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(principleCell);

                            // Interest cell
                            PdfPCell interestCell = new PdfPCell(new Phrase("0", tableCellFont))
                            {
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 3,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderWidthTop = 0f,
                                BorderWidthBottom = 1f,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(interestCell);

                            // Amount cell
                            PdfPCell amountCell = new PdfPCell(new Phrase("0", tableCellFont))
                            {
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 3,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderWidthTop = 0f,
                                BorderWidthBottom = 1f,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(amountCell);

                            // Loan Balance cell
                            PdfPCell balanceCell = new PdfPCell(new Phrase(issuedAmount.ToString("N0"), tableCellFont))
                            {
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 3,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 1f,
                                BorderWidthTop = 0f,
                                BorderWidthBottom = 1f,
                                BorderColorRight = DarkGray,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(balanceCell);
                        }

                        document.Add(transTable);

                        // Loan Summary - Moved closer to table
                        //if (loan.Summary != null)
                        //{
                        //    // Removed document.Add(new Paragraph("\n")); to eliminate blank line

                        //    var summaryPara = new Paragraph();
                        //    summaryPara.Alignment = Element.ALIGN_LEFT;

                        //    string periodText = "";
                        //    if (startDate.HasValue && endDate.HasValue)
                        //        periodText = $"for period {startDate.Value:dd/MM/yyyy} - {endDate.Value:dd/MM/yyyy}";
                        //    else if (startDate.HasValue)
                        //        periodText = $"from {startDate.Value:dd/MM/yyyy}";
                        //    else if (endDate.HasValue)
                        //        periodText = $"up to {endDate.Value:dd/MM/yyyy}";

                        //    if (!string.IsNullOrEmpty(periodText))
                        //    {
                        //        summaryPara.Add(new Chunk($"Summary {periodText}: ", boldFont));
                        //    }
                        //    else
                        //    {
                        //        summaryPara.Add(new Chunk("Loan Summary: ", boldFont));
                        //    }

                        //    summaryPara.Add(new Chunk($"Principal Paid: {loan.Summary.TotalPrincipalRepaid:N0}", normalFont));
                        //    summaryPara.Add(new Chunk(" | ", normalFont));
                        //    summaryPara.Add(new Chunk($"Interest Accrued: {loan.Summary.TotalInterestAccrued:N0}", normalFont));
                        //    summaryPara.Add(new Chunk(" | ", normalFont));
                        //    summaryPara.Add(new Chunk($"Interest Paid: {loan.Summary.TotalInterestPaid:N0}", normalFont));

                        //    summaryPara.SpacingAfter = 8f;  // Reduced from 15f
                        //    document.Add(summaryPara);
                        //}

                        loanCounter++;
                    }
                }

                // ===== FOOTER =====
                document.Add(new Paragraph("\n"));
                string footerText = $"Statement Generated on: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";

                if (startDate.HasValue || endDate.HasValue)
                {
                    string dateRangeInfo = "";
                    if (startDate.HasValue && endDate.HasValue)
                        dateRangeInfo = $" | Period: {startDate.Value:dd/MM/yyyy} - {endDate.Value:dd/MM/yyyy}";
                    else if (startDate.HasValue)
                        dateRangeInfo = $" | From: {startDate.Value:dd/MM/yyyy}";
                    else if (endDate.HasValue)
                        dateRangeInfo = $" | Up to: {endDate.Value:dd/MM/yyyy}";

                    footerText += dateRangeInfo;
                }

                footerText += $" | Total Accounts: {memberStatement.TotalAccounts}";

                var footerPara = new Paragraph(footerText, smallFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 8f
                };
                document.Add(footerPara);

                // ===== FOOTER NOTES =====
                document.Add(new Paragraph("\n"));
                document.Add(new Paragraph("This is a system generated detailed statement for all member accounts.", smallFont)
                {
                    Alignment = Element.ALIGN_CENTER
                });
                document.Add(new Paragraph("For any queries, contact: rubanisacco@gmail.com", smallFont)
                {
                    Alignment = Element.ALIGN_CENTER
                });

                document.Close();
                writer.Close();

                return ms.ToArray();
            }
        }

        // Helper method to create cells with NO BORDERS (not used anymore, but kept for backward compatibility)
        private PdfPCell CreateStyledCell(string text, Font font, int alignment = Element.ALIGN_LEFT)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text ?? "", font));
            cell.HorizontalAlignment = alignment;
            cell.Padding = 4f;

            // REMOVE ALL BORDERS
            cell.BorderWidthLeft = 0f;
            cell.BorderWidthRight = 0f;
            cell.BorderWidthTop = 0f;
            cell.BorderWidthBottom = 0f;

            return cell;
        }


        public class LoanPaymentRow
        {
            public string TransactionDate { get; set; }
            public decimal OpeningBalance { get; set; }
            public decimal Principal { get; set; }
            public decimal Interest { get; set; }
            public decimal Amount { get; set; }
            public decimal LoanBalance { get; set; }
            public string TransactionType { get; set; }
            public string Description { get; set; }
        }

        public class LoanDetails
        {
            public string LoanNumber { get; set; }
            public string LoanProductType { get; set; }
            public decimal AppliedAmount { get; set; }
            public decimal MonthlyRepayment { get; set; }
            public string MemberNumber { get; set; }
            public string DisbursedDate { get; set; }
        }

        public class LoanStatementResult
        {
            public string LoanNumber { get; set; }
            public CustomerInfo Customer { get; set; }
            public LoanDetails LoanDetails { get; set; }
            public List<LoanStatementRow> Statement { get; set; }
            public LoanSummary Summary { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
        }
        public class CustomerData
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Mobile { get; set; }
            public string Email { get; set; }
            public string Reference2 { get; set; }
            public string Reference3 { get; set; }
            public int BranchCode { get; set; }
            public int CustomerSerialNumber { get; set; }
            public int ProductCode { get; set; }
            public int TargetProductCode { get; set; }
        }

        public class CustomerInfo
        {
            public string FullName { get; set; }
            public string AccountNumber { get; set; }
            public string StaffNo { get; set; }
            public string PFNumber { get; set; }
            public string Mobile { get; set; }
            public string Email { get; set; }
        }



        // Helper method to create cells WITH BORDERS for loan table alignment
        private PdfPCell CreateTableCell(string text, Font font, int alignment = Element.ALIGN_LEFT, BaseColor borderColor = null, bool showBorders = false)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text ?? "", font));
            cell.HorizontalAlignment = alignment;
            cell.Padding = 5f;
            cell.PaddingTop = 8f;
            cell.PaddingBottom = 8f;

            if (showBorders && borderColor != null)
            {
                // Add borders for better alignment
                cell.BorderWidthLeft = 1f;
                cell.BorderWidthRight = 1f;
                cell.BorderWidthTop = 1f;
                cell.BorderWidthBottom = 1f;
                cell.BorderColor = borderColor;
            }
            else
            {
                // No borders for shares table
                cell.BorderWidthLeft = 0f;
                cell.BorderWidthRight = 0f;
                cell.BorderWidthTop = 0f;
                cell.BorderWidthBottom = 0f;
            }

            return cell;
        }





        public class CustomerShareStatementRow
        {
            public string Date { get; set; }
            public decimal ShareContribution { get; set; }
            public decimal Cumulative { get; set; }
            public string Description { get; set; }
        }

        public class CustomerShareStatementResult
        {
            public List<CustomerShareStatementRow> Statement { get; set; }
            public decimal TotalContribution { get; set; }
        }

        public class LoanStatementRow
        {
            public string PostingDate { get; set; }
            public string TransactionType { get; set; }
            public string DocumentNo { get; set; }
            public string Description { get; set; }
            public decimal Debit { get; set; }
            public decimal Credit { get; set; }
            public decimal Balance { get; set; }
            public decimal Amount { get; set; }

            public string TransDate { get; set; }
            public decimal OpeningBalance { get; set; }
            public decimal Principle { get; set; }
            public decimal Interest { get; set; }
            public decimal LoanBalance { get; set; }
        }

        public class LoanSummary
        {
            public decimal TotalDisbursed { get; set; }
            public decimal TotalPrincipalRepaid { get; set; }
            public decimal TotalInterestPaid { get; set; }
            public decimal TotalInterestAccrued { get; set; }
            public decimal OutstandingLoanAmount { get; set; }
            public decimal OutstandingLoanInterest { get; set; }
            public decimal TotalOutstandingBalance { get; set; }
            public decimal OpeningBalance { get; set; }

        }

        public class RecentTransaction
        {
            public string PostingDate { get; set; }
            public string TransactionType { get; set; }
            public string DocumentNo { get; set; }
            public string Description { get; set; }
            public decimal Debit { get; set; }
            public decimal Credit { get; set; }
            public decimal Balance { get; set; }
        }


        public class CreditBatchImportRequest
        {
            public string FileName { get; set; }
        }

        private async Task<Guid?> GetCustomerIdByReference2(string reference2)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                SELECT Id 
                FROM swiftFin_Customers 
                WHERE Reference2 = @reference2";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.Add("@reference2", SqlDbType.NVarChar).Value = reference2;

                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            return (Guid)result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Error in GetCustomerIdByReference2: {ex.Message}");
            }

            return null;
        }


        [HttpGet]
        [Route("GetAllSharesStatementByReference2/{reference2}")]
        public async Task<HttpResponseMessage> GetAllSharesStatementByReference2(
    string reference2,
    DateTime? startDate = null,
    DateTime? endDate = null,
    bool downloadPdf = false)
        {
            try
            {
                // Step 1: Get the customer ID from reference2
                var customerId = await GetCustomerIdByReference2(reference2);

                if (!customerId.HasValue)
                {
                    var notFoundResponse = Request.CreateResponse(HttpStatusCode.NotFound);
                    notFoundResponse.Content = new StringContent(
                        JsonConvert.SerializeObject(new ApiResponse<object>
                        {
                            Success = false,
                            Message = $"No customer found with Reference2/Member Number: '{reference2}'",
                            Data = null
                        }),
                        Encoding.UTF8,
                        "application/json");
                    return notFoundResponse;
                }

                // Step 2: Call the shares-only method with the found customer ID
                return await GetAllSharesStatement(customerId.Value, startDate, endDate, downloadPdf);
            }
            catch (Exception ex)
            {
                var errorResponse = Request.CreateResponse(HttpStatusCode.InternalServerError);
                errorResponse.Content = new StringContent(
                    JsonConvert.SerializeObject(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "An error occurred while retrieving shares statement by reference2.",
                        Data = ex.Message + " | Inner: " + (ex.InnerException?.Message ?? "None")
                    }),
                    Encoding.UTF8,
                    "application/json");
                return errorResponse;
            }
        }

        [HttpGet]
        [Route("GetAllSharesStatement/{customerId}")]
        public async Task<HttpResponseMessage> GetAllSharesStatement(
    Guid customerId,
    DateTime? startDate = null,
    DateTime? endDate = null,
    bool downloadPdf = false)
        {
            try
            {
                var sharesStatementResult = new SharesOnlyStatementResult
                {
                    CustomerId = customerId,
                    StartDate = startDate,
                    EndDate = endDate
                };

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // ===== GET SHARES INFORMATION ONLY =====
                    var allSharesStatements = new List<SharesStatementResult>();

                    using (var sharesCommand = new SqlCommand("sp_GenerateAllSharesStatement", connection))
                    {
                        sharesCommand.CommandType = CommandType.StoredProcedure;
                        sharesCommand.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;

                        if (startDate.HasValue)
                            sharesCommand.Parameters.Add("@StartDate", SqlDbType.Date).Value = startDate.Value.Date;
                        else
                            sharesCommand.Parameters.Add("@StartDate", SqlDbType.Date).Value = DBNull.Value;

                        if (endDate.HasValue)
                            sharesCommand.Parameters.Add("@EndDate", SqlDbType.Date).Value = endDate.Value.Date;
                        else
                            sharesCommand.Parameters.Add("@EndDate", SqlDbType.Date).Value = DBNull.Value;

                        using (var reader = await sharesCommand.ExecuteReaderAsync())
                        {
                            // Dictionary to group transactions by account
                            var accountTransactions = new Dictionary<Guid, List<SharesTransaction>>();
                            var accountDetails = new Dictionary<Guid, (string ProductName, decimal TotalContribution)>();

                            // Check if there are any result sets
                            bool hasSharesData = false;

                            // OUTPUT 0: Account Header (first result set) - Skip it
                            if (await reader.NextResultAsync())
                            {
                                // First result set is now the Detailed Statement (OUTPUT 1)
                                while (await reader.ReadAsync())
                                {
                                    // Skip if it's a message result set
                                    if (reader.FieldCount == 1 && reader.GetName(0) == "Message")
                                        continue;

                                    hasSharesData = true;

                                    var customerAccountId = reader["CustomerAccountId"] != DBNull.Value ?
                                        (Guid)reader["CustomerAccountId"] : Guid.Empty;

                                    var transaction = new SharesTransaction
                                    {
                                        TransactionDate = reader["Date"]?.ToString() ?? "",
                                        Description = reader["Description"]?.ToString() ?? "",
                                        DepositAmount = reader["Share Contribution"] != DBNull.Value ?
                                            Convert.ToDecimal(reader["Share Contribution"]) : 0m,
                                        WithdrawalAmount = 0m,
                                        RunningBalance = reader["Cumulative"] != DBNull.Value ?
                                            Convert.ToDecimal(reader["Cumulative"]) : 0m
                                    };

                                    if (!accountTransactions.ContainsKey(customerAccountId))
                                        accountTransactions[customerAccountId] = new List<SharesTransaction>();

                                    accountTransactions[customerAccountId].Add(transaction);
                                }
                            }

                            // Move to Summary result set (OUTPUT 2)
                            if (hasSharesData && await reader.NextResultAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    // Skip if it's a message
                                    if (reader.FieldCount == 1 && reader.GetName(0) == "Message")
                                        continue;

                                    var customerAccountId = reader["CustomerAccountId"] != DBNull.Value ?
                                        (Guid)reader["CustomerAccountId"] : Guid.Empty;
                                    var productName = reader["ProductName"]?.ToString() ?? "";
                                    var totalContribution = reader["TotalContribution"] != DBNull.Value ?
                                        Convert.ToDecimal(reader["TotalContribution"]) : 0m;

                                    accountDetails[customerAccountId] = (productName, totalContribution);
                                }
                            }

                            // Skip the third result set (Summary Stats - OUTPUT 3) if it exists
                            if (hasSharesData)
                            {
                                await reader.NextResultAsync();
                            }

                            // Create shares statement results for each account
                            foreach (var account in accountDetails)
                            {
                                var transactions = accountTransactions.ContainsKey(account.Key)
                                    ? accountTransactions[account.Key]
                                    : new List<SharesTransaction>();

                                // Calculate summary values from transactions
                                decimal openingBalance = 0m;
                                decimal totalDeposits = transactions.Sum(t => t.DepositAmount);
                                decimal closingBalance = transactions.Any()
                                    ? transactions.Last().RunningBalance
                                    : 0m;

                                // Use the TotalContribution from the SP for the summary
                                decimal actualTotalContribution = account.Value.TotalContribution;

                                // Create shares statement result - RENAMED to shareResult to avoid conflict
                                var shareResult = new SharesStatementResult
                                {
                                    StatementType = "SHARES/SAVINGS STATEMENT",
                                    ProductName = account.Value.ProductName,
                                    AccountType = "Share Account",
                                    ProductCode = 0,
                                    Period = $"{(startDate.HasValue ? startDate.Value.ToString("dd/MM/yyyy") : "Beginning")} to {(endDate.HasValue ? endDate.Value.ToString("dd/MM/yyyy") : DateTime.Now.ToString("dd/MM/yyyy"))}",
                                    OpeningBalance = openingBalance,
                                    TotalDeposits = totalDeposits,
                                    TotalWithdrawals = 0m,
                                    ClosingBalance = closingBalance,
                                    Transactions = transactions,
                                    Summary = new SharesAccountSummary
                                    {
                                        AccountName = account.Value.ProductName,
                                        AccountType = "Share Account",
                                        OpeningBalance = openingBalance,
                                        TotalDeposits = actualTotalContribution,
                                        TotalWithdrawals = 0m,
                                        ClosingBalance = closingBalance,
                                        NetMovement = actualTotalContribution
                                    }
                                };

                                allSharesStatements.Add(shareResult);
                            }
                        }
                    }

                    // Get customer info
                    var customerInfo = new CustomerInfo
                    {
                        FullName = await GetCustomerName(connection, customerId),
                        AccountNumber = "N/A", // We don't have account number from shares SP
                        StaffNo = await GetCustomerStaffNo(connection, customerId),
                        Mobile = await GetCustomerMobile(connection, customerId),
                        Email = await GetCustomerEmail(connection, customerId),
                        PFNumber = await GetCustomerPFNumber(connection, customerId)
                    };

                    // Populate shares statement result
                    sharesStatementResult.Customer = customerInfo;
                    sharesStatementResult.SharesStatements = allSharesStatements;
                    sharesStatementResult.TotalSharesBalance = allSharesStatements.Sum(s => s.ClosingBalance);
                    sharesStatementResult.TotalAccounts = allSharesStatements.Count;

                    // Check if we found any shares data
                    if (sharesStatementResult.SharesStatements.Count == 0)
                    {
                        // Return success with empty data and appropriate message
                        var emptyResponse = Request.CreateResponse(HttpStatusCode.OK);
                        emptyResponse.Content = new StringContent(
                            JsonConvert.SerializeObject(new ApiResponse<object>
                            {
                                Success = true,
                                Message = "No shares/savings accounts found for this customer. Customer information is provided.",
                                Data = sharesStatementResult
                            }),
                            Encoding.UTF8,
                            "application/json");
                        return emptyResponse;
                    }

                    // If PDF download is requested
                    if (downloadPdf)
                    {
                        byte[] pdfBytes = GenerateSharesOnlyStatementPdf(sharesStatementResult, startDate, endDate);

                        var response = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new ByteArrayContent(pdfBytes)
                        };

                        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

                        string customerName = sharesStatementResult.Customer?.FullName?.Replace(" ", "_") ?? "Customer";
                        string dateRange = "";
                        if (startDate.HasValue && endDate.HasValue)
                            dateRange = $"{startDate.Value:yyyyMMdd}_{endDate.Value:yyyyMMdd}";
                        else if (startDate.HasValue)
                            dateRange = $"from_{startDate.Value:yyyyMMdd}";
                        else if (endDate.HasValue)
                            dateRange = $"to_{endDate.Value:yyyyMMdd}";

                        response.Content.Headers.ContentDisposition =
                            new ContentDispositionHeaderValue("attachment")
                            {
                                FileName = $"SharesStatement_{customerName}_{dateRange}_{DateTime.Now:yyyyMMdd}.pdf"
                            };

                        return response;
                    }
                    else
                    {
                        // Return JSON response
                        var response = Request.CreateResponse(HttpStatusCode.OK);
                        string message = $"Found {sharesStatementResult.SharesStatements.Count} shares/savings account(s).";

                        response.Content = new StringContent(
                            JsonConvert.SerializeObject(new ApiResponse<object>
                            {
                                Success = true,
                                Message = message,
                                Data = sharesStatementResult
                            }),
                            Encoding.UTF8,
                            "application/json");
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent(
                    JsonConvert.SerializeObject(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "An error occurred while retrieving shares statement.",
                        Data = ex.Message + " | Inner: " + (ex.InnerException?.Message ?? "None")
                    }),
                    Encoding.UTF8,
                    "application/json");
                return response;
            }
        }

        // New result class for shares-only statements
        public class SharesOnlyStatementResult
        {
            public Guid CustomerId { get; set; }
            public CustomerInfo Customer { get; set; }
            public List<SharesStatementResult> SharesStatements { get; set; } = new List<SharesStatementResult>();
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public decimal TotalSharesBalance { get; set; }
            public int TotalAccounts { get; set; }
        }

        // PDF generation method for shares-only statements
        private byte[] GenerateSharesOnlyStatementPdf(SharesOnlyStatementResult sharesStatement, DateTime? startDate = null, DateTime? endDate = null)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Create document with same margins as individual statements
                Document document = new Document(PageSize.A4, 30, 30, 50, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);

                document.Open();

                // ===== RUBANI SACCO COLOR THEME =====
                BaseColor SkyBlue = new BaseColor(0, 174, 239); // #00AEEF
                BaseColor Red = new BaseColor(255, 0, 0);       // #FF0000
                BaseColor Green = new BaseColor(0, 150, 0);     // #009600
                BaseColor DarkGray = new BaseColor(26, 26, 26); // #1A1A1A
                BaseColor LightGray = new BaseColor(217, 217, 217); // #D9D9D9
                BaseColor MediumGray = new BaseColor(128, 128, 128); // #808080
                BaseColor White = BaseColor.WHITE;

                // ===== FONTS - BOOK ANTIQUA FONT WITH SIZE 11 =====
                string bookAntiquaFontName = "Book Antiqua";

                // Try to create Book Antiqua fonts, fallback to Times if not available
                Font titleFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 16f, Font.BOLD, DarkGray);
                Font normalFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.NORMAL, DarkGray);
                Font boldFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.BOLD, DarkGray);
                Font smallFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 9f, Font.NORMAL, DarkGray);
                Font companyNameFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 14f, Font.BOLD, SkyBlue);
                Font companyInfoFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.NORMAL, DarkGray);
                Font sectionHeaderFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12f, Font.BOLD, SkyBlue);
                Font tableHeaderFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 9f, Font.BOLD, DarkGray);
                Font tableCellFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 10f, Font.NORMAL, DarkGray);

                // Fallback fonts if Book Antiqua is not available
                try
                {
                    // Test if Book Antiqua is available
                    var testFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f);
                }
                catch
                {
                    // Fallback to Times New Roman if Book Antiqua is not available
                    bookAntiquaFontName = "Times New Roman";
                    titleFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 16f, Font.BOLD, DarkGray);
                    normalFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.NORMAL, DarkGray);
                    boldFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.BOLD, DarkGray);
                    smallFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 9f, Font.NORMAL, DarkGray);
                    companyNameFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 14f, Font.BOLD, SkyBlue);
                    companyInfoFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.NORMAL, DarkGray);
                    sectionHeaderFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12f, Font.BOLD, SkyBlue);
                    tableHeaderFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 9f, Font.BOLD, DarkGray);
                    tableCellFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 10f, Font.NORMAL, DarkGray);
                }

                // ===== CUSTOM HEADER WITH RUBANI SACCO LOGO =====
                try
                {
                    // Create a table with 1 column for left-aligned content
                    PdfPTable headerTable = new PdfPTable(1)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 8f
                    };

                    // Row 1: Logo left-aligned at top
                    PdfPCell logoCell = new PdfPCell();
                    logoCell.Border = Rectangle.NO_BORDER;
                    logoCell.HorizontalAlignment = Element.ALIGN_LEFT;
                    logoCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    logoCell.PaddingBottom = 3f;

                    // Try to load logo from local path
                    string logoPath = @"C:\Users\ADMIN\source\repos\SwiftFinancialsNew\SwiftFinancialsSolution\TestApis\Assets\Images\rubani-logo.jpeg";
                    if (File.Exists(logoPath))
                    {
                        try
                        {
                            Image logo = Image.GetInstance(logoPath);
                            logo.ScaleToFit(100, 100);
                            logoCell.AddElement(logo);
                        }
                        catch (Exception)
                        {
                            logoCell.AddElement(new Paragraph("RUBANI SACCO", companyNameFont)
                            {
                                Alignment = Element.ALIGN_LEFT
                            });
                        }
                    }
                    else
                    {
                        logoCell.AddElement(new Paragraph("RUBANI SACCO", companyNameFont)
                        {
                            Alignment = Element.ALIGN_LEFT
                        });
                    }

                    headerTable.AddCell(logoCell);

                    // Row 2: Company Info - LEFT ALIGNED
                    PdfPCell infoCell = new PdfPCell();
                    infoCell.Border = Rectangle.NO_BORDER;
                    infoCell.HorizontalAlignment = Element.ALIGN_LEFT;
                    infoCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    infoCell.PaddingTop = 3f;

                    var companyNamePara = new Paragraph("RUBANI SACCO", companyNameFont)
                    {
                        Alignment = Element.ALIGN_LEFT
                    };
                    infoCell.AddElement(companyNamePara);

                    var address = new Paragraph("Rubani House, Off Airport North Embakasi", companyInfoFont)
                    {
                        Alignment = Element.ALIGN_LEFT
                    };
                    infoCell.AddElement(address);

                    var email = new Paragraph("rubanisacco@gmail.com", companyInfoFont)
                    {
                        Alignment = Element.ALIGN_LEFT
                    };
                    infoCell.AddElement(email);

                    headerTable.AddCell(infoCell);
                    document.Add(headerTable);

                    // Add decorative line (Blue-Red-Blue)
                    var lineTable = new PdfPTable(3)
                    {
                        WidthPercentage = 100,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        SpacingAfter = 8f
                    };
                    lineTable.SetWidths(new float[] { 33, 34, 33 });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = SkyBlue,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = Red,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = SkyBlue,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    document.Add(lineTable);
                }
                catch (Exception)
                {
                    var fallbackPara = new Paragraph("RUBANI SACCO\nRubani House, Off Airport North Embakasi\nrubanisacco@gmail.com",
                        companyInfoFont)
                    {
                        Alignment = Element.ALIGN_LEFT,
                        SpacingAfter = 10f
                    };
                    document.Add(fallbackPara);
                }

                // ===== SHARES STATEMENT TITLE =====
                string titleText = "SHARES/SAVINGS STATEMENT";
                if (startDate.HasValue || endDate.HasValue)
                {
                    string dateRangeText = "";

                    if (startDate.HasValue && endDate.HasValue)
                        dateRangeText = $"{startDate.Value:dd/MM/yyyy} to {endDate.Value:dd/MM/yyyy}";
                    else if (startDate.HasValue)
                        dateRangeText = $"From {startDate.Value:dd/MM/yyyy}";
                    else if (endDate.HasValue)
                        dateRangeText = $"To {endDate.Value:dd/MM/yyyy}";

                    if (!string.IsNullOrEmpty(dateRangeText))
                    {
                        document.Add(new Paragraph(titleText, titleFont)
                        {
                            Alignment = Element.ALIGN_CENTER,
                            SpacingAfter = 3f
                        });

                        document.Add(new Paragraph(dateRangeText,
                            FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12f, Font.BOLD, DarkGray))
                        {
                            Alignment = Element.ALIGN_CENTER,
                            SpacingAfter = 8f
                        });
                    }
                    else
                    {
                        document.Add(new Paragraph(titleText, titleFont)
                        {
                            Alignment = Element.ALIGN_CENTER,
                            SpacingAfter = 8f
                        });
                    }
                }
                else
                {
                    document.Add(new Paragraph(titleText, titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 8f
                    });
                }

                // ===== MEMBER INFORMATION SECTION =====
                if (sharesStatement.Customer != null)
                {
                    // Create a 2-column table for better alignment
                    PdfPTable memberInfoTable = new PdfPTable(2)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 10f
                    };
                    memberInfoTable.SetWidths(new float[] { 40, 60 });

                    // Left column: Name, Staff No, Mobile
                    Paragraph leftColumn = new Paragraph();
                    leftColumn.Add(new Chunk("Name: ", boldFont));
                    leftColumn.Add(new Chunk(sharesStatement.Customer.FullName, normalFont));
                    leftColumn.Add(Chunk.NEWLINE);
                    leftColumn.Add(new Chunk("Staff No: ", boldFont));
                    leftColumn.Add(new Chunk(sharesStatement.Customer.PFNumber ?? "N/A", normalFont));
                    leftColumn.Add(Chunk.NEWLINE);
                    leftColumn.Add(new Chunk("Mobile: ", boldFont));
                    leftColumn.Add(new Chunk(sharesStatement.Customer.Mobile ?? "N/A", normalFont));

                    PdfPCell leftCell = new PdfPCell(leftColumn)
                    {
                        Border = Rectangle.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_LEFT,
                        Padding = 3
                    };
                    memberInfoTable.AddCell(leftCell);

                    // Right column: MemberNo, Email
                    Paragraph rightColumn = new Paragraph();
                    rightColumn.Add(new Chunk("MemberNo: ", boldFont));
                    rightColumn.Add(new Chunk(sharesStatement.Customer.StaffNo ?? "N/A", normalFont));
                    rightColumn.Add(Chunk.NEWLINE);
                    rightColumn.Add(new Chunk("Email: ", boldFont));
                    rightColumn.Add(new Chunk(sharesStatement.Customer.Email ?? "N/A", normalFont));

                    PdfPCell rightCell = new PdfPCell(rightColumn)
                    {
                        Border = Rectangle.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_LEFT,
                        Padding = 3
                    };
                    memberInfoTable.AddCell(rightCell);

                    document.Add(memberInfoTable);
                }

                // ===== SHARES/SAVINGS DETAILED SECTION =====
                if (sharesStatement.SharesStatements.Count > 0)
                {
                    int sharesCounter = 1;
                    foreach (var shares in sharesStatement.SharesStatements)
                    {
                        // Account Header - Show product name only
                        var accountHeaderPara = new Paragraph($"ACCOUNT #{sharesCounter}: {shares.ProductName}", sectionHeaderFont)
                        {
                            Alignment = Element.ALIGN_LEFT,
                            SpacingAfter = 8f
                        };
                        document.Add(accountHeaderPara);

                        // Account Period
                        if (!string.IsNullOrEmpty(shares.Period))
                        {
                            var periodPara = new Paragraph($"Period: {shares.Period}", normalFont)
                            {
                                Alignment = Element.ALIGN_LEFT,
                                SpacingAfter = 5f
                            };
                            document.Add(periodPara);
                        }

                        // Transactions Section - Show ALL transactions
                        if (shares.Transactions.Count > 0)
                        {
                            var allTransactions = shares.Transactions;

                            PdfPTable transTable = new PdfPTable(5)
                            {
                                WidthPercentage = 100,
                                SpacingAfter = 5f
                            };
                            transTable.SetWidths(new float[] { 20, 35, 15, 15, 15 });

                            // Table headers
                            PdfPCell dateHeaderCell = new PdfPCell(new Phrase("Date", tableHeaderFont))
                            {
                                BackgroundColor = LightGray,
                                HorizontalAlignment = Element.ALIGN_CENTER,
                                Padding = 4,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthTop = 1f,
                                BorderWidthBottom = 1f,
                                BorderWidthLeft = 1f,
                                BorderWidthRight = 0f,
                                BorderColorTop = DarkGray,
                                BorderColorBottom = DarkGray,
                                BorderColorLeft = DarkGray
                            };
                            transTable.AddCell(dateHeaderCell);

                            PdfPCell descHeaderCell = new PdfPCell(new Phrase("Description", tableHeaderFont))
                            {
                                BackgroundColor = LightGray,
                                HorizontalAlignment = Element.ALIGN_CENTER,
                                Padding = 4,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthTop = 1f,
                                BorderWidthBottom = 1f,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderColorTop = DarkGray,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(descHeaderCell);

                            PdfPCell depositHeaderCell = new PdfPCell(new Phrase("Deposit", tableHeaderFont))
                            {
                                BackgroundColor = LightGray,
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 4,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthTop = 1f,
                                BorderWidthBottom = 1f,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderColorTop = DarkGray,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(depositHeaderCell);

                            // Withdrawal header
                            PdfPCell withdrawalHeaderCell = new PdfPCell(new Phrase("Withdrawal", tableHeaderFont))
                            {
                                BackgroundColor = LightGray,
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 4,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthTop = 1f,
                                BorderWidthBottom = 1f,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderColorTop = DarkGray,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(withdrawalHeaderCell);

                            PdfPCell balanceHeaderCell = new PdfPCell(new Phrase("Balance", tableHeaderFont))
                            {
                                BackgroundColor = LightGray,
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 4,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthTop = 1f,
                                BorderWidthBottom = 1f,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 1f,
                                BorderColorTop = DarkGray,
                                BorderColorBottom = DarkGray,
                                BorderColorRight = DarkGray
                            };
                            transTable.AddCell(balanceHeaderCell);

                            // Add ALL transactions
                            for (int i = 0; i < allTransactions.Count; i++)
                            {
                                var transaction = allTransactions[i];
                                bool isLastRow = (i == allTransactions.Count - 1);

                                PdfPCell dateCell = new PdfPCell(new Phrase(transaction.TransactionDate, tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_CENTER,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 1f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorLeft = DarkGray,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(dateCell);

                                PdfPCell descCell = new PdfPCell(new Phrase(transaction.Description ?? "", tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_LEFT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(descCell);

                                PdfPCell depositCell = new PdfPCell(new Phrase(transaction.DepositAmount.ToString("N2"), tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(depositCell);

                                // Withdrawal cell - always empty for shares
                                PdfPCell withdrawalCell = new PdfPCell(new Phrase("", tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(withdrawalCell);

                                PdfPCell balanceCell = new PdfPCell(new Phrase(transaction.RunningBalance.ToString("N2"), tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 1f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorRight = DarkGray,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(balanceCell);
                            }

                            document.Add(transTable);
                        }

                        // Account Summary
                        if (shares.Summary != null)
                        {
                            var summaryPara = new Paragraph();
                            summaryPara.Alignment = Element.ALIGN_LEFT;

                            string accountTypeName = shares.ProductName ?? shares.AccountType;
                            string totalLabel = accountTypeName.Contains("Share", StringComparison.OrdinalIgnoreCase) ?
                                               "Total Share Capital" : $"Total {accountTypeName}";

                            summaryPara.Add(new Chunk($"{totalLabel}: ", boldFont));
                            summaryPara.Add(new Chunk(shares.Summary.NetMovement.ToString("N2"),
                                FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.BOLD, shares.Summary.NetMovement >= 0 ? Green : Red)));

                            summaryPara.SpacingAfter = 8f;
                            document.Add(summaryPara);
                        }

                        sharesCounter++;
                    }
                }

                // ===== FOOTER =====
                document.Add(new Paragraph("\n"));
                string footerText = $"Statement Generated on: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";

                if (startDate.HasValue || endDate.HasValue)
                {
                    string dateRangeInfo = "";
                    if (startDate.HasValue && endDate.HasValue)
                        dateRangeInfo = $" | Period: {startDate.Value:dd/MM/yyyy} - {endDate.Value:dd/MM/yyyy}";
                    else if (startDate.HasValue)
                        dateRangeInfo = $" | From: {startDate.Value:dd/MM/yyyy}";
                    else if (endDate.HasValue)
                        dateRangeInfo = $" | Up to: {endDate.Value:dd/MM/yyyy}";

                    footerText += dateRangeInfo;
                }

                footerText += $" | Total Accounts: {sharesStatement.TotalAccounts}";

                var footerPara = new Paragraph(footerText, smallFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 8f
                };
                document.Add(footerPara);

                // ===== FOOTER NOTES =====
                document.Add(new Paragraph("\n"));
                document.Add(new Paragraph("This is a system generated shares/savings statement.", smallFont)
                {
                    Alignment = Element.ALIGN_CENTER
                });
                document.Add(new Paragraph("For any queries, contact: rubanisacco@gmail.com", smallFont)
                {
                    Alignment = Element.ALIGN_CENTER
                });

                document.Close();
                writer.Close();

                return ms.ToArray();
            }
        }


        [HttpGet]
        [Route("GetMemberLoanStatementByReference2/{reference2}")]
        public async Task<HttpResponseMessage> GetMemberLoanStatementByReference2(
     string reference2,
     DateTime? startDate = null,
     DateTime? endDate = null,
     bool downloadPdf = false)
        {
            try
            {
                // Step 1: Get the customer ID from reference2
                var customerId = await GetCustomerIdByReference2(reference2);

                if (!customerId.HasValue)
                {
                    // If no customer found and PDF is requested, return a PDF with "no customer" message
                    if (downloadPdf)
                    {
                        byte[] pdfBytes = GenerateNoCustomerPdf(reference2);

                        var response = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new ByteArrayContent(pdfBytes)
                        };

                        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                        response.Content.Headers.ContentDisposition =
                            new ContentDispositionHeaderValue("attachment")
                            {
                                FileName = $"NoCustomer_{reference2}_{DateTime.Now:yyyyMMdd}.pdf"
                            };

                        return response;
                    }
                    else
                    {
                        var notFoundResponse = Request.CreateResponse(HttpStatusCode.NotFound);
                        notFoundResponse.Content = new StringContent(
                            JsonConvert.SerializeObject(new ApiResponse<object>
                            {
                                Success = false,
                                Message = $"No customer found with Reference2/Member Number: '{reference2}'",
                                Data = null
                            }),
                            Encoding.UTF8,
                            "application/json");
                        return notFoundResponse;
                    }
                }

                // Step 2: Call the loan-only method with the found customer ID
                return await GetMemberLoanStatementOnly(customerId.Value, startDate, endDate, downloadPdf);
            }
            catch (Exception ex)
            {
                var errorResponse = Request.CreateResponse(HttpStatusCode.InternalServerError);
                errorResponse.Content = new StringContent(
                    JsonConvert.SerializeObject(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "An error occurred while retrieving member loan statement by reference2.",
                        Data = ex.Message + " | Inner: " + (ex.InnerException?.Message ?? "None")
                    }),
                    Encoding.UTF8,
                    "application/json");
                return errorResponse;
            }
        }



        [HttpGet]
        [Route("GetMemberLoanStatementOnly/{customerId}")]
        public async Task<HttpResponseMessage> GetMemberLoanStatementOnly(
    Guid customerId,
    DateTime? startDate = null,
    DateTime? endDate = null,
    bool downloadPdf = false)
        {
            try
            {
                var memberLoanStatement = new MemberLoanOnlyStatementResult
                {
                    CustomerId = customerId,
                    StartDate = startDate,
                    EndDate = endDate
                };

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // ===== GET LOANS INFORMATION ONLY =====
                    var allLoanStatements = new List<LoanStatementResult>();

                    using (var loanCommand = new SqlCommand("sp_GenerateMemberLoanStatement", connection))
                    {
                        loanCommand.CommandType = CommandType.StoredProcedure;
                        loanCommand.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;

                        if (startDate.HasValue)
                            loanCommand.Parameters.Add("@StartDate", SqlDbType.Date).Value = startDate.Value.Date;
                        else
                            loanCommand.Parameters.Add("@StartDate", SqlDbType.Date).Value = DBNull.Value;

                        if (endDate.HasValue)
                            loanCommand.Parameters.Add("@EndDate", SqlDbType.Date).Value = endDate.Value.Date;
                        else
                            loanCommand.Parameters.Add("@EndDate", SqlDbType.Date).Value = DBNull.Value;

                        using (var reader = await loanCommand.ExecuteReaderAsync())
                        {
                            // Process each loan (each iteration through the outer while loop is one loan)
                            do
                            {
                                // Result Set 1: Loan Header for current loan
                                if (!await reader.ReadAsync())
                                {
                                    // No more loans or no loans at all
                                    break;
                                }

                                var loanHeader = new
                                {
                                    LoanNumber = reader["LoanNumber"]?.ToString() ?? "",
                                    LoanProductType = reader["LoanProductType"]?.ToString() ?? "",
                                    AppliedLoanAmount = reader["AppliedLoanAmount"] != DBNull.Value ? Convert.ToDecimal(reader["AppliedLoanAmount"]) : 0m,
                                    MonthlyRepayment = reader["MonthlyRepayment"] != DBNull.Value ? Convert.ToDecimal(reader["MonthlyRepayment"]) : 0m,
                                    CustomerAccountId = reader["CustomerAccountId"] != DBNull.Value ? (Guid)reader["CustomerAccountId"] : Guid.Empty,
                                    MemberNumber = reader["MemberNumber"]?.ToString() ?? "",
                                    DisbursedDate = reader["DisbursedDate"] != DBNull.Value ?
                                        Convert.ToDateTime(reader["DisbursedDate"]).ToString("yyyy-MM-dd") : ""
                                };

                                var statementRows = new List<LoanStatementRow>();
                                var summary = new LoanSummary();
                                DateTime? statementStartDate = null;
                                DateTime? statementEndDate = null;

                                // Result Set 2: Statement rows for this loan
                                if (await reader.NextResultAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        var row = new LoanStatementRow
                                        {
                                            TransDate = reader["TransDate"] != DBNull.Value ?
                                                Convert.ToDateTime(reader["TransDate"]).ToString("yyyy-MM-dd") : "",
                                            OpeningBalance = reader["OpeningBalance"] != DBNull.Value ?
                                                Convert.ToDecimal(reader["OpeningBalance"]) : 0m,
                                            Principle = reader["Principle"] != DBNull.Value ?
                                                Convert.ToDecimal(reader["Principle"]) : 0m,
                                            Interest = reader["Interest"] != DBNull.Value ?
                                                Convert.ToDecimal(reader["Interest"]) : 0m,
                                            Amount = reader["Amount"] != DBNull.Value ?
                                                Convert.ToDecimal(reader["Amount"]) : 0m,
                                            LoanBalance = reader["LoanBalance"] != DBNull.Value ?
                                                Convert.ToDecimal(reader["LoanBalance"]) : 0m,
                                            PostingDate = reader["TransDate"] != DBNull.Value ?
                                                Convert.ToDateTime(reader["TransDate"]).ToString("yyyy-MM-dd") : "",
                                            Balance = reader["LoanBalance"] != DBNull.Value ?
                                                Convert.ToDecimal(reader["LoanBalance"]) : 0m
                                        };
                                        statementRows.Add(row);
                                    }
                                }

                                // Result Set 3: Summary for this loan
                                if (await reader.NextResultAsync())
                                {
                                    if (await reader.ReadAsync())
                                    {
                                        summary = new LoanSummary
                                        {
                                            TotalDisbursed = reader["TotalDisbursed"] != DBNull.Value ? Convert.ToDecimal(reader["TotalDisbursed"]) : 0m,
                                            TotalPrincipalRepaid = reader["TotalPrincipalPaid"] != DBNull.Value ? Convert.ToDecimal(reader["TotalPrincipalPaid"]) : 0m,
                                            TotalInterestPaid = reader["TotalInterestPaid"] != DBNull.Value ? Convert.ToDecimal(reader["TotalInterestPaid"]) : 0m,
                                            TotalInterestAccrued = reader["TotalInterestCharged"] != DBNull.Value ? Convert.ToDecimal(reader["TotalInterestCharged"]) : 0m,
                                            OutstandingLoanAmount = reader["OutstandingPrincipal"] != DBNull.Value ? Convert.ToDecimal(reader["OutstandingPrincipal"]) : 0m,
                                            OutstandingLoanInterest = reader["OutstandingInterest"] != DBNull.Value ? Convert.ToDecimal(reader["OutstandingInterest"]) : 0m,
                                            TotalOutstandingBalance = reader["TotalOutstandingBalance"] != DBNull.Value ? Convert.ToDecimal(reader["TotalOutstandingBalance"]) : 0m,
                                            OpeningBalance = reader["OpeningBalance"] != DBNull.Value ? Convert.ToDecimal(reader["OpeningBalance"]) : 0m
                                        };

                                        if (reader["StartDate"] != DBNull.Value)
                                            statementStartDate = Convert.ToDateTime(reader["StartDate"]);
                                        if (reader["EndDate"] != DBNull.Value)
                                            statementEndDate = Convert.ToDateTime(reader["EndDate"]);
                                    }
                                }

                                // Get customer details for this loan
                                var customerData = await GetCustomerDetails(connection, loanHeader.CustomerAccountId, customerId);

                                // Build the full account number
                                string fullAccountNumber = string.Format("{0}-{1}-{2}-{3}",
                                    customerData.BranchCode.ToString().PadLeft(3, '0'),
                                    customerData.CustomerSerialNumber.ToString().PadLeft(7, '0'),
                                    customerData.ProductCode.ToString().PadLeft(3, '0'),
                                    customerData.TargetProductCode.ToString().PadLeft(3, '0'));

                                // Create the loan statement result
                                var loanStatementResult = new LoanStatementResult
                                {
                                    LoanNumber = loanHeader.LoanNumber,
                                    Customer = new CustomerInfo
                                    {
                                        FullName = $"{customerData.FirstName} {customerData.LastName}".Trim(),
                                        AccountNumber = fullAccountNumber,
                                        StaffNo = customerData.Reference2,
                                        PFNumber = customerData.Reference3,
                                        Mobile = customerData.Mobile,
                                        Email = customerData.Email
                                    },
                                    LoanDetails = new LoanDetails
                                    {
                                        LoanNumber = loanHeader.LoanNumber,
                                        LoanProductType = loanHeader.LoanProductType,
                                        AppliedAmount = loanHeader.AppliedLoanAmount,
                                        MonthlyRepayment = loanHeader.MonthlyRepayment,
                                        MemberNumber = loanHeader.MemberNumber,
                                        DisbursedDate = loanHeader.DisbursedDate
                                    },
                                    Statement = statementRows,
                                    Summary = summary,
                                    StartDate = statementStartDate,
                                    EndDate = statementEndDate
                                };

                                allLoanStatements.Add(loanStatementResult);

                                // Move to next loan's first result set (if any)
                            } while (await reader.NextResultAsync());
                        }
                    }

                    // Get customer info
                    CustomerInfo customerInfo = new CustomerInfo
                    {
                        FullName = await GetCustomerName(connection, customerId),
                        AccountNumber = "N/A",
                        StaffNo = await GetCustomerStaffNo(connection, customerId),
                        Mobile = await GetCustomerMobile(connection, customerId),
                        Email = await GetCustomerEmail(connection, customerId),
                        PFNumber = await GetCustomerPFNumber(connection, customerId)
                    };

                    // If there are loans, update customer info with account number from first loan
                    if (allLoanStatements.Count > 0 && allLoanStatements.First().Customer != null)
                    {
                        customerInfo.AccountNumber = allLoanStatements.First().Customer.AccountNumber;
                    }

                    // Populate member loan statement
                    memberLoanStatement.Customer = customerInfo;
                    memberLoanStatement.LoanStatements = allLoanStatements;

                    // Calculate totals
                    memberLoanStatement.TotalLoanBalance = allLoanStatements.Sum(l => l.Summary?.TotalOutstandingBalance ?? 0);
                    memberLoanStatement.TotalLoanAccounts = allLoanStatements.Count;

                    // ALWAYS return PDF if downloadPdf is true, regardless of whether there are loans
                    if (downloadPdf)
                    {
                        byte[] pdfBytes = GenerateMemberLoanOnlyPdf(memberLoanStatement, startDate, endDate);

                        var response = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new ByteArrayContent(pdfBytes)
                        };

                        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

                        string customerName = memberLoanStatement.Customer?.FullName?.Replace(" ", "_") ?? "Customer";
                        string dateRange = "";
                        if (startDate.HasValue && endDate.HasValue)
                            dateRange = $"{startDate.Value:yyyyMMdd}_{endDate.Value:yyyyMMdd}";
                        else if (startDate.HasValue)
                            dateRange = $"from_{startDate.Value:yyyyMMdd}";
                        else if (endDate.HasValue)
                            dateRange = $"to_{endDate.Value:yyyyMMdd}";

                        response.Content.Headers.ContentDisposition =
                            new ContentDispositionHeaderValue("attachment")
                            {
                                FileName = $"MemberLoanStatement_{customerName}_{dateRange}_{DateTime.Now:yyyyMMdd}.pdf"
                            };

                        return response;
                    }
                    else
                    {
                        // Return JSON response only when not requesting PDF
                        var response = Request.CreateResponse(HttpStatusCode.OK);
                        string message = allLoanStatements.Count > 0
                            ? $"Found {memberLoanStatement.LoanStatements.Count} loan account(s)."
                            : "No active loan accounts found for this customer. Customer information is provided.";

                        response.Content = new StringContent(
                            JsonConvert.SerializeObject(new ApiResponse<object>
                            {
                                Success = true,
                                Message = message,
                                Data = memberLoanStatement
                            }),
                            Encoding.UTF8,
                            "application/json");
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent(
                    JsonConvert.SerializeObject(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "An error occurred while retrieving member loan statement.",
                        Data = ex.Message + " | Inner: " + (ex.InnerException?.Message ?? "None")
                    }),
                    Encoding.UTF8,
                    "application/json");
                return response;
            }
        }

        private byte[] GenerateNoCustomerPdf(string reference2)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4, 30, 30, 50, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);

                document.Open();

                // Colors
                BaseColor SkyBlue = new BaseColor(0, 174, 239);
                BaseColor Red = new BaseColor(255, 0, 0);
                BaseColor DarkGray = new BaseColor(26, 26, 26);

                // Fonts
                Font titleFont = FontFactory.GetFont("Book Antiqua", 16f, Font.BOLD, DarkGray);
                Font normalFont = FontFactory.GetFont("Book Antiqua", 12f, Font.NORMAL, DarkGray);

                // Add title
                document.Add(new Paragraph("MEMBER LOAN STATEMENT", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20f
                });

                // Add message
                document.Add(new Paragraph($"No customer found with Reference2/Member Number: '{reference2}'", normalFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 10f
                });

                document.Add(new Paragraph($"Statement Generated on: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", normalFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 20f
                });

                document.Close();
                writer.Close();

                return ms.ToArray();
            }
        }


        private byte[] GenerateMemberLoanOnlyPdf(MemberLoanOnlyStatementResult memberLoanStatement, DateTime? startDate = null, DateTime? endDate = null)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Create document with same margins as individual statements
                Document document = new Document(PageSize.A4, 30, 30, 50, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);

                document.Open();

                // ===== RUBANI SACCO COLOR THEME =====
                BaseColor SkyBlue = new BaseColor(0, 174, 239); // #00AEEF
                BaseColor Red = new BaseColor(255, 0, 0);       // #FF0000
                BaseColor Green = new BaseColor(0, 150, 0);     // #009600
                BaseColor DarkGray = new BaseColor(26, 26, 26); // #1A1A1A
                BaseColor LightGray = new BaseColor(217, 217, 217); // #D9D9D9
                BaseColor MediumGray = new BaseColor(128, 128, 128); // #808080 - Added for section headers
                BaseColor White = BaseColor.WHITE;

                // ===== FONTS - BOOK ANTIQUA FONT WITH SIZE 11 =====
                string bookAntiquaFontName = "Book Antiqua";

                // Try to create Book Antiqua fonts, fallback to Times if not available
                Font titleFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 16f, Font.BOLD, DarkGray);
                Font normalFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.NORMAL, DarkGray);
                Font boldFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.BOLD, DarkGray);
                Font smallFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 9f, Font.NORMAL, DarkGray);
                Font companyNameFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 14f, Font.BOLD, SkyBlue);
                Font companyInfoFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.NORMAL, DarkGray);
                Font sectionHeaderFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12f, Font.BOLD, SkyBlue);
                Font tableHeaderFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 9f, Font.BOLD, DarkGray);
                Font tableCellFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 10f, Font.NORMAL, DarkGray);

                // Fallback fonts if Book Antiqua is not available
                try
                {
                    // Test if Book Antiqua is available
                    var testFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f);
                }
                catch
                {
                    // Fallback to Times New Roman if Book Antiqua is not available
                    bookAntiquaFontName = "Times New Roman";
                    titleFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 16f, Font.BOLD, DarkGray);
                    normalFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.NORMAL, DarkGray);
                    boldFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.BOLD, DarkGray);
                    smallFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 9f, Font.NORMAL, DarkGray);
                    companyNameFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 14f, Font.BOLD, SkyBlue);
                    companyInfoFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.NORMAL, DarkGray);
                    sectionHeaderFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12f, Font.BOLD, SkyBlue);
                    tableHeaderFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 9f, Font.BOLD, DarkGray);
                    tableCellFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 10f, Font.NORMAL, DarkGray);
                }

                // ===== CUSTOM HEADER WITH RUBANI SACCO LOGO =====
                try
                {
                    // Create a table with 1 column for left-aligned content
                    PdfPTable headerTable = new PdfPTable(1)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 8f
                    };

                    // Row 1: Logo left-aligned at top
                    PdfPCell logoCell = new PdfPCell();
                    logoCell.Border = Rectangle.NO_BORDER;
                    logoCell.HorizontalAlignment = Element.ALIGN_LEFT;
                    logoCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    logoCell.PaddingBottom = 3f;

                    // Try to load logo from local path
                    string logoPath = @"C:\Users\ADMIN\source\repos\SwiftFinancialsNew\SwiftFinancialsSolution\TestApis\Assets\Images\rubani-logo.jpeg";
                    if (File.Exists(logoPath))
                    {
                        try
                        {
                            Image logo = Image.GetInstance(logoPath);
                            logo.ScaleToFit(100, 100);
                            logoCell.AddElement(logo);
                        }
                        catch (Exception)
                        {
                            logoCell.AddElement(new Paragraph("RUBANI SACCO", companyNameFont)
                            {
                                Alignment = Element.ALIGN_LEFT
                            });
                        }
                    }
                    else
                    {
                        logoCell.AddElement(new Paragraph("RUBANI SACCO", companyNameFont)
                        {
                            Alignment = Element.ALIGN_LEFT
                        });
                    }

                    headerTable.AddCell(logoCell);

                    // Row 2: Company Info - LEFT ALIGNED
                    PdfPCell infoCell = new PdfPCell();
                    infoCell.Border = Rectangle.NO_BORDER;
                    infoCell.HorizontalAlignment = Element.ALIGN_LEFT;
                    infoCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    infoCell.PaddingTop = 3f;

                    var companyNamePara = new Paragraph("RUBANI SACCO", companyNameFont)
                    {
                        Alignment = Element.ALIGN_LEFT
                    };
                    infoCell.AddElement(companyNamePara);

                    var address = new Paragraph("Rubani House, Off Airport North Embakasi", companyInfoFont)
                    {
                        Alignment = Element.ALIGN_LEFT
                    };
                    infoCell.AddElement(address);

                    var email = new Paragraph("rubanisacco@gmail.com", companyInfoFont)
                    {
                        Alignment = Element.ALIGN_LEFT
                    };
                    infoCell.AddElement(email);

                    headerTable.AddCell(infoCell);
                    document.Add(headerTable);

                    // Add decorative line (Blue-Red-Blue)
                    var lineTable = new PdfPTable(3)
                    {
                        WidthPercentage = 100,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        SpacingAfter = 8f
                    };
                    lineTable.SetWidths(new float[] { 33, 34, 33 });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = SkyBlue,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = Red,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = SkyBlue,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    document.Add(lineTable);
                }
                catch (Exception)
                {
                    var fallbackPara = new Paragraph("RUBANI SACCO\nRubani House, Off Airport North Embakasi\nrubanisacco@gmail.com",
                        companyInfoFont)
                    {
                        Alignment = Element.ALIGN_LEFT,
                        SpacingAfter = 10f
                    };
                    document.Add(fallbackPara);
                }

                // ===== MEMBER LOAN STATEMENT TITLE =====
                string titleText = "MEMBER LOAN STATEMENT";
                if (startDate.HasValue || endDate.HasValue)
                {
                    titleText = "MEMBER LOAN STATEMENT";
                    string dateRangeText = "";

                    if (startDate.HasValue && endDate.HasValue)
                        dateRangeText = $"{startDate.Value:dd/MM/yyyy} to {endDate.Value:dd/MM/yyyy}";
                    else if (startDate.HasValue)
                        dateRangeText = $"From {startDate.Value:dd/MM/yyyy}";
                    else if (endDate.HasValue)
                        dateRangeText = $"To {endDate.Value:dd/MM/yyyy}";

                    if (!string.IsNullOrEmpty(dateRangeText))
                    {
                        document.Add(new Paragraph(titleText, titleFont)
                        {
                            Alignment = Element.ALIGN_CENTER,
                            SpacingAfter = 3f
                        });

                        document.Add(new Paragraph(dateRangeText,
                            FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12f, Font.BOLD, DarkGray))
                        {
                            Alignment = Element.ALIGN_CENTER,
                            SpacingAfter = 8f
                        });
                    }
                    else
                    {
                        document.Add(new Paragraph(titleText, titleFont)
                        {
                            Alignment = Element.ALIGN_CENTER,
                            SpacingAfter = 8f
                        });
                    }
                }
                else
                {
                    document.Add(new Paragraph(titleText, titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 8f
                    });
                }

                // ===== MEMBER INFORMATION SECTION =====
                if (memberLoanStatement.Customer != null)
                {
                    // Create a 2-column table for better alignment
                    PdfPTable memberInfoTable = new PdfPTable(2)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 10f
                    };
                    memberInfoTable.SetWidths(new float[] { 40, 60 });

                    // Left column: Name, Staff No, Mobile
                    Paragraph leftColumn = new Paragraph();
                    leftColumn.Add(new Chunk("Name: ", boldFont));
                    leftColumn.Add(new Chunk(memberLoanStatement.Customer.FullName, normalFont));
                    leftColumn.Add(Chunk.NEWLINE);
                    leftColumn.Add(new Chunk("Staff No: ", boldFont));
                    leftColumn.Add(new Chunk(memberLoanStatement.Customer.PFNumber ?? "N/A", normalFont));
                    leftColumn.Add(Chunk.NEWLINE);
                    leftColumn.Add(new Chunk("Mobile: ", boldFont));
                    leftColumn.Add(new Chunk(memberLoanStatement.Customer.Mobile ?? "N/A", normalFont));

                    PdfPCell leftCell = new PdfPCell(leftColumn)
                    {
                        Border = Rectangle.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_LEFT,
                        Padding = 3
                    };
                    memberInfoTable.AddCell(leftCell);

                    // Right column: MemberNo, Account No, Email
                    Paragraph rightColumn = new Paragraph();
                    rightColumn.Add(new Chunk("MemberNo: ", boldFont));
                    rightColumn.Add(new Chunk(memberLoanStatement.Customer.StaffNo ?? "N/A", normalFont));
                    rightColumn.Add(Chunk.NEWLINE);
                    rightColumn.Add(new Chunk("Account No: ", boldFont));
                    rightColumn.Add(new Chunk(memberLoanStatement.Customer.AccountNumber, normalFont));
                    rightColumn.Add(Chunk.NEWLINE);
                    rightColumn.Add(new Chunk("Email: ", boldFont));
                    rightColumn.Add(new Chunk(memberLoanStatement.Customer.Email ?? "N/A", normalFont));

                    PdfPCell rightCell = new PdfPCell(rightColumn)
                    {
                        Border = Rectangle.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_LEFT,
                        Padding = 3
                    };
                    memberInfoTable.AddCell(rightCell);

                    document.Add(memberInfoTable);
                }

                // ===== LOANS DETAILED SECTION =====
                if (memberLoanStatement.LoanStatements.Count > 0)
                {
                    var loansHeader = new Paragraph("LOANS STATEMENT",
                        FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12f, Font.BOLD, White))
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 3f
                    };

                    PdfPTable loansHeaderTable = new PdfPTable(1)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 10f
                    };

                    PdfPCell loansHeaderCell = new PdfPCell(loansHeader)
                    {
                        BackgroundColor = MediumGray,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 6,
                        Border = Rectangle.NO_BORDER,
                        BorderWidthBottom = 2f,
                        BorderColorBottom = SkyBlue
                    };
                    loansHeaderTable.AddCell(loansHeaderCell);
                    document.Add(loansHeaderTable);

                    int loanCounter = 1;
                    foreach (var loan in memberLoanStatement.LoanStatements)
                    {
                        // Loan Header
                        var loanHeaderPara = new Paragraph($"LOAN #{loanCounter}: {loan.LoanNumber}", sectionHeaderFont)
                        {
                            Alignment = Element.ALIGN_LEFT,
                            SpacingAfter = 3f
                        };
                        document.Add(loanHeaderPara);

                        // Loan Details - 3 column layout with Disbursed Date centered
                        PdfPTable loanDetailsTable = new PdfPTable(3)
                        {
                            WidthPercentage = 100,
                            SpacingAfter = 3f
                        };
                        loanDetailsTable.SetWidths(new float[] { 33, 34, 33 });

                        // Format the disbursed date
                        string mainDisbursedDateDisplay = "N/A";
                        if (!string.IsNullOrEmpty(loan.LoanDetails.DisbursedDate))
                        {
                            DateTime disbursedDate;
                            if (DateTime.TryParse(loan.LoanDetails.DisbursedDate, out disbursedDate))
                            {
                                mainDisbursedDateDisplay = disbursedDate.ToString("dd/MM/yyyy");
                            }
                            else
                            {
                                mainDisbursedDateDisplay = loan.LoanDetails.DisbursedDate;
                            }
                        }

                        // Column 1: Loan Product - Left aligned
                        Paragraph col1Details = new Paragraph();
                        col1Details.Add(new Chunk("Loan Product: ", boldFont));
                        col1Details.Add(new Chunk(loan.LoanDetails.LoanProductType, normalFont));

                        PdfPCell col1Cell = new PdfPCell(col1Details)
                        {
                            Border = Rectangle.NO_BORDER,
                            Padding = 3,
                            HorizontalAlignment = Element.ALIGN_LEFT,
                            VerticalAlignment = Element.ALIGN_MIDDLE
                        };
                        loanDetailsTable.AddCell(col1Cell);

                        // Column 2: Disbursed Date - CENTERED
                        Paragraph col2Details = new Paragraph();
                        col2Details.Add(new Chunk("Disbursed Date: ", boldFont));
                        col2Details.Add(new Chunk(mainDisbursedDateDisplay, normalFont));

                        PdfPCell col2Cell = new PdfPCell(col2Details)
                        {
                            Border = Rectangle.NO_BORDER,
                            Padding = 3,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            VerticalAlignment = Element.ALIGN_MIDDLE
                        };
                        loanDetailsTable.AddCell(col2Cell);

                        // Column 3: Issued Amount - Right aligned
                        Paragraph col3Details = new Paragraph();
                        col3Details.Add(new Chunk("Issued Amount: ", boldFont));
                        col3Details.Add(new Chunk(loan.LoanDetails.AppliedAmount.ToString("N0"), normalFont));

                        PdfPCell col3Cell = new PdfPCell(col3Details)
                        {
                            Border = Rectangle.NO_BORDER,
                            Padding = 3,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            VerticalAlignment = Element.ALIGN_MIDDLE
                        };
                        loanDetailsTable.AddCell(col3Cell);

                        document.Add(loanDetailsTable);

                        // CURRENT OUTSTANDING - Single line, left and right aligned
                        if (loan.Summary != null)
                        {
                            PdfPTable outstandingTable = new PdfPTable(2)
                            {
                                WidthPercentage = 100,
                                SpacingAfter = 5f
                            };
                            outstandingTable.SetWidths(new float[] { 50, 50 });

                            // Left cell: "CURRENT OUTSTANDING:" label
                            PdfPCell labelCell = new PdfPCell(new Paragraph("CURRENT OUTSTANDING:",
                                FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.BOLD, DarkGray)))
                            {
                                Border = Rectangle.NO_BORDER,
                                HorizontalAlignment = Element.ALIGN_LEFT,
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 3,
                                PaddingTop = 0
                            };
                            outstandingTable.AddCell(labelCell);

                            // Right cell: Value
                            PdfPCell valueCell = new PdfPCell(new Paragraph(loan.Summary.TotalOutstandingBalance.ToString("N0"),
                                FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.BOLD, Red)))
                            {
                                Border = Rectangle.NO_BORDER,
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 3,
                                PaddingTop = 0
                            };
                            outstandingTable.AddCell(valueCell);

                            document.Add(outstandingTable);
                        }

                        // Transaction Table
                        PdfPTable transTable = new PdfPTable(6)
                        {
                            WidthPercentage = 100,
                            SpacingAfter = 5f
                        };
                        transTable.SetWidths(new float[] { 15, 18, 15, 15, 15, 22 });

                        // Table headers
                        PdfPCell dateHeaderCell = new PdfPCell(new Phrase("Date", tableHeaderFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            Padding = 4,
                            PaddingTop = 6,
                            PaddingBottom = 6,
                            BorderWidthTop = 1f,
                            BorderWidthBottom = 1f,
                            BorderWidthLeft = 1f,
                            BorderWidthRight = 0f,
                            BorderColorTop = DarkGray,
                            BorderColorBottom = DarkGray,
                            BorderColorLeft = DarkGray
                        };
                        transTable.AddCell(dateHeaderCell);

                        PdfPCell openingBalanceHeaderCell = new PdfPCell(new Phrase("Opening Balance", tableHeaderFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 4,
                            PaddingTop = 6,
                            PaddingBottom = 6,
                            BorderWidthTop = 1f,
                            BorderWidthBottom = 1f,
                            BorderWidthLeft = 0f,
                            BorderWidthRight = 0f,
                            BorderColorTop = DarkGray,
                            BorderColorBottom = DarkGray
                        };
                        transTable.AddCell(openingBalanceHeaderCell);

                        PdfPCell principleHeaderCell = new PdfPCell(new Phrase("Principle", tableHeaderFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 4,
                            PaddingTop = 6,
                            PaddingBottom = 6,
                            BorderWidthTop = 1f,
                            BorderWidthBottom = 1f,
                            BorderWidthLeft = 0f,
                            BorderWidthRight = 0f,
                            BorderColorTop = DarkGray,
                            BorderColorBottom = DarkGray
                        };
                        transTable.AddCell(principleHeaderCell);

                        PdfPCell interestHeaderCell = new PdfPCell(new Phrase("Interest", tableHeaderFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 4,
                            PaddingTop = 6,
                            PaddingBottom = 6,
                            BorderWidthTop = 1f,
                            BorderWidthBottom = 1f,
                            BorderWidthLeft = 0f,
                            BorderWidthRight = 0f,
                            BorderColorTop = DarkGray,
                            BorderColorBottom = DarkGray
                        };
                        transTable.AddCell(interestHeaderCell);

                        PdfPCell amountHeaderCell = new PdfPCell(new Phrase("Amount", tableHeaderFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 4,
                            PaddingTop = 6,
                            PaddingBottom = 6,
                            BorderWidthTop = 1f,
                            BorderWidthBottom = 1f,
                            BorderWidthLeft = 0f,
                            BorderWidthRight = 0f,
                            BorderColorTop = DarkGray,
                            BorderColorBottom = DarkGray
                        };
                        transTable.AddCell(amountHeaderCell);

                        PdfPCell loanBalanceHeaderCell = new PdfPCell(new Phrase("Loan Balance", tableHeaderFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 4,
                            PaddingTop = 6,
                            PaddingBottom = 6,
                            BorderWidthTop = 1f,
                            BorderWidthBottom = 1f,
                            BorderWidthLeft = 0f,
                            BorderWidthRight = 1f,
                            BorderColorTop = DarkGray,
                            BorderColorBottom = DarkGray,
                            BorderColorRight = DarkGray
                        };
                        transTable.AddCell(loanBalanceHeaderCell);

                        // Add transactions if they exist
                        if (loan.Statement != null && loan.Statement.Count > 0)
                        {
                            for (int i = 0; i < loan.Statement.Count; i++)
                            {
                                var row = loan.Statement[i];
                                bool isLastRow = (i == loan.Statement.Count - 1);

                                // Format date
                                string transDate = "";
                                if (!string.IsNullOrEmpty(row.TransDate))
                                {
                                    DateTime date;
                                    if (DateTime.TryParse(row.TransDate, out date))
                                        transDate = date.ToString("dd/MM/yyyy");
                                    else
                                        transDate = row.TransDate;
                                }

                                // Date cell
                                PdfPCell dateCell = new PdfPCell(new Phrase(transDate, tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_CENTER,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 1f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorLeft = DarkGray,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(dateCell);

                                // Opening Balance cell
                                PdfPCell openingBalanceCell = new PdfPCell(new Phrase(row.OpeningBalance.ToString("N0"), tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(openingBalanceCell);

                                // Principle cell
                                PdfPCell principleCell = new PdfPCell(new Phrase(row.Principle.ToString("N0"), tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(principleCell);

                                // Interest cell
                                PdfPCell interestCell = new PdfPCell(new Phrase(row.Interest.ToString("N0"), tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(interestCell);

                                // Amount cell - with color coding
                                Font amountCellFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 10f, Font.BOLD, row.Amount > 0 ? Green : DarkGray);
                                PdfPCell amountCell = new PdfPCell(new Phrase(row.Amount.ToString("N0"), amountCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(amountCell);

                                // Loan Balance cell
                                PdfPCell balanceCell = new PdfPCell(new Phrase(row.LoanBalance.ToString("N0"), tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 1f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorRight = DarkGray,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(balanceCell);
                            }
                        }
                        else
                        {
                            // No transactions - show initial disbursement
                            decimal issuedAmount = loan.LoanDetails.AppliedAmount;

                            string noTransactionsDateDisplay = "N/A";
                            if (!string.IsNullOrEmpty(loan.LoanDetails.DisbursedDate))
                            {
                                DateTime noTransactionsDate;
                                if (DateTime.TryParse(loan.LoanDetails.DisbursedDate, out noTransactionsDate))
                                {
                                    noTransactionsDateDisplay = noTransactionsDate.ToString("dd/MM/yyyy");
                                }
                                else
                                {
                                    noTransactionsDateDisplay = loan.LoanDetails.DisbursedDate;
                                }
                            }

                            // Date cell
                            PdfPCell dateCell = new PdfPCell(new Phrase(noTransactionsDateDisplay, tableCellFont))
                            {
                                HorizontalAlignment = Element.ALIGN_CENTER,
                                Padding = 3,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthLeft = 1f,
                                BorderWidthRight = 0f,
                                BorderWidthTop = 0f,
                                BorderWidthBottom = 1f,
                                BorderColorLeft = DarkGray,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(dateCell);

                            // Opening Balance cell
                            PdfPCell openingBalanceCell = new PdfPCell(new Phrase(issuedAmount.ToString("N0"), tableCellFont))
                            {
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 3,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderWidthTop = 0f,
                                BorderWidthBottom = 1f,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(openingBalanceCell);

                            // Principle cell
                            PdfPCell principleCell = new PdfPCell(new Phrase("0", tableCellFont))
                            {
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 3,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderWidthTop = 0f,
                                BorderWidthBottom = 1f,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(principleCell);

                            // Interest cell
                            PdfPCell interestCell = new PdfPCell(new Phrase("0", tableCellFont))
                            {
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 3,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderWidthTop = 0f,
                                BorderWidthBottom = 1f,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(interestCell);

                            // Amount cell
                            PdfPCell amountCell = new PdfPCell(new Phrase("0", tableCellFont))
                            {
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 3,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderWidthTop = 0f,
                                BorderWidthBottom = 1f,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(amountCell);

                            // Loan Balance cell
                            PdfPCell balanceCell = new PdfPCell(new Phrase(issuedAmount.ToString("N0"), tableCellFont))
                            {
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 3,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 1f,
                                BorderWidthTop = 0f,
                                BorderWidthBottom = 1f,
                                BorderColorRight = DarkGray,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(balanceCell);
                        }

                        document.Add(transTable);

                        loanCounter++;
                    }
                }

                // ===== FOOTER =====
                document.Add(new Paragraph("\n"));
                string footerText = $"Statement Generated on: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";

                if (startDate.HasValue || endDate.HasValue)
                {
                    string dateRangeInfo = "";
                    if (startDate.HasValue && endDate.HasValue)
                        dateRangeInfo = $" | Period: {startDate.Value:dd/MM/yyyy} - {endDate.Value:dd/MM/yyyy}";
                    else if (startDate.HasValue)
                        dateRangeInfo = $" | From: {startDate.Value:dd/MM/yyyy}";
                    else if (endDate.HasValue)
                        dateRangeInfo = $" | Up to: {endDate.Value:dd/MM/yyyy}";

                    footerText += dateRangeInfo;
                }

                footerText += $" | Total Loan Accounts: {memberLoanStatement.TotalLoanAccounts}";

                var footerPara = new Paragraph(footerText, smallFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 8f
                };
                document.Add(footerPara);

                // ===== FOOTER NOTES =====
                document.Add(new Paragraph("\n"));
                document.Add(new Paragraph("This is a system generated loan statement.", smallFont)
                {
                    Alignment = Element.ALIGN_CENTER
                });
                document.Add(new Paragraph("For any queries, contact: rubanisacco@gmail.com", smallFont)
                {
                    Alignment = Element.ALIGN_CENTER
                });

                document.Close();
                writer.Close();

                return ms.ToArray();
            }
        }




        public class MemberLoanOnlyStatementResult
        {
            public Guid CustomerId { get; set; }
            public CustomerInfo Customer { get; set; }
            public List<LoanStatementResult> LoanStatements { get; set; } = new List<LoanStatementResult>();
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public decimal TotalLoanBalance { get; set; }
            public int TotalLoanAccounts { get; set; }
        }





        [HttpGet]
        [Route("GetCustomers")]
        public async Task<IHttpActionResult> GetCustomers()
        {
            // Build service context
            var serviceHeader = master.GetServiceHeader();

            // Execute domain call
            var customers = await master._channelService
                                        .FindCustomersAsync(serviceHeader);

            // Guardrail: empty result ≠ error
            if (customers == null || !customers.Any())
                return Ok(new
                {
                    success = true,
                    data = Array.Empty<object>(),
                    message = "No customers found"
                });

            // Standardized response envelope
            return Ok(new
            {
                success = true,
                data = customers,
                count = customers.Count()
            });
        }



        #region
        private LoanCaseStatus? ResolveLoanStatus(string status)
        {

            switch (status.Trim().ToLower())
            {
                case "registered":
                    return LoanCaseStatus.Registered;

                case "appraised":
                    return LoanCaseStatus.Appraised;

                case "approved":
                    return LoanCaseStatus.Approved;

                case "disbursed":
                    return LoanCaseStatus.Disbursed;

                case "rejected":
                    return LoanCaseStatus.Rejected;

                case "deferred":
                    return LoanCaseStatus.Deferred;

                case "audited":
                case "verified": // supporting alias
                    return LoanCaseStatus.Audited;

                case "restructured":
                    return LoanCaseStatus.Restructured;

                default:
                    return null;
            }
        }

        private LoanCaseFilter? ResolveLoanFilter(int filterType)
        {
            if (Enum.IsDefined(typeof(LoanCaseFilter), filterType))
                return (LoanCaseFilter)filterType;

            return null;
        }
        public class SendMessageRequest
        {
            public string PhoneNumber { get; set; }
            public string Message { get; set; }
        }
        public sealed class LoanAppraisalRequest
        {
            [Required]
            public Guid LoanCaseId { get; set; }

            [Range(0, int.MaxValue)]
            public int LoanAuditOption { get; set; }
        }
        private IHttpActionResult FailureResponse(string message) =>
            Content(HttpStatusCode.InternalServerError,
                new ApiResponse<object> { Success = false, Message = message });

        private IHttpActionResult ValidationErrorResponse(object errors) =>
            Content(HttpStatusCode.ExpectationFailed,
                new ApiResponse<object> { Success = false, Message = "Validation failed.", Data = errors });

        private IHttpActionResult NotFoundResponse(string message) =>
            Content(HttpStatusCode.NotFound,
                new ApiResponse<object> { Success = false, Message = message });

        private ApiResponse<object> SuccessResponse(string message) =>
            new ApiResponse<object> { Success = true, Message = message };
        private void MapLoanProductAttributes(LoanCaseDTO2 dto, LoanProductDTO p)
        {
            if (dto == null || p == null)
                return;

            dto.LoanRegistrationPaymentFrequencyPerYear = p.LoanRegistrationPaymentFrequencyPerYear;
            dto.LoanRegistrationMinimumAmount = p.LoanRegistrationMinimumAmount;
            dto.LoanRegistrationMinimumInterestAmount = p.LoanRegistrationMinimumInterestAmount;
            dto.LoanRegistrationMinimumGuarantors = p.LoanRegistrationMinimumGuarantors;
            dto.LoanRegistrationMinimumMembershipPeriod = p.LoanRegistrationMinimumMembershipPeriod;
            dto.LoanRegistrationMaximumGuarantees = p.LoanRegistrationMaximumGuarantees;
            dto.LoanRegistrationExcludeOutstandingLoansOnMaximumEntitlement = p.LoanRegistrationExcludeOutstandingLoansOnMaximumEntitlement;
            dto.LoanRegistrationMaximumSelfGuaranteeEligiblePercentage = p.LoanRegistrationMaximumSelfGuaranteeEligiblePercentage;
            dto.LoanRegistrationLoanProductSection = p.LoanRegistrationLoanProductSection;
            dto.LoanRegistrationLoanProductCategory = p.LoanRegistrationLoanProductCategory;
            dto.LoanRegistrationConsecutiveIncome = p.LoanRegistrationConsecutiveIncome;
            dto.LoanRegistrationInvestmentsMultiplier = p.LoanRegistrationInvestmentsMultiplier;
            dto.LoanRegistrationRejectIfMemberHasBalance = p.LoanRegistrationRejectIfMemberHasBalance;
            dto.LoanRegistrationSecurityRequired = p.LoanRegistrationSecurityRequired;
            dto.LoanRegistrationAllowSelfGuarantee = p.LoanRegistrationAllowSelfGuarantee;
            dto.LoanRegistrationGracePeriod = p.LoanRegistrationGracePeriod;
            dto.LoanRegistrationPaymentDueDate = p.LoanRegistrationPaymentDueDate;
            dto.LoanRegistrationPayoutRecoveryMode = p.LoanRegistrationPayoutRecoveryMode;
            dto.LoanRegistrationPayoutRecoveryPercentage = p.LoanRegistrationPayoutRecoveryPercentage;
            dto.LoanRegistrationAggregateCheckOffRecoveryMode = p.LoanRegistrationAggregateCheckOffRecoveryMode;
            dto.LoanRegistrationChargeClearanceFee = p.LoanRegistrationChargeClearanceFee;
            dto.LoanRegistrationMicrocredit = p.LoanRegistrationMicrocredit;
            dto.LoanRegistrationStandingOrderTrigger = p.LoanRegistrationStandingOrderTrigger;
            dto.LoanRegistrationTrackArrears = p.LoanRegistrationTrackArrears;
            dto.LoanRegistrationChargeArrearsFee = p.LoanRegistrationChargeArrearsFee;
            dto.LoanRegistrationEnforceSystemAppraisalRecommendation = p.LoanRegistrationEnforceSystemAppraisalRecommendation;
            dto.LoanRegistrationBypassAudit = p.LoanRegistrationBypassAudit;
            dto.LoanRegistrationGuarantorSecurityMode = p.LoanRegistrationGuarantorSecurityMode;
            dto.LoanRegistrationRoundingType = p.LoanRegistrationRoundingType;
            dto.LoanRegistrationDisburseMicroLoanLessDeductions = p.LoanRegistrationDisburseMicroLoanLessDeductions;
            dto.LoanRegistrationConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal = p.LoanRegistrationConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal;
            dto.LoanRegistrationThrottleScheduledArrearsRecovery = p.LoanRegistrationThrottleScheduledArrearsRecovery;
            dto.LoanRegistrationCreateStandingOrderOnLoanAudit = p.LoanRegistrationCreateStandingOrderOnLoanAudit;

            // Interest attributes
            dto.LoanInterestAnnualPercentageRate = p.LoanInterestAnnualPercentageRate;
            dto.LoanInterestChargeMode = p.LoanInterestChargeMode;
            dto.LoanInterestRecoveryMode = p.LoanInterestRecoveryMode;
            dto.LoanInterestCalculationMode = p.LoanInterestCalculationMode;

            // Loan product descriptions
            dto.LoanProductDescription = p.Description;
            dto.InterestCalculationModeDescription = p.LoanInterestCalculationModeDescription;
            dto.LoanProductSectionDescription = p.LoanRegistrationLoanProductSectionDescription;

            // Term & ceilings
            dto.LoanRegistrationTermInMonths = p.LoanRegistrationTermInMonths;
            dto.LoanRegistrationMaximumAmount = p.LoanRegistrationMaximumAmount;

            // Take home rules
            dto.TakeHomeType = p.TakeHomeType;
            dto.TakeHomePercentage = p.TakeHomePercentage;
            dto.TakeHomeFixedAmount = p.TakeHomeFixedAmount;

            // Core identity linkage
            dto.LoanProductId = p.Id;
        }
        #endregion



        // ===================== HELPERS =====================
        private string GetConnectionString()
        {
            // Get connection string from your configuration
            // This could be from web.config, appsettings.json, or other configuration source
            return System.Configuration.ConfigurationManager.ConnectionStrings["SwiftFinancialsDB_Live"].ConnectionString;
        }


        private void IncrementTrials(SqlConnection conn, string memberNo)
        {
            var cmd = new SqlCommand(
                "UPDATE Registration SET Trials = Trials + 1 WHERE MemberNo = @m", conn);
            cmd.Parameters.AddWithValue("@m", memberNo);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Resets failed trials and updates all login tracking fields.
        /// </summary>
        private void UpdateLoginTracking(SqlConnection conn, string memberNo, string ipAddress, string userAgent)
        {
            var cmd = new SqlCommand(@"
        UPDATE Registration 
        SET Trials           = 0,
            LastLoginAt      = GETUTCDATE(),
            LastLoginIP      = @ip,
            LastUserAgent    = @userAgent,
            UpdatedAt        = GETUTCDATE(),
            UpdatedBy        = 'SYSTEM'
        WHERE MemberNo = @memberNo", conn);

            cmd.Parameters.AddWithValue("@memberNo", memberNo);
            cmd.Parameters.AddWithValue("@ip", ipAddress);
            cmd.Parameters.AddWithValue("@userAgent", userAgent);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Extracts real client IP — handles proxies and load balancers.
        /// </summary>
        private string GetClientIp()
        {
            var request = HttpContext.Current?.Request;
            if (request == null) return null;

            // X-Forwarded-For is set by proxies/load balancers
            string ip = request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (!string.IsNullOrEmpty(ip))
                ip = ip.Split(',')[0].Trim(); // Take first IP (original client)
            else
                ip = request.ServerVariables["REMOTE_ADDR"];

            return ip;
        }
    }

    // ===================== SECURITY =====================
    static class PinSecurity
    {
        public static void Create(string pin, out byte[] hash, out byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(pin, 16, 100_000, HashAlgorithmName.SHA256))
            {
                salt = pbkdf2.Salt;
                hash = pbkdf2.GetBytes(32);
            }
        }

        public static bool Verify(string pin, byte[] hash, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(pin, salt, 100_000, HashAlgorithmName.SHA256))
            {
                var computed = pbkdf2.GetBytes(32);
                return FixedTimeEquals(computed, hash);
            }
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;

            var diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }

    }



}

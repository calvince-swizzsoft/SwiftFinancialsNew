using Application.MainBoundedContext.DTO.BackOfficeModule;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using TestApis.Helpers;
using TestApis.Models;

namespace TestApis.Controllers
{
    [RoutePrefix("api/GuarantorManagement")]
    public class GuarantorManagementController : ApiController
    {
        private readonly MasterController _master;
        private readonly string _conn;

        public GuarantorManagementController()
        {
            _master = new MasterController();
            _conn = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }
        public class LoanGuarantorsRequest
        {
            public Guid LoanCaseId { get; set; }
            public ObservableCollection<LoanGuarantorDTO> Guarantors { get; set; }

        }

        public class ChangeGuarantorRequest
        {
            public Guid LoanCaseId { get; set; }
            public Guid GuarantorId { get; set; }

            public decimal? GuaranteedAmount { get; set; }
            public bool? IsActive { get; set; }
            public string Remarks { get; set; }
            public int? Status { get; set; }
        }

        [HttpPost]
        [Route("ChangeGuarantorDetails")]
        public async Task<IHttpActionResult> UpdateSingleGuarantor([FromBody] ChangeGuarantorRequest request)
        {
            if (request == null ||
                request.LoanCaseId == Guid.Empty ||
                request.GuarantorId == Guid.Empty)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid request payload."
                });
            }

            var serviceHeader = _master.GetServiceHeader();

            // 1. Load guarantors for loan case
            var guarantors =
                await _master._channelService.FindLoanGuarantorsByLoanCaseIdAsync(request.LoanCaseId, serviceHeader);

            if (guarantors == null || !guarantors.Any())
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "No guarantors found for this loan case."
                });
            }

            // 2. Locate target guarantor
            var guarantor = guarantors
                .FirstOrDefault(g => g.GuarantorId == request.GuarantorId);

            if (guarantor == null)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Guarantor not found for this loan case."
                });
            }

            // 3. Apply only provided fields (no blind overwrites)
            if (request.GuaranteedAmount.HasValue)
                guarantor.AmountGuaranteed = request.GuaranteedAmount.Value;



            if (!string.IsNullOrWhiteSpace(request.Remarks))
                guarantor.Remarks = request.Remarks;

            if (request.Status.HasValue)
                guarantor.Status = request.Status.Value;

            // 4. Persist
            var result =
                await _master._channelService.UpdateLoanGuarantorsByLoanCaseIdAsync(
                    request.LoanCaseId,
                    guarantors,
                    serviceHeader);

            return Json(new ApiResponse<object>
            {
                Success = result,
                Message = result
                    ? "Guarantor updated successfully."
                    : "Guarantor update failed."
            });
        }






        [HttpGet]
        [Route("GetLoanGuarantors/{loanCaseId:guid}")]
        public async Task<IHttpActionResult> GetLoanGuarantors([FromUri] Guid loanCaseId)
        {
            if (loanCaseId == Guid.Empty)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid loan case identifier."
                });
            }

            try
            {
                var serviceHeader = _master.GetServiceHeader();

                var guarantors =
                    await _master._channelService
                        .FindLoanGuarantorsByLoanCaseIdAsync(loanCaseId, serviceHeader);

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = guarantors?.Count > 0
                        ? $"{guarantors.Count} loan guarantors retrieved."
                        : "No loan guarantors found.",
                    Data = guarantors
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error retrieving loan guarantors.",
                    Data = ex.Message
                });
            }
        }



        [HttpGet]
        [Route("GetLoanGuaranteed/{GuarantorMemberNo}")]
        public async Task<IHttpActionResult> GuarantorMemberNo([FromUri] string GuarantorMemberNo)
        {
            if (string.IsNullOrWhiteSpace(GuarantorMemberNo))
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid guarantor member number."
                });
            }

            try
            {
                var serviceHeader = _master.GetServiceHeader();
                var customers = await _master._channelService.FindCustomersAsync(serviceHeader);

                // Defensive null check
                var coa = customers?.FirstOrDefault(c => c?.Reference2 == GuarantorMemberNo);
                if (coa == null)
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"No customer found with member number {GuarantorMemberNo}."
                    });
                }

                var guarantors = await _master._channelService.FindLoanGuarantorsByCustomerIdAsync(coa.Id, serviceHeader);

                var guarantorList = guarantors?.ToList() ?? new List<LoanGuarantorDTO>();

                return Json(new ApiResponse<List<LoanGuarantorDTO>>
                {
                    Success = true,
                    Message = guarantorList.Any()
                        ? $"{guarantorList.Count} loan guarantor(s) retrieved."
                        : "No loan guarantors found.",
                    Data = guarantorList
                });

            }
            catch (Exception ex)
            {
                // Optional: log ex somewhere for debugging
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error retrieving loan guarantors.",
                    Data = ex.Message
                });
            }
        }


        public class ReplaceGuarantorRequest
        {
            public Guid LoanCaseId { get; set; }
            public Guid OldGuarantorCustomerId { get; set; }
            public Guid NewGuarantorCustomerId { get; set; }
            public decimal? AmountGuaranteed { get; set; }   // optional - keep old amount if not provided
            public string Remarks { get; set; }
            public string ReplacedBy { get; set; }
        }

        [HttpPost]
        [Route("ReplaceGuarantor")]
        public async Task<IHttpActionResult> ReplaceGuarantor([FromBody] ReplaceGuarantorRequest request)
        {
            if (request == null ||
                request.LoanCaseId == Guid.Empty ||
                request.OldGuarantorCustomerId == Guid.Empty ||
                request.NewGuarantorCustomerId == Guid.Empty)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid request payload."
                });
            }

            if (request.OldGuarantorCustomerId == request.NewGuarantorCustomerId)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "New guarantor must be different from the old guarantor."
                });
            }

            var serviceHeader = _master.GetServiceHeader();

            try
            {
                // ================== LOAD LOAN CASE (for loanee + amount checks) ==================
                decimal amountApplied = 0;
                Guid loaneeCustomerId = Guid.Empty;
                Guid loanProductId = Guid.Empty;

                using (var conn = new SqlConnection(_conn))
                {
                    await conn.OpenAsync();

                    using (var cmd = new SqlCommand(@"
                SELECT [CustomerId], [LoanProductId], [AmountApplied]
                FROM [dbo].[swiftFin_LoanCases]
                WHERE [Id] = @LoanCaseId", conn))
                    {
                        cmd.Parameters.Add("@LoanCaseId", SqlDbType.UniqueIdentifier).Value = request.LoanCaseId;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!reader.Read())
                                return Json(new ApiResponse<object>
                                {
                                    Success = false,
                                    Message = "Loan case not found."
                                });

                            loaneeCustomerId = reader.GetGuid(reader.GetOrdinal("CustomerId"));
                            loanProductId = reader.GetGuid(reader.GetOrdinal("LoanProductId"));
                            amountApplied = Convert.ToDecimal(reader["AmountApplied"] == DBNull.Value ? 0 : reader["AmountApplied"]);
                        }
                    }
                }

                // ================== CHECK NEW GUARANTOR IS NOT THE LOANEE ==================
                if (request.NewGuarantorCustomerId == loaneeCustomerId)
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Loanee cannot be added as their own guarantor."
                    });
                }

                // ================== CHECK NEW GUARANTOR ISN'T ALREADY ON THIS LOAN ==================
                using (var conn = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(@"
            SELECT COUNT(1)
            FROM [dbo].[swiftFin_LoanGuarantors]
            WHERE [LoanCaseId] = @LoanCaseId
              AND [CustomerId] = @NewGuarantorCustomerId", conn))
                {
                    cmd.Parameters.Add("@LoanCaseId", SqlDbType.UniqueIdentifier).Value = request.LoanCaseId;
                    cmd.Parameters.Add("@NewGuarantorCustomerId", SqlDbType.UniqueIdentifier).Value = request.NewGuarantorCustomerId;

                    await conn.OpenAsync();
                    var exists = (int)await cmd.ExecuteScalarAsync();

                    if (exists > 0)
                        return Json(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "The new guarantor is already a guarantor for this loan."
                        });
                }

                // ================== LOAD OLD GUARANTOR RECORD ==================
                Guid oldGuarantorRowId = Guid.Empty;
                decimal oldAmountGuaranteed = 0;
                int oldStatus = 0;
                decimal totalShares = 0;
                decimal committedShares = 0;
                decimal amountPledged = 0;
                decimal appraisalFactor = 0;

                using (var conn = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(@"
            SELECT TOP 1
                [Id],
                [AmountGuaranteed],
                [Status],
                [TotalShares],
                [CommittedShares],
                [AmountPledged],
                [AppraisalFactor]
            FROM [dbo].[swiftFin_LoanGuarantors]
            WHERE [LoanCaseId] = @LoanCaseId
              AND [CustomerId] = @OldGuarantorCustomerId", conn))
                {
                    cmd.Parameters.Add("@LoanCaseId", SqlDbType.UniqueIdentifier).Value = request.LoanCaseId;
                    cmd.Parameters.Add("@OldGuarantorCustomerId", SqlDbType.UniqueIdentifier).Value = request.OldGuarantorCustomerId;

                    await conn.OpenAsync();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (!reader.Read())
                            return Json(new ApiResponse<object>
                            {
                                Success = false,
                                Message = "Old guarantor record not found for this loan case."
                            });

                        oldGuarantorRowId = reader.GetGuid(reader.GetOrdinal("Id"));
                        oldAmountGuaranteed = Convert.ToDecimal(reader["AmountGuaranteed"] == DBNull.Value ? 0 : reader["AmountGuaranteed"]);
                        oldStatus = Convert.ToInt32(reader["Status"] == DBNull.Value ? 0 : reader["Status"]);
                        totalShares = Convert.ToDecimal(reader["TotalShares"] == DBNull.Value ? 0 : reader["TotalShares"]);
                        committedShares = Convert.ToDecimal(reader["CommittedShares"] == DBNull.Value ? 0 : reader["CommittedShares"]);
                        amountPledged = Convert.ToDecimal(reader["AmountPledged"] == DBNull.Value ? 0 : reader["AmountPledged"]);
                        appraisalFactor = Convert.ToDecimal(reader["AppraisalFactor"] == DBNull.Value ? 0 : reader["AppraisalFactor"]);
                    }
                }

                decimal newAmountGuaranteed = request.AmountGuaranteed ?? oldAmountGuaranteed;

                if (newAmountGuaranteed <= 0)
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Guaranteed amount must be greater than zero."
                    });

                // ================== PERFORM REPLACEMENT IN A TRANSACTION ==================
                using (var conn = new SqlConnection(_conn))
                {
                    await conn.OpenAsync();

                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            // 1. REMOVE OLD GUARANTOR
                            using (var deleteCmd = new SqlCommand(@"
                        DELETE FROM [dbo].[swiftFin_LoanGuarantors]
                        WHERE [Id] = @Id", conn, tx))
                            {
                                deleteCmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = oldGuarantorRowId;

                                if (await deleteCmd.ExecuteNonQueryAsync() != 1)
                                    throw new Exception("Failed to remove old guarantor.");
                            }

                            // 2. INSERT NEW GUARANTOR
                            using (var insertCmd = new SqlCommand(@"
                        INSERT INTO [dbo].[swiftFin_LoanGuarantors]
                            ([Id], [CustomerId], [LoaneeCustomerId], [LoanProductId], [LoanCaseId],
                             [Status], [TotalShares], [CommittedShares], [AmountGuaranteed],
                             [AmountPledged], [AppraisalFactor], [SequentialId], [CreatedBy], [CreatedDate])
                        VALUES
                            (NEWID(), @CustomerId, @LoaneeCustomerId, @LoanProductId, @LoanCaseId,
                             @Status, @TotalShares, @CommittedShares, @AmountGuaranteed,
                             @AmountPledged, @AppraisalFactor, NEWID(), @CreatedBy, GETUTCDATE())", conn, tx))
                            {
                                insertCmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = request.NewGuarantorCustomerId;
                                insertCmd.Parameters.Add("@LoaneeCustomerId", SqlDbType.UniqueIdentifier).Value = loaneeCustomerId;
                                insertCmd.Parameters.Add("@LoanProductId", SqlDbType.UniqueIdentifier).Value = loanProductId;
                                insertCmd.Parameters.Add("@LoanCaseId", SqlDbType.UniqueIdentifier).Value = request.LoanCaseId;
                                insertCmd.Parameters.Add("@Status", SqlDbType.Int).Value = oldStatus;

                                var pTotalShares = insertCmd.Parameters.Add("@TotalShares", SqlDbType.Decimal);
                                pTotalShares.Precision = 18; pTotalShares.Scale = 2;
                                pTotalShares.Value = totalShares;

                                var pCommittedShares = insertCmd.Parameters.Add("@CommittedShares", SqlDbType.Decimal);
                                pCommittedShares.Precision = 18; pCommittedShares.Scale = 2;
                                pCommittedShares.Value = committedShares;

                                var pAmountGuaranteed = insertCmd.Parameters.Add("@AmountGuaranteed", SqlDbType.Decimal);
                                pAmountGuaranteed.Precision = 18; pAmountGuaranteed.Scale = 2;
                                pAmountGuaranteed.Value = newAmountGuaranteed;

                                var pAmountPledged = insertCmd.Parameters.Add("@AmountPledged", SqlDbType.Decimal);
                                pAmountPledged.Precision = 18; pAmountPledged.Scale = 2;
                                pAmountPledged.Value = amountPledged;

                                var pAppraisalFactor = insertCmd.Parameters.Add("@AppraisalFactor", SqlDbType.Decimal);
                                pAppraisalFactor.Precision = 18; pAppraisalFactor.Scale = 2;
                                pAppraisalFactor.Value = appraisalFactor;

                                insertCmd.Parameters.Add("@CreatedBy", SqlDbType.NVarChar, 200).Value =
                                    string.IsNullOrEmpty(request.ReplacedBy) ? "System" : request.ReplacedBy;

                                if (await insertCmd.ExecuteNonQueryAsync() != 1)
                                    throw new Exception("Failed to insert new guarantor.");
                            }

                            tx.Commit();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }

                // ================== VALIDATE GUARANTOR COVERAGE STILL HOLDS ==================
                decimal totalGuaranteed = 0;
                using (var conn = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(@"
            SELECT ISNULL(SUM([AmountGuaranteed]), 0)
            FROM [dbo].[swiftFin_LoanGuarantors]
            WHERE [LoanCaseId] = @LoanCaseId", conn))
                {
                    cmd.Parameters.Add("@LoanCaseId", SqlDbType.UniqueIdentifier).Value = request.LoanCaseId;
                    await conn.OpenAsync();
                    totalGuaranteed = Convert.ToDecimal(await cmd.ExecuteScalarAsync());
                }

                bool coverageWarning = totalGuaranteed < amountApplied;

                // ================== SMS TO NEW GUARANTOR ==================
                try
                {
                    var serviceHeaderForSms = _master.GetServiceHeader();

                    var newGuarantorCustomer = await _master._channelService
                        .FindCustomerAsync(request.NewGuarantorCustomerId, serviceHeaderForSms);

                    var loaneeCustomer = await _master._channelService
                        .FindCustomerAsync(loaneeCustomerId, serviceHeaderForSms);

                    var loanProduct = await _master._channelService
                        .FindLoanProductAsync(loanProductId, serviceHeaderForSms);

                    if (newGuarantorCustomer != null &&
                        !string.IsNullOrWhiteSpace(newGuarantorCustomer.AddressMobileLine))
                    {
                        var guarantorFullName = $"{newGuarantorCustomer.IndividualFirstName} {newGuarantorCustomer.IndividualLastName}";
                        var loaneeFullName = $"{loaneeCustomer?.IndividualFirstName} {loaneeCustomer?.IndividualLastName}";

                        string message =
                            $"Dear {guarantorFullName}, " +
                            $"you have been added as a guarantor for a {loanProduct?.Description} loan of " +
                            $"KES {newAmountGuaranteed:N0} for {loaneeFullName}.";

                        await SmsHelper.SendMessageAsync(newGuarantorCustomer.AddressMobileLine, message);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Guarantor replacement SMS failed: {ex.Message}");
                }

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = coverageWarning
                        ? "Guarantor replaced successfully. Warning: total guaranteed amount is now below the loan amount applied."
                        : "Guarantor replaced successfully.",
                    Data = new
                    {
                        LoanCaseId = request.LoanCaseId,
                        OldGuarantorCustomerId = request.OldGuarantorCustomerId,
                        NewGuarantorCustomerId = request.NewGuarantorCustomerId,
                        AmountGuaranteed = newAmountGuaranteed,
                        TotalGuaranteed = totalGuaranteed,
                        AmountApplied = amountApplied,
                        CoverageWarning = coverageWarning
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error replacing guarantor.",
                    Data = ex.Message
                });
            }
        }

    }
}

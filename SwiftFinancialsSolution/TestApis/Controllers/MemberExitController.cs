using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.MessagingModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using Org.BouncyCastle.Ocsp;
using SwiftFinancials.Presentation.Infrastructure.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using TestApis.Services;


namespace TestApis.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [AllowAnonymous]
    [RoutePrefix("api/MemberExit")]
    public class MemberExitController : ApiController
    {
        private readonly MasterController master;

        public MemberExitController()
        {
            master = new MasterController();
        }
        private ServiceHeader GetServiceHeader() => new ServiceHeader();

        [HttpGet]
        [Route("GetAllInsuarance")]
        public async Task<IHttpActionResult> GetAllInsuarance()
        {
            try
            {
                ObservableCollection<InsuranceCompanyDTO> page;

                page = await master._channelService.FindInsuranceCompaniesAsync(GetServiceHeader());

                return Ok(new ApiResponse<ObservableCollection<InsuranceCompanyDTO>>
                {
                    Success = true,
                    Message = "Insuarance Company retrieved successfully",
                    Data = page
                });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Errors = ex.Message
                });
            }
        }

        [HttpPost, Route("CreateInsuranceCompany")]
        public async Task<IHttpActionResult> CreateInsuranceCompany(InsuranceCompanyDTO dto)
        {
            try
            {
                dto.ValidateAll();
                if (dto.HasErrors)
                    return Ok(new ApiResponse<InsuranceCompanyDTO>
                    {
                        Success = false,
                        Message = "Validation failed",
                        Data = dto,
                        Errors = dto.ErrorMessages
                    });

                var created = await master._channelService.AddInsuranceCompanyAsync(dto, GetServiceHeader());

                return Ok(new ApiResponse<InsuranceCompanyDTO>
                {
                    Success = true,
                    Message = "Insuarance Company Created successfully",
                    Data = created
                });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse<WithdrawalNotificationDTO>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Errors = ex.Message
                });
            }
        }


        [HttpGet]
        [Route("GetGuarantorSubstitutionHistory")]
        public async Task<IHttpActionResult> GetGuarantorSubstitutionHistory(
    int status,
    DateTime startDate,
    DateTime endDate,
    string text = "",
    int pageIndex = 1,
    int pageSize = 20)
        {
            try
            {
                var history = await master._channelService
                    .FindLoanGuarantorAttachmentHistoryByStatusAndFilterInPageAsync(
                        status,
                        startDate,
                        endDate,
                        text,
                        pageIndex,
                        pageSize,
                        GetServiceHeader());

                return Ok(new ApiResponse<PageCollectionInfo<LoanGuarantorAttachmentHistoryDTO>>
                {
                    Success = true,
                    Message = "Guarantor substitution history retrieved successfully",
                    Data = history
                });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Errors = ex.Message
                });
            }
        }




        [HttpPost, Route("UpdateInsuranceCompany")]
        public async Task<IHttpActionResult> UpdateInsuranceCompany(InsuranceCompanyDTO dto)
        {
            try
            {
                dto.ValidateAll();
                if (dto.HasErrors)
                    return Ok(new ApiResponse<InsuranceCompanyDTO>
                    {
                        Success = false,
                        Message = "Validation failed",
                        Data = dto,
                        Errors = dto.ErrorMessages
                    });

                var created = await master._channelService.UpdateInsuranceCompanyAsync(dto, GetServiceHeader());

                return Ok(new ApiResponse<InsuranceCompanyDTO>
                {
                    Success = true,
                    Message = "Insuarance Comapany Updated successfully",
                    Data = dto
                });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse<WithdrawalNotificationDTO>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Errors = ex.Message
                });
            }
        }


        // =============================================================================
        // Step 1 — Frontend calls GetLoanGuaranteed to show the member's active loans
        // Step 2 — User picks a replacement guarantor
        // Step 3 — Frontend calls SubstituteGuarantor with the loans and replacement ID
        // =============================================================================

        // -------------------------------------------------------------------------
        // Request DTO for the substitution endpoint.
        // Add this class to your DTOs / request models folder.
        //
        public class SubstituteGuarantorDTO
        {
            public Guid ExitingMemberCustomerId { get; set; }
            // The member who is exiting and needs to be released from guarantorship.

            public Guid ReplacementGuarantorCustomerId { get; set; }
            // The member who will take over the guarantor obligations.

            public int ModuleNavigationItemCode { get; set; }
        }
        // -------------------------------------------------------------------------


        /// <summary>
        /// GET: withdrawal-notifications/GetLoanGuaranteed?customerId={id}
        /// Fetches all loans a member is currently guaranteeing.
        /// Call this first to show the user what needs to be substituted.
        /// </summary>
        [HttpGet]
        [Route("GetLoanGuaranteed")]
        public async Task<IHttpActionResult> GetLoanGuaranteed(Guid customerId)
        {
            try
            {
                var loanGuarantors = await master._channelService
                    .FindLoanGuarantorsByCustomerIdAsync(customerId, GetServiceHeader());

                return Ok(new ApiResponse<ObservableCollection<LoanGuarantorDTO>>
                {
                    Success = true,
                    Message = "Loan guarantors retrieved successfully",
                    Data = loanGuarantors
                });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Errors = ex.Message
                });
            }
        }


        /// <summary>
        /// POST: withdrawal-notifications/SubstituteGuarantor
        /// Transfers all active guarantor obligations from an exiting member
        /// to a replacement member, then verifies the exiting member is fully released.
        ///
        /// Typical flow:
        ///   1. Call GetLoanGuaranteed to retrieve the exiting member's attached loans.
        ///   2. User selects a replacement guarantor.
        ///   3. Call this endpoint — it substitutes all attached loans in one operation.
        ///   4. Response confirms the member is fully released and safe to proceed with exit.
        /// </summary>
        [HttpPost]
        [Route("SubstituteGuarantor")]
        public async Task<IHttpActionResult> SubstituteGuarantor(SubstituteGuarantorDTO substituteDto)
        {
            try
            {
                // -------------------------------------------------------------------------
                // Input validation
                // -------------------------------------------------------------------------
                if (substituteDto == null)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid request",
                        Data = false,
                        Errors = "Request body is required"
                    });
                }

                if (substituteDto.ExitingMemberCustomerId == Guid.Empty)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid request",
                        Data = false,
                        Errors = "ExitingMemberCustomerId is required"
                    });
                }

                if (substituteDto.ReplacementGuarantorCustomerId == Guid.Empty)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid request",
                        Data = false,
                        Errors = "ReplacementGuarantorCustomerId is required"
                    });
                }

                if (substituteDto.ExitingMemberCustomerId == substituteDto.ReplacementGuarantorCustomerId)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid request",
                        Data = false,
                        Errors = "The exiting member and the replacement guarantor cannot be the same person"
                    });
                }

                // -------------------------------------------------------------------------
                // Fetch current attached guarantees for the exiting member
                // -------------------------------------------------------------------------
                var loanGuarantors = await master._channelService.FindLoanGuarantorsByCustomerIdAsync(
                    substituteDto.ExitingMemberCustomerId,
                    GetServiceHeader());

                var activeGuarantees = loanGuarantors?
                    .Where(lg => lg.Status == (int)LoanGuarantorStatus.Attached)
                    .ToList();

                if (activeGuarantees == null || !activeGuarantees.Any())
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Message = "Member has no active guarantor obligations — no substitution needed",
                        Data = true
                    });
                }

                // -------------------------------------------------------------------------
                // Perform the substitution — all attached loans transferred in one call
                // -------------------------------------------------------------------------
                var loansToSubstitute = new ObservableCollection<LoanGuarantorDTO>(activeGuarantees);

                bool substitutionOk = await master._channelService.SubstituteLoanGuarantorsAsync(
                    substituteDto.ReplacementGuarantorCustomerId,
                    loansToSubstitute,
                    substituteDto.ModuleNavigationItemCode,
                    GetServiceHeader());

                if (!substitutionOk)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Guarantor substitution failed",
                        Data = false,
                        Errors = "Could not substitute the replacement guarantor. " +
                                  "Please verify the replacement member: " +
                                  "(1) has sufficient share capital, " +
                                  "(2) has not exceeded their maximum guarantee limit, " +
                                  "and (3) is an active member in good standing."
                    });
                }

                // -------------------------------------------------------------------------
                // Verify the exiting member is fully released — re-fetch and confirm
                // no Attached records remain. This is a safety confirmation step.
                // -------------------------------------------------------------------------
                var remainingGuarantees = await master._channelService.FindLoanGuarantorsByCustomerIdAsync(
                    substituteDto.ExitingMemberCustomerId,
                    GetServiceHeader());

                var stillAttached = remainingGuarantees?
                    .Where(lg => lg.Status == (int)LoanGuarantorStatus.Attached)
                    .ToList();

                if (stillAttached != null && stillAttached.Any())
                {
                    var remainingDetails = string.Join("; ", stillAttached
                        .Select(g => $"Loan #{g.LoanCasePaddedCaseNumber} (Borrower: {g.LoaneeCustomerFullName})"));

                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Partial substitution — member not fully released",
                        Data = false,
                        Errors = $"Substitution completed but the following loan(s) could not be transferred: " +
                                  $"{remainingDetails}. Please resolve these manually before proceeding with exit."
                    });
                }

                var substitutedCount = activeGuarantees.Count;

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = $"Guarantor substitution completed. " +
                              $"{substitutedCount} loan guarantee(s) successfully transferred to the replacement member. " +
                              $"Exiting member is fully released and may now proceed with exit.",
                    Data = true
                });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An unexpected error occurred during guarantor substitution",
                    Data = false,
                    Errors = ex.Message
                });
            }
        }



        [HttpPost, Route("")]
        public async Task<IHttpActionResult> Create(WithdrawalNotificationDTO dto)
        {
            try
            {
                // Validate the DTO
                dto.ValidateAll();
                if (dto.HasErrors)
                {
                    return Ok(new ApiResponse<WithdrawalNotificationDTO>
                    {
                        Success = false,
                        Message = "Validation failed",
                        Data = dto,
                        Errors = dto.ErrorMessageResult
                    });
                }

                // DO NOT set MaturityDate here - let the service calculate it
                // The service will calculate based on:
                // - For Deceased: DateTime.Today
                // - For Voluntary/Retiree: Business day based on notice period (60 days)

                var created = await master._channelService.AddWithdrawalNotificationAsync(dto, GetServiceHeader());

                if (created == null)
                {
                    return Ok(new ApiResponse<WithdrawalNotificationDTO>
                    {
                        Success = false,
                        Message = "Failed to create withdrawal notification",
                        Data = null,
                        Errors = "Unable to create withdrawal notification"
                    });
                }

                // Check if there was an error from the service (e.g., existing notification)
                if (!string.IsNullOrEmpty(created.ErrorMessageResult))
                {
                    return Ok(new ApiResponse<WithdrawalNotificationDTO>
                    {
                        Success = false,
                        Message = created.ErrorMessageResult,
                        Data = created,
                        Errors = created.ErrorMessageResult
                    });
                }

                return Ok(new ApiResponse<WithdrawalNotificationDTO>
                {
                    Success = true,
                    Message = "Withdrawal notification created successfully. Awaiting approval.",
                    Data = created
                });
            }
            catch (Exception ex)
            {
                // Log the exception
                // _logger.LogError(ex, "Error creating withdrawal notification for customer {CustomerId}", dto.CustomerId);

                return Ok(new ApiResponse<WithdrawalNotificationDTO>
                {
                    Success = false,
                    Message = "An unexpected error occurred while creating the withdrawal notification",
                    Errors = "Please contact system administrator"
                });
            }
        }


        [HttpPost, Route("approve")]
        public async Task<IHttpActionResult> Approve(ApproveWithdrawalDTO dto)
        {
            try
            {
                if (dto == null || dto.WithdrawalNotificationId == Guid.Empty)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid withdrawal notification",
                        Data = false,
                        Errors = "Withdrawal notification ID is required"
                    });
                }

                // First get the existing notification
                var existingNotification = await master._channelService.FindWithdrawalNotificationAsync(dto.WithdrawalNotificationId, GetServiceHeader());

                if (existingNotification == null)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Withdrawal notification not found",
                        Data = false,
                        Errors = "The specified withdrawal notification does not exist"
                    });
                }

                // Check if it's in a valid state for approval
                if (existingNotification.Status != (int)WithdrawalNotificationStatus.Registered &&
                    existingNotification.Status != (int)WithdrawalNotificationStatus.Deferred)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Withdrawal notification cannot be approved",
                        Data = false,
                        Errors = $"Notification is in {existingNotification.StatusDescription} state. Only Registered or Deferred notifications can be approved."
                    });
                }

                // Apply date adjustments if requested (only for Approve, not for Defer)
                if (dto.ApprovalOption == (int)MembershipWithdrawalApprovalOption.Approve)
                {
                    // Adjust Maturity Date if requested
                    if (dto.AdjustMaturityDate && dto.NewMaturityDate.HasValue)
                    {
                        // Validate new maturity date
                        if (dto.NewMaturityDate.Value.Date < DateTime.Today)
                        {
                            return Ok(new ApiResponse<bool>
                            {
                                Success = false,
                                Message = "Invalid maturity date",
                                Data = false,
                                Errors = "Maturity date cannot be in the past"
                            });
                        }

                        existingNotification.MaturityDate = dto.NewMaturityDate.Value;
                    }

                    // Adjust Created Date if requested
                    if (dto.AdjustCreatedDate && dto.NewCreatedDate.HasValue)
                    {
                        // Validate new created date (cannot be in future)
                        if (dto.NewCreatedDate.Value.Date > DateTime.Today)
                        {
                            return Ok(new ApiResponse<bool>
                            {
                                Success = false,
                                Message = "Invalid created date",
                                Data = false,
                                Errors = "Created date cannot be in the future"
                            });
                        }

                        existingNotification.CreatedDate = dto.NewCreatedDate.Value;
                    }
                }

                // Update the DTO with approval information
                existingNotification.ApprovalRemarks = dto.ApprovalRemarks;

                var approvalResult = await master._channelService.ApproveWithdrawalNotificationAsync(
                    existingNotification,
                    dto.ApprovalOption, // 1 = Approve, 2 = Defer
                    GetServiceHeader());

                if (!approvalResult)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Failed to approve withdrawal notification",
                        Data = false,
                        Errors = "Database error occurred while processing approval"
                    });
                }

                var approvalMessage = dto.ApprovalOption == (int)MembershipWithdrawalApprovalOption.Approve
                    ? "approved" : "deferred";

                var adjustmentMessage = "";
                if (dto.ApprovalOption == (int)MembershipWithdrawalApprovalOption.Approve)
                {
                    if (dto.AdjustMaturityDate)
                        adjustmentMessage += $" Maturity date adjusted to {dto.NewMaturityDate:yyyy-MM-dd}.";
                    if (dto.AdjustCreatedDate)
                        adjustmentMessage += $" Created date adjusted to {dto.NewCreatedDate:yyyy-MM-dd}.";
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = $"Withdrawal notification {approvalMessage} successfully.{adjustmentMessage}",
                    Data = true
                });
            }
            catch (Exception ex)
            {
                // Log the exception
                // _logger.LogError(ex, "Error approving withdrawal notification {NotificationId}", dto.WithdrawalNotificationId);

                return Ok(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An unexpected error occurred while approving the withdrawal notification",
                    Data = false,
                    Errors = "Please contact system administrator"
                });
            }
        }


        public class ApproveWithdrawalDTO
        {
            [Required]
            public Guid WithdrawalNotificationId { get; set; }

            [Required]
            [Range(1, 2, ErrorMessage = "Approval option must be 1 (Approve) or 2 (Defer)")]
            public int ApprovalOption { get; set; }

            public string ApprovalRemarks { get; set; }

            // Date adjustment properties (only applicable for Approval)
            public bool AdjustMaturityDate { get; set; }
            public DateTime? NewMaturityDate { get; set; }

            public bool AdjustCreatedDate { get; set; }
            public DateTime? NewCreatedDate { get; set; }
        }
        // GET: Get by ID
        [HttpGet, Route("{id:guid}")]
        public async Task<IHttpActionResult> Get(Guid id)
        {
            try
            {
                var dto = await master._channelService.FindWithdrawalNotificationAsync(id, GetServiceHeader());
                if (dto == null)
                    return Ok(new ApiResponse<WithdrawalNotificationDTO>
                    {
                        Success = false,
                        Message = "Withdrawal notification not found"
                    });

                return Ok(new ApiResponse<WithdrawalNotificationDTO>
                {
                    Success = true,
                    Message = "Withdrawal notification retrieved successfully",
                    Data = dto
                });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse<WithdrawalNotificationDTO>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Errors = ex.Message
                });
            }
        }

        // GET: Get All with paging & optional filtering
        [HttpGet]
        [Route("GetAll")]
        public async Task<IHttpActionResult> GetAll(int pageIndex = 0, int pageSize = 50, string text = null, int? status = null, int? customerFilter = null)
        {
            try
            {
                PageCollectionInfo<WithdrawalNotificationDTO> page;

                if (status.HasValue || customerFilter.HasValue || !string.IsNullOrEmpty(text))
                {
                    page = await master._channelService.FindWithdrawalNotificationsByStatusAndFilterInPageAsync(
                        DateTime.MinValue, DateTime.MaxValue,
                        status ?? (int)WithdrawalNotificationStatus.Registered,
                        text ?? string.Empty,
                        customerFilter ?? 0,
                        pageIndex, pageSize,
                        GetServiceHeader());
                }
                else
                {
                    page = await master._channelService.FindWithdrawalNotificationsInPageAsync(pageIndex, pageSize, GetServiceHeader());
                }

                return Ok(new ApiResponse<PageCollectionInfo<WithdrawalNotificationDTO>>
                {
                    Success = true,
                    Message = "Withdrawal notifications retrieved successfully",
                    Data = page
                });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Errors = ex.Message
                });
            }
        }



        // PUT: Update
        [HttpPut, Route("{id:guid}")]
        public async Task<IHttpActionResult> Update(Guid id, WithdrawalNotificationDTO dto)
        {
            try
            {
                var updated = await master._channelService.UpdateWithdrawalNotificationAsync(dto, GetServiceHeader());
                return Ok(new ApiResponse<bool>
                {
                    Success = updated,
                    Message = updated ? "Withdrawal notification updated successfully" : "Update failed",
                    Data = updated
                });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Errors = ex.Message
                });
            }
        }

        [HttpPost, Route("audit")]
        public async Task<IHttpActionResult> Audit(AuditWithdrawalDTO auditDto)
        {
            try
            {
                if (auditDto == null || auditDto.WithdrawalNotificationId == Guid.Empty)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid audit request",
                        Data = false,
                        Errors = "Withdrawal notification ID is required"
                    });
                }

                // Get existing notification
                var existingNotification = await master._channelService.FindWithdrawalNotificationAsync(auditDto.WithdrawalNotificationId, GetServiceHeader());

                if (existingNotification == null)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Withdrawal notification not found",
                        Data = false,
                        Errors = "The specified withdrawal notification does not exist"
                    });
                }

                // Check if it's in Approved status (must be approved before audit)
                if (existingNotification.Status != (int)WithdrawalNotificationStatus.Approved)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Cannot audit withdrawal notification",
                        Data = false,
                        Errors = $"Notification is in {existingNotification.StatusDescription} state. Only Approved notifications can be audited."
                    });
                }

                // Set audit information
                existingNotification.AuditRemarks = auditDto.AuditRemarks;
                // Note: AuditedBy and AuditedDate will be set in the service

                // Call audit service
                bool auditResult = await master._channelService.AuditWithdrawalNotificationAsync(
                    existingNotification,
                    auditDto.AuditOption, // 1 = Audit, 2 = Defer
                    GetServiceHeader());

                if (!auditResult)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Failed to audit withdrawal notification",
                        Data = false,
                        Errors = existingNotification.ErrorMessageResult ?? "Database error occurred while processing audit"
                    });
                }

                var auditMessage = auditDto.AuditOption == (int)MembershipWithdrawalAuditOption.Audit
                    ? "verified successfully. Ready for settlement."
                    : "deferred for further review.";

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = $"Withdrawal notification {auditMessage}",
                    Data = true
                });
            }
            catch (Exception ex)
            {
                // Log the exception
                // _logger.LogError(ex, "Error auditing withdrawal notification {NotificationId}", auditDto?.WithdrawalNotificationId);

                return Ok(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An unexpected error occurred while auditing the withdrawal notification",
                    Data = false,
                    Errors = "Please contact system administrator"
                });
            }
        }

        public class AuditWithdrawalDTO
        {
            [Required]
            public Guid WithdrawalNotificationId { get; set; }

            [Required]
            [Range(1, 2, ErrorMessage = "Audit option must be 1 (Audit) or 2 (Defer)")]
            public int AuditOption { get; set; } // 1 = Audit, 2 = Defer

            public string AuditRemarks { get; set; }
        }

        public class SettleWithdrawalDTO
        {
            [Required]
            public Guid WithdrawalNotificationId { get; set; }

            [Required]
            [Range(1, 3, ErrorMessage = "Settlement type must be 1 (Normal), 2 (Express), or 3 (Waiver)")]
            public int SettlementType { get; set; } // 1=Normal, 2=Express, 3=Waiver

            public string SettlementRemarks { get; set; }

            public DateTime? SettledDate { get; set; } // Optional, defaults to DateTime.Now in service

            public int ModuleNavigationItemCode { get; set; } // Required for journal entries

            public bool HasReplacementGuarantor { get; set; }

            public decimal NetRefundable { get; set; } // Calculated net refundable amount

            public Guid ReplacementGuarantorCustomerId { get; set; }
        }

        public class WithdrawalSettlementResponseDTO
        {
            public Guid WithdrawalNotificationId { get; set; }
            public string Status { get; set; }
            public DateTime? SettledDate { get; set; }
            public decimal NetRefundable { get; set; }
            public string SettlementType { get; set; }
        }


        //[HttpPost, Route("settle")]
        //public async Task<IHttpActionResult> Settle(SettleWithdrawalDTO settleDto)
        //{
        //    try
        //    {
        //        // Validate input
        //        if (settleDto == null || settleDto.WithdrawalNotificationId == Guid.Empty)
        //        {
        //            return Ok(new ApiResponse<bool>
        //            {
        //                Success = false,
        //                Message = "Invalid settlement request",
        //                Data = false,
        //                Errors = "Withdrawal notification ID is required"
        //            });
        //        }

        //        // Get existing notification
        //        var existingNotification = await master._channelService.FindWithdrawalNotificationAsync(settleDto.WithdrawalNotificationId, GetServiceHeader());

        //        if (existingNotification == null)
        //        {
        //            return Ok(new ApiResponse<bool>
        //            {
        //                Success = false,
        //                Message = "Withdrawal notification not found",
        //                Data = false
        //            });
        //        }

        //        // Check status
        //        if (existingNotification.Status != (int)WithdrawalNotificationStatus.Audited)
        //        {
        //            return Ok(new ApiResponse<bool>
        //            {
        //                Success = false,
        //                Message = "Cannot settle withdrawal notification",
        //                Data = false,
        //                Errors = $"Notification is in {existingNotification.StatusDescription} state. Only Audited notifications can be settled."
        //            });
        //        }

        //        // Check for loan guarantees
        //        var loanGuarantors = await master._channelService.FindLoanGuarantorsByCustomerIdAsync(
        //            existingNotification.CustomerId,
        //            GetServiceHeader());

        //        var hasActiveGuarantees = loanGuarantors != null && loanGuarantors.Any(lg => lg.RecordStatus == 1); // 1 = Active

        //        if (hasActiveGuarantees && !settleDto.HasReplacementGuarantor)
        //        {
        //            return Ok(new ApiResponse<bool>
        //            {
        //                Success = false,
        //                Message = "Cannot process withdrawal - Member is guaranteeing other loans",
        //                Data = false,
        //                Errors = "Member must find a replacement guarantor before exiting."
        //            });
        //        }

        //        // Update DTO with settlement information
        //        existingNotification.SettlementRemarks = settleDto.SettlementRemarks;
        //        existingNotification.SettlementType = settleDto.SettlementType;

        //        // Apply the settlement
        //        bool settlementResult = await master._channelService.SettleWithdrawalNotificationAsync(
        //            existingNotification,
        //            (int)MembershipWithdrawalSettlementOption.Settle,
        //            settleDto.ModuleNavigationItemCode,
        //            GetServiceHeader());

        //        if (settlementResult)
        //        {
        //            return Ok(new ApiResponse<bool>
        //            {
        //                Success = true,
        //                Message = "Member exit completed successfully",
        //                Data = true
        //            });
        //        }
        //        else
        //        {
        //            return Ok(new ApiResponse<bool>
        //            {
        //                Success = false,
        //                Message = "Failed to settle withdrawal notification",
        //                Data = false,
        //                Errors = existingNotification.ErrorMessageResult ?? "An error occurred during settlement processing"
        //            });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Ok(new ApiResponse<bool>
        //        {
        //            Success = false,
        //            Message = "An unexpected error occurred",
        //            Data = false,
        //            Errors = ex.Message
        //        });
        //    }
        //}

        [HttpPost, Route("settle")]
        public async Task<IHttpActionResult> Settle(SettleWithdrawalDTO settleDto)
        {
            try
            {
                // -------------------------------------------------------------------------
                // Basic input validation
                // -------------------------------------------------------------------------
                if (settleDto == null || settleDto.WithdrawalNotificationId == Guid.Empty)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid settlement request",
                        Data = false,
                        Errors = "Withdrawal notification ID is required"
                    });
                }

                // -------------------------------------------------------------------------
                // Fetch the notification
                // -------------------------------------------------------------------------
                var existingNotification = await master._channelService.FindWithdrawalNotificationAsync(
                    settleDto.WithdrawalNotificationId,
                    GetServiceHeader());

                if (existingNotification == null)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Withdrawal notification not found",
                        Data = false,
                        Errors = $"No withdrawal notification found for ID: {settleDto.WithdrawalNotificationId}"
                    });
                }

                // -------------------------------------------------------------------------
                // Status check — must be Audited before settlement can proceed
                // -------------------------------------------------------------------------
                if (existingNotification.Status != (int)WithdrawalNotificationStatus.Audited)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Cannot settle withdrawal notification",
                        Data = false,
                        Errors = $"Notification is in '{existingNotification.StatusDescription}' state. " +
                                  $"Only Audited notifications can be settled."
                    });
                }

                // -------------------------------------------------------------------------
                // Rule 7: 60-Day Notice Period
                //
                // The notice period is measured from CreatedDate (when the withdrawal
                // notification was first raised).
                //
                // Normal  → hard block if fewer than 60 days have elapsed.
                // Express → bypass allowed; PrematureMembershipTerminationCharges apply in service.
                // Waiver  → full bypass, no charges.
                // -------------------------------------------------------------------------
                var settlementType = (MembershipWithdrawalSettlementType)settleDto.SettlementType;

                if (settlementType == MembershipWithdrawalSettlementType.Normal)
                {
                    var daysSinceNotice = (DateTime.Now - existingNotification.CreatedDate).TotalDays;

                    if (daysSinceNotice < 60)
                    {
                        return Ok(new ApiResponse<bool>
                        {
                            Success = false,
                            Message = "Notice period not yet completed",
                            Data = false,
                            Errors = $"Member has only served {(int)daysSinceNotice} of the required 60 days notice. " +
                                      $"Use Express settlement to proceed early (premature termination charges will apply), " +
                                      $"or Waiver if all charges are being waived."
                        });
                    }
                }

                // -------------------------------------------------------------------------
                // Rule 8: Loan Guarantor Check
                //
                // A guarantor record is considered active when:
                //   LoanGuarantorDTO.Status == LoanGuarantorStatus.Attached (0)
                //
                // If the member is still attached to any loan as a guarantor, settlement
                // is blocked unless a replacement guarantor has already been confirmed
                // (settleDto.HasReplacementGuarantor == true).
                // -------------------------------------------------------------------------
                var loanGuarantors = await master._channelService.FindLoanGuarantorsByCustomerIdAsync(
                    existingNotification.CustomerId,
                    GetServiceHeader());

                var activeGuarantees = loanGuarantors?
                    .Where(lg => lg.Status == (int)LoanGuarantorStatus.Attached)
                    .ToList();

                var hasActiveGuarantees = activeGuarantees != null && activeGuarantees.Any();

                if (hasActiveGuarantees && !settleDto.HasReplacementGuarantor)
                {
                    var guaranteeDetails = string.Join("; ", activeGuarantees
                        .Select(g => $"Loan #{g.LoanCasePaddedCaseNumber} (Borrower: {g.LoaneeCustomerFullName})"));

                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Member is an active loan guarantor",
                        Data = false,
                        Errors = $"Settlement cannot proceed. Member is guaranteeing the following loan(s): " +
                                  $"{guaranteeDetails}. A replacement guarantor must be arranged first."
                    });
                }

                // -------------------------------------------------------------------------
                // Build the notification DTO with settlement details and process.
                //
                // Rules 2, 4, 5, 9 are enforced inside the service:
                //   Rule 2 — Net refundable >= 0          (requires loan + investment balance data)
                //   Rule 4 — DEPOSITS-only savings offset  (requires savings account query)
                //   Rule 5 — Loan clearance tariffs        (requires loan product config)
                //   Rule 9 — Share Capital / Entrance Fee  (requires product code loop)
                // -------------------------------------------------------------------------
                existingNotification.SettlementRemarks = settleDto.SettlementRemarks;
                existingNotification.SettlementType = settleDto.SettlementType;

                bool settlementResult = await master._channelService.SettleWithdrawalNotificationAsync(
                    existingNotification,
                    (int)MembershipWithdrawalSettlementOption.Settle,
                    settleDto.ModuleNavigationItemCode,
                    GetServiceHeader());

                if (settlementResult)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Message = "Member exit completed successfully",
                        Data = true
                    });
                }

                // Service sets ErrorMessageResult for Rule 2 failures and any internal errors
                return Ok(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Settlement could not be completed",
                    Data = false,
                    Errors = existingNotification.ErrorMessageResult
                              ?? "An error occurred during settlement processing"
                });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Data = false,
                    Errors = ex.Message
                });
            }
        }



        //// POST: Approve
        //[HttpPost, Route("DeathClaim")]
        //public async Task<IHttpActionResult> DeathClaim( )
        //{
        //    try
        //    {

        //        bool approved = await master._channelService.ProcessDeathSettlementsAsync(dto, (int)MembershipWithdrawalSettlementOption.Settle, 0, GetServiceHeader());
        //        if (approved == true)
        //            return Ok(new ApiResponse<bool>
        //            {
        //                Success = true,
        //                Message = "Member Exit completed Successfull",
        //            });
        //        else
        //            return Ok(new ApiResponse<bool>
        //            {
        //                Success = false,
        //                Message = "An unexpected error occurred",
        //                Errors = dto.ErrorMessageResult
        //            });

        //    }
        //    catch (Exception ex)
        //    {
        //        return Ok(new ApiResponse<bool>
        //        {
        //            Success = false,
        //            Message = "An unexpected error occurred",
        //            Errors = ex.Message
        //        });
        //    }
        //}



        [HttpGet]
        [Route("GetAllSettlementsById")]
        public async Task<IHttpActionResult> GetAllSettlementsBywithdrowalid([FromUri] Guid Id)
        {
            try
            {
                var page = await master._channelService.FindWithdrawalSettlementsByWithdrawalNotificationIdAsync(Id, true, GetServiceHeader());

                return Ok(new ApiResponse<ObservableCollection<WithdrawalSettlementDTO>>
                {
                    Success = true,
                    Message = "Withdrawal notifications retrieved successfully",
                    Data = page
                });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Errors = ex.Message
                });
            }
        }




        [HttpGet]
        [Route("GetTrasactions")]
        public async Task<IHttpActionResult> GetTrasactions()
        {
            try
            {
                var loanCases = await master._channelService.FindLoanCasesAsync(GetServiceHeader());

                var loanCaseDto = loanCases?.FirstOrDefault(lc => lc.AmountApplied > 0);
                bool includeBalances = true;
                bool includeProductDescription = true;
                bool includeInterestBalanceForLoanAccounts = true;
                bool considerMaturityPeriodForInvestmentAccounts = true;

                var selectedCustomerAccount = await master._channelService.FindCustomerAccountAsync(new Guid("3125519E-68FB-F011-BDD2-901B0ECBCFB5"), includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

                var page = await master._channelService.FindGeneralLedgerTransactionsByCustomerAccountIdAndDateRangeAsync(selectedCustomerAccount,(DateTime)loanCaseDto.CreatedDate,DateTime.Now, true, GetServiceHeader());

                return Ok(new ApiResponse<PageCollectionInfo<GeneralLedgerTransaction>>
                {
                    Success = true,
                    Message = "Transactions retrieved successfully",
                    Data = page
                });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Errors = ex.Message
                });
            }
        }

        // Additional endpoints for Verify, Settle, DeathClaim follow same pattern...

    }


    public class Withdrawal
    {
        public Guid Id { get; set; }
        public int status { get; set; }
        public DateTime? SettledDate { get; set; }
        public string SettlementRemarks { get; set; }


    }
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public object Errors { get; set; }
    }
}






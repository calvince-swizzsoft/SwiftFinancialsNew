//using Application.MainBoundedContext.DTO;
//using Application.MainBoundedContext.DTO.AccountsModule;
//using Application.MainBoundedContext.DTO.MessagingModule;
//using Application.MainBoundedContext.DTO.RegistryModule;
//using Infrastructure.Crosscutting.Framework.Utils;
//using Org.BouncyCastle.Ocsp;
//using SwiftFinancials.Presentation.Infrastructure.Models;
//using System;
//using System.Collections.ObjectModel;
//using System.Configuration;
//using System.Linq;
//using System.Security.Cryptography.X509Certificates;
//using System.Threading.Tasks;
//using System.Web.Http;
//using System.Web.Http.Cors;
//using TestApis.Services;

//namespace TestApis.Controllers
//{
//    [EnableCors(origins: "*", headers: "*", methods: "*")]
//    [AllowAnonymous]
//    [RoutePrefix("api/MemberExit")]
//    public class MemberExitController : ApiController
//    {
//        private readonly MasterController master;

//        public MemberExitController()
//        {
//            master = new MasterController();
//        }
//        private ServiceHeader GetServiceHeader() => new ServiceHeader();

//        [HttpGet]
//        [Route("GetAllInsuarance")]
//        public async Task<IHttpActionResult> GetAllInsuarance()
//        {
//            try
//            {
//                ObservableCollection<InsuranceCompanyDTO> page;

//                page = await master._channelService.FindInsuranceCompaniesAsync(GetServiceHeader());

//                return Ok(new ApiResponse<ObservableCollection<InsuranceCompanyDTO>>
//                {
//                    Success = true,
//                    Message = "Insuarance Company retrieved successfully",
//                    Data = page
//                });
//            }
//            catch (Exception ex)
//            {
//                return Ok(new ApiResponse<object>
//                {
//                    Success = false,
//                    Message = "An unexpected error occurred",
//                    Errors = ex.Message
//                });
//            }
//        }

//        [HttpPost, Route("CreateInsuranceCompany")]
//        public async Task<IHttpActionResult> CreateInsuranceCompany(InsuranceCompanyDTO dto)
//        {
//            try
//            {
//                dto.ValidateAll();
//                if (dto.HasErrors)
//                    return Ok(new ApiResponse<InsuranceCompanyDTO>
//                    {
//                        Success = false,
//                        Message = "Validation failed",
//                        Data = dto,
//                        Errors = dto.ErrorMessages
//                    });

//                var created = await master._channelService.AddInsuranceCompanyAsync(dto, GetServiceHeader());

//                return Ok(new ApiResponse<InsuranceCompanyDTO>
//                {
//                    Success = true,
//                    Message = "Insuarance Company Created successfully",
//                    Data = created
//                });
//            }
//            catch (Exception ex)
//            {
//                return Ok(new ApiResponse<WithdrawalNotificationDTO>
//                {
//                    Success = false,
//                    Message = "An unexpected error occurred",
//                    Errors = ex.Message
//                });
//            }
//        }



//        [HttpPost, Route("UpdateInsuranceCompany")]
//        public async Task<IHttpActionResult> UpdateInsuranceCompany(InsuranceCompanyDTO dto)
//        {
//            try
//            {
//                dto.ValidateAll();
//                if (dto.HasErrors)
//                    return Ok(new ApiResponse<InsuranceCompanyDTO>
//                    {
//                        Success = false,
//                        Message = "Validation failed",
//                        Data = dto,
//                        Errors = dto.ErrorMessages
//                    });

//                var created = await master._channelService.UpdateInsuranceCompanyAsync(dto, GetServiceHeader());

//                return Ok(new ApiResponse<InsuranceCompanyDTO>
//                {
//                    Success = true,
//                    Message = "Insuarance Comapany Updated successfully",
//                    Data = dto
//                });
//            }
//            catch (Exception ex)
//            {
//                return Ok(new ApiResponse<WithdrawalNotificationDTO>
//                {
//                    Success = false,
//                    Message = "An unexpected error occurred",
//                    Errors = ex.Message
//                });
//            }
//        }


//        // POST: Create
//        [HttpPost, Route("")]
//        public async Task<IHttpActionResult> Create(WithdrawalNotificationDTO dto)
//        {
//            try
//            {
//                dto.ValidateAll();
//                if (dto.HasErrors)
//                    return Ok(new ApiResponse<WithdrawalNotificationDTO>
//                    {
//                        Success = false,
//                        Message = "Validation failed",
//                        Data = dto,
//                        Errors = dto.ErrorMessageResult
//                    });
//                dto.MaturityDate = new DateTime(2056, 1, 7); // pick exact business-approved date
//                var created = await master._channelService.AddWithdrawalNotificationAsync(dto, GetServiceHeader());

//                return Ok(new ApiResponse<WithdrawalNotificationDTO>
//                {
//                    Success = true,
//                    Message = "Withdrawal notification created successfully",
//                    Data = created
//                });
//            }
//            catch (Exception ex)
//            {
//                return Ok(new ApiResponse<WithdrawalNotificationDTO>
//                {
//                    Success = false,
//                    Message = "An unexpected error occurred",
//                    Errors = ex.Message
//                });
//            }
//        }

//        // GET: Get by ID
//        [HttpGet, Route("{id:guid}")]
//        public async Task<IHttpActionResult> Get(Guid id)
//        {
//            try
//            {
//                var dto = await master._channelService.FindWithdrawalNotificationAsync(id, GetServiceHeader());
//                if (dto == null)
//                    return Ok(new ApiResponse<WithdrawalNotificationDTO>
//                    {
//                        Success = false,
//                        Message = "Withdrawal notification not found"
//                    });

//                return Ok(new ApiResponse<WithdrawalNotificationDTO>
//                {
//                    Success = true,
//                    Message = "Withdrawal notification retrieved successfully",
//                    Data = dto
//                });
//            }
//            catch (Exception ex)
//            {
//                return Ok(new ApiResponse<WithdrawalNotificationDTO>
//                {
//                    Success = false,
//                    Message = "An unexpected error occurred",
//                    Errors = ex.Message
//                });
//            }
//        }

//        // GET: Get All with paging & optional filtering
//        [HttpGet]
//        [Route("GetAll")]
//        public async Task<IHttpActionResult> GetAll(int pageIndex = 0, int pageSize = 50, string text = null, int? status = null, int? customerFilter = null)
//        {
//            try
//            {
//                PageCollectionInfo<WithdrawalNotificationDTO> page;

//                if (status.HasValue || customerFilter.HasValue || !string.IsNullOrEmpty(text))
//                {
//                    page = await master._channelService.FindWithdrawalNotificationsByStatusAndFilterInPageAsync(
//                        DateTime.MinValue, DateTime.MaxValue,
//                        status ?? (int)WithdrawalNotificationStatus.Registered,
//                        text ?? string.Empty,
//                        customerFilter ?? 0,
//                        pageIndex, pageSize,
//                        GetServiceHeader());
//                }
//                else
//                {
//                    page = await master._channelService.FindWithdrawalNotificationsInPageAsync(pageIndex, pageSize, GetServiceHeader());
//                }

//                return Ok(new ApiResponse<PageCollectionInfo<WithdrawalNotificationDTO>>
//                {
//                    Success = true,
//                    Message = "Withdrawal notifications retrieved successfully",
//                    Data = page
//                });
//            }
//            catch (Exception ex)
//            {
//                return Ok(new ApiResponse<object>
//                {
//                    Success = false,
//                    Message = "An unexpected error occurred",
//                    Errors = ex.Message
//                });
//            }
//        }



//        // PUT: Update
//        [HttpPut, Route("{id:guid}")]
//        public async Task<IHttpActionResult> Update(Guid id, WithdrawalNotificationDTO dto)
//        {
//            try
//            {
//                var updated = await master._channelService.UpdateWithdrawalNotificationAsync(dto, GetServiceHeader());
//                return Ok(new ApiResponse<bool>
//                {
//                    Success = updated,
//                    Message = updated ? "Withdrawal notification updated successfully" : "Update failed",
//                    Data = updated
//                });
//            }
//            catch (Exception ex)
//            {
//                return Ok(new ApiResponse<bool>
//                {
//                    Success = false,
//                    Message = "An unexpected error occurred",
//                    Errors = ex.Message
//                });
//            }
//        }


//        // POST: Approve
//        [HttpPost, Route("Settle")]
//        public async Task<IHttpActionResult> Settle(Withdrawal withdrawal)
//        {
//            try
//            {
//                var dto = await master._channelService.FindWithdrawalNotificationAsync(withdrawal.Id, GetServiceHeader());
//                if (dto == null)

//                    return Ok(new ApiResponse<bool> { Success = false, Message = "Notification not found" });
//                dto.Category = (int)WithdrawalNotificationCategory.Voluntary;
//                dto.SettlementRemarks = withdrawal.SettlementRemarks;
//                dto.SettledDate = withdrawal.SettledDate;
//                int membershipWithdrawalSettlementOption = withdrawal.status;
//                bool approved = await master._channelService.SettleWithdrawalNotificationAsync(dto, (int)MembershipWithdrawalSettlementOption.Settle, 0, GetServiceHeader());
//                if (approved == true)
//                    return Ok(new ApiResponse<bool>
//                    {
//                        Success = true,
//                        Message = "Member Exit completed Successfull",
//                    });
//                else
//                    return Ok(new ApiResponse<bool>
//                    {
//                        Success = false,
//                        Message = "An unexpected error occurred",
//                        Errors = dto.ErrorMessageResult
//                    });

//            }
//            catch (Exception ex)
//            {
//                return Ok(new ApiResponse<bool>
//                {
//                    Success = false,
//                    Message = "An unexpected error occurred",
//                    Errors = ex.Message
//                });
//            }
//        }




//        //// POST: Approve
//        //[HttpPost, Route("DeathClaim")]
//        //public async Task<IHttpActionResult> DeathClaim( )
//        //{
//        //    try
//        //    {

//        //        bool approved = await master._channelService.ProcessDeathSettlementsAsync(dto, (int)MembershipWithdrawalSettlementOption.Settle, 0, GetServiceHeader());
//        //        if (approved == true)
//        //            return Ok(new ApiResponse<bool>
//        //            {
//        //                Success = true,
//        //                Message = "Member Exit completed Successfull",
//        //            });
//        //        else
//        //            return Ok(new ApiResponse<bool>
//        //            {
//        //                Success = false,
//        //                Message = "An unexpected error occurred",
//        //                Errors = dto.ErrorMessageResult
//        //            });

//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        return Ok(new ApiResponse<bool>
//        //        {
//        //            Success = false,
//        //            Message = "An unexpected error occurred",
//        //            Errors = ex.Message
//        //        });
//        //    }
//        //}



//        [HttpGet]
//        [Route("GetAllSettlementsById")]
//        public async Task<IHttpActionResult> GetAllSettlementsBywithdrowalid([FromUri] Guid Id)
//        {
//            try
//            {
//                var page = await master._channelService.FindWithdrawalSettlementsByWithdrawalNotificationIdAsync(Id, true, GetServiceHeader());

//                return Ok(new ApiResponse<ObservableCollection<WithdrawalSettlementDTO>>
//                {
//                    Success = true,
//                    Message = "Withdrawal notifications retrieved successfully",
//                    Data = page
//                });
//            }
//            catch (Exception ex)
//            {
//                return Ok(new ApiResponse<object>
//                {
//                    Success = false,
//                    Message = "An unexpected error occurred",
//                    Errors = ex.Message
//                });
//            }
//        }



//        [HttpGet]
//        [Route("GetTrasactions")]
//        public async Task<IHttpActionResult> GetTrasactions()
//        {
//            try
//            {
//                var loanCases = await master._channelService.FindLoanCasesAsync(GetServiceHeader());

//                var loanCaseDto = loanCases?.FirstOrDefault(lc => lc.AmountApplied > 0);
//                bool includeBalances = true;
//                bool includeProductDescription = true;
//                bool includeInterestBalanceForLoanAccounts = true;
//                bool considerMaturityPeriodForInvestmentAccounts = true;

//                var selectedCustomerAccount = await master._channelService.FindCustomerAccountAsync(new Guid("3125519E-68FB-F011-BDD2-901B0ECBCFB5"), includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

//                var page = await master._channelService.FindGeneralLedgerTransactionsByCustomerAccountIdAndDateRangeAsync(selectedCustomerAccount,(DateTime)loanCaseDto.CreatedDate,DateTime.Now, true, GetServiceHeader());

//                return Ok(new ApiResponse<PageCollectionInfo<GeneralLedgerTransaction>>
//                {
//                    Success = true,
//                    Message = "Transactions retrieved successfully",
//                    Data = page
//                });
//            }
//            catch (Exception ex)
//            {
//                return Ok(new ApiResponse<object>
//                {
//                    Success = false,
//                    Message = "An unexpected error occurred",
//                    Errors = ex.Message
//                });
//            }
//        }

//        // Additional endpoints for Verify, Settle, DeathClaim follow same pattern...
//    }


//    public class Withdrawal
//    {
//        public Guid Id { get; set; }
//        public int status { get; set; }
//        public DateTime? SettledDate { get; set; }
//        public string SettlementRemarks { get; set; }


//    }
//    public class ApiResponse<T>
//    {
//        public bool Success { get; set; }
//        public string Message { get; set; }
//        public T Data { get; set; }
//        public object Errors { get; set; }
//    }
//}


using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
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
        }

        public class WithdrawalSettlementResponseDTO
        {
            public Guid WithdrawalNotificationId { get; set; }
            public string Status { get; set; }
            public DateTime? SettledDate { get; set; }
            public decimal NetRefundable { get; set; }
            public string SettlementType { get; set; }
        }


        [HttpPost, Route("settle")]
        public async Task<IHttpActionResult> Settle(SettleWithdrawalDTO settleDto)
        {
            try
            {
                // Validate input
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

                // Get existing notification
                var existingNotification = await master._channelService.FindWithdrawalNotificationAsync(settleDto.WithdrawalNotificationId, GetServiceHeader());

                if (existingNotification == null)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Withdrawal notification not found",
                        Data = false
                    });
                }

                // Check status
                if (existingNotification.Status != (int)WithdrawalNotificationStatus.Audited)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Cannot settle withdrawal notification",
                        Data = false,
                        Errors = $"Notification is in {existingNotification.StatusDescription} state. Only Audited notifications can be settled."
                    });
                }

                // Check for loan guarantees
                var loanGuarantors = await master._channelService.FindLoanGuarantorsByCustomerIdAsync(
                    existingNotification.CustomerId,
                    GetServiceHeader());

                var hasActiveGuarantees = loanGuarantors != null && loanGuarantors.Any(lg => lg.RecordStatus == 1); // 1 = Active

                if (hasActiveGuarantees && !settleDto.HasReplacementGuarantor)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Cannot process withdrawal - Member is guaranteeing other loans",
                        Data = false,
                        Errors = "Member must find a replacement guarantor before exiting."
                    });
                }

                // Update DTO with settlement information
                existingNotification.SettlementRemarks = settleDto.SettlementRemarks;
                existingNotification.SettlementType = settleDto.SettlementType;

                // Apply the settlement
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
                else
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Failed to settle withdrawal notification",
                        Data = false,
                        Errors = existingNotification.ErrorMessageResult ?? "An error occurred during settlement processing"
                    });
                }
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

                var page = await master._channelService.FindGeneralLedgerTransactionsByCustomerAccountIdAndDateRangeAsync(selectedCustomerAccount, (DateTime)loanCaseDto.CreatedDate, DateTime.Now, true, GetServiceHeader());

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












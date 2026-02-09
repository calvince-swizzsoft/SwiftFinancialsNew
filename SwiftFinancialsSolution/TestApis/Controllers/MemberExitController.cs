using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.MessagingModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using Org.BouncyCastle.Ocsp;
using SwiftFinancials.Presentation.Infrastructure.Models;
using System;
using System.Collections.ObjectModel;
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


        // POST: Create
        [HttpPost, Route("")]
        public async Task<IHttpActionResult> Create(WithdrawalNotificationDTO dto)
        {
            try
            {
                dto.ValidateAll();
                if (dto.HasErrors)
                    return Ok(new ApiResponse<WithdrawalNotificationDTO>
                    {
                        Success = false,
                        Message = "Validation failed",
                        Data = dto,
                        Errors = dto.ErrorMessageResult
                    });
                dto.MaturityDate = new DateTime(2056, 1, 7); // pick exact business-approved date
                var created = await master._channelService.AddWithdrawalNotificationAsync(dto, GetServiceHeader());

                return Ok(new ApiResponse<WithdrawalNotificationDTO>
                {
                    Success = true,
                    Message = "Withdrawal notification created successfully",
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


        // POST: Approve
        [HttpPost, Route("Settle")]
        public async Task<IHttpActionResult> Settle(Withdrawal withdrawal)
        {
            try
            {
                var dto = await master._channelService.FindWithdrawalNotificationAsync(withdrawal.Id, GetServiceHeader());
                if (dto == null)

                    return Ok(new ApiResponse<bool> { Success = false, Message = "Notification not found" });
                dto.Category = (int)WithdrawalNotificationCategory.Voluntary;
                dto.SettlementRemarks = withdrawal.SettlementRemarks;
                dto.SettledDate = withdrawal.SettledDate;
                int membershipWithdrawalSettlementOption = withdrawal.status;
                bool approved = await master._channelService.SettleWithdrawalNotificationAsync(dto, (int)MembershipWithdrawalSettlementOption.Settle, 0, GetServiceHeader());
                if (approved == true)
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Message = "Member Exit completed Successfull",
                    });
                else
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "An unexpected error occurred",
                        Errors = dto.ErrorMessageResult
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






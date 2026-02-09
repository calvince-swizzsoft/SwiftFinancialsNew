using Application.MainBoundedContext.DTO.BackOfficeModule;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
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

    }
}

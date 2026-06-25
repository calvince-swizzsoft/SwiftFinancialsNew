using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using SwiftFinancials.Web.Controllers;
using System;
using System.EnterpriseServices;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using System.Windows.Forms.VisualStyles;

namespace CashDepositController.Controllers
{
    [System.Web.Http.RoutePrefix("api/cashdeposits")]
    public class CashDepositController : MasterController
    {
        public async Task<IHttpActionResult> Index()
        {
            try
            {

                var cashdeposits = await _channelService.FindCashDepositRequestsAsync();

                if (cashdeposits == null)
                {
                    return NotFound();
                }

                return Ok(cashdeposits);
            }

            catch (Exception ex)
            {

                return InternalServerError(ex);
            }
        }

        [HttpPost]
        public async Task<IHttpActionResult> Create(CashDepositRequestDTO cashDepositRequestDTO)
        {
            try
            {
                //treasuryDTO.ValidateAll();

                cashDepositRequestDTO.ValidateAll();

                if (!cashDepositRequestDTO.HasErrors)
                {

                    var createdCashDepositRequestDTO = await _channelService.AddCashDepositRequestAsync(cashDepositRequestDTO);

                    return Ok(createdCashDepositRequestDTO);
                }

                else
                {
                    return BadRequest(cashDepositRequestDTO.ErrorMessages.ToString());
                }

            }

            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }



        [HttpPut]
        public async Task<IHttpActionResult> UpdateCashDepositRequestDTO(CashDepositRequestDTO cashDepositRequestDTO)
        {
            try
            {

             var updatedCashDepositRequestDTO = await _channelService.AddCashDepositRequestAsync(cashDepositRequestDTO, GetServiceHeader());
                return Ok(updatedCashDepositRequestDTO);
            }

            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        public async Task<IHttpActionResult> AuthorizeCashDepositRequest(Guid cashDepositRequestId, int customerTransactionAuthOption)
        {

            Guid parseId; 

            try
            {
                if (cashDepositRequestId == Guid.Empty || !Guid.TryParse(cashDepositRequestId.ToString(), out parseId))
                {
                    return BadRequest("Invalid Id");
                }

                var cashDepositRequestDTO = await _channelService.FindCashDepositRequestAsync(cashDepositRequestId, GetServiceHeader());

                var AuthorizedCashDepositRequest = await _channelService.AuthorizeCashDepositRequestAsync(cashDepositRequestDTO, customerTransactionAuthOption, GetServiceHeader());

                return Ok(AuthorizedCashDepositRequest);

            }

            catch (Exception ex)
            {

                return InternalServerError(ex);
            }
        }

        //employee
    }

}
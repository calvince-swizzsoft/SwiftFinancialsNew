using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using TestApis.Controllers;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    [RoutePrefix("api/standingorder")]
    public class StandingorderexecutionController : MasterController
    {
        //public async Task<ActionResult> Create(Guid? Id)
        //{
        //    await ServeNavigationMenus();
        //    ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(string.Empty);
        //    ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(string.Empty);
        //    ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(string.Empty);
        //    ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
        //    ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);


        //    Guid parseId;

        //    //if (Id == Guid.Empty || !Guid.TryParse(Id.ToString(), out parseId))
        //    //{
        //    //    return View();
        //    //}

        //    var loanProduct = await _channelService.FindLoanProductAsync(parseId, GetServiceHeader());

        //    RecurringBatchDTO loanProductDTO = new RecurringBatchDTO();

        //    if (loanProduct != null)
        //    {
        //        loanProductDTO.Month = loanProduct.LoanRegistrationTermInMonths;
        //        loanProductDTO.PostingPeriodId = loanProduct.Id;
        //        //loanProductDTO.EnforceMonthValueDate = loanProduct.Id;
        //        //loanProductDTO.InterestChargedChartOfAccountId = loanProduct.Id;
        //        //loanProductDTO.InterestReceivedChartOfAccountAccountName = loanProduct.AccountName;
        //    }
        //    return View();
        //}

        [HttpPost]
        [Route("")]
        //public async Task<IHttpActionResult> Create(RecurringBatchDTO recurringBatchDTO, List<LoanProductDTO> selectedRows, List<LoanProductDTO> selectedRows1, List<InvestmentProductDTO> selectedRows2, List<EmployeeDTO> selectedRows3, ObservableCollection<LoanProductDTO> loans)
        public async Task<IHttpActionResult> Create([FromBody] StandingOrderBatchRequest standingOrderBatchRequest)
        {
            var recurringBatchDTO = standingOrderBatchRequest.recurringBatch;
            var selectedRows = standingOrderBatchRequest.loanProducts;
            var selectedRows1 = standingOrderBatchRequest.loanProducts1;
            var selectedRows2 = standingOrderBatchRequest.investmentProducts;
            var selectedRows3 = standingOrderBatchRequest.employers;
            var savings = standingOrderBatchRequest.savings;


            recurringBatchDTO.Type = (int)RecurringBatchType.StandingOrder;

            recurringBatchDTO.ValidateAll();

            int Priority = recurringBatchDTO.Priority;

            bool success = false;

            var savingsCollection = new ObservableCollection<SavingsProductDTO>(savings);


            if (!recurringBatchDTO.HasErrors)
            {
                //if (selectedRows.Any())
                if (savingsCollection.Any())
                {
                    foreach (var savingCollection in savingsCollection)
                    {
                        // var savingsProductDTO = await _channelService.ChargeLoanDynamicFeesAsync(recurringBatchDTO, selectedRows1, GetServiceHeader())
                        //await _channelService.ChargeLoanDynamicFeesAsync(recurringBatchDTO, loans, GetServiceHeader());

                        success = await _channelService.ChargeSavingsDynamicFeesAsync(recurringBatchDTO, savingsCollection, GetServiceHeader());
                    }
                }
                return Ok(success);
            }

            else
            {
                var errorMessages = recurringBatchDTO.ErrorMessages;
                //  return View(recurringBatchDTO);
                return Ok(errorMessages);
            }
        }


        [HttpGet]
        [Route("loansproducts")]
        public async Task<IHttpActionResult> GetLoanProductsAsync()
        {
            var loanProductsDTOs = await _channelService.FindLoanProductsAsync(GetServiceHeader());

            return Ok(loanProductsDTOs);
        }


        [HttpGet]
        [Route("savingsproducts")]
        public async Task<IHttpActionResult> GetSavingsProductsAsync()
        {
            try
            {

                var savingProductsDTOs = await _channelService.FindSavingsProductsAsync(GetServiceHeader());


                return Ok(savingProductsDTOs);

            }

            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
       }

        [HttpGet]
        [Route("investmentproducts")]
        public async Task<IHttpActionResult> GetInvestmentProductsAsync()
        {

            try
            {
                var investmentProductsDTOs = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());

                return Ok(investmentProductsDTOs);
            }

            catch (Exception ex){

                return InternalServerError(ex); 
            }
        }

        //[HttpGet]
        //[Route("recurringbatches")]
        //public async Task<IHttpActionResult> GetRecurringBatches()
        //{
        //    try
        //    {

        //        var recurringBatchDTOs = await _channelService.FindRecurringBatchesAsync(GetServiceHeader());

        //        return Ok(recurringBatchDTOs);
        //    }

        //    catch (Exception ex)
        //    {
        //        return InternalServerError(ex);
        //    }


        //}

        //[HttpGet]
        //[Route("recurringbatchentries")]
        //public async Task<IHttpActionResult> GetRecurringBatchEntries()
        //{
        //    try
        //    {

        //        var recurringBatchEntryDTOs = await _channelService.FindRecurringBatchEntriesAsync();

        //        return Ok(recurringBatchEntryDTOs);
        //    }

        //    catch (Exception ex)
        //    {
        //        return InternalServerError(ex);
        //    }


        //}
        public class StandingOrderBatchRequest
        {

            public RecurringBatchDTO recurringBatch { get; set; }

            public List<LoanProductDTO> loanProducts { get; set; } = new List<LoanProductDTO>();

            public List<LoanProductDTO> loanProducts1 { get; set; } = new List<LoanProductDTO>();

            public List<InvestmentProductDTO> investmentProducts { get; set; } = new List<InvestmentProductDTO>();

            public List<EmployeeDTO> employers { get; set; } = new List<EmployeeDTO>();

            public List<SavingsProductDTO> savings { get; set; } = new List<SavingsProductDTO>();
         }


    }
}
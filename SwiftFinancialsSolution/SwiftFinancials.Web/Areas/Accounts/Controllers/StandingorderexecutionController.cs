using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class StandingorderexecutionController : MasterController
    {
        public async Task<ActionResult> Create(Guid? Id)
        {
            await ServeNavigationMenus();
            ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(string.Empty);
            ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(string.Empty);
            ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);


            Guid parseId;

            if (Id == Guid.Empty || !Guid.TryParse(Id.ToString(), out parseId))
            {
                return View();
            }

            var loanProduct = await _channelService.FindLoanProductAsync(parseId, GetServiceHeader());

            RecurringBatchDTO loanProductDTO = new RecurringBatchDTO();

            if (loanProduct != null)
            {
                loanProductDTO.Month = loanProduct.LoanRegistrationTermInMonths;
                loanProductDTO.PostingPeriodId = loanProduct.Id;
                //loanProductDTO.EnforceMonthValueDate = loanProduct.Id;
                //loanProductDTO.InterestChargedChartOfAccountId = loanProduct.Id;
                //loanProductDTO.InterestReceivedChartOfAccountAccountName = loanProduct.AccountName;
            }
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(RecurringBatchDTO recurringBatchDTO, List<LoanProductDTO> selectedRows, ObservableCollection<LoanProductDTO> selectedRows1, List<InvestmentProductDTO> selectedRows2, List<EmployeeDTO> selectedRows3)
        {


            recurringBatchDTO.ValidateAll();
            int Priority = recurringBatchDTO.Priority;
            if (!recurringBatchDTO.HasErrors)
            {
                if (selectedRows.Any())
                {
                    foreach (var selectedRow in selectedRows)
                    {
                        var savingsProductDTO = await _channelService.ChargeLoanDynamicFeesAsync(recurringBatchDTO, selectedRows1, GetServiceHeader());

                      //   await _channelService.UpdateSavingsProductAsync(savingsProductDTO, GetServiceHeader());

                    }
                }
                ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(recurringBatchDTO.Month.ToString());
                ViewBag.MonthsSelectList = GetMonthsAsync(recurringBatchDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetCreditBatchesAsync(recurringBatchDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(recurringBatchDTO.Priority.ToString());
                ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(recurringBatchDTO.Type.ToString());
                return RedirectToAction("Create");
            }

            else
            {
                var errorMessages = recurringBatchDTO.ErrorMessages;
                return View(recurringBatchDTO);
            }
        }
       

        [HttpGet]
        public async Task<JsonResult> GetLoanProductsAsync()
        {
            var loanProductsDTOs = await _channelService.FindLoanProductsAsync(GetServiceHeader());

            return Json(loanProductsDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
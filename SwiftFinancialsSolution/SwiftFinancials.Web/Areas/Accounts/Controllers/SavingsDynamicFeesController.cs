using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Serilog.Core;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
  

    public class SavingsDynamicFeesController : MasterController
    {
       
        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(string.Empty);
            ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(string.Empty);
            ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out _))
            {
                return View();
            }

            var loanProduct = await _channelService.FindChartOfAccountAsync(id.Value, GetServiceHeader());

            if (loanProduct != null)
            {
                var loanProductDTO = new LoanProductDTO
                {
                    ChartOfAccountId = loanProduct.Id,
                    InterestReceivedChartOfAccountId = loanProduct.Id,
                    InterestReceivableChartOfAccountId = loanProduct.Id,
                    InterestChargedChartOfAccountId = loanProduct.Id,
                    ChartOfAccountName = loanProduct.AccountName
                };
                return View(loanProductDTO);
            }
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> Add(RecurringBatchDTO recurringBatchDTO, List<HttpPostedFileBase> selectedFiles)
        {
            await ServeNavigationMenus();

            var recurringBatchDTOs = TempData["RecurringBatchDTOs"] as ObservableCollection<RecurringBatchDTO>;

            if (recurringBatchDTOs == null)
                recurringBatchDTOs = new ObservableCollection<RecurringBatchDTO>();

           // Assuming ExpensePayableEntryDTOs is defined somewhere in your controller
            foreach (var recouringDTO in recurringBatchDTO.Entries)
            {
                // Assuming you need to do some processing before adding to the collection
                // Add your logic here...

                recurringBatchDTOs.Add(recouringDTO);
            }

            TempData["RecurringBatchDTOs"] = recurringBatchDTOs;

            // Populate ViewBag or ViewData if needed for rendering in the view
            ViewBag.RecurringBatchDTOs = recurringBatchDTOs;

            return RedirectToAction("Create", recurringBatchDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(RecurringBatchDTO recurringBatchDTO, List<HttpPostedFileBase> selectedFiles)
        {
            recurringBatchDTO.ValidateAll();

            if (!recurringBatchDTO.HasErrors)
            {
                await _channelService.CapitalizeInterestAsync(1, GetServiceHeader());
                ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(recurringBatchDTO.Month.ToString());
                ViewBag.MonthsSelectList = GetMonthsAsync(recurringBatchDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetCreditBatchesAsync(recurringBatchDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(recurringBatchDTO.Priority.ToString());
                ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(recurringBatchDTO.Type.ToString());
                return RedirectToAction("Index");
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

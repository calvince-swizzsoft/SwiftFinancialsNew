using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Newtonsoft.Json;
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

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var recurringBatchDTOs = await _channelService.FindSavingsProductAsync(parseId, GetServiceHeader());

            RecurringBatchDTO RecurringBatch = new RecurringBatchDTO();

            if (recurringBatchDTOs != null)
            {
                RecurringBatch.Id = recurringBatchDTOs.Id;
                RecurringBatch.BranchDescription = recurringBatchDTOs.Description;
                //RecurringBatch.PostingPeriodDescription = recurringBat;
                //RecurringBatch.CustomerAccountCustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers;
                //RecurringBatch.CustomerAccountCustomerSerialNumber = customer.CustomerSerialNumber;
                //RecurringBatch.CustomerAccountCustomerIndividualIdentityCardNumber = customer.CustomerIndividualIdentityCardNumber;
            }
            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Create(RecurringBatchDTO recurringBatchDTO, List<SavingsProductDTO> selectedRows)
        {           
            
            recurringBatchDTO.ValidateAll();
         int Priority=  recurringBatchDTO.Priority;
            if (!recurringBatchDTO.HasErrors)
            {
                foreach (var selectedRow in selectedRows)
                {
                    var savingsProductDTO = await _channelService.FindSavingsProductAsync(selectedRow.Id, GetServiceHeader());
                    savingsProductDTO.AutomateLedgerFeeCalculation = true;
                    await _channelService.CapitalizeInterestAsync(1, GetServiceHeader());

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

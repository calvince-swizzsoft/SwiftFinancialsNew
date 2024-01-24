using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Infrastructure.Crosscutting.Framework.Models;
using SwiftFinancials.Presentation.Infrastructure.Models;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class CashManagementController : MasterController
    {



        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            bool includeBalance = false;

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var tellers = await _channelService.FindTellerAsync(parseId, includeBalance, GetServiceHeader());

            FiscalCountDTO fiscalCountDTO = new FiscalCountDTO();

            if (tellers != null)
            {
                fiscalCountDTO.TellerId = tellers.Id;
                fiscalCountDTO.TellerDescription = tellers.Description;

            }
            return View(fiscalCountDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Add(TransactionModel transactionModel)
        {
            await ServeNavigationMenus();

            TransactionModels = TempData["TransactionModel"] as ObservableCollection<TransactionModel>;

            if (TransactionModels == null)
                TransactionModels = new ObservableCollection<TransactionModel>();

            foreach (var expensePayableEntryDTO in transactionModel.TransactionModels)
            {

                expensePayableEntryDTO.TotalValue = expensePayableEntryDTO.TotalValue;
                expensePayableEntryDTO.PrimaryDescription = expensePayableEntryDTO.PrimaryDescription;
                expensePayableEntryDTO.SecondaryDescription = expensePayableEntryDTO.SecondaryDescription;
                expensePayableEntryDTO.Reference = expensePayableEntryDTO.Reference;
                TransactionModels.Add(expensePayableEntryDTO);
            };

            TempData["ExpensePayableEntryDTO"] = ExpensePayableEntryDTOs;

            TempData["ExpensePayableDTO"] = transactionModel;

            ViewBag.ExpensePayableEntryDTOs = ExpensePayableEntryDTOs;
            //ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(transactionModel.Type.ToString());
            //ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(transactionModel.Type.ToString());
            //ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(transactionModel.Type.ToString());
            return View("Create", transactionModel);
        }

        [HttpPost]
        public async Task<ActionResult> Create(FiscalCountDTO fiscalCountDTO)
        {
            fiscalCountDTO.ValidateAll();

            if (!fiscalCountDTO.HasErrors)
            {

                TransactionModel transactionModel = new TransactionModel();

                await _channelService.AddCashManagementJournalAsync(fiscalCountDTO, transactionModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = fiscalCountDTO.ErrorMessages;

                return View(fiscalCountDTO);
            }
        }

    }
}
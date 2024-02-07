using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.AdministrationModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Infrastructure.Crosscutting.Framework.Models;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Presentation.Infrastructure.Models;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class CashManagementController : MasterController
    {

        public async Task<ActionResult> Search(Guid? id)
        {
            //string Remarks = "";
            await ServeNavigationMenus();

            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindEmployeeAsync(parseId, GetServiceHeader());

            FiscalCountDTO fiscalCountDTO = new FiscalCountDTO();

            //WithdrawalNotificationDTOs = TempData["WithdrawalNotificationDTOs"] as ObservableCollection<WithdrawalNotificationDTO>;

            if (customer != null)
            {

                fiscalCountDTO.TellerId = customer.Id;
                fiscalCountDTO.Id = customer.Id;
                fiscalCountDTO.TellerDescription = customer.CustomerFullName;
                fiscalCountDTO.BranchDescription = customer.BranchDescription;
                // fiscalCountDTO.SecondaryDescription = customer.EmployeeTypeDescription;
                //fiscalCountDTO.CustomerIndividualIdentityCardNumber = customer.IndividualIdentityCardNumber;
                //fiscalCountDTO.CustomerStationDescription = customer.StationDescription;
                //fiscalCountDTO.CustomerStationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
                //Session["Test"] =Request.Form["h"] + "";
                //string mimi = Session["Test"].ToString();
                if (Session["Reference"] != null)
                {
                    fiscalCountDTO.Reference = Session["Reference"].ToString();
                }
                if (Session["TellerDescription"] != null)
                {
                    fiscalCountDTO.TellerDescription = Session["TellerDescription"].ToString();
                }
                if (Session["BranchDescription"] != null)
                {
                    fiscalCountDTO.TellerDescription = Session["BranchDescription"].ToString();
                }
                //
            }


            return View("Create", fiscalCountDTO);
        }

        [HttpPost]
        public ActionResult AssignText(string Reference, string TellerDescription)
        {
            Session["TellerDescription"] = TellerDescription;
            Session["Reference"] = Reference;
            return null;
        }

        public async Task<ActionResult> Create(Guid? id, FiscalCountDTO fiscalCountDTO)
        {
            await ServeNavigationMenus();

            ViewBag.TreasuryTransactionTypeSelectList = GetTreasuryTransactionTypeSelectList(string.Empty);


            bool includeBalance = false;

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            var bankDTO = await _channelService.FindBankAsync(parseId, GetServiceHeader());

            if (bankDTO != null)
            {

                fiscalCountDTO.BranchId = bankDTO.Id;
                fiscalCountDTO.BranchDescription = bankDTO.Description;
                fiscalCountDTO.Description = bankDTO.Description;
              
            }


            var tellers = await _channelService.FindTellerAsync(parseId, includeBalance, GetServiceHeader());



            if (tellers != null)
            {
                fiscalCountDTO.TellerId = tellers.EmployeeCustomerId;
                fiscalCountDTO.Description = tellers.EmployeeCustomerFullName;

            }


            var treasury = await _channelService.FindTreasuryAsync(parseId, includeBalance, GetServiceHeader());



            if (treasury != null)
            {
                fiscalCountDTO.TreasuryId = treasury.Id;
                fiscalCountDTO.Description = treasury.Description;

            }

            return View(fiscalCountDTO);
        }



        [HttpPost]
        public async Task<ActionResult> Create(FiscalCountDTO fiscalCountDTO)
        {
            fiscalCountDTO.ValidateAll();

            ViewBag.TreasuryTransactionTypeSelectList = GetTreasuryTransactionTypeSelectList(fiscalCountDTO.TransactionType.ToString());

            if (!fiscalCountDTO.HasErrors)
            {

                TransactionModel transactionModel = new TransactionModel();

                await _channelService.AddCashManagementJournalAsync(fiscalCountDTO, transactionModel, GetServiceHeader());


                ViewBag.TreasuryTransactionTypeSelectList = GetTreasuryTransactionTypeSelectList(fiscalCountDTO.TransactionType.ToString());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = fiscalCountDTO.ErrorMessages;
                ViewBag.TreasuryTransactionTypeSelectList = GetTreasuryTransactionTypeSelectList(fiscalCountDTO.TransactionType.ToString());
                return View(fiscalCountDTO);
            }
        }

    }
}
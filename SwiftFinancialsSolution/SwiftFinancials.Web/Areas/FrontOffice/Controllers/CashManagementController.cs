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
                if (Session["TotalAmount"] != null)
                {
                    fiscalCountDTO.TotalAmount = Session["TotalAmount"].ToString();
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
        public ActionResult AssignText(string Reference, string TotalAmount)
        {
            Session["TotalAmount"] = TotalAmount;
            Session["Reference"] = Reference;
            return null;
        }






        public ActionResult YourAction()
        {
            ViewBag.TreasuryTransactionTypeSelectList = GetTreasuryTransactionTypeSelectList(string.Empty);
            return View();
        }




        public async Task<ActionResult> Create(Guid? id, FiscalCountDTO fiscalCountDTO)
        {
            await ServeNavigationMenus();
            // Retrieve the SelectList from ViewBag
            var selectList = ViewBag.TreasuryTransactionTypeSelectList as SelectList;

            // Check if selectList is not null and has a selected value
            if (selectList != null)
            {
                var selectedValue = selectList.SelectedValue;
                // Now you have the selected value
            }


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

                ViewBag.TreasuryTransactionTypeSelectList = GetTreasuryTransactionTypeSelectList(TreasuryTransactionType.TreasuryToBank.ToString());

                ViewBag.TreasuryTransactionTypeSelectList = GetTreasuryTransactionTypeSelectList(TreasuryTransactionType.BankToTreasury.ToString());
            }


            var tellers = await _channelService.FindTellerAsync(parseId, includeBalance, GetServiceHeader());



            if (tellers != null)
            {
                fiscalCountDTO.TellerId = tellers.EmployeeCustomerId;
                fiscalCountDTO.Description = tellers.EmployeeCustomerFullName;
                ViewBag.TreasuryTransactionTypeSelectList = GetTreasuryTransactionTypeSelectList(TreasuryTransactionType.TreasuryToTeller.ToString());
            }


            var treasury = await _channelService.FindTreasuryAsync(parseId, includeBalance, GetServiceHeader());



            if (treasury != null)
            {
                fiscalCountDTO.TreasuryId = treasury.Id;
                fiscalCountDTO.Description = treasury.Description;
                ViewBag.TreasuryTransactionTypeSelectList = GetTreasuryTransactionTypeSelectList(TreasuryTransactionType.TreasuryToTreasury.ToString());
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
                var tariffs = new ObservableCollection<TariffWrapper>();

                TransactionModel transactionModel = new TransactionModel();

                var Journal = await _channelService.AddJournalAsync(transactionModel, tariffs, GetServiceHeader());


                if (Journal != null)
                {
                    var Fiscal = new ObservableCollection<FiscalCountDTO>();

                    foreach (var fiscalCountDTO1 in Fiscal)
                    {
                        fiscalCountDTO1.BranchId = Journal.Id;
                        fiscalCountDTO1.PostingPeriodId = Journal.Id;
                        fiscalCountDTO1.TellerId = Journal.Id;
                        fiscalCountDTO1.TreasuryId = Journal.Id;
                        


                        Fiscal.Add(fiscalCountDTO1);
                    };

                    if (Fiscal.Any())
                        await _channelService.AddCashManagementJournalAsync(fiscalCountDTO, transactionModel, GetServiceHeader());

                }





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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;



namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class CustomerAccountstandingorderController : MasterController
    {

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;
            int customerAccountFilter = 0;
            int customerFilter = 0;
            bool includeProductDescription = false;
            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindStandingOrdersByFilterInPageAsync(jQueryDataTablesModel.sSearch, customerAccountFilter, customerFilter, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, includeProductDescription, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<StandingOrderDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        public async Task<ActionResult> Create(Guid? id, StandingOrderDTO standingOrderDTO)
        {
            await ServeNavigationMenus();



            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);
            ViewBag.loanRegistrationStandingOrderTriggers = GetLoanRegistrationStandingOrderTriggerSelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            bool includeBalances = false;
            bool includeProductDescription = false;
            bool includeInterestBalanceForLoanAccounts = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;
            var benefactorAccounts = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());


            if (standingOrderDTO != null)
            {
                standingOrderDTO.benefactor = benefactorAccounts;
                Session["benefactorAccounts"] = standingOrderDTO.benefactor;
                standingOrderDTO.BenefactorCustomerAccountId = benefactorAccounts.CustomerId;
                standingOrderDTO.BenefactorCustomerAccountCustomerIndividualFirstName = benefactorAccounts.CustomerIndividualFirstName;
                standingOrderDTO.BenefactorCustomerAccountCustomerIndividualLastName = benefactorAccounts.CustomerFullName;
                standingOrderDTO.BenefactorCustomerAccountCustomerSerialNumber = benefactorAccounts.CustomerSerialNumber;
                standingOrderDTO.BenefactorProductDescription = benefactorAccounts.CustomerAccountTypeProductCodeDescription;
                standingOrderDTO.BenefactorCustomerAccountCustomerId = standingOrderDTO.benefactor.Id;


            }

            if (Session["beneficiaryAccounts"] != null)
            {
                standingOrderDTO.Beneficiary = Session["beneficiaryAccounts"] as CustomerAccountDTO;
                Session["beneficiaryAccounts"] = standingOrderDTO.Beneficiary;

            }


            return View(standingOrderDTO);
        }





        [HttpPost]
        public async Task<ActionResult> Create(StandingOrderDTO standingOrderDTO)
        {
            //bool includeBalances = false;
            //bool includeProductDescription = false;
            //bool includeInterestBalanceForLoanAccounts = false;
            //bool considerMaturityPeriodForInvestmentAccounts = false;

            //var benefactorAccounts = await _channelService.FindCustomerAccountAsync(standingOrderDTO.BenefactorCustomerAccountId, false, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());
            //var beneficiaryAccounts = await _channelService.FindCustomerAccountAsync(standingOrderDTO.Beneficiary.CustomerId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            //benefactorAccounts.CustomerId = standingOrderDTO.BenefactorCustomerAccountId;

            //beneficiaryAccounts.CustomerId = standingOrderDTO.BeneficiaryCustomerAccountId;


            standingOrderDTO.Beneficiary = Session["beneficiaryAccounts"] as CustomerAccountDTO;

            standingOrderDTO.benefactor = Session["benefactorAccounts"] as CustomerAccountDTO;

            standingOrderDTO.BenefactorCustomerAccountId = standingOrderDTO.benefactor.CustomerId;
            standingOrderDTO.BenefactorCustomerAccountId = standingOrderDTO.benefactor.Id;
            standingOrderDTO.BenefactorCustomerAccountCustomerId = standingOrderDTO.benefactor.Id;
            standingOrderDTO.BeneficiaryCustomerAccountId = standingOrderDTO.Beneficiary.CustomerId;
            standingOrderDTO.BeneficiaryCustomerAccountId = standingOrderDTO.Beneficiary.Id;
            standingOrderDTO.BeneficiaryCustomerAccountCustomerId = standingOrderDTO.Beneficiary.Id;
            standingOrderDTO.Id = standingOrderDTO.Beneficiary.Id;
            standingOrderDTO.ValidateAll();


            if (!standingOrderDTO.HasErrors)
            {
                await _channelService.AddStandingOrderAsync(standingOrderDTO, GetServiceHeader());
                ViewBag.MonthsSelectList = GetMonthsAsync(standingOrderDTO.ScheduleFrequency.ToString());
                ViewBag.loanRegistrationStandingOrderTriggers = GetLoanRegistrationStandingOrderTriggerSelectList(standingOrderDTO.ScheduleFrequency.ToString());
                ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(standingOrderDTO.ChargeType.ToString());
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = standingOrderDTO.ErrorMessages;
                return View(standingOrderDTO);
            }
        }


        public async Task<ActionResult> Search(Guid? id, StandingOrderDTO standingOrderDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);
            ViewBag.loanRegistrationStandingOrderTriggers = GetLoanRegistrationStandingOrderTriggerSelectList(string.Empty);


            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            bool includeBalances = false;
            bool includeProductDescription = false;
            bool includeInterestBalanceForLoanAccounts = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;



            if (Session["benefactorAccounts"] != null)
            {
                standingOrderDTO.benefactor = Session["benefactorAccounts"] as CustomerAccountDTO;
            }



            var beneficiaryAccounts = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            if (beneficiaryAccounts != null)
            {
                standingOrderDTO.Beneficiary = beneficiaryAccounts;
                Session["beneficiaryAccounts"] = standingOrderDTO.Beneficiary;

                standingOrderDTO.BeneficiaryCustomerAccountId = beneficiaryAccounts.CustomerId;
                standingOrderDTO.BeneficiaryCustomerAccountCustomerIndividualFirstName = beneficiaryAccounts.CustomerIndividualFirstName;
                standingOrderDTO.BeneficiaryCustomerAccountCustomerSerialNumber = beneficiaryAccounts.CustomerSerialNumber;
                standingOrderDTO.BeneficiaryProductDescription = beneficiaryAccounts.CustomerAccountTypeProductCodeDescription;
                standingOrderDTO.BeneficiaryCustomerAccountCustomerId = standingOrderDTO.Beneficiary.Id;




            }

            return View("Create", standingOrderDTO);
        }



        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var expensePayableDTO = await _channelService.FindExpensePayableAsync(id, GetServiceHeader());
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);
            ViewBag.loanRegistrationStandingOrderTriggers = GetLoanRegistrationStandingOrderTriggerSelectList(string.Empty);
            return View(expensePayableDTO);


        }

        ////[HttpPost]
        ////[ValidateAntiForgeryToken]
        ////public async Task<ActionResult> Edit(Guid id, StandingOrderDTO standingOrderDTO)
        ////{

        ////    standingOrderDTO.ValidateAll();
        ////    if (!standingOrderDTO.HasErrors)
        ////    {
        ////        await _channelService.UpdateStandingOrderAsync(standingOrderDTO, GetServiceHeader());
        ////        ViewBag.MonthsSelectList = GetMonthsAsync(standingOrderDTO.ScheduleFrequency.ToString());
        ////        ViewBag.loanRegistrationStandingOrderTriggers = GetLoanRegistrationStandingOrderTriggerSelectList(standingOrderDTO.ScheduleFrequency.ToString());
        ////        ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(standingOrderDTO.ChargeType.ToString());

        ////        return RedirectToAction("Index");
        ////    }
        ////    else
        ////    {
        ////        var errorMessages = standingOrderDTO.ErrorMessages;
        ////        ViewBag.MonthsSelectList = GetMonthsAsync(standingOrderDTO.ScheduleFrequency.ToString());
        ////        ViewBag.loanRegistrationStandingOrderTriggers = GetLoanRegistrationStandingOrderTriggerSelectList(standingOrderDTO.ScheduleFrequency.ToString());
        ////        ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(standingOrderDTO.ChargeType.ToString());
        ////        return View(standingOrderDTO);
        ////    }
        //}


    }
}
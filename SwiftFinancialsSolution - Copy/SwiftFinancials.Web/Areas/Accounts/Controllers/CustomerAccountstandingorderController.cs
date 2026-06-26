using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Infrastructure.Crosscutting.Framework.Utils;
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


        [HttpPost]
        public async Task<JsonResult> BeneficiaryIndex(JQueryDataTablesModel jQueryDataTablesModel, int? productCode2, int? recordStatus2)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = new PageCollectionInfo<CustomerAccountDTO>();

            if (productCode2 != null && recordStatus2 != null)
            {
                pageCollectionInfo = await _channelService.FindCustomerAccountsByProductCodeAndRecordStatusAndFilterInPageAsync((int)productCode2, (int)recordStatus2, jQueryDataTablesModel.sSearch, (int)CustomerFilter.FirstName, pageIndex, jQueryDataTablesModel.iDisplayLength, false, false, false, false, GetServiceHeader());
            }
            else if (productCode2 != null && recordStatus2 == null)
            {
                pageCollectionInfo = await _channelService.FindCustomerAccountsByProductCodeAndFilterInPageAsync((int)productCode2, jQueryDataTablesModel.sSearch, (int)CustomerFilter.FirstName, pageIndex, jQueryDataTablesModel.iDisplayLength, false, false, false, false, GetServiceHeader());
            }
            else if (productCode2 == null && recordStatus2 == null)
            {
                pageCollectionInfo = await _channelService.FindCustomerAccountsInPageAsync(pageIndex, jQueryDataTablesModel.iDisplayLength, false, false, false, false, GetServiceHeader());
            }
            else
            {
                pageCollectionInfo = await _channelService.FindCustomerAccountsInPageAsync(pageIndex, jQueryDataTablesModel.iDisplayLength, false, false, false, false, GetServiceHeader());
            }




            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(ChartOfAccountDTO => ChartOfAccountDTO.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CustomerAccountDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> BenefactorCustomerAccountLookUp(Guid? id, StandingOrderDTO standingOrderDTO)
        {
            await ServeNavigationMenus();

            ViewBag.TransactionTypeSelectList = GetDataAttachmentTransactionTypeTypeSelectList(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);
            ViewBag.loanRegistrationStandingOrderTriggers = GetLoanRegistrationStandingOrderTriggerSelectList(string.Empty);

            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);

            ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);

            ViewBag.ProductCode2 = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus2 = GetRecordStatusSelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            bool includeBalances = true;
            bool includeProductDescription = true;
            bool includeInterestBalanceForLoanAccounts = true;
            bool considerMaturityPeriodForInvestmentAccounts = true;
            var benefactorAccounts = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());


            if (standingOrderDTO != null)
            {
                standingOrderDTO.benefactor = benefactorAccounts;
                Session["benefactorAccounts"] = standingOrderDTO.benefactor;
                standingOrderDTO.BenefactorCustomerAccountId = benefactorAccounts.Id;
                standingOrderDTO.BenefactorCustomerAccountCustomerIndividualFirstName = benefactorAccounts.CustomerIndividualSalutationDescription + " " + benefactorAccounts.CustomerIndividualFirstName + " " + benefactorAccounts.CustomerIndividualLastName;
                standingOrderDTO.BenefactorCustomerAccountCustomerSerialNumber = benefactorAccounts.CustomerSerialNumber;
                standingOrderDTO.BenefactorProductDescription = benefactorAccounts.CustomerAccountTypeTargetProductDescription;
                standingOrderDTO.BenefactorCustomerAccountCustomerId = standingOrderDTO.benefactor.CustomerId;
                standingOrderDTO.BenefactorCustomerAccountCustomerReference1 = standingOrderDTO.benefactor.FullAccountNumber;

                if (Session["beneficiaryAccounts"] != null)
                {
                    standingOrderDTO.Beneficiary = Session["beneficiaryAccounts"] as CustomerAccountDTO;
                    Session["beneficiaryAccounts"] = standingOrderDTO.Beneficiary;

                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        BenefactorCustomerAccountId = standingOrderDTO.BenefactorCustomerAccountId,
                        BenefactorCustomerAccountCustomerFullName = standingOrderDTO.BenefactorCustomerAccountCustomerIndividualFirstName,
                        BenefactorCustomerAccountCustomerSerialNumber = standingOrderDTO.BenefactorCustomerAccountCustomerSerialNumber,
                        BenefactorProductDescription = standingOrderDTO.BenefactorProductDescription,
                        BenefactorCustomerAccountCustomerId = standingOrderDTO.BenefactorCustomerAccountCustomerId,
                        BenefactorFullAccountNumber = standingOrderDTO.BenefactorCustomerAccountCustomerReference1,
                    }
                });
            }

            return Json(new { success = false, message = "Customer account not found" });
        }

        public async Task<ActionResult> BeneficiaryCustomerAccountLookUp(Guid? id, StandingOrderDTO standingOrderDTO)
        {
            await ServeNavigationMenus();

            ViewBag.TransactionTypeSelectList = GetDataAttachmentTransactionTypeTypeSelectList(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);
            ViewBag.loanRegistrationStandingOrderTriggers = GetLoanRegistrationStandingOrderTriggerSelectList(string.Empty);

            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);

            ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);

            ViewBag.ProductCode2 = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus2 = GetRecordStatusSelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            bool includeBalances = true;
            bool includeProductDescription = true;
            bool includeInterestBalanceForLoanAccounts = true;
            bool considerMaturityPeriodForInvestmentAccounts = true;
            var beneficiaryAccounts = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());


            if (standingOrderDTO != null)
            {
                standingOrderDTO.Beneficiary = beneficiaryAccounts;
                Session["benefactorAccounts"] = standingOrderDTO.benefactor;
                standingOrderDTO.BeneficiaryCustomerAccountId = beneficiaryAccounts.Id;
                standingOrderDTO.BeneficiaryCustomerAccountCustomerIndividualFirstName = beneficiaryAccounts.CustomerIndividualSalutationDescription + " " + beneficiaryAccounts.CustomerIndividualFirstName + " " + beneficiaryAccounts.CustomerIndividualLastName;
                standingOrderDTO.BeneficiaryCustomerAccountCustomerSerialNumber = beneficiaryAccounts.CustomerSerialNumber;
                standingOrderDTO.BeneficiaryProductDescription = beneficiaryAccounts.CustomerAccountTypeTargetProductDescription;
                standingOrderDTO.BeneficiaryCustomerAccountCustomerId = beneficiaryAccounts.CustomerId;
                standingOrderDTO.BeneficiaryCustomerAccountCustomerReference1 = beneficiaryAccounts.FullAccountNumber;

                if (Session["beneficiaryAccounts"] != null)
                {
                    standingOrderDTO.Beneficiary = Session["beneficiaryAccounts"] as CustomerAccountDTO;
                    Session["beneficiaryAccounts"] = standingOrderDTO.Beneficiary;

                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        BeneficiaryCustomerAccountId = standingOrderDTO.BeneficiaryCustomerAccountId,
                        BeneficiaryCustomerAccountCustomerFullName = standingOrderDTO.BeneficiaryCustomerAccountCustomerIndividualFirstName,
                        BeneficiaryCustomerAccountCustomerSerialNumber = standingOrderDTO.BeneficiaryCustomerAccountCustomerSerialNumber,
                        BeneficiaryProductDescription = standingOrderDTO.BeneficiaryProductDescription,
                        BeneficiaryCustomerAccountCustomerId = standingOrderDTO.BeneficiaryCustomerAccountCustomerId,
                        BeneficiaryFullAccountNumber = standingOrderDTO.BeneficiaryCustomerAccountCustomerReference1,
                    }
                });
            }

            return Json(new { success = false, message = "Customer account not found" });
        }




        public async Task<ActionResult> Create(Guid? id, StandingOrderDTO standingOrderDTO)
        {
            await ServeNavigationMenus();

            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);
            ViewBag.loanRegistrationStandingOrderTriggers = GetLoanRegistrationStandingOrderTriggerSelectList(string.Empty);

            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);

            ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);

            ViewBag.ProductCode2 = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus2 = GetRecordStatusSelectList(string.Empty);

            return View();
        }



        [HttpPost]
        public async Task<ActionResult> Create(StandingOrderDTO standingOrderDTO)
        {
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

        public async Task<ActionResult> Edit(Guid? id, StandingOrderDTO standingOrderDTO)
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
            var benefactorAccounts1 = await _channelService.FindStandingOrdersByBenefactorCustomerIdAsync(parseId, 1, includeProductDescription, GetServiceHeader());


            if (benefactorAccounts != null)
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
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(StandingOrderDTO standingOrderDTO)
        {

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
                await _channelService.UpdateStandingOrderAsync(standingOrderDTO, GetServiceHeader());
                ViewBag.MonthsSelectList = GetMonthsAsync(standingOrderDTO.ScheduleFrequency.ToString());
                ViewBag.loanRegistrationStandingOrderTriggers = GetLoanRegistrationStandingOrderTriggerSelectList(standingOrderDTO.ScheduleFrequency.ToString());
                ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(standingOrderDTO.ChargeType.ToString());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = standingOrderDTO.ErrorMessages;
                ViewBag.MonthsSelectList = GetMonthsAsync(standingOrderDTO.ScheduleFrequency.ToString());
                ViewBag.loanRegistrationStandingOrderTriggers = GetLoanRegistrationStandingOrderTriggerSelectList(standingOrderDTO.ScheduleFrequency.ToString());
                ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(standingOrderDTO.ChargeType.ToString());
                return View(standingOrderDTO);
            }
        }


    }
}
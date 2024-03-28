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

        [HttpPost]
        public ActionResult AssignText2(Guid BeneficiaryCustomerAccountId, string BeneficiaryCustomerAccountCustomerFullName)
        {
            Session["BeneficiaryCustomerAccountCustomerFullName"] = BeneficiaryCustomerAccountCustomerFullName;
            Session["BranchDescription"] = BeneficiaryCustomerAccountId;

            return null;
        }


        [HttpPost]
        public ActionResult AssignText(string BenefactorCustomerAccountCustomerFullName, Guid ? BenefactorCustomerAccountId)
        {
            // Store data in session
            Session["BenefactorCustomerAccountCustomerFullName"] = BenefactorCustomerAccountCustomerFullName;
            Session["BenefactorCustomerAccountId"] = BenefactorCustomerAccountId;
            MemoryCache cache = MemoryCache.Default;

            // Setting the value in the cache
            cache["BenefactorCustomerAccountCustomerFullName"] = BenefactorCustomerAccountCustomerFullName;
            cache["BenefactorCustomerAccountId"] = BenefactorCustomerAccountId;

            // Retrieving the value from the cache
            string cachedValue = cache["BenefactorCustomerAccountId"] as string;
            // Return JSON result to indicate success
            return null;
        }




        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();
            MemoryCache cache = MemoryCache.Default;

            StandingOrderDTO standingOrderDTO = new StandingOrderDTO();
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
        

                if (standingOrderDTO.BenefactorCustomerAccountId !=null)
                {
                    var benefactorAccounts = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

                    if (benefactorAccounts != null)
                    {
                        standingOrderDTO.BenefactorCustomerAccountId = benefactorAccounts.Id;
                        standingOrderDTO.BenefactorCustomerAccountCustomerIndividualFirstName = benefactorAccounts.CustomerIndividualFirstName;
                        standingOrderDTO.BenefactorCustomerAccountCustomerIndividualLastName = benefactorAccounts.CustomerFullName;
                        standingOrderDTO.BenefactorCustomerAccountCustomerSerialNumber = benefactorAccounts.CustomerSerialNumber;
                        standingOrderDTO.BenefactorProductDescription = benefactorAccounts.CustomerAccountTypeProductCodeDescription;

                        TempData.Keep("BenefactorCustomerAccountId");
                        TempData.Keep("BenefactorCustomerAccountCustomerFullName");
                        TempData.Keep("BenefactorProductDescription");
                        // Store data in session for Benefactor

                    }

                }
            Session["BeneficiaryCustomerAccountCustomerIndividualFirstName"] = standingOrderDTO.BeneficiaryCustomerAccountCustomerIndividualFirstName;
            Session["BeneficiaryCustomerAccountCustomerIndividualLastName"] = standingOrderDTO.BeneficiaryCustomerAccountCustomerFullName;
            Session["BeneficiaryCustomerAccountCustomerSerialNumber"] = standingOrderDTO.BeneficiaryCustomerAccountCustomerSerialNumber;
            Session["BeneficiaryProductDescription"] = standingOrderDTO.BeneficiaryProductDescription;
            Session["BeneficiaryCustomerAccountCustomerIndividualIdentityCardNumber"] = standingOrderDTO.BeneficiaryCustomerAccountCustomerIndividualIdentityCardNumber;
            Session["BenefactorCustomerAccountCustomerIndividualFirstName"] = standingOrderDTO.BenefactorCustomerAccountCustomerIndividualFirstName;
            Session["BenefactorCustomerAccountCustomerIndividualLastName"] = standingOrderDTO.BenefactorCustomerAccountCustomerFullName;
            Session["BenefactorCustomerAccountCustomerSerialNumber"] = standingOrderDTO.BenefactorCustomerAccountCustomerSerialNumber;
            Session["BenefactorProductDescription"] = standingOrderDTO.BenefactorProductDescription;
            cache["BenefactorCustomerAccountCustomerFullName"] = standingOrderDTO.BenefactorCustomerAccountCustomerFullName;
           

            //if (standingOrderDTO.BeneficiaryCustomerAccountId != null)
            //{
            //    var beneficiaryAccounts = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());


            //    if (beneficiaryAccounts != null)
            //    {
            //        standingOrderDTO.BeneficiaryCustomerAccountId = beneficiaryAccounts.Id;
            //        standingOrderDTO.BeneficiaryCustomerAccountCustomerIndividualFirstName = beneficiaryAccounts.CustomerIndividualFirstName;
            //        standingOrderDTO.BeneficiaryCustomerAccountCustomerIndividualLastName = beneficiaryAccounts.CustomerFullName;
            //        standingOrderDTO.BeneficiaryCustomerAccountCustomerSerialNumber = beneficiaryAccounts.CustomerSerialNumber;
            //        standingOrderDTO.BeneficiaryProductDescription = beneficiaryAccounts.CustomerAccountTypeProductCodeDescription;
            //        standingOrderDTO.BeneficiaryCustomerAccountCustomerIndividualIdentityCardNumber = beneficiaryAccounts.CustomerIndividualIdentityCardNumber;
            //    }
            //}
           
            if (Session["BenefactorCustomerAccountId"] != null)
            {
                string accountIdString = Session["BenefactorCustomerAccountId"].ToString();
                if (Guid.TryParse(accountIdString, out Guid accountId))
                {
                    standingOrderDTO.BenefactorCustomerAccountId = accountId;
                }
            }
            if (Session["BenefactorCustomerAccountCustomerIndividualFirstName"] != null)
            {
                standingOrderDTO.BenefactorCustomerAccountCustomerIndividualFirstName = Session["BenefactorCustomerAccountCustomerIndividualFirstName"].ToString();
            }
            if (Session["BenefactorProductDescription"] != null)
            {
                standingOrderDTO.BenefactorProductDescription = Session["BenefactorProductDescription"].ToString();
            }
            return View(standingOrderDTO);
        }


        private async Task ProcessBenefactorAccount(Guid parseId, StandingOrderDTO standingOrderDTO)
        {
            var benefactorAccounts = await _channelService.FindCustomerAccountAsync(parseId, false, false, false, false, GetServiceHeader());

            if (benefactorAccounts != null)
            {
                if (Session["BenefactorCustomerAccountId"] != null)
                {
                    standingOrderDTO.BenefactorCustomerAccountId = (Guid)Session["BenefactorCustomerAccountId"];
                }
                Session["BenefactorCustomerAccountCustomerIndividualFirstName"] = benefactorAccounts.CustomerIndividualFirstName;
                Session["BenefactorCustomerAccountCustomerIndividualLastName"] = benefactorAccounts.CustomerFullName;
                Session["BenefactorCustomerAccountCustomerSerialNumber"] = benefactorAccounts.CustomerSerialNumber;
                Session["BenefactorProductDescription"] = benefactorAccounts.CustomerAccountTypeProductCodeDescription;
            }
        }

        private async Task ProcessBeneficiaryAccount(Guid parseId, StandingOrderDTO standingOrder)
        {
            bool includeBalances = false;
            bool includeProductDescription = false;
            bool includeInterestBalanceForLoanAccounts = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;

            var beneficiaryAccounts = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            if (beneficiaryAccounts != null)
            {
                // Storing data in session for Beneficiary
                if (Session["BeneficiaryCustomerAccountId"] != null)
                {
                    standingOrder.BeneficiaryCustomerAccountId = (Guid)Session["BeneficiaryCustomerAccountId"];
                }
                Session["BeneficiaryCustomerAccountCustomerIndividualFirstName"] = beneficiaryAccounts.CustomerIndividualFirstName;
                Session["BeneficiaryCustomerAccountCustomerIndividualLastName"] = beneficiaryAccounts.CustomerFullName;
                Session["BeneficiaryCustomerAccountCustomerSerialNumber"] = beneficiaryAccounts.CustomerSerialNumber;
                Session["BeneficiaryProductDescription"] = beneficiaryAccounts.CustomerAccountTypeProductCodeDescription;
                Session["BeneficiaryCustomerAccountCustomerIndividualIdentityCardNumber"] = beneficiaryAccounts.CustomerIndividualIdentityCardNumber;
            }
        }


        private async Task SetBeneficiarySessionData(Guid parseId, StandingOrderDTO standingOrder)
        {
            var beneficiaryAccounts = await _channelService.FindCustomerAccountAsync(parseId, false, false, false, false, GetServiceHeader());

            if (beneficiaryAccounts != null)
            {
                // Storing data in session for Beneficiary
                if (Session["BeneficiaryCustomerAccountId"] != null)
                {
                    standingOrder.BeneficiaryCustomerAccountId = (Guid)Session["BeneficiaryCustomerAccountId"];
                }
                Session["BeneficiaryCustomerAccountCustomerIndividualFirstName"] = beneficiaryAccounts.CustomerIndividualFirstName;
                Session["BeneficiaryCustomerAccountCustomerIndividualLastName"] = beneficiaryAccounts.CustomerFullName;
                Session["BeneficiaryCustomerAccountCustomerSerialNumber"] = beneficiaryAccounts.CustomerSerialNumber;
                Session["BeneficiaryProductDescription"] = beneficiaryAccounts.CustomerAccountTypeProductCodeDescription;
                Session["BeneficiaryCustomerAccountCustomerIndividualIdentityCardNumber"] = beneficiaryAccounts.CustomerIndividualIdentityCardNumber;
            }
        }

        private async Task SetBenefactorSessionData(Guid parseId, StandingOrderDTO standingOrderDTO)
        {
            var benefactorAccounts = await _channelService.FindCustomerAccountAsync(parseId, false, false, false, false, GetServiceHeader());

            if (benefactorAccounts != null)
            {
                if (Session["BenefactorCustomerAccountId"] != null)
                {
                    standingOrderDTO.BenefactorCustomerAccountId = (Guid)Session["BenefactorCustomerAccountId"];
                }
                Session["BenefactorCustomerAccountCustomerIndividualFirstName"] = benefactorAccounts.CustomerIndividualFirstName;
                Session["BenefactorCustomerAccountCustomerIndividualLastName"] = benefactorAccounts.CustomerFullName;
                Session["BenefactorCustomerAccountCustomerSerialNumber"] = benefactorAccounts.CustomerSerialNumber;
                Session["BenefactorProductDescription"] = benefactorAccounts.CustomerAccountTypeProductCodeDescription;
            }
        }

        public ActionResult LoadPartialView(string BenefactorCustomerAccountCustomerFullName, Guid BenefactorCustomerAccountId)
        {
            // Store data in session
            Session["BenefactorCustomerAccountCustomerFullName"] = BenefactorCustomerAccountCustomerFullName;
            Session["BenefactorCustomerAccountId"] = BenefactorCustomerAccountId;

            // Optionally, you can also save it in TempData
            TempData["BenefactorCustomerAccountId"] = BenefactorCustomerAccountId;

            // Return the partial view
            return PartialView("_YourPartialViewName");
        }




        public async Task<ActionResult> SearchBenefactorAccount(Guid? id, StandingOrderDTO standingOrder)
        {
            await ServeNavigationMenus();
            StandingOrderDTO standingOrderDTO = new StandingOrderDTO();
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

            if (benefactorAccounts != null)
            {
                standingOrderDTO.BenefactorCustomerAccountId = benefactorAccounts.CustomerId;
                standingOrderDTO.BenefactorCustomerAccountCustomerIndividualFirstName = benefactorAccounts.CustomerIndividualFirstName;
                standingOrderDTO.BenefactorCustomerAccountCustomerIndividualLastName = benefactorAccounts.CustomerFullName;
                standingOrderDTO.BenefactorCustomerAccountCustomerSerialNumber = benefactorAccounts.CustomerSerialNumber;
                standingOrderDTO.BenefactorProductDescription = benefactorAccounts.CustomerAccountTypeProductCodeDescription;





                Session["BenefactorCustomerAccountCustomerIndividualFirstName"] = benefactorAccounts.CustomerIndividualFirstName;
                Session["BenefactorCustomerAccountCustomerIndividualLastName"] = benefactorAccounts.CustomerFullName;
                Session["BenefactorCustomerAccountCustomerSerialNumber"] = benefactorAccounts.CustomerSerialNumber;
                Session["BenefactorProductDescription"] = benefactorAccounts.CustomerAccountTypeProductCodeDescription;
                TempData.Keep("BenefactorCustomerAccountId");
                TempData.Keep("BenefactorCustomerAccountCustomerFullName");
                TempData.Keep("BenefactorProductDescription");
                // Store data in session for Benefactor
            }

            if (Session["BenefactorCustomerAccountId"] != null)
            {
                string accountIdString = Session["BenefactorCustomerAccountId"].ToString();
                if (Guid.TryParse(accountIdString, out Guid accountId))
                {
                    standingOrderDTO.BenefactorCustomerAccountId = accountId;
                }
            }
            if (Session["BenefactorCustomerAccountCustomerIndividualFirstName"] != null)
            {
                standingOrderDTO.BenefactorCustomerAccountCustomerIndividualFirstName = Session["BenefactorCustomerAccountCustomerIndividualFirstName"].ToString();
            }
            if (Session["BenefactorProductDescription"] != null)
            {
                standingOrderDTO.BenefactorProductDescription = Session["BenefactorProductDescription"].ToString();
            }

            return View("Create", standingOrder);
        }
        [HttpPost]
        public async Task<JsonResult> BenefactorAccountList(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;
            bool includeInterestBalanceForLoanAccounts = false;
            bool includeBalances = false;
            bool includeProductDescription = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortColumnIndex = jQueryDataTablesModel.iSortCol_.FirstOrDefault();
            var sortColumnDirection = jQueryDataTablesModel.sSortDir_.FirstOrDefault();

            //var sortPropertyName = ""; // Define the property name for sorting

            //if (sortColumnIndex >= 0 && sortColumnIndex < jQueryDataTablesModel.iColumns.Count)
            //{
            //    sortPropertyName = jQueryDataTablesModel.GetSortedColumns()[sortColumnIndex].PropertyName;
            //}

            var pageCollectionInfo = await _channelService.FindCustomerAccountsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, jQueryDataTablesModel.iColumns, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = totalRecordCount;

                return this.DataTablesJson(
                    items: pageCollectionInfo.PageCollection,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho);
            }
            else
            {
                return this.DataTablesJson(
                    items: new List<CustomerAccountDTO>(),
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho);
            }
        }


        [HttpPost]
        public ActionResult benefactor(string BenefactorCustomerAccountId, string BenefactorCustomerAccountCustomerFullName, string BenefactorProductDescription)
        {

            Session["BenefactorCustomerAccountId"] = BenefactorCustomerAccountId;
            Session["BenefactorCustomerAccountCustomerFullName"] = BenefactorCustomerAccountCustomerFullName;
            Session["BenefactorProductDescription"] = BenefactorProductDescription;
            //Session["CustomerReference1"] = CustomerReference1;
            //Session["CustomerReference2"] = CustomerReference2;
            StandingOrderDTO standingOrderDTO = new StandingOrderDTO();
            TempData["BenefactorCustomerAccountCustomerFullName"] = standingOrderDTO.BenefactorCustomerAccountCustomerFullName;
            TempData["BenefactorProductDescription"] = standingOrderDTO.BenefactorProductDescription;
            TempData["BeneficiaryCustomerAccountId"] = standingOrderDTO.BeneficiaryCustomerAccountId;
            TempData["BeneficiaryCustomerAccountCustomerFullName"] = standingOrderDTO.BeneficiaryCustomerAccountCustomerFullName;
            TempData["BeneficiaryProductDescription"] = standingOrderDTO.BeneficiaryProductDescription;
            return null;
        }


        [HttpPost]
        public ActionResult beneficiary(string BeneficiaryCustomerAccountId, string BeneficiaryCustomerAccountCustomerFullName, string BeneficiaryProductDescription)
        {

            Session["BeneficiaryCustomerAccountId"] = BeneficiaryCustomerAccountId;
            Session["BeneficiaryCustomerAccountCustomerFullName"] = BeneficiaryCustomerAccountCustomerFullName;
            Session["BeneficiaryProductDescription"] = BeneficiaryProductDescription;
            //Session["CustomerReference1"] = CustomerReference1;
            //Session["CustomerReference2"] = CustomerReference2;
            StandingOrderDTO standingOrderDTO = new StandingOrderDTO();
            TempData["BenefactorCustomerAccountCustomerFullName"] = standingOrderDTO.BenefactorCustomerAccountCustomerFullName;
            TempData["BenefactorProductDescription"] = standingOrderDTO.BenefactorProductDescription;
            TempData["BeneficiaryCustomerAccountId"] = standingOrderDTO.BeneficiaryCustomerAccountId;
            TempData["BeneficiaryCustomerAccountCustomerFullName"] = standingOrderDTO.BeneficiaryCustomerAccountCustomerFullName;
            TempData["BeneficiaryProductDescription"] = standingOrderDTO.BeneficiaryProductDescription;
            return null;
        }


        [HttpPost]
        public async Task<JsonResult> BeneficiaryAccountList(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;
            bool includeInterestBalanceForLoanAccounts = false;
            bool includeBalances = false;
            bool includeProductDescription = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortColumnIndex = jQueryDataTablesModel.iSortCol_.FirstOrDefault();
            var sortColumnDirection = jQueryDataTablesModel.sSortDir_.FirstOrDefault();

            //var sortPropertyName = ""; // Define the property name for sorting

            //if (sortColumnIndex >= 0 && sortColumnIndex < jQueryDataTablesModel.iColumns.Count)
            //{
            //    sortPropertyName = jQueryDataTablesModel.GetSortedColumns()[sortColumnIndex].PropertyName;
            //}

            var pageCollectionInfo = await _channelService.FindCustomerAccountsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, jQueryDataTablesModel.iColumns, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = totalRecordCount;

                return this.DataTablesJson(
                    items: pageCollectionInfo.PageCollection,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho);
            }
            else
            {
                return this.DataTablesJson(
                    items: new List<CustomerAccountDTO>(),
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho);
            }
        }

        public async Task<ActionResult> SearchBeneficiaryAccount(Guid? id, StandingOrderDTO standingOrderDTO)
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

            //if (Session["BenefactorCustomerAccountCustomerIndividualFirstName"] != null)
            //{
            //    standingOrderDTO.BenefactorCustomerAccountCustomerIndividualFirstName = Session["BenefactorCustomerAccountCustomerIndividualFirstName"].ToString();
            //}
            bool includeBalances = false;
            bool includeProductDescription = false;
            bool includeInterestBalanceForLoanAccounts = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;

            var beneficiaryAccounts = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            if (beneficiaryAccounts != null)
            {
                standingOrderDTO.BeneficiaryCustomerAccountId = beneficiaryAccounts.CustomerId;
                standingOrderDTO.BeneficiaryCustomerAccountCustomerIndividualFirstName = beneficiaryAccounts.CustomerIndividualFirstName;
                standingOrderDTO.BeneficiaryCustomerAccountCustomerIndividualLastName = beneficiaryAccounts.CustomerFullName;
                standingOrderDTO.BeneficiaryCustomerAccountCustomerSerialNumber = beneficiaryAccounts.CustomerSerialNumber;
                standingOrderDTO.BeneficiaryProductDescription = beneficiaryAccounts.CustomerAccountTypeProductCodeDescription;
                standingOrderDTO.BeneficiaryCustomerAccountCustomerIndividualIdentityCardNumber = beneficiaryAccounts.CustomerIndividualIdentityCardNumber;
            }
            //if (Session["BeneficiaryCustomerAccountId"] != null)
            //{
            //    standingOrderDTO.BeneficiaryCustomerAccountId = (Guid)Session["BeneficiaryCustomerAccountId"];
            //}
            //if (Session["BeneficiaryCustomerAccountCustomerIndividualFirstName"] != null)
            //{
            //    standingOrderDTO.BeneficiaryCustomerAccountCustomerIndividualFirstName = Session["BeneficiaryCustomerAccountCustomerIndividualFirstName"].ToString();
            //}
            //if (Session["BeneficiaryProductDescription"] != null)
            //{
            //    standingOrderDTO.BeneficiaryProductDescription = Session["BeneficiaryProductDescription"].ToString();
            //}
            //if (Session["BeneficiaryCustomerAccountCustomerIndividualFirstName"] != null)
            //{
            //    standingOrderDTO.BeneficiaryCustomerAccountCustomerIndividualFirstName = Session["BeneficiaryCustomerAccountCustomerIndividualFirstName"].ToString();
            //}
            if (Session["BenefactorCustomerAccountId"] != null)
            {
                standingOrderDTO.BenefactorCustomerAccountId = (Guid)Session["BenefactorCustomerAccountId"];
            }
            if (Session["BenefactorCustomerAccountCustomerIndividualFirstName"] != null)
            {
                standingOrderDTO.BenefactorCustomerAccountCustomerIndividualFirstName = Session["BenefactorCustomerAccountCustomerIndividualFirstName"].ToString();
            }
            if (Session["BenefactorProductDescription"] != null)
            {
                standingOrderDTO.BenefactorProductDescription = Session["BenefactorProductDescription"].ToString();
            }
            TempData["BenefactorCustomerAccountCustomerFullName"] = standingOrderDTO.BenefactorCustomerAccountCustomerFullName;
            TempData["BenefactorProductDescription"] = standingOrderDTO.BenefactorProductDescription;
            TempData["BeneficiaryCustomerAccountId"] = standingOrderDTO.BeneficiaryCustomerAccountId;
            TempData["BeneficiaryCustomerAccountCustomerFullName"] = standingOrderDTO.BeneficiaryCustomerAccountCustomerFullName;
            TempData["BeneficiaryProductDescription"] = standingOrderDTO.BeneficiaryProductDescription;



            return View("Create", standingOrderDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(StandingOrderDTO standingOrderDTO)
        {

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


        public async Task<ActionResult> Search(Guid? id)
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
            StandingOrderDTO standingOrderDTO = new StandingOrderDTO();




            bool includeBalances = false;
            bool includeProductDescription = false;
            bool includeInterestBalanceForLoanAccounts = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;


            var beneficiaryAccounts = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            if (beneficiaryAccounts != null)
            {
                // Store data in session for Beneficiary
                standingOrderDTO.BeneficiaryCustomerAccountId = beneficiaryAccounts.CustomerId;
                standingOrderDTO.BeneficiaryCustomerAccountCustomerIndividualFirstName = beneficiaryAccounts.CustomerIndividualFirstName;
                standingOrderDTO.BeneficiaryCustomerAccountCustomerIndividualLastName = beneficiaryAccounts.CustomerFullName;
                standingOrderDTO.BeneficiaryCustomerAccountCustomerSerialNumber = beneficiaryAccounts.CustomerSerialNumber;
                standingOrderDTO.BeneficiaryProductDescription = beneficiaryAccounts.CustomerAccountTypeProductCodeDescription;
                standingOrderDTO.BeneficiaryCustomerAccountCustomerIndividualIdentityCardNumber = beneficiaryAccounts.CustomerIndividualIdentityCardNumber;

                Session["BeneficiaryCustomerAccountId"] = beneficiaryAccounts.CustomerId;
                Session["BeneficiaryCustomerAccountCustomerIndividualFirstName"] = beneficiaryAccounts.CustomerIndividualFirstName;
                Session["BeneficiaryCustomerAccountCustomerIndividualLastName"] = beneficiaryAccounts.CustomerFullName;
                Session["BeneficiaryCustomerAccountCustomerSerialNumber"] = beneficiaryAccounts.CustomerSerialNumber;
                Session["BeneficiaryProductDescription"] = beneficiaryAccounts.CustomerAccountTypeProductCodeDescription;
                Session["BeneficiaryCustomerAccountCustomerIndividualIdentityCardNumber"] = beneficiaryAccounts.CustomerIndividualIdentityCardNumber;
            }
            TempData.Keep("BeneficiaryCustomerAccountId");
            TempData.Keep("BeneficiaryCustomerAccountCustomerFullName");
            TempData.Keep("BeneficiaryProductDescription");

            return View("Create", standingOrderDTO);
        }

        [HttpGet]
        public async Task<JsonResult> GetLoanProductsAsync()
        {
            var loanProductsDTOs = await _channelService.FindLoanProductsAsync(GetServiceHeader());

            return Json(loanProductsDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
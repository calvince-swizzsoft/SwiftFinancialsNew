using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;


namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class CustomerAccountsController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();
            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
       {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindCustomerAccountsByProductCodeAndRecordStatusAndFilterInPageAsync(productCode, recordStatus, jQueryDataTablesModel.sSearch, 2, pageIndex, jQueryDataTablesModel.iDisplayLength, false, false, false, false, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(l => l.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CustomerAccountDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();
            bool includeInterestBalanceForLoanAccounts = false;
            bool includeBalances = false;
            bool includeProductDescription = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;
            var CustomerAccount = await _channelService.FindCustomerAccountAsync(id, includeInterestBalanceForLoanAccounts, includeBalances, includeProductDescription, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            return View();
        }
        [HttpPost]
        public async Task<ActionResult> Add(Guid? id, CustomerAccountDTO customerAccountDTO)
        {
            await ServeNavigationMenus();

            savingsProductDTOs = TempData["savingsProductDTOs"] as ObservableCollection<SavingsProductDTO>;

            if (savingsProductDTOs == null)
                savingsProductDTOs = new ObservableCollection<SavingsProductDTO>();

            foreach (var expensePayableEntryDTO in customerAccountDTO.savingsProducts)
            {
                expensePayableEntryDTO.Id = customerAccountDTO.savingsProducts[0].Id;
                expensePayableEntryDTO.ChartOfAccountId = customerAccountDTO.CustomerAccountTypeTargetProductId;
                expensePayableEntryDTO.ChartOfAccountAccountName = expensePayableEntryDTO.ChartOfAccountAccountName;
                expensePayableEntryDTO.Description = expensePayableEntryDTO.Description;
                //expensePayableEntryDTO.ChartOfAccountAccountName = expensePayableEntryDTO.ChartOfAccountName;
                //expensePayableEntryDTO.TotalValue = expensePayableEntryDTO.TotalValue;
                //expensePayableEntryDTO.PrimaryDescription = expensePayableEntryDTO.PrimaryDescription;
                //expensePayableEntryDTO.SecondaryDescription = expensePayableEntryDTO.SecondaryDescription;
                //expensePayableEntryDTO.Reference = expensePayableEntryDTO.Reference;
                savingsProductDTOs.Add(expensePayableEntryDTO);
            };

            TempData["savingsProductDTOs"] = savingsProductDTOs;

            TempData["customerAccountDTO"] = customerAccountDTO;
            Session["savingsProductDTOs"] = savingsProductDTOs;
            ViewBag.savingsProductDTOs = savingsProductDTOs;
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(customerAccountDTO.Type.ToString());
            ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(customerAccountDTO.Type.ToString());
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(customerAccountDTO.Type.ToString());
            if (Session["investmentProductDTOs"] != null)
            {
                investmentProductDTOs = Session["investmentProductDTOs"] as ObservableCollection<InvestmentProductDTO>;
                ViewBag.investmentProductDTOs = investmentProductDTOs;
            }
            if (Session["customerAccountDTO"] != null)
            {
                customerAccountDTO = Session["customerAccountDTO"] as CustomerAccountDTO;
            }
            return View("Create", customerAccountDTO);
        }



        [HttpPost]
        public async Task<ActionResult> AddInvestments(Guid? id, CustomerAccountDTO customerAccountDTO)
        {
            await ServeNavigationMenus();

            investmentProductDTOs = TempData["investmentProductDTOs"] as ObservableCollection<InvestmentProductDTO>;

            if (investmentProductDTOs == null)
                investmentProductDTOs = new ObservableCollection<InvestmentProductDTO>();

            foreach (var expensePayableEntryDTO in customerAccountDTO.investmentProducts)
            {
                expensePayableEntryDTO.Id = customerAccountDTO.investmentProducts[0].Id;
                var k = await _channelService.FindInvestmentProductAsync(expensePayableEntryDTO.Id,GetServiceHeader()); 
                expensePayableEntryDTO.ChartOfAccountId = customerAccountDTO.CustomerAccountTypeTargetProductId;
                expensePayableEntryDTO.ChartOfAccountAccountName = expensePayableEntryDTO.ChartOfAccountAccountName;
                expensePayableEntryDTO.Description = expensePayableEntryDTO.Description;
                //expensePayableEntryDTO.ChartOfAccountAccountName = expensePayableEntryDTO.ChartOfAccountName;
                //expensePayableEntryDTO.TotalValue = expensePayableEntryDTO.TotalValue;
                //expensePayableEntryDTO.PrimaryDescription = expensePayableEntryDTO.PrimaryDescription;
                //expensePayableEntryDTO.SecondaryDescription = expensePayableEntryDTO.SecondaryDescription;
                //expensePayableEntryDTO.Reference = expensePayableEntryDTO.Reference;
                investmentProductDTOs.Add(expensePayableEntryDTO);
            };

            TempData["investmentProductDTOs"] = investmentProductDTOs;

            TempData["customerAccountDTO"] = customerAccountDTO;

            ViewBag.investmentProductDTOs = investmentProductDTOs;
            Session["investmentProductDTOs"] = investmentProductDTOs;
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(customerAccountDTO.Type.ToString());
            ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(customerAccountDTO.Type.ToString());
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(customerAccountDTO.Type.ToString());
            if (Session["savingsProductDTOs"] != null)
            {
                savingsProductDTOs = Session["savingsProductDTOs"] as ObservableCollection<SavingsProductDTO>;
                ViewBag.savingsProductDTOs = savingsProductDTOs;
            }
            if (Session["customerAccountDTO"] != null)
            {
                customerAccountDTO = Session["customerAccountDTO"] as CustomerAccountDTO;
            }
            return View("Create", customerAccountDTO);
        }

        public async Task<ActionResult> Create(Guid? id,CustomerAccountDTO customerAccount)
        {
            Session["branchid"] = customerAccount.BranchId;
            await ServeNavigationMenus();
            ViewBag.CustomerAccountManagementActionSelectList = GetCustomerAccountManagementActionSelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            CustomerAccountDTO customerAccountDTO = new CustomerAccountDTO();

            if (customer != null)
            {

                customerAccountDTO.CustomerId = customer.Id;
                customerAccountDTO.Customers = customer;
                customerAccountDTO.CustomerIndividualFirstName = customer.FullName;
                customerAccountDTO.CustomerIndividualPayrollNumbers = customer.IndividualPayrollNumbers;
                customerAccountDTO.CustomerSerialNumber = customer.SerialNumber;
                customerAccountDTO.CustomerIndividualIdentityCardNumber = customer.IndividualIdentityCardNumber;
                customerAccountDTO.CustomerStationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
            }
            Session["customerDTO"] = customer;

            Session["customerAccountDTO"] = customerAccountDTO;
            if (Session["benefactorAccounts"] != null)
            {
                customerAccountDTO.Investments = Session["benefactorAccounts"] as InvestmentProductDTO;
            }
            if (Session["beneficiaryAccounts"] != null)
            {
                customerAccountDTO.Savings = Session["beneficiaryAccounts"] as SavingsProductDTO;
            }
            if (Session["investmentProductDTOs"] != null)
            {
                investmentProductDTOs = Session["investmentProductDTOs"] as ObservableCollection<InvestmentProductDTO>;
                ViewBag.investmentProductDTOs = investmentProductDTOs;
            }
            if (Session["savingsProductDTOs"] != null)
            {
                savingsProductDTOs = Session["savingsProductDTOs"] as ObservableCollection<SavingsProductDTO>;
                ViewBag.savingsProductDTOs = savingsProductDTOs;
            }
            return View(customerAccountDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(CustomerAccountDTO customerAccountDTO)
        {
            savingsProductDTOs = Session["savingsProductDTOs"] as ObservableCollection<SavingsProductDTO>;
            investmentProductDTOs = Session["investmentProductDTOs"] as ObservableCollection<InvestmentProductDTO>;
            var customerDTO = Session["customerDTO"] as CustomerDTO;
            //customerAccountDTO = Session["customerAccountDTO"] as CustomerAccountDTO;


            //customerAccountDTO.Investments = Session["benefactorAccounts"] as InvestmentProductDTO;

            customerAccountDTO.TotalValue = 1;
            customerAccountDTO.CustomerId = customerDTO.Id;
            //customerAccountDTO.BranchId = customerDTO.Id;
            customerDTO.BranchId = customerAccountDTO.BranchId;
            customerAccountDTO.CustomerAccountTypeTargetProductId = savingsProductDTOs[0].Id;
            //customerAccountDTO.CustomerAccountTypeProductCode = customerAccountDTO.Savings.Code;


            customerAccountDTO.ValidateAll();

            if (!customerAccountDTO.HasErrors)
            {
                await _channelService.AddCustomerAccountsAsync(customerDTO, savingsProductDTOs, investmentProductDTOs, null, GetServiceHeader());
                TempData["SuccessMessage"] = "Successfully Created Customer Account for  " + customerDTO.FullName;

                Session.Clear();
                Session["customerAccountDTO"] = null;
                Session["savingsProductDTOs"] = null;
                Session["investmentProductDTOs"] = null;
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = customerAccountDTO.ErrorMessages;
                Session["customerAccountDTO"] = "";
                Session["savingsProductDTOs"] = "";
                Session["investmentProductDTOs"] = "";
                return View(customerAccountDTO);
            }
        }
        public async Task<ActionResult> SavingsProduct(Guid? id, CustomerAccountDTO customerAccountDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("Create");
            }



            if (Session["customerAccountDTO"] != null)
            {
                customerAccountDTO = Session["customerAccountDTO"] as CustomerAccountDTO;
            }

            if (Session["benefactorAccounts"] != null)
            {
                customerAccountDTO.Investments = Session["benefactorAccounts"] as InvestmentProductDTO;
            }

            var savingsProduct = await _channelService.FindSavingsProductAsync(parseId, GetServiceHeader());
            if (savingsProduct != null)
            {
                ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
                ViewBag.ChargeType = GetChargeTypeSelectList(string.Empty);
                customerAccountDTO.Savings = savingsProduct;
                Session["beneficiaryAccounts"] = customerAccountDTO.Savings;
                customerAccountDTO.CustomerAccountTypeTargetProductId = savingsProduct.Id;
                customerAccountDTO.CustomerAccountTypeTargetProductDescription = savingsProduct.Description;
                customerAccountDTO.CustomerAccountTypeTargetProductChartOfAccountName = savingsProduct.ChartOfAccountName;
                customerAccountDTO.CustomerAccountTypeProductCode = savingsProduct.Code;

                Session["savingsProductId"] = customerAccountDTO.CustomerAccountTypeTargetProductId;
                Session["savingsProductDescription"] = customerAccountDTO.CustomerAccountTypeTargetProductDescription;
            }
            // Ensure savingsProductDTOs is properly initialized
            if (savingsProductDTOs == null)
            {
                savingsProductDTOs = new ObservableCollection<SavingsProductDTO>();
                savingsProductDTOs.Add(savingsProduct);
            }




            return View("Create", customerAccountDTO);
        }



        public async Task<ActionResult> LoansProduct(Guid? id, CustomerAccountDTO directDebitDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("Create");
            }

            var loanProductsdetails = await _channelService.FindLoanProductAsync(parseId, GetServiceHeader());
            if (loanProductsdetails != null)
            {
                ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
                ViewBag.ChargeType = GetChargeTypeSelectList(string.Empty);

                directDebitDTO.CustomerAccountTypeTargetProductId = loanProductsdetails.Id;
                directDebitDTO.CustomerAccountTypeTargetProductDescription = loanProductsdetails.Description;
                directDebitDTO.CustomerAccountTypeTargetProductChartOfAccountName = loanProductsdetails.ChartOfAccountAccountName;


                directDebitDTO.CustomerAccountTypeProductCode = loanProductsdetails.Code;

                Session["loanProductId"] = directDebitDTO.CustomerAccountTypeTargetProductId;
                Session["loanProductDescription"] = directDebitDTO.CustomerAccountTypeTargetProductDescription;
                Session["loanProductDescription"] = loanProductsdetails;
            }


            return View("Create", directDebitDTO);
        }



        public async Task<ActionResult> InvestmentProduct(Guid? id, CustomerAccountDTO directDebitDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("Create");
            }

            if (Session["beneficiaryAccounts"] != null)
            {
                directDebitDTO.Savings = Session["beneficiaryAccounts"] as SavingsProductDTO;
            }
            if (Session["customerAccountDTO"] != null)
            {
                directDebitDTO.Customers = Session["customerAccountDTO"] as CustomerDTO;
            }

            var investmentProductDetails = await _channelService.FindInvestmentProductAsync(parseId, GetServiceHeader());
            if (investmentProductDetails != null)
            {
                ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
                ViewBag.ChargeType = GetChargeTypeSelectList(string.Empty);

                directDebitDTO.Investments = investmentProductDetails;
                directDebitDTO.investmentProducts[0].Id = investmentProductDetails.Id;
                directDebitDTO.investmentProducts[1].Description = investmentProductDetails.Description;
                Session["benefactorAccounts"] = directDebitDTO.Investments;
                directDebitDTO.CustomerAccountTypeTargetProductId = investmentProductDetails.Id;
                directDebitDTO.CustomerAccountTypeTargetProductDescription = investmentProductDetails.Description;
                directDebitDTO.CustomerAccountTypeTargetProductChartOfAccountName = investmentProductDetails.ChartOfAccountAccountName;
                directDebitDTO.CustomerAccountTypeTargetProductParentId = investmentProductDetails.ChartOfAccountId;
                directDebitDTO.CustomerAccountTypeProductCode = 3;

                Session["investmentProductId"] = directDebitDTO.CustomerAccountTypeTargetProductId;
                Session["investmentProductDescription"] = directDebitDTO.CustomerAccountTypeTargetProductDescription;
            }

            ViewBag.k = directDebitDTO.Investments;

            return View("Create", directDebitDTO);
        }
        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            bool includeInterestBalanceForLoanAccounts = false;
            bool includeBalances = false;
            bool includeProductDescription = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }
            var CustomerAccount = await _channelService.FindCustomerAccountAsync(id, includeInterestBalanceForLoanAccounts, includeBalances, includeProductDescription, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());
            CustomerAccountDTO customerAccountDTO = new CustomerAccountDTO();

            if (CustomerAccount != null)
            {

                customerAccountDTO.CustomerId = CustomerAccount.Id;
                customerAccountDTO.CustomerIndividualFirstName = CustomerAccount.CustomerIndividualFirstName;
                customerAccountDTO.CustomerIndividualPayrollNumbers = CustomerAccount.CustomerIndividualPayrollNumbers;
                customerAccountDTO.CustomerSerialNumber = CustomerAccount.CustomerSerialNumber;
                customerAccountDTO.CustomerIndividualIdentityCardNumber = CustomerAccount.CustomerIndividualIdentityCardNumber;
                customerAccountDTO.CustomerStationZoneDivisionEmployerDescription = CustomerAccount.CustomerStationZoneDivisionEmployerDescription;
            }
            return View(customerAccountDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, CustomerAccountDTO customerAccountDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateCustomerAccountAsync(customerAccountDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(customerAccountDTO);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetCustomersCountAsync(CustomerAccountDTO customerAccountDTO)
        {
            await ServeNavigationMenus();
            var customerAccountId = customerAccountDTO.Id;
            bool includeInterestBalanceForLoanAccounts = false;
            bool includeBalances = false;
            bool includeProductDescription = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;
            var CustomerAccount = await _channelService.FindCustomerAccountAsync(customerAccountId, includeInterestBalanceForLoanAccounts, includeBalances, includeProductDescription, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            return Json(customerAccountDTO, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> CustomerManagement(Guid id)
        {
            await ServeNavigationMenus();
            bool includeInterestBalanceForLoanAccounts = false;
            bool includeBalances = false;
            bool includeProductDescription = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;


            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            CustomerAccountDTO customerAccountDTO = new CustomerAccountDTO();

            var CustomerAccount = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());
            if (CustomerAccount != null)
            {
                customerAccountDTO.CustomerId = CustomerAccount.Id;
                customerAccountDTO.CustomerIndividualFirstName = CustomerAccount.CustomerIndividualFirstName;
                customerAccountDTO.CustomerIndividualPayrollNumbers = CustomerAccount.CustomerIndividualPayrollNumbers;
                customerAccountDTO.CustomerSerialNumber = CustomerAccount.CustomerSerialNumber;
                customerAccountDTO.CustomerIndividualIdentityCardNumber = CustomerAccount.CustomerIndividualIdentityCardNumber;
                customerAccountDTO.CustomerStationZoneDivisionEmployerDescription = CustomerAccount.CustomerStationZoneDivisionEmployerDescription;
            }
            return View(customerAccountDTO);
        }

        [HttpPost]
        public async Task<ActionResult> CustomerManagement(Guid id, CustomerAccountDTO customerAccountHistoryDTO)
        {
            if (ModelState.IsValid)
            {
                int managementAction = 0;

                string remarks = "";

                int remarkType = 0;

                await _channelService.ManageCustomerAccountAsync(id, managementAction, remarks, remarkType, GetServiceHeader());

                ViewBag.CustomerAccountManagementActionSelectList = GetCustomerAccountManagementActionSelectList(customerAccountHistoryDTO.CustomerIndividualSalutationDescription.ToString());
                return RedirectToAction("Index");
            }
            else
            {
                return View(customerAccountHistoryDTO);
            }
        }

    }
}
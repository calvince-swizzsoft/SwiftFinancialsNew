using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;


namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class CoA_RegisterController : MasterController
    {
       

        //Index With Filters
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, int productCode, int recordStatus)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();


            var pageCollectionInfo = await _channelService.FindCustomerAccountsByProductCodeAndRecordStatusAndFilterInPageAsync(productCode, recordStatus, jQueryDataTablesModel.sSearch, 2, pageIndex, jQueryDataTablesModel.iDisplayLength, false, false, false, false, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(ChartOfAccountDTO => ChartOfAccountDTO.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CustomerAccountDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }
        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();
            var customerAccountDTO = await _channelService.FindCustomerAccountAsync(id, false, false, false, false, GetServiceHeader());
            return View(customerAccountDTO);
        }


        public async Task<ActionResult> Create(Guid? id)
        
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            return View();
        }

        //public async Task<ActionResult> SavingsProduct(SavingsProductDTO savingsProductDTO, ObservableCollection<SavingsProductDTO> savingProductRowData)
        //{
        //    Session["savingsProductIds"] = savingProductRowData;
        //    return View("Create", savingProductRowData);
        //}

        //public async Task<ActionResult> LoansProduct(ObservableCollection<LoanProductDTO> loansProductRowData)
        //{
        //    Session["loansProductIds"] = loansProductRowData;
        //    return View("Create", loansProductRowData);
        //}

        //public async Task<ActionResult> InvestmentsProduct(ObservableCollection<InvestmentProductDTO> investmentProductRowData)
        //{
        //    Session["investmentsProductIds"] = investmentProductRowData;
        //    return View("Create", investmentProductRowData);
        //}


        [HttpPost]
        public async Task<ActionResult> Add(Guid ?id,CustomerAccountDTO customerAccountDTO)
        {
            await ServeNavigationMenus();

            savingsProductDTOs = TempData["savingsProductDTOs"] as ObservableCollection<SavingsProductDTO>;

            if (savingsProductDTOs == null)
                savingsProductDTOs = new ObservableCollection<SavingsProductDTO>();
            
            foreach (var expensePayableEntryDTO in customerAccountDTO.savingsProducts)
            {
                expensePayableEntryDTO.Id = customerAccountDTO.Savings.Id;
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

            ViewBag.savingsProductDTOs = savingsProductDTOs;
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(customerAccountDTO.Type.ToString());
            ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(customerAccountDTO.Type.ToString());
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(customerAccountDTO.Type.ToString());
            return View("Create", customerAccountDTO);
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
                customerAccountDTO.Customers = Session["customerAccountDTO"] as CustomerDTO;
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
                customerAccountDTO.CustomerAccountTypeTargetProductChartOfAccountName = savingsProduct.ChartOfAccountAccountName;
                customerAccountDTO.CustomerAccountTypeProductCode = 1;

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


                directDebitDTO.CustomerAccountTypeProductCode = 2;

                Session["loanProductId"] = directDebitDTO.CustomerAccountTypeTargetProductId;
                Session["loanProductDescription"] = directDebitDTO.CustomerAccountTypeTargetProductDescription;
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

        [HttpPost]
        public async Task<ActionResult> Create(CustomerAccountDTO customerAccountDTO, ObservableCollection<SavingsProductDTO> selectedRows)
        {

            customerAccountDTO.Savings = Session["beneficiaryAccounts"] as SavingsProductDTO;

            customerAccountDTO.Customers = Session["customerAccountDTO"] as CustomerDTO;


            customerAccountDTO.Investments = Session["benefactorAccounts"] as InvestmentProductDTO;

            customerAccountDTO.TotalValue = 1;
            customerAccountDTO.CustomerId = customerAccountDTO.Customers.Id;
            customerAccountDTO.BranchId = customerAccountDTO.BranchId;
            
            customerAccountDTO.CustomerAccountTypeTargetProductId = customerAccountDTO.Savings.ChartOfAccountId;
            customerAccountDTO.CustomerAccountTypeProductCode = customerAccountDTO.Savings.Code;

            if (!customerAccountDTO.HasErrors)
            {
                await _channelService.AddCustomerAccountAsync(customerAccountDTO,  GetServiceHeader());

                TempData["AlertMessage"] = "Customer Account created successfully";
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = customerAccountDTO.ErrorMessages;

                TempData["EditError"] = "Failed to Create Customer Account for " + customerAccountDTO.Customers.FullName;
                return View(customerAccountDTO);
            }

        }


        public async Task<ActionResult> Holddata(Guid? CustomerAccountTypeTargetProductId, string CustomerAccountTypeTargetProductDescription, CustomerAccountDTO customerAccountDTO)
        {
            await ServeNavigationMenus();
            string targetProductDescription = customerAccountDTO.CustomerAccountTypeTargetProductDescription;
            ViewBag.CustomerAccountTypeTargetProductDescription = targetProductDescription;

            return View("create");
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.record = GetRecordStatusSelectList(string.Empty);
            var customerAccountDTO = await _channelService.FindCustomerAccountAsync(id, false, false, false, false, GetServiceHeader());
            return View(customerAccountDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, CustomerAccountDTO customerAccountDTO)
        {
            customerAccountDTO.CreatedDate = DateTime.Today;
            ViewBag.record = GetRecordStatusSelectList(customerAccountDTO.RecordStatus.ToString());
            if (!customerAccountDTO.HasErrors)
            {
                await _channelService.UpdateCustomerAccountAsync(customerAccountDTO, GetServiceHeader());

                TempData["Edit"] = "Edited Customer Account successfully";

                return RedirectToAction("Index", "CustomerAccounts");
            }
            else
            {
                var errorMessages = customerAccountDTO.ErrorMessages;

                TempData["EditError"] = "Failed to Edit Customer Account";
                return View(customerAccountDTO);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetSavingsProductsAsync()
        {
            var savingsProductDTOs = await _channelService.FindSavingsProductsAsync(GetServiceHeader());           
            return Json(savingsProductDTOs, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<JsonResult> GetInvestmentProductsAsync()
        {
            var investmentProductDTOs = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());          
            return Json(investmentProductDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}

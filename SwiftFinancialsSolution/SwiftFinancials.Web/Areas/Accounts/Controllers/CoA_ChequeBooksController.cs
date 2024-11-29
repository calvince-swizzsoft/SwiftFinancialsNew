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
    public class CoA_ChequeBooksController : MasterController
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

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindChequeBooksByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength,GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(ChartOfAccountDTO => ChartOfAccountDTO.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<ChartOfAccountDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();
            var customerAccountDTO = await _channelService.FindCustomerAccountAsync(id, false, false, false, false, GetServiceHeader());
            return View(customerAccountDTO);
        }
        //[HttpPost]
        //public async Task<ActionResult> Add( CustomerAccountDTO commissionDTO)
        //{
        //    await ServeNavigationMenus();

        //    customerAccountDTOs = TempData["customerAccountDTOs"] as ObservableCollection<CustomerAccountDTO>;

        //    double sumPercentages = 0;



        //    if (ChargeSplitDTOs == null)
        //        ChargeSplitDTOs = new ObservableCollection<CommissionSplitDTO>();

        //    //if (commissionDTO.pa == null || commissionDTO.MaximumCharge == 0)
        //    //{
        //    //    TempData["tPercentage"] = "Charge name and maximum charge required to proceed.";
        //    //}
        //    //else
        //    //{
        //   // Guid parseId;

        //    //if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
        //    //{
        //    //    TempData["tPercentage"] = "Could not add charge split. Choose G/L Account.";

        //    //    await ServeNavigationMenus();

        //    //    return View("Create", commissionDTO);
        //    //}




        //    //foreach (var chargeSplitDTO in commissionDTO.chargeSplit)
        //    //{
        //    //    chargeSplitDTO.Id = parseId;
        //    //    chargeSplitDTO.Description = chargeSplitDTO.Description;
        //    //    chargeSplitDTO.ChartOfAccountId = commissionDTO.Id;
        //    //    chargeSplitDTO.ChartOfAccountAccountName = glAccount.ChartOfAccountAccountName;
        //    //    chargeSplitDTO.Percentage = chargeSplitDTO.Percentage;
        //    //    chargeSplitDTO.Leviable = chargeSplitDTO.Leviable;


        //    //    TempData["tPercentage"] = "";


        //    //    if (chargeSplitDTO.Description == null || chargeSplitDTO.ChartOfAccountAccountName == null || chargeSplitDTO.Percentage == 0)
        //    //    {
        //    //        TempData["tPercentage"] = "Could not add charge split. Make sure to provide all charge split details to proceed.";
        //    //    }
        //    //    else
        //    //    {
        //    //        ChargeSplitDTOs.Add(chargeSplitDTO);

        //    //        sumPercentages = ChargeSplitDTOs.Sum(cs => cs.Percentage);


        //    //        if (sumPercentages > 100)
        //    //        {
        //    //            TempData["tPercentage"] = "Failed to add \"" + chargeSplitDTO.Description.ToUpper() + "\". Total percentage exceeded 100%.";

        //    //            ChargeSplitDTOs.Remove(chargeSplitDTO);
        //    //        }
        //    //        else if (sumPercentages < 1)
        //    //        {
        //    //            TempData["tPercentage"] = "Total percentage must be at least greater than 1%.";
        //    //        }
        //    //    }
        //    //;
        //    //}

        //    ViewBag.ChargeSplitDTOs = ChargeSplitDTOs;

        //    ViewBag.totalPercentage = sumPercentages;

        //    Session["totalPercentage"] = sumPercentages;

        //    TempData["ChargeSplitDTOs"] = ChargeSplitDTOs;


        //    TempData["ChargeDTO"] = commissionDTO;

        //    return View("Create");
        //}

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.CustomerFilterSelectList = GetCustomerFilterSelectList(string.Empty);

            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);
            ViewBag.GetChequeType = GetChequeType(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            var customer = await _channelService.FindCustomerAccountAsync(parseId, false, false, false, false, GetServiceHeader());

            ChequeBookDTO customerAccountDTO = new ChequeBookDTO();

            if (customer != null)
            {

                customerAccountDTO.CustomerAccountId = customer.Id;
                customerAccountDTO.CustomerAccountCustomerIndividualLastName= customer.FullAccountNumber;

                customerAccountDTO.CustomerAccountCustomerIndividualFirstName = customer.CustomerFullName;
                customerAccountDTO.CustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers;
                customerAccountDTO.SerialNumber = customer.CustomerSerialNumber;
                customerAccountDTO.CustomerIndividualIdentityCardNumber = customer.CustomerIndividualIdentityCardNumber;
                //customerAccountDTO.s = customer.CustomerStationZoneDivisionEmployerDescription;
                //customerAccountDTO.CustomerStationZoneDivisionEmployerDescription = customer.CustomerStationZoneDivisionEmployerDescription;
                customerAccountDTO.CustomerAccountCustomerReference1 = customer.CustomerReference1;
                customerAccountDTO.CustomerAccountCustomerReference2 = customer.CustomerReference2;
                customerAccountDTO.CustomerAccountCustomerReference3 = customer.CustomerReference3;
                //customerAccountDTO.CustomerAccountBranchCode = customer.BranchId;
                //customerAccountDTO.BranchDescription = customer.BranchDescription;


            }
           var k= await _channelService.FindPaymentVouchersByChequeBookIdAsync(parseId, GetServiceHeader());

            ViewBag.j = k;

            return View(customerAccountDTO);
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
        public async Task<ActionResult> Add(CustomerAccountDTO customerAccountDTO)
        {
            await ServeNavigationMenus();

            savingsProductDTOs = TempData["savingsProductDTOs"] as ObservableCollection<SavingsProductDTO>;

            if (savingsProductDTOs == null)
                savingsProductDTOs = new ObservableCollection<SavingsProductDTO>();

            //foreach (var expensePayableEntryDTO in customerAccountDTO.savingsProduct)
            //{

            //    expensePayableEntryDTO.ChartOfAccountId = customerAccountDTO.Id;
            //    expensePayableEntryDTO.ChartOfAccountAccountName = expensePayableEntryDTO.ChartOfAccountAccountName;
            //    expensePayableEntryDTO.Description = expensePayableEntryDTO.Description;
            //    //expensePayableEntryDTO.ChartOfAccountAccountName = expensePayableEntryDTO.ChartOfAccountName;
            //    //expensePayableEntryDTO.TotalValue = expensePayableEntryDTO.TotalValue;
            //    //expensePayableEntryDTO.PrimaryDescription = expensePayableEntryDTO.PrimaryDescription;
            //    //expensePayableEntryDTO.SecondaryDescription = expensePayableEntryDTO.SecondaryDescription;
            //    //expensePayableEntryDTO.Reference = expensePayableEntryDTO.Reference;
            //    savingsProductDTOs.Add(expensePayableEntryDTO);
            //};

            TempData["savingsProductDTOs"] = savingsProductDTOs;

            TempData["customerAccountDTO"] = customerAccountDTO;

            ViewBag.savingsProductDTOs = savingsProductDTOs;
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(customerAccountDTO.Type.ToString());
            ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(customerAccountDTO.Type.ToString());
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(customerAccountDTO.Type.ToString());
            return View("Create", customerAccountDTO);
        }



        [HttpPost]
        public async Task<ActionResult> Create(ChequeBookDTO customerAccountDTO, ObservableCollection<SavingsProductDTO> selectedRows)
        {


            customerAccountDTO.ValidateAll();

            if (!customerAccountDTO.HasErrors)
            {
                var result = await _channelService.AddChequeBookAsync(customerAccountDTO,1,GetServiceHeader());
               
                TempData["SuccessMessage"] = "Cheque Book created successfully";
                return RedirectToAction("Create");
            }

            TempData["Error"] = "Failed to create Customer Account";

           
            return View("Create");
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

                return RedirectToAction("Index");
            }

            TempData["EditError"] = "Failed to Edit Customer Account";
            return View(customerAccountDTO);
        }

        [HttpGet]
        public async Task<JsonResult> GetSavingsProductsAsync()
        {
            var savingsProductDTOs = await _channelService.FindSavingsProductsAsync(GetServiceHeader());
            ViewBag.customerAccountSignatoryDTOs = savingsProductDTOs;
            TempData["customerAccountSignatoryDTOs"] = savingsProductDTOs;
            return Json(savingsProductDTOs, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<JsonResult> GetInvestmentProductsAsync()
        {
            var investmentProductDTOs = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());
            ViewBag.customerAccountSignatoryDTOs = investmentProductDTOs;
            TempData["customerAccountSignatoryDTOs"] = investmentProductDTOs;
            return Json(investmentProductDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}

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

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc";
            var sortedColumns = jQueryDataTablesModel.GetSortedColumns().Select(s => s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindCustomerAccountsByFilterInPageAsync(
                jQueryDataTablesModel.sSearch, 1, jQueryDataTablesModel.iDisplayStart,
                jQueryDataTablesModel.iDisplayLength, false, false, false, false, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;
                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(costCenter => costCenter.CreatedDate).ToList();
                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(pageCollectionInfo.PageCollection, totalRecordCount, searchRecordCount, jQueryDataTablesModel.sEcho);
            }

            return this.DataTablesJson(new List<CustomerAccountDTO>(), totalRecordCount, searchRecordCount, jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();
            var customerAccountDTO = await _channelService.FindCustomerAccountAsync(id, false, false, false, false, GetServiceHeader());
            return View(customerAccountDTO);
        }

        public async Task<ActionResult> Create(Guid? id, CustomerAccountDTO customerAccountDTO)
        {
            await ServeNavigationMenus();
            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());
            if (customer != null)
            {
                customerAccountDTO.CustomerId = customer.Id;
                customerAccountDTO.CustomerIndividualFirstName = customer.FullName;
                customerAccountDTO.CustomerIndividualPayrollNumbers = customer.IndividualPayrollNumbers;
                customerAccountDTO.CustomerSerialNumber = customer.SerialNumber;
                customerAccountDTO.CustomerIndividualIdentityCardNumber = customer.IndividualIdentityCardNumber;
                customerAccountDTO.CustomerStationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
                customerAccountDTO.CustomerReference1 = customer.Reference1;
                customerAccountDTO.CustomerReference2 = customer.Reference2;
                customerAccountDTO.CustomerReference3 = customer.Reference3;
                customerAccountDTO.CustomerNonIndividualRegistrationNumber = customer.NonIndividualRegistrationNumber;
            }

            return View(customerAccountDTO);
        }

        public async Task<ActionResult> SavingsProduct(SavingsProductDTO savingsProductDTO, ObservableCollection<SavingsProductDTO> savingProductRowData)
        {
            Session["savingsProductIds"] = savingProductRowData;
            return View("Create", savingProductRowData);
        }

        public async Task<ActionResult> LoansProduct(ObservableCollection<LoanProductDTO> loansProductRowData)
        {
            Session["loansProductIds"] = loansProductRowData;
            return View("Create", loansProductRowData);
        }

        public async Task<ActionResult> InvestmentsProduct(ObservableCollection<InvestmentProductDTO> investmentProductRowData)
        {
            Session["investmentsProductIds"] = investmentProductRowData;
            return View("Create", investmentProductRowData);
        }

        [HttpPost]
        public async Task<ActionResult> Create(CustomerAccountDTO customerAccountDTO)
        {
            // Retrieve the saved collections from the session
            var savingsProductDTOs = Session["savingsProductIds"] as ObservableCollection<SavingsProductDTO>;
            var investmentProductDTOs = Session["investmentsProductIds"] as ObservableCollection<InvestmentProductDTO>;
            var loanProductDTOs = Session["loansProductIds"] as ObservableCollection<LoanProductDTO>;

            customerAccountDTO.ValidateAll();
            
            if (!customerAccountDTO.HasErrors)
            {
                bool result= await _channelService.AddCustomerAccountsAsync(
                    customerAccountDTO.MapTo<CustomerDTO>(), savingsProductDTOs, investmentProductDTOs, loanProductDTOs, GetServiceHeader());

                TempData["AlertMessage"] = "Customer Account created successfully";
                return RedirectToAction("Index");
            }

            TempData["Error"] = "Failed to create Customer Account";
            return View(customerAccountDTO);
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
            var customerAccountDTO = await _channelService.FindCustomerAccountAsync(id, false, false, false, false, GetServiceHeader());
            return View(customerAccountDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, CustomerAccountDTO customerAccountDTO)
        {
            customerAccountDTO.CreatedDate = DateTime.Today;

            if (ModelState.IsValid)
            {
                await _channelService.UpdateCustomerAccountAsync(customerAccountDTO, GetServiceHeader());
                TempData["Edit"] = "Edited Customer Account successfully";
                return RedirectToAction("Index");
            }

            TempData["EditError"] = "Failed to Edit Customer Account";
            return View(customerAccountDTO);
        }
    }
}

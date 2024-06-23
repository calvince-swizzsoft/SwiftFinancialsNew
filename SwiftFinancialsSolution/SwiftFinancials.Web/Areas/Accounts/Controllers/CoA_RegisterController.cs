using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

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

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindCustomerAccountsByFilterInPageAsync(jQueryDataTablesModel.sSearch, 1, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, false, false, false, false, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(costCenter => costCenter.CreatedDate).ToList();

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


        public async Task<ActionResult> Create(Guid? id, CustomerAccountDTO customerAccountDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());
            if(customer!=null)
            {
                customerAccountDTO.Customers = customer;
            }

            return View();
        }



        public async Task<ActionResult>SavingsProduct(ObservableCollection<SavingsProductDTO> savingProductRowData)
        {
            Session["savingsProductIds"] = savingProductRowData;

            return View("Create", savingProductRowData);
        } 
        
        public async Task<ActionResult>LoansProduct(ObservableCollection<LoanProductDTO> loansProductRowData)
        {
            Session["loansProductIds"] = loansProductRowData;

            return View("Create", loansProductRowData);
        } 
        
        public async Task<ActionResult>InvestmentsProduct(ObservableCollection<InvestmentProductDTO> investmentProductRowData)
        {
            Session["investmentsProductIds"] = investmentProductRowData;

            return View("Create", investmentProductRowData);
        }



        [HttpPost]
        public async Task<ActionResult> Create(CustomerAccountDTO customerAccountDTO)
        {
            if(Session["savingsProductIds"] != null)
            {
                ObservableCollection<SavingsProductDTO> sRowData = Session["savingsProductIds"] as ObservableCollection<SavingsProductDTO>;
            }
            
            if(Session["loansProductIds"] != null)
            {
                ObservableCollection<LoanProductDTO> sRowData = Session["loansProductIds"] as ObservableCollection<LoanProductDTO>;
            }
            
            if(Session["investmentsProductIds"] != null)
            {
                ObservableCollection<InvestmentProductDTO> sRowData = Session["investmentsProductIds"] as ObservableCollection<InvestmentProductDTO>;
            }

            customerAccountDTO.ValidateAll();

            if (!customerAccountDTO.HasErrors)
            {
                var result = await _channelService.AddCustomerAccountAsync(customerAccountDTO, GetServiceHeader());

                TempData["AlertMessage"] = "Customer Account created successfully";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = customerAccountDTO.ErrorMessages;

                TempData["Error"] = "Failed to create Customer Account";

                return View(customerAccountDTO);
            }
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
            else
            {
                TempData["EditError"] = "Failed to Edit Customer Account";

                return View(customerAccountDTO);
            }
        }
    }
}

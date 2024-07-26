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

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindCustomerAccountsByFilterInPageAsync(jQueryDataTablesModel.sSearch, 0, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, false, false, false, false, GetServiceHeader());

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

                return RedirectToAction("Index");
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

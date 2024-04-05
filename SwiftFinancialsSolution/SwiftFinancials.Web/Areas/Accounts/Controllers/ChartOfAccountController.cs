using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class ChartOfAccountController : MasterController
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

            var pageCollectionInfo = await _channelService.FindChartOfAccountsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(ChartOfAccount => ChartOfAccount.CreatedDate).ToList();
                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<ChartOfAccountDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var chartOfAccountDTO = await _channelService.FindChartOfAccountAsync(id, GetServiceHeader());

            return View(chartOfAccountDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.ChartOfAccountTypeSelectList = GetChartOfAccountTypeSelectList(string.Empty);
            ViewBag.ChartOfAccountCategorySelectList = GetChartOfAccountCategorySelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(ChartOfAccountDTO chartOfAccountDTO)
        {
            chartOfAccountDTO.ValidateAll();

            if (!chartOfAccountDTO.HasErrors)
            {
                await _channelService.AddChartOfAccountAsync(chartOfAccountDTO, GetServiceHeader());
                TempData["SuccessMessage"] = "Create successful.";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["ErrorMessage"] = "Edit Unsuccessful.";
                var errorMessages = chartOfAccountDTO.ErrorMessages;

                ViewBag.ChartOfAccountTypeSelectList = GetChartOfAccountTypeSelectList(chartOfAccountDTO.AccountType.ToString());
                ViewBag.ChartOfAccountCategorySelectList = GetChartOfAccountCategorySelectList(chartOfAccountDTO.AccountCategory.ToString());

                return View(chartOfAccountDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.ChartOfAccountTypeSelectList = GetChartOfAccountTypeSelectList(string.Empty);
            ViewBag.ChartOfAccountCategorySelectList = GetChartOfAccountCategorySelectList(string.Empty);

            var chartOfAccountDTO = await _channelService.FindChartOfAccountAsync(id, GetServiceHeader());

            return View(chartOfAccountDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, ChartOfAccountDTO chartOfAccountBindingModel)
        {
            if (!chartOfAccountBindingModel.HasErrors)
            {
                await _channelService.UpdateChartOfAccountAsync(chartOfAccountBindingModel, GetServiceHeader());
                ViewBag.ChartOfAccountTypeSelectList = GetChartOfAccountTypeSelectList(chartOfAccountBindingModel.AccountType.ToString());
                ViewBag.ChartOfAccountCategorySelectList = GetChartOfAccountCategorySelectList(chartOfAccountBindingModel.AccountCategory.ToString());

                TempData["SuccessMessage"] = "Edit successful.";
                return RedirectToAction("Index");
            }
            else
            {

                TempData["ErrorMessage"] = "Edit Unsuccessful.";
                return View("Create");
            }
        }

         [HttpGet]
         public async Task<JsonResult> GetChartOfAccountsAsync()
         {
             var chartOfAccountsDTOs = await _channelService.FindChartOfAccountsAsync(GetServiceHeader());

             return Json(chartOfAccountsDTOs, JsonRequestBehavior.AllowGet);
         }
    }
}
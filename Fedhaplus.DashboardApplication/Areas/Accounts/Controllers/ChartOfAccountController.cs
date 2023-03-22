using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Fedhaplus.DashboardApplication.Controllers;
using Fedhaplus.DashboardApplication.Helpers;
using Infrastructure.Crosscutting.Framework.Adapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Fedhaplus.DashboardApplication.Areas.Accounts.Controllers
{
    public class ChartOfAccountController : MasterController
    {
        // GET: Accounts/ChartOfAccount
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

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }

            else return this.DataTablesJson(items: new List<ChartOfAccountDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var chartOfAccountDTO = await _channelService.FindChartOfAccountAsync(id, GetServiceHeader());

            return View(chartOfAccountDTO.ProjectedAs<ChartOfAccountDTO>());
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.ChartOfAccountTypesList = GetChartOfAccountTypes(string.Empty);
            ViewBag.ChartOfAccountCategoriesList = GetChartOfAccountCategories(string.Empty);

            return View();
        }

        //[HttpPost]
        //public async Task<ActionResult> Create(ChartOfAccountBindingModel chartOfAccountBindingModel)
        //{
        //    chartOfAccountBindingModel.ValidateAll();

        //    if (!chartOfAccountBindingModel.HasErrors)
        //    {
        //        await _channelService.AddChartOfAccountAsync(chartOfAccountBindingModel.MapTo<ChartOfAccountDTO>(), GetServiceHeader());

        //        return RedirectToAction("Index");
        //    }
        //    else
        //    {
        //        IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

        //        ViewBag.ChartOfAccountTypesList = GetChartOfAccountTypes(string.Empty);
        //        ViewBag.ChartOfAccountCategoriesList = GetChartOfAccountCategories(string.Empty);

        //        TempData["Error"] = string.Join(",", allErrors);

        //        return View(chartOfAccountBindingModel);
        //    }
        //}

        //public async Task<ActionResult> Edit(Guid id)
        //{
        //    await ServeNavigationMenus();

        //    var chartOfAccountDTO = await _channelService.FindChartOfAccountAsync(id, GetServiceHeader());

        //    return View(chartOfAccountDTO.MapTo<ChartOfAccountBindingModel>());
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> Edit(Guid id, ChartOfAccountBindingModel ChartOfAccountBindingModel)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        await _channelService.UpdateChartOfAccountAsync(ChartOfAccountBindingModel.MapTo<ChartOfAccountDTO>(), GetServiceHeader());

        //        return RedirectToAction("Index");
        //    }
        //    else
        //    {
        //        IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

        //        TempData["Error"] = string.Join(",", allErrors);

        //        return View(ChartOfAccountBindingModel);
        //    }
        //}

        [HttpGet]
        public async Task<JsonResult> GetChartOfAccountsAsync()
        {
            var chartOfAccountDTOs = await _channelService.FindChartOfAccountsAsync(GetServiceHeader());

            return Json(chartOfAccountDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
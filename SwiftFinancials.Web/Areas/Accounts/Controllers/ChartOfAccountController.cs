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
       /* public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
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
        }*/

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var chartOfAccountDTO = await _channelService.FindChartOfAccountAsync(id, GetServiceHeader());

            return View(chartOfAccountDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(ChartOfAccountDTO chartOfAccountDTO)
        {
            chartOfAccountDTO.ValidateAll();

            if (!chartOfAccountDTO.HasErrors)
            {
                await _channelService.AddChartOfAccountAsync(chartOfAccountDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = chartOfAccountDTO.ErrorMessages;

                return View(chartOfAccountDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var chartOfAccountDTO = await _channelService.FindChartOfAccountAsync(id, GetServiceHeader());

            return View(chartOfAccountDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, ChartOfAccountDTO chartOfAccountBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateChartOfAccountAsync(chartOfAccountBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(chartOfAccountBindingModel);
            }
        }

        /* [HttpGet]
         public async Task<JsonResult> GetChartOfAccountsAsync()
         {
             var chartOfAccountsDTOs = await _channelService.FindCompaniesAsync(GetServiceHeader());

             return Json(chartOfAccountsDTOs, JsonRequestBehavior.AllowGet);
         }*/
    }
}
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AdministrationModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Admin.Controllers
{
    public class ReportController : MasterController
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

            var pageCollectionInfo = await _channelService.FindReportFilterInPageAsync(jQueryDataTablesModel.iDisplayStart,
              jQueryDataTablesModel.iDisplayLength, sortedColumns, jQueryDataTablesModel.sSearch, sortAscending, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<ReportDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var reportDTO = await _channelService.FindReportAsync(id, GetServiceHeader());

            return View(reportDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(ReportDTO reportDTO)
        {
            reportDTO.ValidateAll();

            if (!reportDTO.HasErrors)
            {
                await _channelService.AddReportAsync(reportDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = reportDTO.ErrorMessages;

                return View(reportDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var reportDTO = await _channelService.FindReportAsync(id, GetServiceHeader());

            return View(reportDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, ReportDTO reportBindingModel)
        {
            reportBindingModel.ValidateAll();

            if (!reportBindingModel.HasErrors)
            {
                await _channelService.UpdateReportAsync(reportBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(reportBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetReportsAsync()
        {
            var reportsDTOs = await _channelService.FindReportsAsync(false);

            return Json(reportsDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
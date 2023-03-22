using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.ReportsModule;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using Fedhaplus.DashboardApplication.Areas.Reports.Models;
using Fedhaplus.DashboardApplication.Controllers;
using Fedhaplus.DashboardApplication.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Fedhaplus.DashboardApplication.Areas.Reports.Controllers
{
    public class ReportController : MasterController
    {

        // GET: SystemReportHeader
        public async Task<ActionResult> AssetRegisters()
        {
            await ServeNavigationMenus();

            return View();
        }

        // GET: SystemReportHeader
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

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? pageCollectionInfo.PageCollection.Count
                    : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else
                return this.DataTablesJson(items: new List<ReportDTO> { }, totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<JsonResult> ReportHeadersList(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindReportHeadersFilterInPageAsync(jQueryDataTablesModel.iDisplayStart,
                jQueryDataTablesModel.iDisplayLength, sortedColumns, jQueryDataTablesModel.sSearch, sortAscending, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? pageCollectionInfo.PageCollection.Count
                    : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else
                return this.DataTablesJson(items: new List<ReportDTO> { }, totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        [HttpGet]
        public ActionResult LoadReports()
        {
            return PartialView("_Reports");
        }

        [HttpGet]
        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            ReportDTO reportDTO = new ReportDTO();

            if (id != null)
            {
                var report = await _channelService.FindReportByIdAsync((Guid)id, GetServiceHeader());

                reportDTO.ParentName = report.Name;

                reportDTO.ParentId = (Guid)id;
            }

            ViewBag.ReportCategoryList = GetReportCategoriesSelectList(string.Empty);

            return View(reportDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(ReportDTO reportDTO)
        {
            reportDTO.ValidateAll();

            if (!ModelState.IsValid)
            {
                string errorMessage = string.Join(" |", ModelState.Values.SelectMany(a => a.Errors).Select(b => b.ErrorMessage));

                TempData["ModelError"] = reportDTO;

                return View("Create", reportDTO);
            }

            reportDTO.CreatedBy = User.Identity.Name;

            ViewBag.ReportCategoryList = GetReportCategoriesSelectList(reportDTO.Category.ToString());

            var createResult = await _channelService.AddNewReportAsync(reportDTO, GetServiceHeader());

            if (createResult.Item1 != null)
            {
                TempData["Success"] = "Report Created Successfully";

                return RedirectToAction("Index", "Report", "Reports");
            }
            TempData["Error"] = "Report Creation Failed. Error message is:" + createResult.Item2;

            return View( reportDTO);
        }

        [HttpGet]
        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var findresult = await _channelService.FindReportByIdAsync(id, GetServiceHeader());

            ViewBag.ReportCategoryList = GetReportCategoriesSelectList(findresult.Category.ToString());

            if (findresult != null)
            {
                Guid parseId;

                if (findresult.ParentId != Guid.Empty || !Guid.TryParse(findresult.ParentId.ToString(), out parseId))
                {
                    var report = await _channelService.FindReportByIdAsync(findresult.ParentId, GetServiceHeader());

                    findresult.ParentName = report.Name;
                }
            }

            return View(findresult);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(ReportDTO reportDTO)
        {
            reportDTO.ValidateAll();

            if (!ModelState.IsValid)
            {
                await ServeNavigationMenus();

                string errorMessage = string.Join(" |", ModelState.Values.SelectMany(a => a.Errors).Select(b => b.ErrorMessage));

                TempData["ModelError"] = reportDTO;

                return View("Create", reportDTO);
            }

            reportDTO.CreatedBy = User.Identity.Name;

            ViewBag.ReportCategoryList = GetReportCategoriesSelectList(reportDTO.Category.ToString());

            var createResult = await _channelService.UpdateReportAsync(reportDTO, GetServiceHeader());

            if (createResult)
            {
                TempData["Success"] = "Report Updated Successfully";

                return RedirectToAction("Index", "Report", "Reports");
            }
            TempData["Error"] = "Report Update Failed. Please try again later";

            return View(reportDTO);
        }

        public async Task<ActionResult> ViewAssetReport()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public ActionResult ViewAssetReport(ReportModel reportModel)
        {
            GlobalVariable.ReportName = "AssetRegister";

            return View();
        }

        public async Task<ActionResult> ViewStockReport()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public ActionResult ViewStockReport(ReportModel reportModel)
        {
            GlobalVariable.ReportName = "StockItems";

            return View();
        }
        
        [HttpGet]
        public async Task<JsonResult> GetReportsAsync()
        {
            var reportDTOs = await _channelService.FindReportsAsync(GetServiceHeader());

            return Json(reportDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
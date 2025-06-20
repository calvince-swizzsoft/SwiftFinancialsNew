using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using SwiftFinancials.Presentation.Infrastructure.Models;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Registry.Controllers
{
    public class CatalogueController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();
            ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);
            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
            ViewBag.movementStatus = GetMovementStatusSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, int? recordStatus, int? customerFilter, int? customerType, string filterValue, int? movementStatus)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var pageCollectionInfo = await _channelService.FindFileRegistersByFilterInPageAsync(
            jQueryDataTablesModel.sSearch,
                (int)customerFilter,
                int.MaxValue,
                int.MaxValue,
                GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(movement => movement.CreatedDate)
                    .ToList();

                totalRecordCount = sortedData.Count;

                var paginatedData = sortedData
                    .Skip(jQueryDataTablesModel.iDisplayStart)
                    .Take(jQueryDataTablesModel.iDisplayLength)
                    .ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? sortedData.Count
                    : totalRecordCount;

                return this.DataTablesJson(
                    items: paginatedData,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
            else
            {
                return this.DataTablesJson(
                    items: new List<FileMovementHistoryDTO>(),
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var fileMovement = await _channelService.FindFileMovementHistoryByFileRegisterIdAsync(id, GetServiceHeader());

            if (fileMovement == null)
            {
                return HttpNotFound();
            }

            return View(fileMovement);
        }

        [HttpPost]
        public async Task<JsonResult> GetMovementHistory(Guid fileRegisterId)
        {
            try
            {
                var history = await _channelService.FindFileMovementHistoryByFileRegisterIdAsync(fileRegisterId, GetServiceHeader());
                return Json(new { success = true, data = history }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        private SelectList GetMovementStatusSelectList(string selectedValue)
        {
            var statusList = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "All Statuses" },
                new SelectListItem { Value = "1", Text = "Dispatched" },
                new SelectListItem { Value = "2", Text = "Received" }
            };

            return new SelectList(statusList, "Value", "Text", selectedValue);
        }
    }
}
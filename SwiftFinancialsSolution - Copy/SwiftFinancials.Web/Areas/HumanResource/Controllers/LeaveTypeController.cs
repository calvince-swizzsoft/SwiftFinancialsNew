using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Presentation.Infrastructure.Util;

namespace SwiftFinancials.Web.Areas.HumanResource.Controllers
{
    public class LeaveTypeController : MasterController
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

            bool sortDescending = jQueryDataTablesModel.sSortDir_.First() == "desc";
            var sortedColumns = jQueryDataTablesModel.GetSortedColumns().Select(s => s.PropertyName).ToList();

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;

            var pageCollectionInfo = await _channelService.FindLeaveTypesFilterInPageAsync(
                jQueryDataTablesModel.sSearch,
                0,
                int.MaxValue,
                GetServiceHeader()
            );


            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(leaveTypeDTO => leaveTypeDTO.CreatedDate)
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

            return this.DataTablesJson(
                items: new List<LeaveTypeDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
        );
        }
        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var leaveTypeDTO = await _channelService.FindLeaveTypeAsync(id, GetServiceHeader());

            return View(leaveTypeDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            ViewBag.GenderSelectList = GetGenderSelectList(string.Empty);
            ViewBag.UnitTypes = GetUnitTypes(string.Empty);
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(LeaveTypeBindingModel leaveTypeBindingModel)
        {
            leaveTypeBindingModel.ValidateAll();

            if (!leaveTypeBindingModel.HasErrors)
            {
                await _channelService.AddNewLeaveTypeAsync(leaveTypeBindingModel.MapTo<LeaveTypeDTO>(), GetServiceHeader());
                TempData["AlertMessage"] = "Leave type created successfully!";
                TempData["AlertType"] = "success";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["AlertMessage"] = string.Join("<br>", leaveTypeBindingModel.ErrorMessages);
                TempData["AlertType"] = "error";
                return View(leaveTypeBindingModel);
            }
        }


        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
           
            var LeaveTypeDTO = await _channelService.FindLeaveTypeAsync(id, GetServiceHeader());
            ViewBag.GenderSelectList = GetGenderSelectList(string.Empty);
            ViewBag.UnitTypes = GetUnitTypes(string.Empty);

            return View(LeaveTypeDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, LeaveTypeDTO leaveTypeBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateLeaveTypeAsync(leaveTypeBindingModel, GetServiceHeader());
                TempData["AlertMessage"] = "Leave type updated successfully!";
                TempData["AlertType"] = "success";

                return RedirectToAction("Index");
            }
            else
            {
                TempData["AlertMessage"] = "Failed to update leave type. Please correct the errors and try again.";
                TempData["AlertType"] = "error";

                return View(leaveTypeBindingModel);
            }
        }


        [HttpGet]
        public async Task<JsonResult> GetLeaveTypesAsync()
        {
            var leaveTypesDTOs = await _channelService.FindLeaveTypesAsync(GetServiceHeader());

            return Json(leaveTypesDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
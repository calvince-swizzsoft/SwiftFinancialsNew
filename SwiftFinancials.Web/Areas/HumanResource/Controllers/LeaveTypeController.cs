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

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindLeaveTypesFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LeaveTypeDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
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

            return View();
        }

        [HttpPost]
       /* public async Task<ActionResult> Create(LeaveTypeBindingModel leaveTypeBindingModel)
        {
            leaveTypeBindingModel.ValidateAll();

            if (!leaveTypeBindingModel.HasErrors)
            {
                await _channelService.AddNewLeaveTypeAsync(leaveTypeBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = leaveTypeBindingModel.ErrorMessages;

                return View(leaveTypeBindingModel);
            }
        }*/

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var LeaveTypeDTO = await _channelService.FindLeaveTypeAsync(id, GetServiceHeader());

            return View(LeaveTypeDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, LeaveTypeDTO leaveTypeBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateLeaveTypeAsync(leaveTypeBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(leaveTypeBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetLeaveTypesAsync()
        {
            var leaveTypesDTOs = await _channelService.FindCompaniesAsync(GetServiceHeader());

            return Json(leaveTypesDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
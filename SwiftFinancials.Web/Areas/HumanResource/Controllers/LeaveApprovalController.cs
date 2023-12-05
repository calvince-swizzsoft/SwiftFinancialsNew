using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.HumanResource.Controllers
{
    public class LeaveApprovalController : MasterController
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

            var pageCollectionInfo = await _channelService.FindLeaveApplicationsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<EmployeeDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var employeeDTO = await _channelService.FindLeaveApplicationAsync(id, GetServiceHeader());

            return View(employeeDTO);
        }

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            //ViewBag.BloodGroupSelectList = GetBloodGroupSelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var Employee = await _channelService.FindEmployersAsync(GetServiceHeader());

            LeaveApplicationDTO LeaveApplicationBindingModel = new LeaveApplicationDTO();

            if (Employee != null)
            {
                //LeaveApplicationBindingModel.EmployeeId = Employee;
                //LeaveApplicationBindingModel.EmployeeCustomerFullName = customer.FullName;
            }

            return View(LeaveApplicationBindingModel);
        }

        [HttpPost]
        public async Task<ActionResult> Create(LeaveApplicationBindingModel LeaveApplicationBindingModel)
        {
            LeaveApplicationBindingModel.ValidateAll();
            LeaveApplicationDTO leaveApplicationDTO = new LeaveApplicationDTO();

            if (!LeaveApplicationBindingModel.HasErrors)
            {

                await _channelService.AddLeaveApplicationAsync(leaveApplicationDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = LeaveApplicationBindingModel.ErrorMessages;
                //ViewBag.BloodGroupSelectList = GetBloodGroupSelectList(employeeBindingModel.BloodGroup.ToString());
                return View(LeaveApplicationBindingModel);
            }
        }

        public async Task<ActionResult> Approve(Guid id)
        {
            await ServeNavigationMenus();

            //var leaveApplicationBindingModel = await _channelService.FindLeaveApplicationAsync(id, GetServiceHeader());

            //return View(leaveApplicationBindingModel);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            //ViewBag.LeaveAuthOption = GetLeaveAuthOption(string.Empty);

            var leaveApplication = await _channelService.FindLeaveApplicationAsync(parseId, GetServiceHeader());

            LeaveApplicationBindingModel leaveApplicationBindingModel = new LeaveApplicationBindingModel();

            if (leaveApplication != null)
            {
                leaveApplicationBindingModel.Id = leaveApplication.Id;
                leaveApplicationBindingModel.EmployeeId = leaveApplication.EmployeeId;
                leaveApplicationBindingModel.EmployeeCustomerFullName = leaveApplication.EmployeeCustomerFullName;
                leaveApplicationBindingModel.DurationStartDate = leaveApplication.DurationStartDate;
                leaveApplicationBindingModel.DurationEndDate = leaveApplication.DurationEndDate;
                leaveApplicationBindingModel.AuthorizationRemarks = leaveApplication.AuthorizationRemarks;
                leaveApplicationBindingModel.LeaveTypeId = leaveApplication.LeaveTypeId;
                leaveApplicationBindingModel.LeaveTypeDescription = leaveApplication.LeaveTypeDescription;
                leaveApplicationBindingModel.Status = leaveApplication.Status;
                leaveApplicationBindingModel.Reason = leaveApplication.Reason;
            }

            return View(leaveApplicationBindingModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Approve(Guid id, LeaveApplicationBindingModel leaveApplicationBindingModel)
        {
            if (ModelState.IsValid)
            {
                
                await _channelService.AuthorizeLeaveApplicationAsync(leaveApplicationBindingModel.MapTo<LeaveApplicationDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(leaveApplicationBindingModel);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var employeeDTO = await _channelService.FindEmployeeAsync(id, GetServiceHeader());

            return View(employeeDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, EmployeeDTO employeeBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateEmployeeAsync(employeeBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(employeeBindingModel);
            }
        }



        public async Task<ActionResult> Approval(Guid id)
        {
            await ServeNavigationMenus();

            var employeeDTO = await _channelService.FindEmployeeAsync(id, GetServiceHeader());

            return View(employeeDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Approval(Guid id, EmployeeDTO employeeBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateEmployeeAsync(employeeBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(employeeBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetEmployeesAsync()
        {
            var employeesDTOs = await _channelService.FindEmployeesAsync(GetServiceHeader());

            return Json(employeesDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
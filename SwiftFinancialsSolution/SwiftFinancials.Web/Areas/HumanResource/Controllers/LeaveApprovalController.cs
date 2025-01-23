using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System.Globalization;


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

            bool sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc";
            var sortedColumns = jQueryDataTablesModel.GetSortedColumns().Select(s => s.PropertyName).ToList();

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;

            var pageCollectionInfo = await _channelService.FindLeaveApplicationsByFilterInPageAsync(
                jQueryDataTablesModel.sSearch,
                0,
                int.MaxValue,
                GetServiceHeader()
            );

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(leaveApplicationDTO => leaveApplicationDTO.CreatedDate)
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
               items: new List<LeaveApplicationDTO>(),
               totalRecords: totalRecordCount,
               totalDisplayRecords: searchRecordCount,
               sEcho: jQueryDataTablesModel.sEcho
               );
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var leaveApplicationDTO = await _channelService.FindLeaveApplicationAsync(id, GetServiceHeader());

            return View(leaveApplicationDTO);
        }

        [HttpGet]
        public async Task<ActionResult> GetEmployeeDetails(Guid employeeId)
        {
            try
            {
                var employee = await _channelService.FindEmployeeAsync(employeeId, GetServiceHeader());

                if (employee == null)
                {
                    return Json(new { success = false, message = "Employee not found." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        EmployeeCustomerFullName = employee.Customer.FullName,
                        EmployeeCustomerId = employee.CustomerId,
                        EmployeeId = employee.Id,
                        EmployeeBloodGroupDescription = employee.BloodGroupDescription,
                        EmployeeNationalHospitalInsuranceFundNumber = employee.NationalHospitalInsuranceFundNumber,
                        EmployeeNationalSocialSecurityFundNumber = employee.NationalSocialSecurityFundNumber,
                        EmployeeCustomerPersonalIdentificationNumber = employee.CustomerPersonalIdentificationNumber,
                        EmployeeEmployeeTypeCategoryDescription = employee.EmployeeTypeCategoryDescription,
                        EmployeeEmployeeTypeDescription = employee.EmployeeTypeDescription,
                        EmployeeDepartmentDescription = employee.DepartmentDescription,
                        EmployeeDepartmentId = employee.DepartmentId,
                        EmployeeDesignationDescription = employee.DesignationDescription,
                        EmployeeDesignationId = employee.DesignationId,
                        EmployeeBranchDescription = employee.BranchDescription,
                        EmployeeBranchId = employee.BranchId,
                        EmployeeCustomerIndividualGenderDescription = employee.CustomerIndividualGenderDescription,
                        EmployeeCustomerIndividualPayrollNumbers = employee.CustomerIndividualPayrollNumbers,
                        EmployeeEmployeeTypeId = employee.EmployeeTypeId,
                        EmployeeEmployeeTypeChartOfAccountId = employee.EmployeeTypeChartOfAccountId,





                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the Employee details." }, JsonRequestBehavior.AllowGet);
            }
        }
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(LeaveApplicationBindingModel leaveApplicationBindingModel)
        {
            leaveApplicationBindingModel.ValidateAll();

            if (!leaveApplicationBindingModel.HasErrors)
            {
                try
                {
                    await _channelService.AddLeaveApplicationAsync(
                        leaveApplicationBindingModel.MapTo<LeaveApplicationDTO>(),
                        GetServiceHeader()
                    );

                    TempData["SuccessMessage"] = "Leave application submitted successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "An error occurred while submitting the leave application. Please try again.";
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                TempData["ErrorMessage"] = "There were validation errors. Please correct them and try again.";
            }

            return View(leaveApplicationBindingModel);
        }




        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var leaveApplicationDTO = await _channelService.FindLeaveApplicationAsync(id, GetServiceHeader());

            return View(leaveApplicationDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, LeaveApplicationBindingModel leaveApplicationBindingModel)
        {
            leaveApplicationBindingModel.ValidateAll();

            if (!leaveApplicationBindingModel.HasErrors)
            {
                try
                {
                    await _channelService.UpdateLeaveApplicationAsync(
                        leaveApplicationBindingModel.MapTo<LeaveApplicationDTO>(),
                        GetServiceHeader()
                    );

                    TempData["SuccessMessage"] = "Leave application updated successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "An error occurred while updating the leave application. Please try again.";
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                TempData["ErrorMessage"] = "There were validation errors. Please correct them and try again.";
            }

            return View(leaveApplicationBindingModel);
        }



        public async Task<ActionResult> Approval(Guid id)
        {
            await ServeNavigationMenus();

            var leaveApplicationDTO = await _channelService.FindLeaveApplicationAsync(id, GetServiceHeader());

            return View(leaveApplicationDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Approval(Guid id, LeaveApplicationDTO leaveApplicationDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.AuthorizeLeaveApplicationAsync(leaveApplicationDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(leaveApplicationDTO);
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
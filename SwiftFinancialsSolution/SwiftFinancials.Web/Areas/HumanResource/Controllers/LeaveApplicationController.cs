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
using System.Windows.Forms;
using Infrastructure.Crosscutting.Framework.Utils;


namespace SwiftFinancials.Web.Areas.HumanResource.Controllers
{
    public class LeaveApplicationController : MasterController
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
                    var holidays = await _channelService.FindHolidaysAsync(GetServiceHeader());

                    var holidayConflict = holidays.Any(h =>
                        leaveApplicationBindingModel.DurationStartDate <= h.DurationEndDate &&
                        leaveApplicationBindingModel.DurationEndDate >= h.DurationStartDate
                    );

                    if (holidayConflict)
                    {
                        TempData["AlertMessage"] = "You cannot apply for leave during a holiday. Please adjust your dates.";
                        TempData["AlertType"] = "warning"; 
                        return RedirectToAction("Create");
                    }

                    await _channelService.AddLeaveApplicationAsync(
                        leaveApplicationBindingModel.MapTo<LeaveApplicationDTO>(),
                        GetServiceHeader()
                    );

                    TempData["AlertMessage"] = "Operation Success: Leave application submitted successfully!";
                    TempData["AlertType"] = "success"; 
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    TempData["AlertMessage"] = "An error occurred while submitting your leave application. Please try again.";
                    TempData["AlertType"] = "error"; 
                }
            }
            else
            {
                TempData["AlertMessage"] = "There were validation errors. Please correct them and try again!";
                TempData["AlertType"] = "error"; 
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
            // Validate the model
            leaveApplicationBindingModel.ValidateAll();

            if (!leaveApplicationBindingModel.HasErrors)
            {
                try
                {
                    leaveApplicationBindingModel.Status = (int)LeaveApplicationStatus.Pending;

                    var result = await _channelService.UpdateLeaveApplicationAsync(
                        leaveApplicationBindingModel.MapTo<LeaveApplicationDTO>(),
                        GetServiceHeader()
                    );

                    if (result)
                    {
                        TempData["AlertMessage"] = "Operation Success: Leave application updated successfully!";
                        TempData["AlertType"] = "success"; 
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["AlertMessage"] = "The start date must not be less than today.";
                        TempData["AlertType"] = "error"; 
                    }
                }
                catch (Exception ex)
                {
                    TempData["AlertMessage"] = "The start date should not be greater than the end date!";
                    TempData["AlertType"] = "error"; 
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                TempData["AlertMessage"] = "There were validation errors. Please correct them and try again!";
                TempData["AlertType"] = "error"; 
            }

            return View("Edit", leaveApplicationBindingModel);
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
                try
                {
                    var authorizationStatus = leaveApplicationDTO.LeaveAuthOption;

                    switch (authorizationStatus)
                    {
                        case LeaveAuthOption.Approve:
                            leaveApplicationDTO.Status = (int)LeaveApplicationStatus.Approved;
                            break;
                        case LeaveAuthOption.Reject:
                            leaveApplicationDTO.Status = (int)LeaveApplicationStatus.Rejected;
                            break;
                        default:
                            break;
                    }

                    var result = await _channelService.AuthorizeLeaveApplicationAsync(leaveApplicationDTO, GetServiceHeader());

                    if (result)
                    {
                        string message = authorizationStatus == LeaveAuthOption.Approve
                            ? "Operation Success: Leave approved successfully!"
                            : "Operation Success: Leave rejected successfully!";

                        TempData["AlertMessage"] = message;
                        TempData["AlertType"] = "success"; 

                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["AlertMessage"] = "An error occurred while updating the leave approval. Please try again.";
                        TempData["AlertType"] = "error";
                    }
                }
                catch (Exception ex)
                {
                    TempData["AlertMessage"] = "An error occurred while updating the leave approval. Please try again.";
                    TempData["AlertType"] = "error";
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                TempData["AlertMessage"] = "There were validation errors. Please correct them and try again.";
                TempData["AlertType"] = "error";
            }

            return RedirectToAction("Index");
        }




        public async Task<ActionResult> Recall(Guid id)
        {
            await ServeNavigationMenus();

            var leaveApplicationDTO = await _channelService.FindLeaveApplicationAsync(id, GetServiceHeader());

            return View(leaveApplicationDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Recall(Guid id, LeaveApplicationDTO leaveApplicationDTO)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Fetch the leave application from the service
                    var leaveApplication = await _channelService.FindLeaveApplicationAsync(id);

                    // If the leave application is not found, show a message and redirect
                    if (leaveApplication == null)
                    {
                        MessageBox.Show(
                            "Leave application not found!",
                            "Leave Management",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information,
                            MessageBoxDefaultButton.Button1,
                            MessageBoxOptions.ServiceNotification
                        );
                        return RedirectToAction("Index");
                    }

                    // If the current date is before the leave's end date, ask for emergency recall confirmation
                    var currentDate = DateTime.Now;
                    if (currentDate < leaveApplication.DurationEndDate)
                    {
                        var dialogResult = MessageBox.Show(
                            "The leave period has not yet ended. Is this an emergency?",
                            "Emergency Recall Confirmation",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning,
                            MessageBoxDefaultButton.Button1,
                            MessageBoxOptions.ServiceNotification
                        );

                        if (dialogResult == DialogResult.No)
                        {
                            MessageBox.Show(
                                "Leave recall canceled. You can only recall this leave after its end date or in case of an emergency.",
                                "Leave Recall Canceled",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information,
                                MessageBoxDefaultButton.Button1,
                                MessageBoxOptions.ServiceNotification
                            );
                            return RedirectToAction("Index");
                        }
                    }

                    leaveApplicationDTO.Status = (int)LeaveApplicationStatus.Recalled;

                    bool isRecallSuccessful = await _channelService.RecallLeaveApplicationAsync(leaveApplicationDTO, GetServiceHeader());

                    if (isRecallSuccessful)
                    {
                        MessageBox.Show(
                            "Operation Success: Leave recall processed successfully!",
                            "Leave Management",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information,
                            MessageBoxDefaultButton.Button1,
                            MessageBoxOptions.ServiceNotification
                        );
                    }
                    else
                    {
                        MessageBox.Show(
                            "Failed to process leave recall. Please try again.",
                            "Error Message",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error,
                            MessageBoxDefaultButton.Button1,
                            MessageBoxOptions.ServiceNotification
                        );
                    }

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "An error occurred while recalling the leave application. Please try again.",
                        "Application Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.ServiceNotification
                    );
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                MessageBox.Show(
                    "There were validation errors. Please correct them and try again.",
                    "Validation Errors",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification
                );
            }

            return RedirectToAction("Index");
        }






        [HttpGet]
        public async Task<JsonResult> GetEmployeesAsync()
        {
            var employeesDTOs = await _channelService.FindEmployeesAsync(GetServiceHeader());

            return Json(employeesDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
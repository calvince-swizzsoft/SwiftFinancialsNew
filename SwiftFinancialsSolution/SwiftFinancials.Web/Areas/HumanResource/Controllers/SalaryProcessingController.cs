using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.AdministrationModule;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Windows.Forms;

namespace SwiftFinancials.Web.Areas.HumanResource.Controllers
{
    public class SalaryProcessingController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Index(JQueryDataTablesModel jQueryDataTablesModel, DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue)
                startDate = DateTime.MinValue;

            if (!endDate.HasValue)
                endDate = DateTime.MaxValue;

            int totalRecordCount = 0;
            int searchRecordCount = 0;

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;

            bool sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc";

            int status = 1;

            try
            {
                var pageCollectionInfo = await _channelService.FindSalaryPeriodsByFilterInPageAsync(
                    status,
                    startDate.Value,
                    endDate.Value,
                    jQueryDataTablesModel.sSearch,
                    0,
                    int.MaxValue,
                    GetServiceHeader()
                );

                if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
                {

                    var sortedData = pageCollectionInfo.PageCollection
                        .OrderByDescending(salaryProcessingDTO => salaryProcessingDTO.CreatedDate)
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
                    items: new List<SalaryProcessingDTO>(),
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
            );
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }



        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var salaryPeriodDTO = await _channelService.FindSalaryPeriodAsync(id, GetServiceHeader());

            return View(salaryPeriodDTO);
        }
        [HttpPost]
        public async Task<ActionResult> CheckEmployeesBySalaryGroup(Guid salaryGroupId)
        {
            if (salaryGroupId == Guid.Empty)
            {
                return Json(new { success = false, message = "Invalid Salary Group ID." });
            }

            try
            {
                var salaryCards = await _channelService.FindSalaryCardsAsync(GetServiceHeader());

                if (salaryCards == null || !salaryCards.Any())
                {
                    return Json(new { success = false, message = "No salary cards found." });
                }

                var employeeDetails = new List<object>();
                var employeeIds = new List<Guid>();

                foreach (var salaryCard in salaryCards)
                {
                    if (salaryCard.SalaryGroupId == salaryGroupId)
                    {
                        employeeDetails.Add(new
                        {
                            EmployeeCustomerFullName = salaryCard.EmployeeCustomerFullName,
                            EmployeeId = salaryCard.EmployeeId, 
                            CardNumber = salaryCard.CardNumber,
                            Remarks = salaryCard.Remarks ?? "N/A", 
                            CreatedDate = salaryCard.CreatedDate.ToString("dd-MM-yyyy")
                        });

                        employeeIds.Add(salaryCard.EmployeeId);
                    }
                }

                if (!employeeDetails.Any())
                {
                    return Json(new { success = false, message = "No employees found for the selected salary group." });
                }

                TempData["SelectedSalaryGroupId"] = salaryGroupId.ToString();
                TempData["EmployeeIds"] = string.Join(",", employeeIds); 

                return Json(new { success = true, data = employeeDetails });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in CheckEmployeesBySalaryGroup: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while processing your request." });
            }
        }




        public async Task<ActionResult> ProcessSalary(Guid id)
        {
            await ServeNavigationMenus();

            var salaryPeriod = await _channelService.FindSalaryPeriodAsync(id, GetServiceHeader());

            // Fetch salary groups
            var salaryGroups = await _channelService.FindSalaryGroupsAsync(GetServiceHeader());
            ViewBag.SalaryGroups = salaryGroups;

            // Fetch branches
            var branches = await _channelService.FindBranchesAsync(GetServiceHeader());
            ViewBag.Branches = branches;

            // Fetch departments
            var departments = await _channelService.FindDepartmentsAsync(GetServiceHeader());
            ViewBag.Departments = departments;

            return View(salaryPeriod);
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> ProcessSalary(Guid id, SalaryProcessingDTO salaryPeriodDTO)
        //{
        //    try
        //    {
        //        // Step 1: Validate input parameters
        //        if (salaryPeriodDTO == null)
        //            throw new ArgumentNullException(nameof(salaryPeriodDTO));

        //        if (TempData["SelectedEmployeeIds"] == null)
        //        {
        //            ShowMessageBox("No employees were selected for processing.", MessageBoxIcon.Warning);
        //            return View(salaryPeriodDTO);
        //        }

        //        // Step 2: Parse selected employee IDs
        //        var employeeIds = TempData["SelectedEmployeeIds"].ToString()
        //                             .Split(',')
        //                             .Select(Guid.Parse)
        //                             .ToList();

        //        if (!employeeIds.Any())
        //        {
        //            ShowMessageBox("No employees found for processing.", MessageBoxIcon.Warning);
        //            return View(salaryPeriodDTO);
        //        }

        //        // Step 3: Fetch employees from the service
        //        var employees = new ObservableCollection<EmployeeDTO>();
        //        foreach (var employeeId in employeeIds)
        //        {
        //            var employee = await _channelService.FindEmployeeAsync(employeeId, GetServiceHeader());
        //            if (employee != null)
        //            {
        //                employees.Add(employee);
        //            }
        //        }

        //        if (!employees.Any())
        //        {
        //            ShowMessageBox("No valid employees found for processing.", MessageBoxIcon.Warning);
        //            return View(salaryPeriodDTO);
        //        }

        //        // Step 4: Call the ProcessSalaryPeriodAsync method
        //        var isProcessed = await _channelService.ProcessSalaryPeriodAsync(salaryPeriodDTO, employees, GetServiceHeader());
        //        if (isProcessed)
        //        {
        //            var payslips = await _channelService.FindPaySlipsBySalaryPeriodIdAsync(salaryPeriodDTO.Id, GetServiceHeader());
        //            foreach (var payslipid in payslips)
        //            {
        //                await _channelService.PostPaySlipAsync(payslipid.Id, 1234, GetServiceHeader());
        //            }


        //            ShowMessageBox("Salary period processed successfully.", MessageBoxIcon.Information);
        //            return RedirectToAction("Index");
        //        }
        //        else
        //        {
        //            ShowMessageBox("Failed to process the salary period.", MessageBoxIcon.Error);
        //            ModelState.AddModelError(string.Empty, "Failed to process the salary period.");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Step 5: Handle exceptions
        //        ShowMessageBox("An unexpected error occurred while processing the salary period. Please try again.", MessageBoxIcon.Error);
        //        Console.Error.WriteLine($"Error: {ex.Message}\nStack Trace: {ex.StackTrace}");
        //        ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
        //    }

        //    // Step 6: Return the view in case of issues
        //    return View(salaryPeriodDTO);
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ProcessSalary(Guid id, SalaryProcessingDTO salaryPeriodDTO)
        {
            try
            {
                if (salaryPeriodDTO == null)
                    throw new ArgumentNullException(nameof(salaryPeriodDTO));

                if (TempData["SelectedSalaryGroupId"] == null || TempData["EmployeeIds"] == null)
                {
                    SetSweetAlert("No salary group or employees were selected.", "warning");
                    return View(salaryPeriodDTO);
                }

                var salaryGroupId = Guid.Parse(TempData["SelectedSalaryGroupId"].ToString());
                var employeeIds = TempData["EmployeeIds"].ToString()
                                                        .Split(',')
                                                        .Select(Guid.Parse)
                                                        .ToList();

                if (!employeeIds.Any())
                {
                    SetSweetAlert("No employees selected.", "warning");
                    return View(salaryPeriodDTO);
                }

                salaryPeriodDTO.ValidateAll();
                if (salaryPeriodDTO.HasErrors)
                {
                    ModelState.AddModelError(string.Empty, string.Join(", ", salaryPeriodDTO.ErrorMessages));
                    return View(salaryPeriodDTO);
                }

                var employees = new ObservableCollection<EmployeeDTO>();
                foreach (var employeeId in employeeIds)
                {
                    var employee = await _channelService.FindEmployeeAsync(employeeId, GetServiceHeader());
                    if (employee != null)
                    {
                        employees.Add(employee);
                    }
                }

                if (!employees.Any())
                {
                    SetSweetAlert("No valid employees found for the provided IDs.", "warning");
                    return View(salaryPeriodDTO);
                }

                var isProcessed = await _channelService.ProcessSalaryPeriodAsync(salaryPeriodDTO, employees, GetServiceHeader());
                if (isProcessed)
                {
                    var payslips = await _channelService.FindPaySlipsBySalaryPeriodIdAsync(salaryPeriodDTO.Id, GetServiceHeader());
                    foreach (var payslipid in payslips)
                    {
                        await _channelService.PostPaySlipAsync(payslipid.Id, 1234, GetServiceHeader());
                    }

                    SetSweetAlert("Salary period processed and pay slips posted successfully.", "success");
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Failed to process the salary period.");
                    SetSweetAlert("Failed to process the salary period.", "error");
                }
            }
            catch (Exception ex)
            {
                SetSweetAlert("An unexpected error occurred. Please try again.", "error");
                Console.Error.WriteLine($"Error: {ex.Message}\nStack Trace: {ex.StackTrace}");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
            }

            return View(salaryPeriodDTO);
        }

        private void SetSweetAlert(string message, string icon)
        {
            TempData["SweetAlertMessage"] = message;
            TempData["SweetAlertIcon"] = icon;
        }













        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, SalaryProcessingDTO salaryPeriodDTO)
        {
            salaryPeriodDTO.ValidateAll();

            if (!salaryPeriodDTO.HasErrors)
            {
                await _channelService.UpdateSalaryPeriodAsync(salaryPeriodDTO, GetServiceHeader());
                MessageBox.Show(
                                                             "Operation Success",
                                                             "Customer Receipts",
                                                             MessageBoxButtons.OK,
                                                             MessageBoxIcon.Information,
                                                             MessageBoxDefaultButton.Button1,
                                                             MessageBoxOptions.ServiceNotification
                                                         );

                return RedirectToAction("Index");
            }
            else
            {
                return View(salaryPeriodDTO);
            }
        }



        [HttpGet]
        public async Task<JsonResult> GetSalaryPeriodsAsync()
        {
            var salaryPeriods = await _channelService.FindSalaryPeriodsAsync(GetServiceHeader());
            return Json(salaryPeriods, JsonRequestBehavior.AllowGet);
        }







    }
}
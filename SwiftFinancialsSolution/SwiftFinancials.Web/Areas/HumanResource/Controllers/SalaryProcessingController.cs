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




        public async Task<ActionResult> Create(Guid id)
        {
            await ServeNavigationMenus();

            var salaryPeriodDTO = await _channelService.FindSalaryPeriodAsync(id, GetServiceHeader());
            ViewBag.MonthTypeSelectList = GetMonthsAsync(string.Empty);
            ViewBag.EmployeeTypeSelectList = GetEmployeeCategorySelectList(string.Empty);

            var salaryGroups = await _channelService.FindSalaryGroupsAsync(GetServiceHeader());
            ViewBag.SalaryGroups = salaryGroups;

            return View(salaryPeriodDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Guid id, SalaryProcessingDTO salaryPeriodDTO)
        {
            try
            {
                if (salaryPeriodDTO == null)
                    throw new ArgumentNullException(nameof(salaryPeriodDTO));

                if (TempData["SelectedSalaryGroupId"] == null || TempData["EmployeeIds"] == null)
                {
                    ShowMessageBox("No salary group or employees were selected.", MessageBoxIcon.Warning);
                    return View(salaryPeriodDTO);
                }

                var salaryGroupId = Guid.Parse(TempData["SelectedSalaryGroupId"].ToString());
                var employeeIds = TempData["EmployeeIds"].ToString()
                                                .Split(',')
                                                .Select(Guid.Parse)
                                                .ToList();

                if (!employeeIds.Any())
                {
                    ShowMessageBox("No employees selected.", MessageBoxIcon.Warning);
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
                    ShowMessageBox("No valid employees found for the provided IDs.", MessageBoxIcon.Warning);
                    return View(salaryPeriodDTO);
                }

                var isProcessed = await _channelService.ProcessSalaryPeriodAsync(salaryPeriodDTO, employees, GetServiceHeader());
                if (isProcessed)
                {
                    // Fetch payslips for the processed salary period
                    var paySlips = await _channelService.FindPaySlipsBySalaryPeriodIdAsync(salaryPeriodDTO.Id, GetServiceHeader());

                    if (paySlips != null && paySlips.Any())
                    {
                        foreach (var paySlip in paySlips)
                        {
                            // Check if payslip already exists
                            var existingPaySlips = await _channelService.FindPaySlipsBySalaryPeriodIdAsync(paySlip.SalaryPeriodId, GetServiceHeader());
                            if (existingPaySlips.Any(p => p.Id == paySlip.Id))
                            {
                                Console.WriteLine($"PaySlip with ID  already exists. Skipping.");
                                continue;
                            }

                            // Post only new payslips
                            var isPosted = await _channelService.PostPaySlipAsync(paySlip.SalaryPeriodId, salaryPeriodDTO.ModuleNavigationItemCode, GetServiceHeader());
                            if (!isPosted)
                            {
                                ShowMessageBox($"PaySlip with ID  failed to post.", MessageBoxIcon.Error);
                                TempData["WarningMessage"] = $"PaySlip with ID failed to post.";
                            }
                        }
                    }

                    ShowMessageBox("Salary period processed and pay slips posted successfully.", MessageBoxIcon.Information);
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Failed to process the salary period.");
                    ShowMessageBox("Failed to process the salary period.", MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                ShowMessageBox("An unexpected error occurred. Please try again.", MessageBoxIcon.Error);
                Console.Error.WriteLine($"Error: {ex.Message}\nStack Trace: {ex.StackTrace}");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
            }

            return View(salaryPeriodDTO);
        }

        private void ShowMessageBox(string message, MessageBoxIcon icon)
        {
            MessageBox.Show(
                message,
                "Salary Processing",
                MessageBoxButtons.OK,
                icon,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.ServiceNotification
            );
        }







        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var salaryPeriod = await _channelService.FindSalaryPeriodAsync(id, GetServiceHeader());

            var salaryGroups = await _channelService.FindSalaryGroupsAsync(GetServiceHeader());
            ViewBag.SalaryGroups = salaryGroups;

            return View(salaryPeriod);
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
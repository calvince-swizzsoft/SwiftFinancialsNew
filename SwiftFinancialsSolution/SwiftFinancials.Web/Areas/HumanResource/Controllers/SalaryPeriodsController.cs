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
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Windows.Forms;

namespace SwiftFinancials.Web.Areas.HumanResource.Controllers
{
    public class SalaryPeriodsController : MasterController
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
            ViewBag.MonthTypeSelectList = GetMonthsAsync(string.Empty);
            ViewBag.EmployeeTypeSelectList = GetEmployeeCategorySelectList(string.Empty);

            return View(salaryPeriodDTO);
        }

        public async Task<ActionResult> ViewPayroll(Guid id)
        {
            

            await ServeNavigationMenus();
            var salaryProcessingDTO = await _channelService.FindSalaryPeriodAsync(id, GetServiceHeader());

            if (salaryProcessingDTO == null)
            {
                TempData["ErrorMessage"] = "Salary period not found.";
                return RedirectToAction("Index");
            }

            return View(salaryProcessingDTO);
        }

        [HttpGet]
        public async Task<ActionResult> GetpostingPerioddDetails(Guid postingPeriodId)
        {
            try
            {
                var salaryPeriod = await _channelService.FindPostingPeriodAsync(postingPeriodId, GetServiceHeader());

                if (salaryPeriod == null)
                {
                    return Json(new { success = false, message = "SalaryPeriod not found." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        PostingPeriodDescription = salaryPeriod.Description,
                        PostingPeriodId = salaryPeriod.Id,








                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the salaryPeriods details." }, JsonRequestBehavior.AllowGet);
            }
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.MonthTypeSelectList = GetMonthsAsync(string.Empty);
            ViewBag.EmployeeTypeSelectList = GetEmployeeCategorySelectList(string.Empty);



            return View("Create");
        }

        [HttpPost]
        public async Task<ActionResult> Create(SalaryProcessingDTO salaryPeriodDTO, Guid? id)
        {
            salaryPeriodDTO.ValidateAll();

            if (!salaryPeriodDTO.HasErrors)
            {
                var salaryPeriod = await _channelService.AddSalaryPeriodAsync(salaryPeriodDTO, GetServiceHeader());

                if (salaryPeriod.ErrorMessageResult != null)
                {
                    TempData["AlertMessage"] = $"Operation Unsuccessful: {salaryPeriod.ErrorMessageResult}";
                    TempData["AlertType"] = "error";

                    await ServeNavigationMenus();
                    ViewBag.MonthTypeSelectList = GetMonthsAsync(salaryPeriodDTO.Month.ToString());
                    ViewBag.EmployeeTypeSelectList = GetEmployeeCategorySelectList(salaryPeriodDTO.EmployeeCategoryDescription.ToString());

                    return View();
                }

                TempData["AlertMessage"] = "Operation Success";
                TempData["AlertType"] = "success";

                ViewBag.MonthTypeSelectList = GetMonthsAsync(salaryPeriodDTO.Month.ToString());
                ViewBag.EmployeeTypeSelectList = GetEmployeeCategorySelectList(salaryPeriodDTO.EmployeeCategoryDescription.ToString());

                return RedirectToAction("Index");
            }
            else
            {
                TempData["AlertMessage"] = "Validation errors found. Please fix the errors and try again.";
                TempData["AlertType"] = "warning";

                return View(salaryPeriodDTO);
            }
        }


        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var salaryPeriod = await _channelService.FindSalaryPeriodAsync(id, GetServiceHeader());
            ViewBag.MonthTypeSelectList = GetMonthsAsync(string.Empty);
            ViewBag.EmployeeTypeSelectList = GetEmployeeCategorySelectList(string.Empty);

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

                TempData["AlertMessage"] = "Operation Success";
                TempData["AlertType"] = "success";

                return RedirectToAction("Index");
            }
            else
            {
                TempData["AlertMessage"] = "Validation errors found. Please fix the errors and try again.";
                TempData["AlertType"] = "warning";

                return View(salaryPeriodDTO);
            }
        }


        [HttpGet]
        public async Task<ActionResult> GetsalaryPeriodDetails(Guid salaryPeriodId)
        {
            try
            {
                var salaryPeriod = await _channelService.FindSalaryPeriodAsync(salaryPeriodId, GetServiceHeader());

                if (salaryPeriod == null)
                {
                    return Json(new { success = false, message = "SalaryPeriod not found." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        PostingPeriodDescription = salaryPeriod.PostingPeriodDescription,
                        PostingPeriodId = salaryPeriod.PostingPeriodId,
                        TaxReliefAmount = salaryPeriod.TaxReliefAmount,
                        MaximumProvidentFundReliefAmount = salaryPeriod.MaximumProvidentFundReliefAmount,
                        MaximumInsuranceReliefAmount = salaryPeriod.MaximumInsuranceReliefAmount,
                        Remarks = salaryPeriod.Remarks,
                        EmployeeCategoryDescription = salaryPeriod.EmployeeCategoryDescription,







                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the salaryPeriods details." }, JsonRequestBehavior.AllowGet);
            }
        }

        public async Task<ActionResult> ProcessSalary(Guid? id)
        {
            await ServeNavigationMenus();

            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);

            var salaryProcessingDTO = new SalaryProcessingDTO();

            if (id.HasValue && id != Guid.Empty)
            {
                try
                {
                    var salaryPeriod = await _channelService.FindSalaryPeriodAsync(
                        id.Value,

                        GetServiceHeader()
                    );

                    if (salaryPeriod != null)
                    {
                        salaryProcessingDTO.PostingPeriodDescription = salaryPeriod.PostingPeriodDescription;
                        salaryProcessingDTO.PostingPeriodId = salaryPeriod.PostingPeriodId;
                        salaryProcessingDTO.Remarks = salaryPeriod.Remarks;
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Customer account details could not be found.";
                        return RedirectToAction("Index");
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"An error occurred while processing salary: {ex.Message}";
                    return RedirectToAction("Index");
                }
            }

            return View(salaryProcessingDTO);
        }



        [HttpPost]
        public async Task<ActionResult> ProcessSalary(SalaryProcessingDTO salaryPeriodDTO, List<Guid> employeeIds)
        {
            salaryPeriodDTO.ValidateAll();

            if (!salaryPeriodDTO.HasErrors)
            {
                var employees = new ObservableCollection<EmployeeDTO>();

                foreach (var employeeId in employeeIds)
                {
                    var employee = await _channelService.FindEmployeeAsync(employeeId, GetServiceHeader());
                    if (employee != null)
                    {
                        employees.Add(employee);
                    }
                }

                if (employees.Any())
                {
                    var isProcessed = await _channelService.ProcessSalaryPeriodAsync(
                        salaryPeriodDTO,
                        employees,
                        GetServiceHeader()
                    );

                    if (isProcessed)
                    {
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Failed to process the salary period. Please try again.");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "No valid employees found for the provided IDs.");
                }

                ViewBag.MonthTypeSelectList = GetMonthsAsync(salaryPeriodDTO.Month.ToString());
                ViewBag.EmployeeTypeSelectList = GetEmployeeCategorySelectList(salaryPeriodDTO.EmployeeCategoryDescription.ToString());
            }
            else
            {
                var errorMessages = salaryPeriodDTO.ErrorMessages;
                ModelState.AddModelError(string.Empty, string.Join(", ", errorMessages));
            }

            return View(salaryPeriodDTO);
        }

        [HttpPost]
        public async Task<JsonResult> GetSalaryPeriodsAsync()
        {
            try
            {
                var salaryPeriods = await _channelService.FindSalaryPeriodsAsync(GetServiceHeader());

                var result = salaryPeriods.Select(sp => new
                {
                    sp.PostingPeriodId,
                    sp.PostingPeriodDescription,
                    sp.StatusDescription,
                    sp.EmployeeCategoryDescription,
                    CreatedDate = sp.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")
                }).ToList();

                return Json(new { aaData = result });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error fetching salary periods: {ex.Message}");
                return Json(new { error = "Failed to load salary periods." });
            }
        }


    }
}
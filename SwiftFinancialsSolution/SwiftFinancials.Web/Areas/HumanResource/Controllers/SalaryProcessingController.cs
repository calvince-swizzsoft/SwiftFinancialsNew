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
        public async Task<ActionResult> Index(JQueryDataTablesModel jQueryDataTablesModel, DateTime startDate, DateTime endDate)
        {
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
                    startDate,
                    endDate,
                    jQueryDataTablesModel.sSearch,
                    pageIndex,
                    pageSize,
                    GetServiceHeader()
                );

                if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
                {
                    totalRecordCount = pageCollectionInfo.ItemsCount;

                    searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                        ? pageCollectionInfo.PageCollection.Count
                        : totalRecordCount;

                    return this.DataTablesJson(
                        items: pageCollectionInfo.PageCollection,
                        totalRecords: totalRecordCount,
                        totalDisplayRecords: searchRecordCount,
                        sEcho: jQueryDataTablesModel.sEcho
                    );
                }
                else
                {
                    return this.DataTablesJson(
                        items: new List<SalaryProcessingDTO>(),
                        totalRecords: totalRecordCount,
                        totalDisplayRecords: searchRecordCount,
                        sEcho: jQueryDataTablesModel.sEcho
                    );
                }
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
            //var chartOfAccount = await _channelService.FindGeneralLedgerAsync(id, GetServiceHeader());

            return View(salaryPeriodDTO);
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

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.MonthTypeSelectList = GetMonthsAsync(string.Empty);
            ViewBag.EmployeeTypeSelectList = GetEmployeeCategorySelectList(string.Empty);



            return View("Create");
        }

        [HttpPost]
        public async Task<ActionResult> Create(SalaryProcessingDTO salaryPeriodDTO, List<Guid> employeeIds)
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
                    MessageBox.Show(
                                                             "Operation Success",
                                                             "Customer Receipts",
                                                             MessageBoxButtons.OK,
                                                             MessageBoxIcon.Information,
                                                             MessageBoxDefaultButton.Button1,
                                                             MessageBoxOptions.ServiceNotification
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


        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var salaryPeriod = await _channelService.FindSalaryPeriodAsync(id, GetServiceHeader());

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
    }
}
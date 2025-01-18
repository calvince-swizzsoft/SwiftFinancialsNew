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
    public class PeriodClosingController : MasterController
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





        public async Task<ActionResult> Close(Guid id)
        {
            await ServeNavigationMenus();

            var salaryPeriod = await _channelService.FindSalaryPeriodAsync(id, GetServiceHeader());
            ViewBag.MonthTypeSelectList = GetMonthsAsync(string.Empty);
            ViewBag.EmployeeTypeSelectList = GetEmployeeCategorySelectList(string.Empty);

            return View(salaryPeriod);
        }


        public async Task<ActionResult> View(Guid id)
        {
            await ServeNavigationMenus();

            //Guid parseId;
            //if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            //{
            //    return Json(new { success = false, message = "Invalid ID" }, JsonRequestBehavior.AllowGet);
            //}
            Session["period"] = id;
            var salaryPeriod = await _channelService.FindSalaryPeriodAsync(id, GetServiceHeader());
            if (salaryPeriod == null)
            {
                return Json(new { success = false, message = "No data found" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = salaryPeriod }, JsonRequestBehavior.AllowGet);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Close()
        {
            Guid id = Guid.Empty;

            if (Session["period"] != null)
                id = (Guid)Session["period"];

            try
            {
                if (id == Guid.Empty)
                {
                    TempData["Message"] = "Invalid ID.";
                    TempData["MessageType"] = "error";
                    return RedirectToAction("Index", "PeriodClosing", new { Area = "HumanResource" });
                }

                var salaryPeriodDTO = await _channelService.FindSalaryPeriodAsync(id, GetServiceHeader());
                salaryPeriodDTO.ValidateAll();

                if (salaryPeriodDTO.HasErrors)
                {
                    TempData["Message"] = "Failed to close salary period due to validation errors.";
                    TempData["MessageType"] = "error";
                    return RedirectToAction("Index", "PeriodClosing", new { Area = "HumanResource" });
                }

                bool result = await _channelService.CloseSalaryPeriodAsync(salaryPeriodDTO, GetServiceHeader());

                if (result)
                {
                    TempData["Message"] = "Salary period closed successfully!";
                    TempData["MessageType"] = "success";
                }
                else
                {
                    TempData["Message"] = "Failed to close salary period. Please try again.";
                    TempData["MessageType"] = "error";
                }

                return RedirectToAction("Index", "PeriodClosing", new { Area = "HumanResource" });
            }
            catch (Exception ex)
            {
                // Log the exception
                TempData["Message"] = "An unexpected error occurred: " + ex.Message;
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", "PeriodClosing", new { Area = "HumanResource" });
            }
        }






    }
}
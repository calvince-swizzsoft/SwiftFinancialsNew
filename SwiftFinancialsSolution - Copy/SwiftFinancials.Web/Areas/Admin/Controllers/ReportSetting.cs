using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AdministrationModule;
using SwiftFinancials.Web.Areas.Admin.Models;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Admin.Controllers
{
    public class ReportSettingController : MasterController
    {
        private string connectionString = "Data Source=(local);Initial Catalog=SwiftFinancialsDB_Live;Persist Security Info=true; User ID=sa;Password=pass123; Pooling=True";


        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }
        [HttpGet]
        public async Task<JsonResult> GetRoles()
        {
            //fetch all roles
            var result = await _channelService.FindReportsAsync(GetServiceHeader());

            return Json(result, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindReportFilterInPageAsync(jQueryDataTablesModel.iDisplayStart,
              jQueryDataTablesModel.iDisplayLength, sortedColumns, jQueryDataTablesModel.sSearch, sortAscending, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<ReportDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var reportDTO = await _channelService.FindReportAsync(id, GetServiceHeader());

            return View(reportDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            var storedProcedures = GetStoredProcedures();
            ViewBag.StoredProcedures = new SelectList(storedProcedures);
            ViewBag.StoredProcedureCount = storedProcedures.Count;
            ViewBag.ReportSelectList = GetreportTemplateCategorySelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(ReportDTO reportDTO)
        {

            await ServeNavigationMenus();
            if (!reportDTO.HasErrors)
            {
                await _channelService.AddReportAsync(reportDTO, GetServiceHeader());
                TempData["SuccessMessage"] = "Successfully Created Report";
                return View("Index");
            }
            else
            {
                var errorMessages = reportDTO.ErrorMessages;
                ViewBag.ReportSelectList = GetreportTemplateCategorySelectList(string.Empty);
                var storedProcedures = GetStoredProcedures();
                ViewBag.StoredProcedures = new SelectList(storedProcedures);
                return View(reportDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var reportDTO = await _channelService.FindReportAsync(id, GetServiceHeader());
            ViewBag.ReportSelectList = GetreportTemplateCategorySelectList(string.Empty);
            var storedProcedures = GetStoredProcedures();
            ViewBag.StoredProcedures = new SelectList(storedProcedures);
            return View(reportDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, ReportDTO reportBindingModel)
        {

            if (!reportBindingModel.HasErrors)
            {
                await _channelService.UpdateReportAsync(reportBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.ReportSelectList = GetreportTemplateCategorySelectList(string.Empty);
                var storedProcedures = GetStoredProcedures();
                ViewBag.StoredProcedures = new SelectList(storedProcedures);
                return View(reportBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetReportsAsync()
        {
            //fetch all roles
            var result = await _channelService.FindReportsAsync(GetServiceHeader());

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
public async Task<JsonResult> GetReportById(Guid id)
{
    // This is a placeholder for any necessary operation or validation before redirecting
    var reportDTO = await _channelService.FindReportAsync(id, GetServiceHeader());
    return Json(reportDTO, JsonRequestBehavior.AllowGet);
}


        /// <summary>
        /// Stored Procedure Actions
        /// </summary>
        /// <param name="storedProcedureName"></param>
        /// <returns></returns>

        private List<string> GetStoredProcedures()
        {
            var storedProcedures = new List<string>();

            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand("SELECT SPECIFIC_NAME FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'PROCEDURE'", connection);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        storedProcedures.Add(reader["SPECIFIC_NAME"].ToString());
                    }
                }
            }

            return storedProcedures;
        }
    }
}
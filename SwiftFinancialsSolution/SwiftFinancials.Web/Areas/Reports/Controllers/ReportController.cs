using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AdministrationModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Reports.Controllers
{
    public class ReportController : MasterController
    {
        public ActionResult DownloadFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Path is required");
            }

            // Validate and sanitize the path if necessary
            // Example: make sure the path is within a specific directory
            var fullPath = Path.GetFullPath(path);
            //if (!System.IO.File.Exists(fullPath))
            //{
            //    return HttpNotFound("File not found");
            //}

            var fileName = Path.GetFileName(fullPath);
            return File(fullPath, "application/octet-stream", fileName);
        }

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
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, List<string> k)
        {
            var pageCollectionInfo = await _channelService.FindReportsAsync(GetServiceHeader());

            return Json(jQueryDataTablesModel);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var ReportDTO = await _channelService.FindReportAsync(id, GetServiceHeader());

            return View(ReportDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(ReportDTO ReportDTO)
        {
            ReportDTO.ValidateAll();

            if (!ReportDTO.ErrorMessages.Any())
            {
                var Report = await _channelService.AddReportAsync(ReportDTO.MapTo<ReportDTO>(), GetServiceHeader());

                //if (Report != null)
                //{
                //    //Update Stations

                //    var stations = new ObservableCollection<StationDTO>();

                //    foreach (var stationDTO in ReportDTO.Stations)
                //    {
                //        stationDTO.ReportId = Report.Id;

                //        stations.Add(stationDTO);
                //    }

                //    await _channelService.UpdateStationsByReportIdAsync(Report.Id, stations, GetServiceHeader());
                //}

                return RedirectToAction("Index");
            }
            else
            {
                IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

                TempData["Error"] = string.Join(",", allErrors);

                return View(ReportDTO);
            }
        }


        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var ReportDTO = await _channelService.FindReportAsync(id, GetServiceHeader());

            return View(ReportDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, ReportDTO ReportDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateReportAsync(ReportDTO.MapTo<ReportDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(ReportDTO);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetReportsAsync()
        {
            var ReportDTOs = await _channelService.FindReportsAsync(GetServiceHeader());

            return Json(ReportDTOs, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<JsonResult> GetStationsAsync()
        {
            var stationsDTOs = await _channelService.FindStationsAsync(GetServiceHeader());

            return Json(stationsDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
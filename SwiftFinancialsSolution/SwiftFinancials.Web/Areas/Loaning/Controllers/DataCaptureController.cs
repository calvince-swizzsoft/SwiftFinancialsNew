using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class DataCaptureController : MasterController
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

            var pageCollectionInfo = await _channelService.FindDataAttachmentPeriodsInPageAsync(jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<DataAttachmentPeriodDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        public async Task<ActionResult> Details(Guid? id)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            return View();
        }



        public async Task<ActionResult> Create(Guid? id, DataAttachmentPeriodDTO dataPeriodDTO)
        {
            await ServeNavigationMenus();

            ViewBag.MonthSelectList = GetMonthsAsync(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var postingPeriod = await _channelService.FindPostingPeriodAsync(parseId, GetServiceHeader());
            if (postingPeriod != null)
            {
                dataPeriodDTO.PostingPeriodId = postingPeriod.Id;
                dataPeriodDTO.PostingPeriodDescription = postingPeriod.Description;
            }

            return View(dataPeriodDTO);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(DataAttachmentPeriodDTO dataPeriodDTO)
        {

            dataPeriodDTO.ValidateAll();

            if (!dataPeriodDTO.HasErrors)
            {
                await _channelService.AddDataAttachmentPeriodAsync(dataPeriodDTO, GetServiceHeader());

                TempData["AlertMessage"] = "Successfully Created Data Period";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = dataPeriodDTO.ErrorMessages.ToString();

                TempData["BugdetBalance"] = errorMessages;

                TempData["approveError"] = "Loan Appraisal Unsuccessful";

                ViewBag.MonthSelectList = GetMonthsAsync(dataPeriodDTO.MonthDescription);

                return View();
            }
        }
    }
}

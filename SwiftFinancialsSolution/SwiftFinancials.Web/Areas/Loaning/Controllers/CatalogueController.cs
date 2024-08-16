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
    public class CatalogueController : MasterController
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


        public async Task<ActionResult> Create(Guid? id, DataAttachmentPeriodDTO dataAttachmentPeriodDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var dataPeriod = await _channelService.FindDataAttachmentPeriodAsync(parseId, GetServiceHeader());
            if (dataPeriod != null)
            {
                dataAttachmentPeriodDTO.Id = dataPeriod.Id;
                dataAttachmentPeriodDTO.DataAttachmentPeriodDescription = dataPeriod.MonthDescription;
                dataAttachmentPeriodDTO.Remarks = dataPeriod.Remarks;
                dataAttachmentPeriodDTO.PostingPeriodId = dataPeriod.PostingPeriodId;

                if (Session["jQueryDataTablesModel"] != null)
                {
                    JQueryDataTablesModel jQueryDataTablesModel = new JQueryDataTablesModel();

                    jQueryDataTablesModel = Session["jQueryDataTablesModel"] as JQueryDataTablesModel;

                    await GetDataAttachmentPeriodEntries(jQueryDataTablesModel);
                }

                //var dataAttachmentPeriodEntries = await _channelService.FindDataAttachmentEntriesByDataAttachmentPeriodIdAndFilterInPageAsync(dataAttachmentPeriodDTO.Id,
                //    string.Empty, 0, 200, true, GetServiceHeader());
                //if (dataAttachmentPeriodEntries != null)
                //    ViewBag.DataAttaPeriodsAndEntries = dataAttachmentPeriodEntries;

                Session["Id"] = dataAttachmentPeriodDTO.Id;
            }

            return View(dataAttachmentPeriodDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(DataAttachmentPeriodDTO dataAttachmentPeriodDTO)
        {
            dataAttachmentPeriodDTO.ValidateAll();

            if (!dataAttachmentPeriodDTO.HasErrors)
            {
                await _channelService.CloseDataAttachmentPeriodAsync(dataAttachmentPeriodDTO, GetServiceHeader());

                TempData["message"] = "Successfully Closed Data Period";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = dataAttachmentPeriodDTO.ErrorMessages.ToString();

                TempData["BugdetBalance"] = errorMessages;

                TempData["messageError"] = "Could not Close Data Period";

                await ServeNavigationMenus();

                return View();
            }
        }




        [HttpPost]
        public async Task<JsonResult> GetDataAttachmentPeriodEntries(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            Session["jQueryDataTablesModel"] = jQueryDataTablesModel;


            DataAttachmentPeriodDTO dataAttachmentPeriodDTO = new DataAttachmentPeriodDTO();

            dataAttachmentPeriodDTO.Id = (Guid)Session["Id"];

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindDataAttachmentEntriesByDataAttachmentPeriodIdAndFilterInPageAsync(dataAttachmentPeriodDTO.Id,
                    string.Empty, 0, 200, true, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<DataAttachmentEntryDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }
    }
}

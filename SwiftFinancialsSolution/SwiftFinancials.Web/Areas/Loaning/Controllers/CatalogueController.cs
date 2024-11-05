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
            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;
            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindDataAttachmentPeriodsInPageAsync(
                pageIndex,
                jQueryDataTablesModel.iDisplayLength,
                GetServiceHeader()
            );

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;
                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? pageCollectionInfo.PageCollection.Count
                    : totalRecordCount;

                var orderedPageCollection = pageCollectionInfo.PageCollection
                    .OrderByDescending(item => item.CreatedDate)
                    .ToList();

                return this.DataTablesJson(
                    items: orderedPageCollection,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
            else
            {
                return this.DataTablesJson(
                    items: new List<DataAttachmentPeriodDTO> { },
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
        }


        public async Task<ActionResult> Create(Guid? id, DataAttachmentEntryDTO dataAttachmentEntryPeriodDTO)
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
                dataAttachmentEntryPeriodDTO.DataAttachmentPeriodId = dataPeriod.Id;
                dataAttachmentEntryPeriodDTO.DataAttachmentPeriodDescription = dataPeriod.MonthDescription;
                dataAttachmentEntryPeriodDTO.DataPeriodRemarks = dataPeriod.Remarks;

                var entries = await _channelService.FindDataAttachmentEntriesByDataAttachmentPeriodIdAndFilterInPageAsync(parseId, null, 0, int.MaxValue, true,
                 GetServiceHeader());

                ViewBag.Entries = entries.PageCollection;
            }

            return View(dataAttachmentEntryPeriodDTO);
        }
    }
}

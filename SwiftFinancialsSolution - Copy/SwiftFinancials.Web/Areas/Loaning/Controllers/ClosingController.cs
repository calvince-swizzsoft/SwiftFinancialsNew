using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Windows.Forms;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class ClosingController : MasterController
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
                if (dataPeriod.Status == (int)DataAttachmentPeriodStatus.Closed)
                {
                    await ServeNavigationMenus();

                    MessageBox.Show(Form.ActiveForm, "The selected Data Period is already closed and therefore cannot be ammended or reopened", "Data Period Closing", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    return View("Index");
                }

                dataAttachmentPeriodDTO.Id = dataPeriod.Id;
                dataAttachmentPeriodDTO.DataAttachmentPeriodDescription = dataPeriod.MonthDescription;
                dataAttachmentPeriodDTO.PostingPeriodId = dataPeriod.PostingPeriodId;

                var entries = await _channelService.FindDataAttachmentEntriesByDataAttachmentPeriodIdAndFilterInPageAsync(parseId, null, 0, int.MaxValue, true,
                GetServiceHeader());

                ViewBag.Entries = entries.PageCollection;
            }

            return View(dataAttachmentPeriodDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(DataAttachmentPeriodDTO dataAttachmentPeriodDTO)
        {
            var findFullDetails = await _channelService.FindDataAttachmentPeriodAsync(dataAttachmentPeriodDTO.Id, GetServiceHeader());

            dataAttachmentPeriodDTO.PostingPeriodId = findFullDetails.PostingPeriodId;
            dataAttachmentPeriodDTO.AuthorizationRemarks = dataAttachmentPeriodDTO.Remarks;

            dataAttachmentPeriodDTO.ValidateAll();

            if (!dataAttachmentPeriodDTO.HasErrors)
            {
                string message = string.Format(
                                 "Proceed to Close Data Period for the period {0}?",findFullDetails.PostingPeriodDescription
                             );

                // Show the message box with Yes/No options
                DialogResult result = MessageBox.Show(
                    message,
                    "Data Period Closing",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification
                );

                if (result == DialogResult.Yes)
                {

                    await _channelService.CloseDataAttachmentPeriodAsync(dataAttachmentPeriodDTO, GetServiceHeader());

                    MessageBox.Show(Form.ActiveForm, "Operation Completed Successfully", "Data Period Closing", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                    return RedirectToAction("Index");
                }
                else
                {
                    await ServeNavigationMenus();
                    MessageBox.Show(Form.ActiveForm, "Operation Cancelled", "Data Period Closing", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                    var entries = await _channelService.FindDataAttachmentEntriesByDataAttachmentPeriodIdAndFilterInPageAsync(findFullDetails.Id, null, 0, int.MaxValue, true,
                GetServiceHeader());

                    ViewBag.Entries = entries.PageCollection;

                    return View(dataAttachmentPeriodDTO);
                }
            }
            else
            {
                var errorMessages = dataAttachmentPeriodDTO.ErrorMessages.ToString();

                TempData["BugdetBalance"] = errorMessages;

                MessageBox.Show(Form.ActiveForm, "Operation Failed", "Data Period Closing", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);


                await ServeNavigationMenus();

                return View();
            }
        }
    }
}

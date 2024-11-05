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



        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var dataCapture = await _channelService.FindDataAttachmentPeriodAsync(id, GetServiceHeader());

            return View(dataCapture);
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

                var isValidPostingPeriod = postingPeriod.IsActive;
                if (!isValidPostingPeriod)
                {
                    MessageBox.Show(Form.ActiveForm, "The selected Posting Period is Inactive. Kindly choose a valid Posting Period.", "Data Periods", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    return View();
                }
            }

            return View(dataPeriodDTO);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(DataAttachmentPeriodDTO dataPeriodDTO)
        {
            await ServeNavigationMenus();

            dataPeriodDTO.ValidateAll();

            if (!dataPeriodDTO.HasErrors)
            {
                string message = string.Format(
                                   "Confirm opening of Data Period {0}.\nProceed?",
                                   dataPeriodDTO.PostingPeriodDescription
                               );

                // Show the message box with Yes/No options
                DialogResult result = MessageBox.Show(
                    message,
                    "Data Period Opening Confirmation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification
                );

                if (result == DialogResult.Yes)
                {
                    var cut = await _channelService.AddDataAttachmentPeriodAsync(dataPeriodDTO, GetServiceHeader());

                    if (cut.ErrorMessageResult != null)
                    {
                        await ServeNavigationMenus();
                        ViewBag.MonthSelectList = GetMonthsAsync(string.Empty);

                        MessageBox.Show(Form.ActiveForm, $"Operation failed: \"{cut.ErrorMessageResult.ToUpper()}\"", "Data Period Failed Message", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                        return View(dataPeriodDTO);
                    }

                    MessageBox.Show(Form.ActiveForm, $"Successfully Created Data Period for month \"{dataPeriodDTO.MonthDescription.ToUpper()}\".", "Data Period Success Message", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                    return RedirectToAction("Index");
                }
                else
                {
                    await ServeNavigationMenus();

                    ViewBag.MonthSelectList = GetMonthsAsync(string.Empty);

                    MessageBox.Show(Form.ActiveForm, "Operation Cancelled.", "Data period Cancellation", MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    return View(dataPeriodDTO);
                }
            }
            else
            {
                await ServeNavigationMenus();
                var errorMessages = dataPeriodDTO.ErrorMessages.ToString();

                TempData["BugdetBalance"] = errorMessages;

                MessageBox.Show(Form.ActiveForm, "Operation failed.", "Data Periods", MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                ViewBag.MonthSelectList = GetMonthsAsync(dataPeriodDTO.MonthDescription);

                await ServeNavigationMenus();

                return View();
            }
        }




        public async Task<ActionResult> Edit(Guid id)
        {

            Session["getId"] = id;
            await ServeNavigationMenus();
            ViewBag.MonthSelectList = GetMonthsAsync(string.Empty);

            var DataAttachmentPeriod = new DataAttachmentPeriodDTO();

            var dPeriod = await _channelService.FindDataAttachmentPeriodAsync(id, GetServiceHeader());
            if (dPeriod != null)
            {
                DataAttachmentPeriod.PostingPeriodId = dPeriod.PostingPeriodId;
                DataAttachmentPeriod.PostingPeriodDescription = dPeriod.PostingPeriodDescription;
                DataAttachmentPeriod.Month = dPeriod.Month;
                DataAttachmentPeriod.Remarks = dPeriod.Remarks;

                Session["values"] = DataAttachmentPeriod;
            }

            return View(DataAttachmentPeriod);
        }



        public async Task<ActionResult> EditLookup(Guid? id)
        {
            await ServeNavigationMenus();

            ViewBag.MonthSelectList = GetMonthsAsync(string.Empty);

            var dataPeriodDTO = new DataAttachmentPeriodDTO();

            if (Session["values"] != null)
            {
                dataPeriodDTO = Session["values"] as DataAttachmentPeriodDTO;
            }

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

                var isValidPostingPeriod = postingPeriod.IsActive;
                if (!isValidPostingPeriod)
                {
                    MessageBox.Show(Form.ActiveForm, "The selected Posting Period is Inactive. Kindly choose a valid Posting Period.", "Data Periods", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    return View();
                }

                Session["newPostingPeriodId"] = dataPeriodDTO.PostingPeriodId;
            }

            return View("Edit", dataPeriodDTO);
        }




        [HttpPost]
        public async Task<ActionResult> Edit(DataAttachmentPeriodDTO dataPeriodDTO)
        {
            await ServeNavigationMenus();

            if (Session["getId"] != null)
                dataPeriodDTO.Id = (Guid)Session["getId"];

            if (Session["newPostingPeriodId"] != null)
                dataPeriodDTO.PostingPeriodId = (Guid)Session["newPostingPeriodId"];

                dataPeriodDTO.ValidateAll();

            if (!dataPeriodDTO.HasErrors)
            {
                string message = string.Format(
                                   "Confirm edit Data Period. Proceed?"
                               );

                // Show the message box with Yes/No options
                DialogResult result = MessageBox.Show(
                    message,
                    "Data Period",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification
                );

                if (result == DialogResult.Yes)
                {
                    var cut = await _channelService.UpdateDataAttachmentPeriodAsync(dataPeriodDTO, GetServiceHeader());

                    if (cut == false)
                    {
                        await ServeNavigationMenus();
                        ViewBag.MonthSelectList = GetMonthsAsync(string.Empty);

                        MessageBox.Show(Form.ActiveForm, "Sorry, but there is already a closed/suspended data period for the selected month!", "Data Period Failed Message", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                        return View(dataPeriodDTO);
                    }

                    MessageBox.Show(Form.ActiveForm, $"Successfully Edited Data Period.", "Data Period Success Message", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                    Session["getId"] = null;
                    Session["values"] = null;
                    Session["newPostingPeriodId"] = null;

                    return RedirectToAction("Index");
                }
                else
                {
                    await ServeNavigationMenus();

                    ViewBag.MonthSelectList = GetMonthsAsync(string.Empty);

                    MessageBox.Show(Form.ActiveForm, "Operation Cancelled.", "Data period Cancellation", MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    return View(dataPeriodDTO);
                }
            }
            else
            {
                await ServeNavigationMenus();
                var errorMessages = dataPeriodDTO.ErrorMessages.ToString();

                TempData["BugdetBalance"] = errorMessages;

                MessageBox.Show(Form.ActiveForm, "Operation failed.", "Data Periods", MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                ViewBag.MonthSelectList = GetMonthsAsync(dataPeriodDTO.MonthDescription);

                await ServeNavigationMenus();

                return View();
            }
        }
    }
}

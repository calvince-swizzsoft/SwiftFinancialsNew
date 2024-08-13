using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Domain.MainBoundedContext.AccountsModule.Aggregates.JournalVoucherAgg;
using Domain.MainBoundedContext.ValueObjects;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class BatchOrigination_VoucherController : MasterController
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

            var pageCollectionInfo = await _channelService.FindJournalVouchersByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(journalVoucher => journalVoucher.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<JournalVoucherDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var journalVoucherDTO = await _channelService.FindJournalVoucherAsync(id, GetServiceHeader());
            var voucherBatches = await _channelService.FindJournalVoucherEntriesByJournalVoucherIdAsync(id, GetServiceHeader());

            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(string.Empty);

            ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(string.Empty);

            ViewBag.JournalVoucherEntryDTOs = voucherBatches;
            return View(journalVoucherDTO);
        }
        public async Task<ActionResult> Create()
        {

            await ServeNavigationMenus();
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(string.Empty);

            ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(string.Empty);

            /*ViewBag.JournalVoucherEntryDTOs = null;*/


            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Add(JournalVoucherDTO journalVoucherDTO)
        {
            await ServeNavigationMenus();

            JournalVoucherEntryDTO jvE = new JournalVoucherEntryDTO();

            jvE.rowId = 0;

            JournalVoucherEntryDTOs = TempData["JournalVoucherEntryDTO"] as ObservableCollection<JournalVoucherEntryDTO>;

            if (JournalVoucherEntryDTOs == null)
                JournalVoucherEntryDTOs = new ObservableCollection<JournalVoucherEntryDTO>();

            var glAccount = journalVoucherDTO.JournalVoucherEntry;

            foreach (var journalVoucherEntryDTO in journalVoucherDTO.JournalVoucherEntries)
            {

                journalVoucherEntryDTO.BranchId = journalVoucherDTO.BranchId;
                journalVoucherEntryDTO.BranchDescription = journalVoucherDTO.BranchDescription;
                journalVoucherEntryDTO.EntryType = journalVoucherEntryDTO.EntryType;
                journalVoucherEntryDTO.ChartOfAccountId = journalVoucherDTO.ChartOfAccountId;//Temporary
                journalVoucherEntryDTO.ChartOfAccountAccountName = glAccount.ChartOfAccountAccountName;
                journalVoucherEntryDTO.PrimaryDescription = journalVoucherDTO.PrimaryDescription;
                journalVoucherEntryDTO.SecondaryDescription = journalVoucherDTO.SecondaryDescription;
                journalVoucherEntryDTO.Reference = journalVoucherEntryDTO.Reference;
                journalVoucherEntryDTO.TotalValue = journalVoucherEntryDTO.TotalValue;
                journalVoucherEntryDTO.Remarks = journalVoucherDTO.Remarks;
                journalVoucherEntryDTO.rowId = journalVoucherDTO.rowId;
                JournalVoucherEntryDTOs.Add(journalVoucherEntryDTO);

                jvE.rowId++;

                Session["journalVoucherEntries"] = JournalVoucherEntryDTOs;
                Session["ChartOfAccountId"] = journalVoucherEntryDTO.ChartOfAccountId;
            };

            TempData["JournalVoucherEntryDTO"] = JournalVoucherEntryDTOs;
            Session["JournalVoucherEntryDTO"] = JournalVoucherEntryDTOs;
            TempData["JournalVoucherDTO"] = journalVoucherDTO;

            ViewBag.JournalVoucherEntryDTOs = JournalVoucherEntryDTOs;

            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(journalVoucherDTO.Type.ToString());
            ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(journalVoucherDTO.EntryType.ToString());

            

            return View("Create");
        }




        [HttpPost]
        public async Task<ActionResult> removeChargeSplit(JournalVoucherDTO journalVoucherDTO)
        {
            journalVoucherDTO = TempData["JournalVoucherDTO"] as JournalVoucherDTO;

            journalVoucherDTO.JournalVoucherEntries = Session["journalVoucherEntries"] as ObservableCollection<JournalVoucherEntryDTO>;

            JournalVoucherEntryDTOs = TempData["JournalVoucherEntryDTO"] as ObservableCollection<JournalVoucherEntryDTO>;

            if (JournalVoucherEntryDTOs == null)
                JournalVoucherEntryDTOs = new ObservableCollection<JournalVoucherEntryDTO>();

            //var id = journalVoucherDTO.JournalVoucherEntries.Select(m => m.ChartOfAccountId);

            foreach (var JournalVoucherEntry in journalVoucherDTO.JournalVoucherEntries)
            {
                JournalVoucherEntry.ChartOfAccountId = (Guid)Session["ChartOfAccountId"];
                JournalVoucherEntry.ChartOfAccountAccountName = JournalVoucherEntry.ChartOfAccountAccountName;
                JournalVoucherEntry.Remarks = JournalVoucherEntry.Remarks;
                JournalVoucherEntry.Amount = JournalVoucherEntry.Amount;

                JournalVoucherEntryDTOs.Remove(JournalVoucherEntry);
            };

            TempData["JournalVoucherEntryDTOs"] = JournalVoucherEntryDTOs;

            TempData["JournalVoucherDTO"] = journalVoucherDTO;



            ViewBag.JournalVoucherEntryDTOs = JournalVoucherEntryDTOs;

            return View("Create", journalVoucherDTO);
        }






        [HttpPost]
        public async Task<ActionResult> Create(JournalVoucherDTO journalVoucherDTO)
        {
            journalVoucherDTO = TempData["JournalVoucherDTO"] as JournalVoucherDTO;
            JournalVoucherEntryDTOs = TempData["JournalVoucherEntryDTOs"] as ObservableCollection<JournalVoucherEntryDTO>;

            if (TempData["JournalVoucherEntryDTO"] != null)
            {
                journalVoucherDTO.JournalVoucherEntries = TempData["JournalVoucherEntryDTO"] as ObservableCollection<JournalVoucherEntryDTO>;
            }

            journalVoucherDTO.ValidateAll();

            if (!journalVoucherDTO.HasErrors)
            {
                var journalVoucherEntries = new ObservableCollection<JournalVoucherEntryDTO>();

                foreach (var journalVoucherEntryDTO in journalVoucherDTO.JournalVoucherEntries)
                {
                    journalVoucherEntryDTO.EntryType = journalVoucherEntryDTO.EntryType;
                    journalVoucherEntryDTO.ChartOfAccountId = journalVoucherEntryDTO.ChartOfAccountId;
                    journalVoucherEntryDTO.ChartOfAccountAccountName = journalVoucherEntryDTO.ChartOfAccountAccountName;
                    journalVoucherEntryDTO.PrimaryDescription = journalVoucherDTO.PrimaryDescription;
                    journalVoucherEntryDTO.SecondaryDescription = journalVoucherDTO.SecondaryDescription;
                    journalVoucherEntryDTO.Reference = journalVoucherEntryDTO.Reference;
                    journalVoucherEntryDTO.Amount = journalVoucherEntryDTO.Amount;
                    journalVoucherEntryDTO.Remarks = journalVoucherDTO.Remarks;

                    journalVoucherEntries.Add(journalVoucherEntryDTO);
                };

                var journalVoucher = await _channelService.AddJournalVoucherAsync(journalVoucherDTO, GetServiceHeader());

                if (journalVoucher.ErrorMessageResult != null)
                {
                    await ServeNavigationMenus();

                    TempData["ErrorMsg"] = journalVoucher.ErrorMessageResult;

                    return View();
                }

                TempData["SuccessMessage"] = "Successfully Created voucher Batch";
                TempData["JournalVoucherDTO"] = "";



                if (journalVoucherEntries.Any())
                    await _channelService.UpdateJournalVoucherEntryCollectionAsync(journalVoucher.Id, journalVoucherEntries, GetServiceHeader());

                TempData["JournalVoucherEntryDTO"] = "";
                TempData["Success"] = "journal and voucher have been created souccessifully";


                return RedirectToAction("Index");
            }


            return RedirectToAction("Create");
        }


        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(string.Empty);

            ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(string.Empty);
            var overDeductionBatchDTO = await _channelService.FindOverDeductionBatchAsync(id, GetServiceHeader());

            return View(overDeductionBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, JournalVoucherDTO journalVoucherDTO)
        {
            journalVoucherDTO.ValidateAll();

            if (!journalVoucherDTO.HasErrors)
            {
                await _channelService.UpdateJournalVoucherAsync(journalVoucherDTO, GetServiceHeader());

                TempData["edit"] = "Successfully edited Journal Voucher";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = journalVoucherDTO.ErrorMessages;
                ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(string.Empty);

                ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(string.Empty);
                return View(journalVoucherDTO);
            }
        }

        /*[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, JournalVoucherDTO journalVoucherDTO)
        {
            journalVoucherDTO.ValidateAll();

            if (!journalVoucherDTO.HasErrors)
            {
                await _channelService.UpdateJournalVoucherAsync(journalVoucherDTO, GetServiceHeader());
                ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(journalVoucherDTO.Type.ToString());
                ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(journalVoucherDTO.EntryType.ToString());
                ViewBag.JournalVoucherAuthOptionSelectList = GetJournalVoucherAuthOptionSelectList(journalVoucherDTO.AuthOption.ToString());
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = journalVoucherDTO.ErrorMessages;
                ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(journalVoucherDTO.Type.ToString());
                ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(journalVoucherDTO.EntryType.ToString());
                ViewBag.JournalVoucherAuthOptionSelectList = GetJournalVoucherAuthOptionSelectList(journalVoucherDTO.AuthOption.ToString());
                return View(journalVoucherDTO);
            }
        }*/

        public async Task<ActionResult> Verify(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(string.Empty);
            ViewBag.JournalVoucherAuthOptionSelectList = GetJournalVoucherAuthOptionSelectList(string.Empty);

            var journalVoucherDTO = await _channelService.FindJournalVoucherAsync(id, GetServiceHeader());

            var voucherBatches = await _channelService.FindJournalVoucherEntriesByJournalVoucherIdAsync(id, GetServiceHeader());

            ViewBag.JournalVoucherEntryDTOs = voucherBatches;

            return View(journalVoucherDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(Guid id, JournalVoucherDTO journalVoucherDTO)
        {
            journalVoucherDTO.ValidateAll();

            if (!journalVoucherDTO.HasErrors)
            {
                await _channelService.AuditJournalVoucherAsync(journalVoucherDTO, 1, GetServiceHeader());
                ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(journalVoucherDTO.Type.ToString());

                ViewBag.JournalVoucherAuthOptionSelectList = GetJournalVoucherAuthOptionSelectList(journalVoucherDTO.AuthOption.ToString());
                TempData["verifySuccess"] = "Journal batches have been verified succesifully";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = journalVoucherDTO.ErrorMessages;

                ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(journalVoucherDTO.Type.ToString());

                ViewBag.JournalVoucherAuthOptionSelectList = GetJournalVoucherAuthOptionSelectList(journalVoucherDTO.AuthOption.ToString());
                return View(journalVoucherDTO);
            }
        }

        public async Task<ActionResult> Authorize(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(string.Empty);
            ViewBag.JournalVoucherAuthOptionSelectList = GetJournalVoucherAuthOptionSelectList(string.Empty);

            var journalVoucherDTO = await _channelService.FindJournalVoucherAsync(id, GetServiceHeader());

            var voucherBatches = await _channelService.FindJournalVoucherEntriesByJournalVoucherIdAsync(id, GetServiceHeader());

            ViewBag.JournalVoucherEntryDTOs = voucherBatches;

            return View(journalVoucherDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Authorize(Guid id, JournalVoucherDTO journalVoucherDTO)
        {

            var journalVoucherAuthOption = journalVoucherDTO.AuthOption;


            journalVoucherDTO.ValidateAll();

            if (!journalVoucherDTO.HasErrors)
            {
                await _channelService.AuthorizeJournalVoucherAsync(journalVoucherDTO, 1, journalVoucherAuthOption, GetServiceHeader());
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = journalVoucherDTO.ErrorMessages;

                ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(journalVoucherDTO.Type.ToString());

                ViewBag.JournalVoucherAuthOptionSelectList = GetJournalVoucherAuthOptionSelectList(journalVoucherDTO.AuthOption.ToString());

                return View(journalVoucherDTO);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetJournalVouchersAsync()
        {
            var journalVoucherDTOs = await _channelService.FindJournalVouchersAsync(GetServiceHeader());

            return Json(journalVoucherDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}

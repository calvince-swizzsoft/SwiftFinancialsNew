using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
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
    public class JournalVoucherController : MasterController
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

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<JournalVoucherDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var journalVoucherDTO = await _channelService.FindJournalVoucherAsync(id, GetServiceHeader());

            return View(journalVoucherDTO);
        }
        public async Task<ActionResult> Create(Guid? id)
        {
            
            await ServeNavigationMenus();
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(string.Empty);
            ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(string.Empty);

            ViewBag.JournalVoucherEntryDTOs = null;
            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var chartOfAccount = await _channelService.FindChartOfAccountAsync(parseId, GetServiceHeader());

            JournalVoucherDTO journalVoucherDTO = new JournalVoucherDTO();

            if (chartOfAccount != null)
            {
                journalVoucherDTO.ChartOfAccountId = chartOfAccount.Id;
                journalVoucherDTO.ChartOfAccountAccountName = chartOfAccount.AccountName;
            }



            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var branch = await _channelService.FindBranchAsync(parseId, GetServiceHeader());

            if (branch != null)
            {
                journalVoucherDTO.BranchId = branch.Id;
                journalVoucherDTO.BranchDescription = branch.Description;
            }

            return View(journalVoucherDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Add(JournalVoucherDTO journalVoucherDTO)
        {
            await ServeNavigationMenus();

            JournalVoucherEntryDTOs = TempData["JournalVoucherEntryDTO"] as ObservableCollection<JournalVoucherEntryDTO>;

            if (JournalVoucherEntryDTOs == null)
                JournalVoucherEntryDTOs = new ObservableCollection<JournalVoucherEntryDTO>();

            foreach (var journalVoucherEntryDTO in journalVoucherDTO.JournalVoucherEntries)
            {
                journalVoucherEntryDTO.EntryType = journalVoucherEntryDTO.EntryType;
                journalVoucherEntryDTO.ChartOfAccountId = journalVoucherDTO.Id;//Temporary
                journalVoucherEntryDTO.BranchId = journalVoucherDTO.Id;//Temporary
                journalVoucherEntryDTO.PrimaryDescription = journalVoucherEntryDTO.PrimaryDescription;
                journalVoucherEntryDTO.SecondaryDescription = journalVoucherEntryDTO.SecondaryDescription;
                journalVoucherEntryDTO.Reference = journalVoucherEntryDTO.Reference;
                journalVoucherEntryDTO.TotalValue = journalVoucherEntryDTO.TotalValue;
                journalVoucherEntryDTO.Remarks = journalVoucherEntryDTO.Remarks;
                JournalVoucherEntryDTOs.Add(journalVoucherEntryDTO);
            };

            TempData["JournalVoucherEntryDTO"] = JournalVoucherEntryDTOs;

            TempData["JournalVoucherDTO"] = journalVoucherDTO;

            ViewBag.JournalVoucherEntryDTOs = JournalVoucherEntryDTOs;

            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(journalVoucherDTO.Type.ToString());
            ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(journalVoucherDTO.EntryType.ToString());

            return View("Create", journalVoucherDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(JournalVoucherDTO journalVoucherDTO)
        {

            Guid journalVoucherEntryChartOfAccountId = journalVoucherDTO.Id;

            var valuedate = Request["valuedate"];
            journalVoucherDTO.ValueDate = DateTime.ParseExact((Request["valuedate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);
            journalVoucherDTO.ValidateAll();

            if (!journalVoucherDTO.HasErrors)
            {

                var journalVoucher = await _channelService.AddJournalVoucherAsync(journalVoucherDTO, GetServiceHeader());

                if (journalVoucher != null)
                {
                    var journalVoucherEntries = new ObservableCollection<JournalVoucherEntryDTO>();

                    foreach (var journalVoucherEntryDTO in journalVoucherDTO.JournalVoucherEntries)
                    {
                        journalVoucherEntryDTO.JournalVoucherId = journalVoucher.Id;
                        journalVoucherEntryDTO.EntryType = journalVoucherEntryDTO.EntryType;
                        journalVoucherEntryDTO.ChartOfAccountId = journalVoucherEntryChartOfAccountId;
                        journalVoucherEntryDTO.PrimaryDescription = journalVoucherEntryDTO.PrimaryDescription;
                        journalVoucherEntryDTO.SecondaryDescription = journalVoucherEntryDTO.SecondaryDescription;
                        journalVoucherEntryDTO.Reference = journalVoucherEntryDTO.Reference;
                        journalVoucherEntryDTO.TotalValue = journalVoucherEntryDTO.TotalValue;
                        journalVoucherEntryDTO.Remarks = journalVoucherEntryDTO.Remarks;
                        journalVoucherEntries.Add(journalVoucherEntryDTO);
                    };

                    if (journalVoucherEntries.Any())

                      await _channelService.UpdateJournalVoucherEntriesByJournalVoucherIdAsync(journalVoucher.Id, journalVoucherEntries, GetServiceHeader());
                }
                        ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(journalVoucherDTO.Type.ToString());
                        ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(journalVoucherDTO.EntryType.ToString());

                    ViewBag.JournalVoucherEntries = await _channelService.FindJournalVoucherEntriesByJournalVoucherIdAsync(journalVoucher.Id, GetServiceHeader());

                    return RedirectToAction("Index");
                }
                else
                {
                    var errorMessages = journalVoucherDTO.ErrorMessages;

                    ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(journalVoucherDTO.Type.ToString());
                    ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(journalVoucherDTO.EntryType.ToString());

                    return View(journalVoucherDTO);
                }
            }
        

            public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(string.Empty);
            ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(string.Empty);

            var journalVoucherDTO = await _channelService.FindJournalVoucherAsync(id, GetServiceHeader());

            return View(journalVoucherDTO);
        }

        [HttpPost]
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
        }

        public async Task<ActionResult> Verify(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(string.Empty);
            ViewBag.JournalVoucherAuthOptionSelectList = GetJournalVoucherAuthOptionSelectList(string.Empty);

            var journalVoucherDTO = await _channelService.FindJournalVoucherAsync(id, GetServiceHeader());

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
                await _channelService.AuthorizeJournalVoucherAsync(journalVoucherDTO, journalVoucherAuthOption,1, GetServiceHeader());
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

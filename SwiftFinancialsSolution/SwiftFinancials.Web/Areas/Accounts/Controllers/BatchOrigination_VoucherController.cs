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
            ViewBag.BatchStatus = GetBatchStatusTypeSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel,int status, DateTime startDate, DateTime endDate)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;

            var pageCollectionInfo = await _channelService.FindJournalVouchersByStatusAndFilterInPageAsync(status, startDate, endDate, jQueryDataTablesModel.sSearch, 0, int.MaxValue, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(k => k.CreatedDate)
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
                items: new List<JournalVoucherDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
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
        
        [HttpPost]
        public async Task<ActionResult> Add(JournalVoucherDTO journalVoucherDTO)
        {
            await ServeNavigationMenus();

            var JournalVoucherEntryDTOs = Session["JournalVoucherEntryDTOs"] as ObservableCollection<JournalVoucherEntryDTO>;
            if (JournalVoucherEntryDTOs == null)
                JournalVoucherEntryDTOs = new ObservableCollection<JournalVoucherEntryDTO>();

            decimal sumAmount = JournalVoucherEntryDTOs.Sum(cs => cs.Amount);
            var addedEntries = new List<JournalVoucherEntryDTO>();

            foreach (var journalVoucherEntryDTO in journalVoucherDTO.JournalVoucherEntries)
            {
                if (journalVoucherEntryDTO.Id == Guid.Empty)
                {
                    journalVoucherEntryDTO.Id = Guid.NewGuid();
                }


                if (journalVoucherEntryDTO.ChartOfAccountName == null ||
                    journalVoucherEntryDTO.Reference == null ||
                    journalVoucherEntryDTO.Amount == 0)
                {
                    return Json(new { success = false, message = "Could not add Journal Voucher Entry. Ensure all required fields are entered." });
                }

                journalVoucherEntryDTO.BranchId = journalVoucherDTO.BranchId;
                journalVoucherEntryDTO.BranchDescription = journalVoucherDTO.BranchDescription;
                journalVoucherEntryDTO.PrimaryDescription = journalVoucherDTO.PrimaryDescription;
                journalVoucherEntryDTO.SecondaryDescription = journalVoucherDTO.SecondaryDescription;
                JournalVoucherEntryDTOs.Add(journalVoucherEntryDTO);
                addedEntries.Add(journalVoucherEntryDTO);

                sumAmount += journalVoucherEntryDTO.Amount;
                if (sumAmount > journalVoucherDTO.TotalValue)
                {
                    JournalVoucherEntryDTOs.Remove(journalVoucherEntryDTO);
                    return Json(new { success = false, message = "Failed to add Journal Voucher Entry. Total Amount exceeded Total Value." });
                }
            }

            Session["JournalVoucherEntryDTOs"] = JournalVoucherEntryDTOs;
            TempData["JournalVoucherEntryDTOs"] = JournalVoucherEntryDTOs;
            Session["journalVoucherEntries"] = JournalVoucherEntryDTOs;

            Session["journalVoucherDTO"] = journalVoucherDTO;
            TempData["journalVoucherDTO"] = journalVoucherDTO;

            return Json(new { success = true, entries = JournalVoucherEntryDTOs });
        }

        [HttpPost]
        public async Task<JsonResult> Remove(Guid id)
        {
            await ServeNavigationMenus();

            var JournalVoucherEntryDTOs = Session["JournalVoucherEntryDTOs"] as ObservableCollection<JournalVoucherEntryDTO>;
            decimal sumAmount = JournalVoucherEntryDTOs.Sum(cs => cs.Amount);
            if (JournalVoucherEntryDTOs != null)
            {
                var entryToRemove = JournalVoucherEntryDTOs.FirstOrDefault(e => e.Id == id);
                if (entryToRemove != null)
                {
                    JournalVoucherEntryDTOs.Remove(entryToRemove);

                    sumAmount -= entryToRemove.Amount;

                    Session["JournalVoucherEntryDTOs"] = JournalVoucherEntryDTOs;
                }
            }



            return Json(new { success = true, data = JournalVoucherEntryDTOs });
        }

        [HttpPost]
        public async Task<ActionResult> removeChargeSplit(JournalVoucherDTO journalVoucherDTO)
        {
            await ServeNavigationMenus();
            ViewBag.JournalVoucherEntryDTOs = JournalVoucherEntryDTOs;
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

            TempData["ChargeSplitDTOs"] = ChargeSplitDTOs;

            ViewBag.JournalVoucherEntryDTOs = JournalVoucherEntryDTOs;



            return View("Create", journalVoucherDTO);
        }

        [HttpPost]
        public async Task<ActionResult> removeJournalEntryEdit(Guid? id, JournalVoucherDTO  journalVoucherDTO)
        {
            await ServeNavigationMenus();

            if (id == null || id == Guid.Empty)
            {
                return View("Edit", journalVoucherDTO);
            }

            JournalVoucherEntryDTOs = TempData["journalEntriesBeforeEdit"] as ObservableCollection<JournalVoucherEntryDTO>;

            if (JournalVoucherEntryDTOs == null)
            {
                JournalVoucherEntryDTOs = new ObservableCollection<JournalVoucherEntryDTO>();
            }

            var journalEntryToRemove = JournalVoucherEntryDTOs.FirstOrDefault(cs => cs.Id == id);
            if (journalEntryToRemove != null)
            {
                JournalVoucherEntryDTOs.Remove(journalEntryToRemove);
            }

            TempData["JournalVoucherEntryDTOs"] = JournalVoucherEntryDTOs;
            ViewBag.journalEntries = ChargeSplitDTOs;

            return View("Edit", JournalVoucherEntryDTOs);
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
        public async Task<ActionResult> Create(JournalVoucherDTO journalVoucherDTO)
        {
            journalVoucherDTO = Session["journalVoucherDTO"] as JournalVoucherDTO;
            JournalVoucherEntryDTOs = Session["JournalVoucherEntryDTOs"] as ObservableCollection<JournalVoucherEntryDTO>;
            if (journalVoucherDTO == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Batch cannot be null."
                });
            }

            decimal sumAmount = JournalVoucherEntryDTOs.Sum(e => e.Amount);
            decimal totalValue = journalVoucherDTO?.TotalValue ?? 0;

            if (JournalVoucherEntryDTOs != null)
            {
                journalVoucherDTO.JournalVoucherEntries = JournalVoucherEntryDTOs;

                if (sumAmount != totalValue)
                {
                    var balance = totalValue - sumAmount;
                    return Json(new { success = false, message = $"The total value ({totalValue}) should be equal to the sum of the entries ({sumAmount}). Balance: {balance}" });
                }

            }

            

            journalVoucherDTO.ValidateAll();

            if (!journalVoucherDTO.HasErrors)
            {
                var journalVoucherEntries = new ObservableCollection<JournalVoucherEntryDTO>();

                foreach (var journalVoucherEntryDTO in journalVoucherDTO.JournalVoucherEntries)
                {
                    journalVoucherEntries.Add(journalVoucherEntryDTO);
                }

                var journalVoucher = await _channelService.AddJournalVoucherAsync(journalVoucherDTO, GetServiceHeader());

                if (journalVoucher.ErrorMessageResult != null)
                {
                    return Json(new
                    {
                        success = false,
                        message = journalVoucher.ErrorMessageResult
                    });
                }

                if (journalVoucherEntries.Any())
                {
                    await _channelService.UpdateJournalVoucherEntryCollectionAsync(journalVoucher.Id, journalVoucherEntries, GetServiceHeader());
                }

               
                Session["JournalVoucherEntryDTOs"] = null;
                Session["journalVoucherDTO"] = null;

                return Json(new
                {
                    success = true,
                    message = "Journal and voucher created successfully."
                });
            }
            else
            {
                // Return validation errors
                return Json(new
                {
                    success = false,
                    message = "Validation failed. Please check the data."
                });
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
            Session["id"] = id;
            var journalVoacherDTO = await _channelService.FindJournalVoucherAsync(id, GetServiceHeader());

            var journalentries = await _channelService.FindJournalVoucherEntriesByJournalVoucherIdAsync(id, GetServiceHeader());


            ViewBag.journalEntries = journalentries;

            TempData["journalEntriesBeforeEdit"] = ViewBag.journalEntries;

            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(string.Empty);

            ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(string.Empty);
            

            return View(journalVoacherDTO);
        }

        [HttpPost]

        public async Task<ActionResult> AddEdit(Guid? id,JournalVoucherDTO journalVoucherDTO)
        {
            await ServeNavigationMenus();

            var journalId = (Guid)Session["Id"];
            var journalEntries = await _channelService.FindJournalVoucherEntriesByJournalVoucherIdAsync(journalId, GetServiceHeader());
            ViewBag.journalEntries = journalEntries;


            
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(journalVoucherDTO.Type.ToString());
            ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(journalVoucherDTO.EntryType.ToString());

            JournalVoucherEntryDTO jvE = new JournalVoucherEntryDTO();

            decimal sumAmount = 0;

            jvE.rowId = 0;

            JournalVoucherEntryDTOs = TempData["JournalVoucherEntryDTO"] as ObservableCollection<JournalVoucherEntryDTO>;

            if (JournalVoucherEntryDTOs == null)
                JournalVoucherEntryDTOs = new ObservableCollection<JournalVoucherEntryDTO>();

            

            foreach (var journalVoucherEntryDTO in journalVoucherDTO.JournalVoucherEntries)
            {

                journalVoucherEntryDTO.BranchId = journalVoucherDTO.BranchId;
                journalVoucherEntryDTO.BranchDescription = journalVoucherDTO.BranchDescription;
                journalVoucherEntryDTO.EntryType = journalVoucherEntryDTO.EntryType;
                journalVoucherEntryDTO.ChartOfAccountId = journalVoucherEntryDTO.ChartOfAccountId;//Temporary
                journalVoucherEntryDTO.ChartOfAccountAccountName = journalVoucherEntryDTO.ChartOfAccountAccountName;
                journalVoucherEntryDTO.PrimaryDescription = journalVoucherDTO.PrimaryDescription;
                journalVoucherEntryDTO.SecondaryDescription = journalVoucherDTO.SecondaryDescription;
                journalVoucherEntryDTO.Reference = journalVoucherEntryDTO.Reference;
                journalVoucherEntryDTO.Amount = journalVoucherEntryDTO.Amount;
                journalVoucherEntryDTO.Remarks = journalVoucherDTO.Remarks;
                journalVoucherEntryDTO.rowId = journalVoucherEntryDTO.rowId;






                if (journalVoucherEntryDTO.PrimaryDescription == null || journalVoucherEntryDTO.SecondaryDescription == null || journalVoucherEntryDTO.Amount == 0)
                {
                    TempData["tPercentage"] = "Could not add Journal Voucher Entry. Ensure all required fields are enterd.";
                }
                else
                {
                    JournalVoucherEntryDTOs.Add(journalVoucherEntryDTO);
                    jvE.rowId++;

                    sumAmount = JournalVoucherEntryDTOs.Sum(cs => cs.Amount);

                    Session["journalVoucherEntries"] = JournalVoucherEntryDTOs;
                    Session["ChartOfAccountId"] = journalVoucherEntryDTO.ChartOfAccountId;

                    if (sumAmount > journalVoucherDTO.TotalValue)
                    {
                        TempData["tPercentage"] = "Failed to add  Journal Voucher Entry Total Amount exceeded Total Value.";

                        JournalVoucherEntryDTOs.Remove(journalVoucherEntryDTO);
                        TempData["tPercentage"] = "Failed to add  Journal Voucher Entry Total Amount exceeded Total Value.";
                        ViewBag.JournalVoucherEntryDTOs = JournalVoucherEntryDTOs;
                        return View("Create");
                    }


                    jvE.rowId--;

                    Session["journalVoucherEntries"] = JournalVoucherEntryDTOs;
                    Session["ChartOfAccountId"] = journalVoucherEntryDTO.ChartOfAccountId;


                    TempData["JournalVoucherEntryDTO"] = JournalVoucherEntryDTOs;
                    Session["JournalVoucherEntryDTO"] = JournalVoucherEntryDTOs;
                    TempData["JournalVoucherDTO"] = journalVoucherDTO;

                    ViewBag.JournalVoucherEntryDTOs = JournalVoucherEntryDTOs;

                    ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(journalVoucherDTO.Type.ToString());
                    ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(journalVoucherDTO.EntryType.ToString());

                }

            };

            TempData["JournalVoucherEntryDTO2"] = JournalVoucherEntryDTOs;
            Session["JournalVoucherEntryDTO2"] = JournalVoucherEntryDTOs;
            TempData["JournalVoucherDTO"] = journalVoucherDTO;

            ViewBag.journalEntries = JournalVoucherEntryDTOs;

            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(journalVoucherDTO.Type.ToString());
            ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(journalVoucherDTO.EntryType.ToString());

            TempData["EntryAdded"] = "Successfully Added an Entry";

            return View("Edit",journalVoucherDTO);
        }

        public async Task<ActionResult> JournalVoucherEdit(JournalVoucherDTO journalVoucherDTO)
        {
            Session["TypeDescription"] = journalVoucherDTO.TypeDescription;
            Session["ChartOfAccountAccountName"] = journalVoucherDTO.ChartOfAccountAccountName;
            Session["SecondaryDescription"] = journalVoucherDTO.SecondaryDescription;
            Session["BranchDescription"] = journalVoucherDTO.BranchDescription;
            Session["TotalValue"] = journalVoucherDTO.TotalValue;
            Session["PrimaryDescription"] = journalVoucherDTO.PrimaryDescription;
            Session["PostingPeriodDescription"] = journalVoucherDTO.PostingPeriodDescription;
            Session["Reference"] = journalVoucherDTO.Reference;
            Session["Remarks"] = journalVoucherDTO.Remarks;

            return View("edit", journalVoucherDTO);
        }

        [HttpPost]
       
        public async Task<ActionResult> Edit(Guid id, JournalVoucherDTO journalVoucherDTO)
        {
            var journalId = (Guid)Session["Id"];

            Session["TypeDescription"] = journalVoucherDTO.TypeDescription;
            Session["ChartOfAccountAccountName"] = journalVoucherDTO.ChartOfAccountAccountName;
            Session["SecondaryDescription"] = journalVoucherDTO.SecondaryDescription;
            Session["BranchDescription"] = journalVoucherDTO.BranchDescription;
            Session["TotalValue"] = journalVoucherDTO.TotalValue;
            Session["PrimaryDescription"] = journalVoucherDTO.PrimaryDescription;
            Session["PostingPeriodDescription"] = journalVoucherDTO.PostingPeriodDescription;
            Session["Reference"] = journalVoucherDTO.Reference;
            Session["Remarks"] = journalVoucherDTO.Remarks;

            journalVoucherDTO.ValidateAll();

            if (TempData["JournalVoucherEntryDTO2"] != null)
            {
                journalVoucherDTO.JournalVoucherEntries = TempData["JournalVoucherEntryDTO2"] as ObservableCollection<JournalVoucherEntryDTO>;

                var JournalVoucherEntries = journalVoucherDTO.JournalVoucherEntries;

                var journalVoucher = await _channelService.AddJournalVoucherAsync(journalVoucherDTO, GetServiceHeader());

                if (journalVoucher.ErrorMessageResult != null)
                {
                    await ServeNavigationMenus();

                    TempData["ErrorMsg"] = journalVoucher.ErrorMessageResult;

                    return View("edit");
                }

                TempData["SuccessMessage"] = "Successfully Created voucher Batch";
                TempData["JournalVoucherDTO"] = "";



                if (JournalVoucherEntries.Any())
                    await _channelService.UpdateJournalVoucherEntryCollectionAsync(journalVoucher.Id, JournalVoucherEntries, GetServiceHeader());

                TempData["JournalVoucherEntryDTO"] = "";
                TempData["Success"] = "journal and voucher have been created successifully";


                return RedirectToAction("Index");
            }

            journalVoucherDTO = TempData["JournalVoucherDTO"] as JournalVoucherDTO;
            JournalVoucherEntryDTOs = TempData["JournalVoucherEntryDTOs"] as ObservableCollection<JournalVoucherEntryDTO>;

            return RedirectToAction("view");
        }

        [HttpGet]
        public async Task<JsonResult> GetJournalVouchersAsync()
        {
            var journalVoucherDTOs = await _channelService.FindJournalVouchersAsync(GetServiceHeader());

            return Json(journalVoucherDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class AddGeneralLedgerController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();


            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;
            GeneralLedgerDTO generalLedgerDTO = new GeneralLedgerDTO();

            DateTime startDate = generalLedgerDTO.CreatedDate;

            DateTime endDate = startDate;


            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindGeneralLedgersByDateRangeAndFilterInPageAsync(startDate, endDate, jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<GeneralLedgerDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var generalLedgerDTO = await _channelService.FindGeneralLedgerAsync(id, GetServiceHeader());

            return View(generalLedgerDTO);
        }
        [HttpPost]
        public async Task<ActionResult> Add(Guid? id, GeneralLedgerDTO generalLedgerDTO)
        {
            await ServeNavigationMenus();

            ViewBag.BudgetEntryTypeSelectList = GetBudgetEntryTypeSelectList(generalLedgerDTO.Status.ToString());

            GeneralLedgerDTOs = TempData["GeneralLedgerDTOs"] as ObservableCollection<GeneralLedgerDTO>;

            if (GeneralLedgerDTOs == null)
                GeneralLedgerDTOs = new ObservableCollection<GeneralLedgerDTO>();

            foreach (var ledger in generalLedgerDTO.generalLedgerDTOs)
            {
                ledger.LedgerNumber = ledger.LedgerNumber;
                ledger.PostingPeriodId = ledger.PostingPeriodId;
                ledger.TotalValue = ledger.TotalValue;

                ledger.Remarks = ledger.Remarks;
                ledger.CreatedBy = ledger.CreatedBy;

                Session["chargeSplit"] = generalLedgerDTO.generalLedgerDTOs;


                if (ledger.TotalValue > generalLedgerDTO.TotalValue)
                {
                    TempData["tPercentage"] = "Amount cannot exceed the total value. The last added Entry has been removed.";

                    GeneralLedgerDTOs.Remove(ledger);

                    Session["chargeSplit"] = GeneralLedgerDTOs;
                }
                else if (ledger.TotalValue <= generalLedgerDTO.TotalValue)
                {
                    TempData["tPercentage"] = "Amount must be Equal to Total Amount.";
                    GeneralLedgerDTOs.Add(ledger);
                }

            };


            TempData["GeneralLedgerDTOs"] = GeneralLedgerDTOs;

            TempData["generalLedgerDTO"] = generalLedgerDTO;

            ViewBag.budgetEntryDTOs = budgetEntryDTOs;

            return View("Create", generalLedgerDTO);
        }
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            ViewBag.SystemGeneralLedgerAccountCodeSelectList = GetSystemGeneralLedgerAccountCodeSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(GeneralLedgerDTO generalLedgerDTO)
        {

            generalLedgerDTO.ValidateAll();

            if (!generalLedgerDTO.HasErrors)
            {
                await _channelService.AddGeneralLedgerAsync(generalLedgerDTO.MapTo<GeneralLedgerDTO>(), GetServiceHeader());
                ViewBag.SystemGeneralLedgerAccountCodeSelectList = GetSystemGeneralLedgerAccountCodeSelectList(generalLedgerDTO.Status.ToString());
                TempData["SuccessMessage"] = "Create successful.";
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = generalLedgerDTO.ErrorMessages;

                return View(generalLedgerDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var postingPeriodDTO = await _channelService.FindGeneralLedgerAsync(id, GetServiceHeader());

            return View(postingPeriodDTO.MapTo<GeneralLedgerDTO>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, GeneralLedgerDTO generalLedgerDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateGeneralLedgerAsync(generalLedgerDTO.MapTo<GeneralLedgerDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(generalLedgerDTO);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetPostingPeriodsAsync()
        {
            var postingPeriodsDTOs = await _channelService.FindPostingPeriodsAsync(GetServiceHeader());

            return Json(postingPeriodsDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
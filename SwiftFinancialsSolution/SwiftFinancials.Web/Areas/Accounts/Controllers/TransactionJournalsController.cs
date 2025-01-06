using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class TransactionJournalsController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();
            ViewBag.CustomerTypeSelectList = GetJournalfielterSelectList(string.Empty);
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(DateTime? startDate, DateTime? endDate, string reference, int? filter)
        {
            int totalRecordCount = 0;

            //bool sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc";
            //var pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            //// Validate date range
            DateTime start = startDate ?? DateTime.Now.AddDays(-30);
            DateTime end = endDate ?? DateTime.Now;
            if (filter == 2)
            {
                var pageCollectionInfo = await _channelService.FindGeneralLedgerTransactionsByDateRangeAndFilterInPageAsync(
                0, int.MaxValue,
                start,
                end,
                reference,
                2, GetServiceHeader()
            );

                if (pageCollectionInfo == null || !pageCollectionInfo.PageCollection.Any())
                {
                    return Json(new
                    {
                        sEcho = 1,
                        iTotalRecords = 0,
                        iTotalDisplayRecords = 0,
                        aaData = new object[] { },
                        message = "No records found for the selected filters."
                    }, JsonRequestBehavior.AllowGet);
                }

                // Prepare data for DataTable
                var result = pageCollectionInfo.PageCollection.Select(t => new
                {
                    BranchDescription = t.BranchDescription,
                    TransactionDate = t.JournalCreatedDate.ToString("dd/MM/yyyy"),
                    PrimaryDescription = t.JournalPrimaryDescription,
                    Debit = t.Debit.ToString("N2"),
                    Credit = t.Credit.ToString("N2"),
                    RunningBalance = t.RunningBalance.ToString("N2"),
                    ContraGLAccountName = t.ContraGLAccountName,
                    Secondary = t.JournalSecondaryDescription
                });
            }
            return Json(new
            {
                sEcho = 2,
            }, JsonRequestBehavior.AllowGet);

        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var systemGeneralLedgerAccountMappingDTO = await _channelService.FindSystemGeneralLedgerAccountMappingAsync(id, GetServiceHeader());

            return View(systemGeneralLedgerAccountMappingDTO);
        }


        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            ViewBag.SystemGeneralLedgerAccountCodeSelectList = GetSystemGeneralLedgerAccountCodeSelectList(string.Empty);
            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Create(SystemGeneralLedgerAccountMappingDTO systemGeneralLedgerAccountMappingDTO)
        {
            systemGeneralLedgerAccountMappingDTO.ValidateAll();

            if (!systemGeneralLedgerAccountMappingDTO.HasErrors)
            {
                var result = await _channelService.AddSystemGeneralLedgerAccountMappingAsync(systemGeneralLedgerAccountMappingDTO, GetServiceHeader());

                if (result.ErrorMessageResult != null)
                {
                    await ServeNavigationMenus();

                    ViewBag.SystemGeneralLedgerAccountCodeSelectList = GetSystemGeneralLedgerAccountCodeSelectList(string.Empty);

                    TempData["ErrorMsg"] = result.ErrorMessageResult;

                    return View();
                }

                TempData["AlertMessage"] = "Successfully mapped G/L Account";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = systemGeneralLedgerAccountMappingDTO.ErrorMessages;
                ViewBag.SystemGeneralLedgerAccountCodeSelectList = GetSystemGeneralLedgerAccountCodeSelectList(systemGeneralLedgerAccountMappingDTO.SystemGeneralLedgerAccountCode.ToString());

                TempData["CreateError"] = "Failed to Map G/L Account";

                return View(systemGeneralLedgerAccountMappingDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.SystemGeneralLedgerAccountCodeSelectList = GetSystemGeneralLedgerAccountCodeSelectList(string.Empty);

            var systemGeneralLedgerAccountMappingDTO = await _channelService.FindSystemGeneralLedgerAccountMappingAsync(id, GetServiceHeader());

            return View(systemGeneralLedgerAccountMappingDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, SystemGeneralLedgerAccountMappingDTO systemGeneralLedgerAccountMappingDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateSystemGeneralLedgerAccountMappingAsync(systemGeneralLedgerAccountMappingDTO, GetServiceHeader());

                TempData["EditMessage"] = "Successfully edited G/L Account Determination";

                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.SystemGeneralLedgerAccountCodeSelectList = GetSystemGeneralLedgerAccountCodeSelectList(systemGeneralLedgerAccountMappingDTO.SystemGeneralLedgerAccountCode.ToString());

                TempData["EditError"] = "Failed to edit G/L Account Determination";

                return View(systemGeneralLedgerAccountMappingDTO);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetSystemGeneralLedgerAccountMappingsAsync()
        {
            var systemGeneralLedgerAccountMappingDTOs = await _channelService.FindSystemGeneralLedgerAccountMappingsAsync(GetServiceHeader());

            return Json(systemGeneralLedgerAccountMappingDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}

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

    public class GuarantorRelievingController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            ViewBag.LoanGuarantorStatus = GetLoanGuarantorStatusTypeSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, DateTime startDate, DateTime endDate, int loanGuarantorStatus, string filterValue)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindLoanGuarantorAttachmentHistoryByStatusAndFilterInPageAsync(loanGuarantorStatus, startDate, endDate,
                filterValue, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(x => x.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanGuarantorAttachmentHistoryDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }
        
        public async Task<ActionResult> View(Guid? id)
        {
            await ServeNavigationMenus();

            Guid parseId;
            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return Json(new { success = false, message = "Invalid ID" }, JsonRequestBehavior.AllowGet);
            }
            Session["LoanGuarantorAttachmentHistoryId"] = parseId;
            var LoanGuarantorAttachmentHistoryEntries = await _channelService.FindLoanGuarantorAttachmentHistoryEntriesByLoanGuarantorAttachmentHistoryIdAsync(parseId, GetServiceHeader());

            if (LoanGuarantorAttachmentHistoryEntries == null)
            {
                return Json(new { success = false, message = "No data found" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = LoanGuarantorAttachmentHistoryEntries }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Update()
        {

            if (Session["LoanGuarantorAttachmentHistoryId"] != null)
            {
                Guid Id = (Guid)Session["LoanGuarantorAttachmentHistoryId"];

                await _channelService.RelieveLoanGuarantorsAsync(Id, 1234, GetServiceHeader());
                TempData["Success"] = "Operation Successful.";
                Session["LoanGuarantorAttachmentHistoryId"] = null;
                return RedirectToAction("Index");
            }
            Session["LoanGuarantorAttachmentHistoryId"] = null;
            TempData["Failed"] = "Operation Failed!";
            return RedirectToAction("Index");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class LoaningRemarkController : MasterController
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

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "desc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindLoaningRemarksByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.Where(item => !item.IsLocked).ToList();

                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(r => r.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else
            {
                return this.DataTablesJson(items: new List<LoaningRemarkDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var loaningRemarkDTO = await _channelService.FindLoaningRemarkAsync(id, GetServiceHeader());

            return View(loaningRemarkDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(LoaningRemarkDTO loaningRemarkDTO)
        {
            loaningRemarkDTO.ValidateAll();

            if (!loaningRemarkDTO.HasErrors)
            {
                var remarks = await _channelService.AddLoaningRemarkAsync(loaningRemarkDTO, GetServiceHeader());

                if(remarks.ErrorMessageResult!=null)
                {
                    await ServeNavigationMenus();

                    TempData["ErrorMsg"] = remarks.ErrorMessageResult;

                    return View();
                }

                TempData["create"] = "Successfully created Loaning Remark";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loaningRemarkDTO.ErrorMessages;

                TempData["createError"] = "Could not create Loaning Remark";

                return View(loaningRemarkDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var loaningRemarkDTO = await _channelService.FindLoaningRemarkAsync(id, GetServiceHeader());

            return View(loaningRemarkDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, LoaningRemarkDTO loaningRemarkBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateLoaningRemarkAsync(loaningRemarkBindingModel, GetServiceHeader());

                TempData["edit"] = "Successfully edited Loaning Remark";

                return RedirectToAction("Index");
            }
            else
            {
                TempData["editError"] = "Failed to edit Loaning Remark";

                return View(loaningRemarkBindingModel);
            }
        }
    }
}
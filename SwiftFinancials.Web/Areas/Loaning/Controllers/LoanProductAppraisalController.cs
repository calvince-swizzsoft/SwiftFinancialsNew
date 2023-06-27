using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class LoanProductAppraisalProductController : MasterController
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

            var pageCollectionInfo = await _channelService.FindLoanProductAppraisalProductsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanProductAppraisalProductDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var LoanProductAppraisalProductDTO = await _channelService.FindLoanProductAppraisalProductAsync(id, GetServiceHeader());

            return View(LoanProductAppraisalProductDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(LoanProductAppraisalProductDTO LoanProductAppraisalProductDTO)
        {
            LoanProductAppraisalProductDTO.ValidateAll();

            if (!LoanProductAppraisalProductDTO.HasErrors)
            {
                await _channelService.AddLoanProductAppraisalProductAsync(LoanProductAppraisalProductDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = LoanProductAppraisalProductDTO.ErrorMessages;

                return View(LoanProductAppraisalProductDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var LoanProductAppraisalProductDTO = await _channelService.FindLoanProductAppraisalProductAsync(id, GetServiceHeader());

            return View(LoanProductAppraisalProductDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, LoanProductAppraisalProductDTO LoanProductAppraisalProductBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateLoanProductAppraisalProductAsync(LoanProductAppraisalProductBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(LoanProductAppraisalProductBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetBudgetsAsync()
        {
            var loanProductAppraisalProductDTOs = await _channelService.FindLoanProductAppraisalProductsAsync(GetServiceHeader());

            return Json(loanProductAppraisalProductDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
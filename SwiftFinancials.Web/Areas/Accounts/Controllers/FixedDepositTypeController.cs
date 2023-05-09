

using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class FixedDepositTypeController : MasterController
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

            var pageCollectionInfo = await _channelService.FindFixedDepositTypesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<FixedDepositTypeDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var fixedDepositTypeDTO = await _channelService.FindFixedDepositTypeAsync(id, GetServiceHeader());

            return View(fixedDepositTypeDTO);
        }
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        //        [HttpPost]
        //        public async Task<ActionResult> Create(FixedDepositTypeDTO fixedDepositTypeDTO)
        //        {
        //            fixedDepositTypeDTO.ValidateAll();

        //            if (!fixedDepositTypeDTO.HasErrors)
        //            {
        //                object p = await _channelService.AddFixedDepositTypeAsync(fixedDepositTypeDTO, GetServiceHeader());

        //                return RedirectToAction("Index");
        //            }
        //            else
        //            {
        //                var errorMessages = fixedDepositTypeDTO.ErrorMessages;

        //                return View(fixedDepositTypeDTO);
        //            }
        //        }

        //        public async Task<ActionResult> Edit(Guid id)
        //        {
        //            await ServeNavigationMenus();

        //            var fixedDepositTypeDTO = await _channelService.FindFixedDepositTypeAsync(id, GetServiceHeader());

        //            return View(fixedDepositTypeDTO);
        //        }

        //        [HttpPost]
        //        [ValidateAntiForgeryToken]
        //        public async Task<ActionResult> Edit(Guid id, FixedDepositTypeDTO fixedDepositTypeBindingModel)
        //        {
        //            if (ModelState.IsValid)
        //            {
        //                await _channelService.UpdateFixedDepositTypeAsync(fixedDepositTypeBindingModel, GetServiceHeader());

        //                return RedirectToAction("Index");
        //            }
        //            else
        //            {
        //                return View(fixedDepositTypeBindingModel);
        //            }
        //        }

        //        //[HttpGet]
        //        //public async Task<JsonResult> GetFixedDepositTypesAsync()
        //        //{
        //        //    var fixedDepositTypeDTOs = await _channelService.FindFixedDepositTypesAsync(GetServiceHeader());

        //        //    return Json(fixedDepositTypeDTOs, JsonRequestBehavior.AllowGet);
        //        //}
    }
}

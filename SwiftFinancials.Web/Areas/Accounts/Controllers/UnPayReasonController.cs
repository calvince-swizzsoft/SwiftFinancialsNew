

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
    public class UnPayReasonController : MasterController
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

            var pageCollectionInfo = await _channelService.FindUnPayReasonsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<UnPayReasonDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var unPayReasonDTO = await _channelService.FindUnPayReasonAsync(id, GetServiceHeader());

            return View(unPayReasonDTO);
        }
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(UnPayReasonDTO unPayReasonDTO)
        {
            unPayReasonDTO.ValidateAll();

            if (!unPayReasonDTO.HasErrors)
            {
                await _channelService.AddUnPayReasonAsync(unPayReasonDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = unPayReasonDTO.ErrorMessages;

                return View(unPayReasonDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var unPayReasonDTO = await _channelService.FindUnPayReasonAsync(id, GetServiceHeader());

            return View(unPayReasonDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, UnPayReasonDTO unPayReasonBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateUnPayReasonAsync(unPayReasonBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(unPayReasonBindingModel);
            }
        }

        /*[HttpGet]
        public async Task<JsonResult> GetUnPayReasonsAsync()
        {
            var unPayReasonDTOs = await _channelService.FindUnPayReasonsAsync(GetServiceHeader());

            return Json(unPayReasonDTOs, JsonRequestBehavior.AllowGet);
        }*/
    }
}


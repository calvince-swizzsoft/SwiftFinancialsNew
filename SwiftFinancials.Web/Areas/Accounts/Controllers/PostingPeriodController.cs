using System;
using System.Collections.Generic;
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
    public class PostingPeriodController : MasterController
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

            var pageCollectionInfo = await _channelService.FindPostingPeriodsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<PostingPeriodDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var postingPeriodDTO = await _channelService.FindPostingPeriodAsync(id, GetServiceHeader());

            return View(postingPeriodDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(PostingPeriodDTO postingPeriodDTO)
        {
            postingPeriodDTO.ValidateAll();

            if (!postingPeriodDTO.HasErrors)
            {
                await _channelService.AddPostingPeriodAsync(postingPeriodDTO.MapTo<PostingPeriodDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = postingPeriodDTO.ErrorMessages;

                return View(postingPeriodDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var postingPeriodDTO = await _channelService.FindPostingPeriodAsync(id, GetServiceHeader());

            return View(postingPeriodDTO.MapTo<PostingPeriodDTO>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, PostingPeriodDTO postingPeriodDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdatePostingPeriodAsync(postingPeriodDTO.MapTo<PostingPeriodDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(postingPeriodDTO);
            }
        }

       /* [HttpGet]
        public async Task<JsonResult> GetPostingPeriodsAsync()
        {
            var postingPeriodsDTOs = await _channelService.FindPostingPeriodsAsync(GetServiceHeader());

            return Json(postingPeriodsDTOs, JsonRequestBehavior.AllowGet);
        }*/
    }
}
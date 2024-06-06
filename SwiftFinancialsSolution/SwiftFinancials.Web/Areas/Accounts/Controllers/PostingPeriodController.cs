using System;
using System.Collections.Generic;
using System.Globalization;
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

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(postingPeriod => postingPeriod.CreatedDate).ToList();

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
            var startDate = Request["startDate"];

            var endDate = Request["endDate"];

            postingPeriodDTO.DurationStartDate = DateTime.Parse(startDate).Date;

            postingPeriodDTO.DurationEndDate = DateTime.Parse(endDate).Date;

            postingPeriodDTO.ValidateAll();

            if (!postingPeriodDTO.HasErrors)
            {
                await _channelService.AddPostingPeriodAsync(postingPeriodDTO.MapTo<PostingPeriodDTO>(), GetServiceHeader());
                TempData["SuccessMessage"] = "Create successful.";
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
            var startDate = Request["startDate"];

            var endDate = Request["endDate"];

            postingPeriodDTO.DurationStartDate = DateTime.Parse(startDate).Date;

            postingPeriodDTO.DurationEndDate = DateTime.Parse(endDate).Date;
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



        public async Task<ActionResult> Close(Guid id,PostingPeriodDTO postingPeriodDTO2)
        {
            await ServeNavigationMenus();

            var postingPeriodDTO = await _channelService.FindPostingPeriodAsync(id, GetServiceHeader());
            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }
            if (postingPeriodDTO != null)
            {

                postingPeriodDTO2.Id = postingPeriodDTO.Id;
                postingPeriodDTO2.Description = postingPeriodDTO.Description;
                postingPeriodDTO2.CreatedDate = postingPeriodDTO.CreatedDate;
                postingPeriodDTO2.DurationEndDate = postingPeriodDTO.DurationEndDate;
                postingPeriodDTO2.DurationStartDate = postingPeriodDTO.DurationStartDate;
                
            }
            return View(postingPeriodDTO2);
        }

           
        

        [HttpPost]
        public async Task<ActionResult> Close(PostingPeriodDTO postingPeriodDTO)
        {
            int moduleNavigationItemCode = 0;
            var closeddate = Request["closeddate"];
            var Enddate = Request["Enddate"];
            var StartDate = Request["StartDate"];

            postingPeriodDTO.DurationStartDate = DateTime.ParseExact((Request["StartDate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            postingPeriodDTO.DurationEndDate = DateTime.ParseExact((Request["Enddate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            postingPeriodDTO.ClosedDate = DateTime.ParseExact((Request["closeddate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);



            if (!postingPeriodDTO.HasErrors)
            {
                await _channelService.ClosePostingPeriodAsync(postingPeriodDTO, moduleNavigationItemCode, GetServiceHeader());
                await _channelService.UpdatePostingPeriodAsync(postingPeriodDTO, GetServiceHeader());
                TempData["SuccessMessage"] = "posting period Closed successful.";
                return RedirectToAction("Index");
            }
            else
            {
                return View(postingPeriodDTO);
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.HumanResource.Controllers
{
    public class HolidayController : MasterController
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

            var pageCollectionInfo = await _channelService.FindHolidaysByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<HolidayDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var holidayDTO = await _channelService.FindHolidaysByPostingPeriodAsync(id, GetServiceHeader());

            return View(holidayDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(HolidayDTO holidayDTO)
        {
            var startDate = Request["startDate"];

            var endDate = Request["endDate"];

            holidayDTO.DurationStartDate = DateTime.Parse(startDate).Date;

            holidayDTO.DurationEndDate = DateTime.Parse(endDate).Date;
            var k = await _channelService.FindPostingPeriodAsync(holidayDTO.PostingPeriodId,GetServiceHeader());
            holidayDTO.PostingPeriodDurationEndDate = k.DurationEndDate;
            holidayDTO.PostingPeriodDurationStartDate = k.DurationStartDate;
            holidayDTO.ValidateAll();

            if (!holidayDTO.HasErrors)
            {
                await _channelService.AddHolidayAsync(holidayDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = holidayDTO.ErrorMessages;

                return View(holidayDTO);
            }
        }

        /*public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var holidayDTO = await _channelService.FindHolidayAsync(id, GetServiceHeader());

            return View(holidayDTO);
        }*/

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, HolidayDTO holidayBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateHolidayAsync(holidayBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(holidayBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetHolidaysAsync()
        {
            var holidaysDTOs = await _channelService.FindCompaniesAsync(GetServiceHeader());

            return Json(holidaysDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
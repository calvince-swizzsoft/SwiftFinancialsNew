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
    public class PaySlipsController : MasterController
    {

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, Guid id)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindPaySlipsBySalaryPeriodIdInPageAsync(id, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<PaySlipDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var paySlipDTO = await _channelService.FindPaySlipsBySalaryPeriodIdAsync(id, GetServiceHeader());

            return View(paySlipDTO);
        }

        //public async Task<ActionResult> Create()
        //{
        //    await ServeNavigationMenus();

        //    return View();
        //}

        //[HttpPost]
        //public async Task<ActionResult> Create(HolidayDTO holidayDTO)
        //{
        //    var startDate = Request["startDate"];

        //    var endDate = Request["endDate"];

        //    holidayDTO.DurationStartDate = DateTime.Parse(startDate).Date;

        //    holidayDTO.DurationEndDate = DateTime.Parse(endDate).Date;
        //    holidayDTO.ValidateAll();

        //    if (!holidayDTO.HasErrors)
        //    {
        //        await _channelService.AddHolidayAsync(holidayDTO, GetServiceHeader());

        //        return RedirectToAction("Index");
        //    }
        //    else
        //    {
        //        var errorMessages = holidayDTO.ErrorMessages;

        //        return View(holidayDTO);
        //    }
        //}


        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> Edit(Guid id, HolidayDTO holidayBindingModel)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        await _channelService.UpdateHolidayAsync(holidayBindingModel, GetServiceHeader());

        //        return RedirectToAction("Index");
        //    }
        //    else
        //    {
        //        return View(holidayBindingModel);
        //    }
        //}

        //[HttpGet]
        //public async Task<JsonResult> GetHolidaysAsync()
        //{
        //    var holidaysDTOs = await _channelService.FindCompaniesAsync(GetServiceHeader());

        //    return Json(holidaysDTOs, JsonRequestBehavior.AllowGet);
        //}
    }
}
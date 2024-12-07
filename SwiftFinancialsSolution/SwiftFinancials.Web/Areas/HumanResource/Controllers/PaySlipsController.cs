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
        public async Task<ActionResult> Index(JQueryDataTablesModel jQueryDataTablesModel, DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue)
                startDate = DateTime.MinValue;

            if (!endDate.HasValue)
                endDate = DateTime.MaxValue;

            int totalRecordCount = 0;
            int searchRecordCount = 0;

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;

            bool sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc";

            int status = 1;

            try
            {
                var pageCollectionInfo = await _channelService.FindSalaryPeriodsByFilterInPageAsync(
                    status,
                    startDate.Value,
                    endDate.Value,
                    jQueryDataTablesModel.sSearch,
                    0,
                    int.MaxValue,
                    GetServiceHeader()
                );

                if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
                {

                    var sortedData = pageCollectionInfo.PageCollection
                        .OrderByDescending(salaryProcessingDTO => salaryProcessingDTO.CreatedDate)
                        .ToList();

                    totalRecordCount = sortedData.Count;

                    var paginatedData = sortedData
                        .Skip(jQueryDataTablesModel.iDisplayStart)
                        .Take(jQueryDataTablesModel.iDisplayLength)
                        .ToList();

                    searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                        ? sortedData.Count
                        : totalRecordCount;

                    return this.DataTablesJson(
                        items: paginatedData,
                        totalRecords: totalRecordCount,
                        totalDisplayRecords: searchRecordCount,
                        sEcho: jQueryDataTablesModel.sEcho
                    );
                }

                return this.DataTablesJson(
                    items: new List<SalaryProcessingDTO>(),
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
            );
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
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
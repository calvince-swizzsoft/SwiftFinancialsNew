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
using System.Windows.Forms;

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

            bool sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc";
            var sortedColumns = jQueryDataTablesModel.GetSortedColumns().Select(s => s.PropertyName).ToList();

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;

            var pageCollectionInfo = await _channelService.FindHolidaysByFilterInPageAsync(
                jQueryDataTablesModel.sSearch,
                0,
                int.MaxValue,
                GetServiceHeader()
            );

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(holidayDTO => holidayDTO.CreatedDate)
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
               items: new List<HolidayDTO>(),
               totalRecords: totalRecordCount,
               totalDisplayRecords: searchRecordCount,
               sEcho: jQueryDataTablesModel.sEcho
               );
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var holidayDTO = await _channelService.FindHolidayAsync(id, GetServiceHeader());

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
            // Validate the DTO
            holidayDTO.ValidateAll();

            if (!holidayDTO.HasErrors)
            {
                // Save the holiday if no errors are present
                await _channelService.AddHolidayAsync(holidayDTO, GetServiceHeader());
                TempData["Message"] = "Operation Success: Holiday Created Successfully!";
                TempData["MessageType"] = "Success";

               

                return RedirectToAction("Index");
            }
            else
            {
                // Display specific validation errors
                if (holidayDTO.ErrorMessages.Any())
                {
                    var errorMessages = string.Join("\n- ", holidayDTO.ErrorMessages);
                    TempData["Message"] = "An unspecified error occurred during validation.";
                    TempData["MessageType"] = "error";

                   
                }
                else
                {
                    TempData["Message"] = "Operation Success: Holiday Created Successfully!";
                    TempData["MessageType"] = "error";
                   
                }

                return View(holidayDTO);
            }
        }



        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var holidayDTO = await _channelService.FindHolidayAsync(id, GetServiceHeader());

            return View(holidayDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, HolidayDTO holidayDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateHolidayAsync(holidayDTO, GetServiceHeader());
                TempData["Message"] = "Operation Success: Holiday has been updated.";
                TempData["MessageType"] = "Success";
               

                return RedirectToAction("Index");
            }
            else
            {
                TempData["Message"] = "An unspecified error occurred during validation.";
                TempData["MessageType"] = "error";
                
            }
            return View(holidayDTO);

        }

    }
}
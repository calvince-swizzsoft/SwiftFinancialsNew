using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.HumanResource.Controllers
{
    public class TrainingPeriodController : MasterController
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

            DateTime startDate = DateTime.Now;

            DateTime endDate = DateTime.Now;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindTrainingPeriodsFilterInPageAsync(startDate, endDate,jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<EmployeeDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var trainingPeriodDTO = await _channelService.FindTrainingPeriodAsync(id, GetServiceHeader());

            return View(trainingPeriodDTO);
        }

        public async Task<ActionResult> Create()
        {

            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(TrainingPeriodDTO trainingPeriodDTO)
        {
            var startDate = Request["startDate"];

            var endDate = Request["endDate"];

            trainingPeriodDTO.DurationStartDate = DateTime.Parse(startDate).Date;

            trainingPeriodDTO.DurationEndDate = DateTime.Parse(endDate).Date;

            trainingPeriodDTO.ValidateAll();

            if (!trainingPeriodDTO.HasErrors)
            {
                await _channelService.AddTrainingPeriodAsync(trainingPeriodDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = trainingPeriodDTO.ErrorMessages;

                return View(trainingPeriodDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var trainingPeriodDTO = await _channelService.FindTrainingPeriodAsync(id, GetServiceHeader());

            return View(trainingPeriodDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, TrainingPeriodDTO trainingPeriodBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateTrainingPeriodAsync(trainingPeriodBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(trainingPeriodBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetTrainingPeriodsAsync()
        {
            var trainingPeriodDTOs = await _channelService.FindCompaniesAsync(GetServiceHeader());

            return Json(trainingPeriodDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
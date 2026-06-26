using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.RegistryModule;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Registry.Controllers
{
    public class RegistereducationvenueController : MasterController
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

            var pageCollectionInfo = await _channelService.FindEducationRegistersByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<EducationRegisterDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var educationRegisterDTO = await _channelService.FindEducationRegisterAsync(id, GetServiceHeader());

            return View(educationRegisterDTO);
        }

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();
           
            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var test = await _channelService.FindEducationVenueAsync(parseId, GetServiceHeader());

            EducationRegisterDTO educationRegisterDTO = new EducationRegisterDTO();

            if (test != null)
            {

                educationRegisterDTO.EducationVenueId = test.Id;
                educationRegisterDTO.EducationVenueDescription = test.Description;                
               
            }

            return View(educationRegisterDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(EducationRegisterDTO educationRegisterDTO)
        {

            if (!educationRegisterDTO.HasErrors)
            {
                await _channelService.AddEducationRegisterAsync(educationRegisterDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

                TempData["Error"] = string.Join(",", allErrors);

                return View(educationRegisterDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var educationregisterDTO = await _channelService.FindEducationRegisterAsync(id, GetServiceHeader());

            return View(educationregisterDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, EducationRegisterDTO educationRegisterDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateEducationRegisterAsync(educationRegisterDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(educationRegisterDTO);
            }
        }
    }
}
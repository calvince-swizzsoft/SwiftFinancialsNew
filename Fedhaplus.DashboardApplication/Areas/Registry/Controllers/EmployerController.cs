using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.RegistryModule;
using Fedhaplus.DashboardApplication.Controllers;
using Fedhaplus.DashboardApplication.Helpers;
using Fedhaplus.Presentation.Infrastructure.Util;
using Infrastructure.Crosscutting.Framework.Adapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Fedhaplus.DashboardApplication.Areas.Registry.Controllers
{
    public class EmployerController : MasterController
    {
        // GET: Registry/Employer
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

            var pageCollectionInfo = await _channelService.FindEmployersByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }

            else return this.DataTablesJson(items: new List<EmployerDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var employerDTO = await _channelService.FindEmployerAsync(id, GetServiceHeader());

            return View(employerDTO.ProjectedAs<EmployerDTO>());
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(EmployerBindingModel employerBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.AddEmployerAsync(employerBindingModel.MapTo<EmployerDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

                TempData["Error"] = string.Join(",", allErrors);

                return View(employerBindingModel);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var employerDTO = await _channelService.FindEmployerAsync(id, GetServiceHeader());

            return View(employerDTO.MapTo<EmployerBindingModel>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, EmployerBindingModel employerBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateEmployerAsync(employerBindingModel.MapTo<EmployerDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

                TempData["Error"] = string.Join(",", allErrors);

                return View(employerBindingModel);
            }
        }
    }
}
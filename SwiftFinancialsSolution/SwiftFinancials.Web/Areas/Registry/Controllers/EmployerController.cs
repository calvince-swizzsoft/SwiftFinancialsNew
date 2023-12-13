using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.RegistryModule;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Registry.Controllers
{
    public class EmployerController : MasterController
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

            var pageCollectionInfo = await _channelService.FindEmployersByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(employer => employer.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;


                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<EmployerDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var employerDTO = await _channelService.FindEmployerAsync(id, GetServiceHeader());

            return View(employerDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(EmployerBindingModel employerBindingModel)
        {
            employerBindingModel.ValidateAll();

            if (!employerBindingModel.ErrorMessages.Any())
            {
                var employer = await _channelService.AddEmployerAsync(employerBindingModel.MapTo<EmployerDTO>(), GetServiceHeader());

                if (employer != null)
                {
                    //Update divisions

                    var divisions = new ObservableCollection<DivisionDTO>();

                    foreach (var divisionDTO in employerBindingModel.Divisions)
                    {
                        divisionDTO.EmployerId = employer.Id;

                        divisions.Add(divisionDTO);
                    }

                    await _channelService.UpdateDivisionsByEmployerIdAsync(employer.Id, divisions, GetServiceHeader());
                }

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

            var EmployerDTO = await _channelService.FindEmployerAsync(id, GetServiceHeader());

            return View(EmployerDTO);
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
                return View(employerBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetEmployersAsync()
        {
            var employerDTOs = await _channelService.FindEmployersAsync(GetServiceHeader());

            return Json(employerDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
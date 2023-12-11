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
    public class EmployeeTypeController : MasterController
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

            var pageCollectionInfo = await _channelService.FindEmployeeTypesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<EmployeeTypeDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var employeeTypeDTO = await _channelService.FindEmployeeTypeAsync(id, GetServiceHeader());

            return View(employeeTypeDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            ViewBag.EmployeeCategorySelectList = GetEmployeeCategorySelectList(string.Empty);
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(EmployeeTypeDTO employeeTypeDTO)
        {
            employeeTypeDTO.ValidateAll();

            if (!employeeTypeDTO.HasErrors)
            {
                await _channelService.AddEmployeeTypeAsync(employeeTypeDTO, GetServiceHeader());
                

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = employeeTypeDTO.ErrorMessages;
                ViewBag.EmployeeCategorySelectList = GetEmployeeCategorySelectList(employeeTypeDTO.Category.ToString());
                return View(employeeTypeDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.EmployeeCategorySelectList = GetEmployeeCategorySelectList(string.Empty);
            var employeeTypeDTO = await _channelService.FindEmployeeTypeAsync(id, GetServiceHeader());

            return View(employeeTypeDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, EmployeeTypeDTO employeeTypeBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateEmployeeTypeAsync(employeeTypeBindingModel, GetServiceHeader());
                ViewBag.EmployeeCategorySelectList = GetEmployeeCategorySelectList(employeeTypeBindingModel.Category.ToString());
                return RedirectToAction("Index");
            }
            else
            {
                return View(employeeTypeBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetEmployeeTypesAsync()
        {
            var employeeTypesDTOs = await _channelService.FindEmployeeTypesAsync(GetServiceHeader());

            return Json(employeeTypesDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
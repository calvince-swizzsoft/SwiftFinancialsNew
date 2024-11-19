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

            bool sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc";
            var sortedColumns = jQueryDataTablesModel.GetSortedColumns().Select(s => s.PropertyName).ToList();

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;

            var pageCollectionInfo = await _channelService.FindEmployeeTypesByFilterInPageAsync(
                jQueryDataTablesModel.sSearch,
                pageIndex,
                pageSize,
                GetServiceHeader()
            );

            if (pageCollectionInfo != null)
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                var sortedData = sortAscending
                    ? pageCollectionInfo.PageCollection
                        .OrderBy(item => sortedColumns.Contains("CreatedDate") ? item.CreatedDate : default(DateTime))
                        .ToList()
                    : pageCollectionInfo.PageCollection
                        .OrderByDescending(item => sortedColumns.Contains("CreatedDate") ? item.CreatedDate : default(DateTime))
                        .ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? sortedData.Count : totalRecordCount;

                return this.DataTablesJson(
                    items: sortedData,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
            else
            {
                return this.DataTablesJson(
                    items: new List<EmployeeTypeDTO>(),
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
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

                TempData["SuccessMessage"] = "EmployeeType Created Successfully!!";
                

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
                TempData["SuccessMessage"] = "EmployeeType Updated Successfully!!";

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
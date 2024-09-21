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
    public class EmployeeController : MasterController
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

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var pageCollectionInfo = await _channelService.FindEmployeesByFilterInPageAsync(jQueryDataTablesModel.sSearch, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

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

            var employeeDTO = await _channelService.FindEmployeeAsync(id, GetServiceHeader());

            return View(employeeDTO);
        }

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            ViewBag.BloodGroupSelectList = GetBloodGroupSelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            EmployeeDTO employeeBindingModel = new EmployeeDTO();

            if (customer != null)
            {
                employeeBindingModel.CustomerId = customer.Id;
                employeeBindingModel.CustomerFullName = customer.FullName;
            }

            return View(employeeBindingModel);
        }

        [HttpPost]
        public async Task<ActionResult> Create(EmployeeDTO employeeBindingModel)
        {
            employeeBindingModel.ValidateAll();

            if (!employeeBindingModel.HasErrors)
            {
                await _channelService.AddEmployeeAsync(employeeBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = employeeBindingModel.ErrorMessages;
                ViewBag.BloodGroupSelectList = GetBloodGroupSelectList(employeeBindingModel.BloodGroup.ToString());
                return View(employeeBindingModel);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var employeeDTO = await _channelService.FindEmployeeAsync(id, GetServiceHeader());

            return View(employeeDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, EmployeeDTO employeeBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateEmployeeAsync(employeeBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(employeeBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetEmployeesAsync()
        {
            var employeesDTOs = await _channelService.FindEmployeesAsync(GetServiceHeader());

            return Json(employeesDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
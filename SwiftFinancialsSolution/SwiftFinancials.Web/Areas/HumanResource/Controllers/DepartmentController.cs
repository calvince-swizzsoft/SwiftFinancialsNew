using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System.Windows.Forms;

namespace SwiftFinancials.Web.Areas.HumanResource.Controllers
{
    public class DepartmentController : MasterController
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

            var pageCollectionInfo = await _channelService.FindDepartmentsByFilterInPageAsync(
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
                    items: new List<DepartmentDTO>(),
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var departmentDTO = await _channelService.FindDepartmentAsync(id, GetServiceHeader());

            return View(departmentDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(DepartmentDTO departmentDTO)
        {
            departmentDTO.ValidateAll();

            if (!departmentDTO.HasErrors)
            {
                await _channelService.AddDepartmentAsync(departmentDTO, GetServiceHeader());
                MessageBox.Show(
                                                             "Operation Success",
                                                             "Customer Receipts",
                                                             MessageBoxButtons.OK,
                                                             MessageBoxIcon.Information,
                                                             MessageBoxDefaultButton.Button1,
                                                             MessageBoxOptions.ServiceNotification
                                                         );

                TempData["SuccessMessage"] = "Department created successfully!";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = departmentDTO.ErrorMessages;

                return View(departmentDTO);
            }
        }


        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var departmentDTO = await _channelService.FindDepartmentAsync(id, GetServiceHeader());

            return View(departmentDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, DepartmentDTO departmentBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateDepartmentAsync(departmentBindingModel, GetServiceHeader()); 
                MessageBox.Show(
                                                              "Operation Success",
                                                              "Customer Receipts",
                                                              MessageBoxButtons.OK,
                                                              MessageBoxIcon.Information,
                                                              MessageBoxDefaultButton.Button1,
                                                              MessageBoxOptions.ServiceNotification
                                                          );

                TempData["SuccessMessage"] = "Department updated successfully!"; 

                return RedirectToAction("Index");
            }
            else
            {
                return View(departmentBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetDepartmentsAsync()
        {
            var departmentsDTOs = await _channelService.FindDepartmentsAsync(GetServiceHeader());

            return Json(departmentsDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
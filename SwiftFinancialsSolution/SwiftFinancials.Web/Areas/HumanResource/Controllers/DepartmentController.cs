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

            bool sortDescending = jQueryDataTablesModel.sSortDir_.First() == "desc";
            var sortedColumns = jQueryDataTablesModel.GetSortedColumns().Select(s => s.PropertyName).ToList();

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;

            var pageCollectionInfo = await _channelService.FindDepartmentsByFilterInPageAsync(
                jQueryDataTablesModel.sSearch,
                0,
                int.MaxValue,
                GetServiceHeader()
            );


            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(departmentDTO => departmentDTO.CreatedDate)
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
                items: new List<DepartmentDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
        );
        }
        //else
        //{
        //    return this.DataTablesJson(
        //        items: new List<DepartmentDTO>(),
        //        totalRecords: totalRecordCount,
        //        totalDisplayRecords: searchRecordCount,
        //        sEcho: jQueryDataTablesModel.sEcho
        //    );
        //}




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
                try
                {
                    await _channelService.AddDepartmentAsync(departmentDTO, GetServiceHeader());

                    MessageBox.Show(
                        "Operation Success: Department created successfully!",
                        "Customer Receipts",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.ServiceNotification
                    );

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);

                    MessageBox.Show(
                        "An error occurred while creating the department. Please try again.",
                        "Department Management",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.ServiceNotification
                    );
                }
            }
            else
            {
                string errorMessages = string.Join(Environment.NewLine, departmentDTO.ErrorMessages);

                MessageBox.Show(
                    $"Validation Errors: {errorMessages}",
                    "Department Management",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification
                );
            }

            return View(departmentDTO);
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
                try
                {
                    await _channelService.UpdateDepartmentAsync(departmentBindingModel, GetServiceHeader());

                    MessageBox.Show(
                        "Operation Success: Department updated successfully!",
                        "Department Management",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.ServiceNotification
                    );


                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);

                    MessageBox.Show(
                        "An error occurred while updating the department. Please try again.",
                        "Update Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.ServiceNotification
                    );
                }
            }
            else
            {
                MessageBox.Show(
                    "Validation Errors: Please correct the errors and try again.",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification
                );
            }

            return View(departmentBindingModel);
        }


        [HttpGet]
        public async Task<JsonResult> GetDepartmentsAsync()
        {
            var departmentsDTOs = await _channelService.FindDepartmentsAsync(GetServiceHeader());

            return Json(departmentsDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
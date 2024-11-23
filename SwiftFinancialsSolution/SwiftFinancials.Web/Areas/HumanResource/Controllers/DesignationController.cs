using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Windows.Forms;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System.Windows.Forms;

namespace SwiftFinancials.Web.Areas.HumanResource.Controllers
{
    public class DesignationController : MasterController
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

            var pageCollectionInfo = await _channelService.FindDesignationsByFilterInPageAsync(
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
                    items: new List<DesignationDTO>(),
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
        }
        

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var designationDTO = await _channelService.FindDesignationAsync(id, GetServiceHeader());

            return View(designationDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(DesignationDTO designationDTO)
        {
            designationDTO.ValidateAll();

            if (!designationDTO.HasErrors)
            {
                await _channelService.AddDesignationAsync(designationDTO, GetServiceHeader());
                MessageBox.Show(
                                                             "Operation Success",
                                                             "Customer Receipts",
                                                             MessageBoxButtons.OK,
                                                             MessageBoxIcon.Information,
                                                             MessageBoxDefaultButton.Button1,
                                                             MessageBoxOptions.ServiceNotification
                                                         );
                TempData["SuccessMessage"] = "Designation Created Succeesfully";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = designationDTO.ErrorMessages;

                return View(designationDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {

            ViewBag.TransactionTypeSelectList = GetSystemTransactionTypeList(string.Empty);
            
            await ServeNavigationMenus();

            var designationDTO = await _channelService.FindDesignationAsync(id, GetServiceHeader());

            var user = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());

            designationDTO.ActiveUser = user.UserName;

            return View(designationDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, DesignationDTO designationBindingModel)
        {
            try
            {
                designationBindingModel.ValidateAll();
                if (!designationBindingModel.HasErrors)
                {
                    // Check if TransactionThresholds are not null and try updating
                    if (designationBindingModel.TransactionThresholds != null)
                    {
                        var updateDesignationResult = await _channelService.UpdateDesignationAsync(designationBindingModel, GetServiceHeader());

                        if (updateDesignationResult)
                        {
                            var updateThresholdResult = await _channelService.UpdateTransactionThresholdCollectionByDesignationIdAsync(
                                designationBindingModel.Id,
                                designationBindingModel.TransactionThresholds,
                                GetServiceHeader()
                            );

                            if (updateThresholdResult)
                            {
                                // Show success message when both operations succeed
                                MessageBox.Show("Designation and transaction thresholds updated successfully.",
                                                "Success",
                                                MessageBoxButtons.OK,
                                                MessageBoxIcon.Information,
                                                MessageBoxDefaultButton.Button1,
                                                MessageBoxOptions.ServiceNotification);
                                return RedirectToAction("Index");
                            }
                            else
                            {
                                // Show error message if updating thresholds fails
                                MessageBox.Show("Failed to update transaction thresholds.",
                                                "Error",
                                                MessageBoxButtons.OK,
                                                MessageBoxIcon.Error,
                                                MessageBoxDefaultButton.Button1,
                                                MessageBoxOptions.ServiceNotification);
                            }
                        }
                        else
                        {
                            // Show error message if updating designation fails
                            MessageBox.Show("Failed to update designation.",
                                            "Error",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Error,
                                            MessageBoxDefaultButton.Button1,
                                            MessageBoxOptions.ServiceNotification);
                        }
                    }
                    else
                    {
                        // If TransactionThresholds are null, try updating designation without them
                        var updateDesignationResult = await _channelService.UpdateDesignationAsync(designationBindingModel, GetServiceHeader());

                        if (updateDesignationResult)
                        {
                            // Show success message when designation is updated
                            MessageBox.Show("Designation updated successfully without transaction thresholds.",
                                            "Success",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Information,
                                            MessageBoxDefaultButton.Button1,
                                            MessageBoxOptions.ServiceNotification);
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            // Show error message if updating designation fails
                            MessageBox.Show("Failed to update designation without thresholds.",
                                            "Error",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Error,
                                            MessageBoxDefaultButton.Button1,
                                            MessageBoxOptions.ServiceNotification);
                        }
                    }
                }
                else
                {
                    // If ModelState is invalid, return the view with validation errors
                    ViewBag.TransactionTypeSelectList = GetSystemTransactionTypeList(string.Empty);
                    return View(designationBindingModel);
                }
            }
            catch (Exception ex)
            {
                // General error handling: show the error message
                MessageBox.Show($"An unexpected error occurred: {ex.Message}",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error,
                                MessageBoxDefaultButton.Button1,
                                MessageBoxOptions.ServiceNotification);
            }

            // In case of any failures, return the view with the model
            ViewBag.TransactionTypeSelectList = GetSystemTransactionTypeList(string.Empty);
            return View(designationBindingModel);
        }


        [HttpGet]
        public async Task<JsonResult> GetDesignationsAsync()
      {
            var designationsDTOs = await _channelService.FindDesignationsAsync(GetServiceHeader());

            return Json(designationsDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
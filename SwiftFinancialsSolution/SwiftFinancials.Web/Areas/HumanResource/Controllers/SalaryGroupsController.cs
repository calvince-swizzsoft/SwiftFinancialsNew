using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System.Windows.Forms;
using Newtonsoft.Json;


namespace SwiftFinancials.Web.Areas.HumanResource.Controllers
{
    public class SalaryGroupsController : MasterController
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


            var pageCollectionInfo = await _channelService.FindSalaryGroupsByFilterInPageAsync(
                jQueryDataTablesModel.sSearch,
                0,
                int.MaxValue,
                GetServiceHeader()
            );

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(salaryGroupDTO => salaryGroupDTO.CreatedDate)
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
                items: new List<SalaryGroupDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
        );
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            // Fetch the salary group details
            var salaryGroupDTO = await _channelService.FindSalaryGroupAsync(id, GetServiceHeader());

            // Fetch a single entry and convert it to a list
            var salaryGroupEntry = await _channelService.FindSalaryGroupEntriesBySalaryGroupIdAsync(id, GetServiceHeader());

            // Store data in ViewBag
            ViewBag.SalaryGroupEntries = salaryGroupEntry; // For individual entries


            // Populate ViewBag data for dropdowns
            ViewBag.ValueTypeSelectList = GetChargeTypeSelectList(string.Empty);
            ViewBag.RoundingTypeSelectList = GetRoundingTypeSelectList(string.Empty);

            return View(salaryGroupDTO);
        }




        [HttpGet]
        public async Task<ActionResult> GetCSalaryHeadDetails(Guid salaryHeadId)
        {
            try
            {
                var salaryHead = await _channelService.FindSalaryHeadAsync(salaryHeadId, GetServiceHeader());


                if (salaryHead == null)
                {
                    return Json(new { success = false, message = "SavingsProduct not found." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        SalaryHeadDescription = salaryHead.Description,
                        SalaryHeadId = salaryHead.Id,
                        SalaryHeadIsOneOff = salaryHead.IsOneOff,
                        SalaryHeadCustomerAccountTypeTargetProductId = salaryHead.CustomerAccountTypeTargetProductId,
                        SalaryHeadCustomerAccountTypeTargetProductCode = salaryHead.CustomerAccountTypeTargetProductCode,
                        SalaryHeadCustomerAccountTypeProductCode = salaryHead.CustomerAccountTypeProductCode,
                        SalaryHeadCategoryDescription = salaryHead.CategoryDescription,
                        SalaryHeadChartOfAccountId = salaryHead.ChartOfAccountId,
                        SalaryHeadTypeDescription = salaryHead.TypeDescription,







                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the saving Product details." }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public ActionResult SaveEntries(SalaryGroupDTO salaryGroupDTO)
        {
            if (salaryGroupDTO == null || salaryGroupDTO.SalaryGroupEntries == null || !salaryGroupDTO.SalaryGroupEntries.Any())
            {
                return new HttpStatusCodeResult(400, "No entries provided.");
            }

            // Store the serialized data in TempData
            TempData["SalaryGroupEntries"] = JsonConvert.SerializeObject(salaryGroupDTO.SalaryGroupEntries);

            return Json(new { Message = "Entries saved successfully!" });
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            ViewBag.ValueTypeSelectList = GetChargeTypeSelectList(string.Empty);
            ViewBag.RoundingTypeSelectList = GetRoundingTypeSelectList(string.Empty);
            ViewBag.SalaryGroupsDTO = null;

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> UpdateTempData(List<SalaryGroupEntryDTO> salaryGroupEntries)
        {
            await ServeNavigationMenus();

            if (salaryGroupEntries == null || salaryGroupEntries.Count == 0)
            {
                return Json(new { success = false, message = "Entries list is empty." });
            }

            // Store entries in TempData
            TempData["Entries"] = salaryGroupEntries;
            TempData.Keep("Entries");

            return Json(new { success = true, message = "Entries updated successfully." });
        }


        [HttpPost]
        public async Task<ActionResult> Create(SalaryGroupDTO salaryGroupDTO)
        {
            var tempEntries = new List<SalaryGroupEntryDTO>();
            try
            {
                // Retrieve entries from TempData
                if (TempData["Entries"] != null)
                {
                    tempEntries = TempData["Entries"] as List<SalaryGroupEntryDTO>;
                }
                else
                {
                    MessageBox.Show("No entries were found in TempData.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return View(salaryGroupDTO);
                }

                salaryGroupDTO.ValidateAll();

                if (!salaryGroupDTO.HasErrors)
                {
                    var salaryGroupEntries = new ObservableCollection<SalaryGroupEntryDTO>(tempEntries);

                    var salaryGroup = await _channelService.AddSalaryGroupAsync(salaryGroupDTO, GetServiceHeader());

                    await _channelService.UpdateSalaryGroupEntriesBySalaryGroupIdAsync(salaryGroup.Id, salaryGroupEntries, GetServiceHeader());

                    return RedirectToAction("Index");
                }
                else
                {
                    MessageBox.Show("Validation errors were found in the submitted data.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                // Handle errors
                ViewBag.RoundingTypeSelectList = GetRoundingTypeSelectList(salaryGroupDTO.ToString());
                ViewBag.ValueTypeSelectList = GetChargeTypeSelectList(salaryGroupDTO.ToString());

                return View(salaryGroupDTO);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return View(salaryGroupDTO); // Return the view with the current data in case of errors
            }
        }






        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var salaryGroupDTO = await _channelService.FindSalaryGroupAsync(id, GetServiceHeader());

            ViewBag.ValueTypeSelectList = GetChargeTypeSelectList(string.Empty);
            ViewBag.RoundingTypeSelectList = GetRoundingTypeSelectList(string.Empty);

            return View(salaryGroupDTO);


        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, SalaryGroupDTO salaryGroupDTO)
        {
            try
            {
                // Retrieve entries from TempData if necessary
                var tempEntries = TempData["Entries"] as List<SalaryGroupEntryDTO>;  // Get the list of entries from TempData

                if (tempEntries != null)
                {
                    // Convert the List to ObservableCollection
                    var salaryGroupEntries = new ObservableCollection<SalaryGroupEntryDTO>(tempEntries);

                    // Merge them with the current salary group entries and convert back to List
                    salaryGroupDTO.SalaryGroupEntries = salaryGroupDTO.SalaryGroupEntries
                        .Concat(salaryGroupEntries)
                        .ToList(); // Convert the merged collection to a List
                }

                // Validate the salary group data
                salaryGroupDTO.ValidateAll();

                if (!salaryGroupDTO.HasErrors)
                {
                    // Update the SalaryGroup
                    await _channelService.UpdateSalaryGroupAsync(salaryGroupDTO, GetServiceHeader());

                    // Update SalaryGroupEntries
                    if (salaryGroupDTO.SalaryGroupEntries.Any())
                    {
                        var salaryGroupEntries = new ObservableCollection<SalaryGroupEntryDTO>(salaryGroupDTO.SalaryGroupEntries);

                        // Call the method to update SalaryGroupEntries
                        bool updateSuccessful = await _channelService.UpdateSalaryGroupEntriesBySalaryGroupIdAsync(id, salaryGroupEntries, GetServiceHeader());

                        if (updateSuccessful)
                        {
                            // Success message or redirect
                            MessageBox.Show("Salary group entries updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Failed to update salary group entries.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }

                    // Set ViewBag for dropdowns or other data needed for the view
                    ViewBag.RoundingTypeSelectList = GetRoundingTypeSelectList(salaryGroupDTO.ToString());
                    ViewBag.ValueTypeSelectList = GetChargeTypeSelectList(salaryGroupDTO.ToString());

                    // Redirect to the Index after a successful update
                    return RedirectToAction("Index");
                }
                else
                {
                    // Handle errors if validation failed
                    var errorMessages = salaryGroupDTO.ErrorMessages;

                    ViewBag.RoundingTypeSelectList = GetRoundingTypeSelectList(salaryGroupDTO.ToString());
                    ViewBag.ValueTypeSelectList = GetChargeTypeSelectList(salaryGroupDTO.ToString());

                    // Return the view with the error messages
                    return View(salaryGroupDTO);
                }
            }
            catch (Exception ex)
            {
                // Handle any unexpected errors
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return View(salaryGroupDTO); // Return the view with the current data in case of errors
            }
        }


        [HttpGet]
        public async Task<JsonResult> GetSalaryHeadsAsync(Guid id)
        {
            var salaryHeadsDTO = await _channelService.FindSalaryHeadAsync(id, GetServiceHeader());

            return Json(salaryHeadsDTO, JsonRequestBehavior.AllowGet);
        }
    }
}




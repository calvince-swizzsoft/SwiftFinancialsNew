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

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindSalaryGroupsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(salaryGroupDTO => salaryGroupDTO.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<SalaryGroupDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
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
                var savingsProduct = await _channelService.FindSalaryHeadAsync(salaryHeadId, GetServiceHeader());
               

                if (savingsProduct == null)
                {
                    return Json(new { success = false, message = "SavingsProduct not found." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        SalaryHeadDescription = savingsProduct.Description,
                        SalaryHeadId = savingsProduct.Id,
                        SalaryHeadIsOneOff = savingsProduct.IsOneOff,
                        SalaryHeadCustomerAccountTypeTargetProductId = savingsProduct.CustomerAccountTypeTargetProductId,
                        SalaryHeadCustomerAccountTypeTargetProductCode = savingsProduct.CustomerAccountTypeTargetProductCode,
                        SalaryHeadCustomerAccountTypeProductCode = savingsProduct.CustomerAccountTypeProductCode,
                        SalaryHeadCategoryDescription = savingsProduct.CategoryDescription,
                        SalaryHeadChartOfAccountId = savingsProduct. ChartOfAccountId,
                        SalaryHeadTypeDescription = savingsProduct.TypeDescription,







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
        public async Task<ActionResult> UpdateTempData(SalaryGroupDTO entries)
        {
            if (entries == null)
            {
                return Json(new { success = false, message = "Entries list is empty." });
            }

            TempData["Entries"] = entries;
            TempData.Keep("Entries");

            return Json(new { success = true, message = "Entries updated successfully." });
        }


        [HttpPost]
        public async Task<ActionResult> Create(SalaryGroupDTO salaryGroupDTO)
        {
            try
            {
                // Retrieve entries from TempData
                var tempEntries = TempData["Entries"] as SalaryGroupDTO;

                if (tempEntries == null)
                {
                    MessageBox.Show("No entries were found in TempData.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                salaryGroupDTO.ValidateAll();

                if (!salaryGroupDTO.HasErrors)
                {
                    var tempSalaryGroupEntries = TempData["SalaryGroupEntryDTO"] as ObservableCollection<SalaryGroupEntryDTO>
                                                  ?? new ObservableCollection<SalaryGroupEntryDTO>();

                    // Save SalaryGroup
                    var salaryGroup = await _channelService.AddSalaryGroupAsync(salaryGroupDTO, GetServiceHeader());

                    if (salaryGroup != null)
                    {
                        var salaryGroupEntries = new ObservableCollection<SalaryGroupEntryDTO>();

                        // Combine entries
                        foreach (var salaryGroupEntry in salaryGroupDTO.SalaryGroupEntries.Concat(tempSalaryGroupEntries))
                        {
                            salaryGroupEntry.SalaryGroupId = salaryGroup.Id;
                            salaryGroupEntry.SalaryGroupDescription = salaryGroup.Description;

                            salaryGroupEntries.Add(salaryGroupEntry);
                        }

                        if (salaryGroupEntries.Any())
                        {
                            await _channelService.UpdateSalaryGroupEntriesBySalaryGroupIdAsync(salaryGroup.Id, salaryGroupEntries, GetServiceHeader());
                        }

                        MessageBox.Show("Salary group and entries were successfully created.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to save the salary group.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    // Redirect to Index after successful creation
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

            salaryGroupDTO.ValidateAll();
            if (!salaryGroupDTO.HasErrors)
            {
                await _channelService.UpdateSalaryGroupAsync(salaryGroupDTO, GetServiceHeader());

                ViewBag.RoundingTypeSelectList = GetRoundingTypeSelectList(salaryGroupDTO.ToString());
                ViewBag.ValueTypeSelectList = GetChargeTypeSelectList(salaryGroupDTO.ToString());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = salaryGroupDTO.ErrorMessages;

                ViewBag.RoundingTypeSelectList = GetRoundingTypeSelectList(salaryGroupDTO.ToString());
                ViewBag.ValueTypeSelectList = GetChargeTypeSelectList(salaryGroupDTO.ToString());

                return View(salaryGroupDTO);
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




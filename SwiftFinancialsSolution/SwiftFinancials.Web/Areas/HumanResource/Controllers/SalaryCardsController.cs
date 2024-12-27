using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System.Globalization;
using System.Windows.Forms;
using System.Collections.ObjectModel;


namespace SwiftFinancials.Web.Areas.HumanResource.Controllers
{
    public class SalaryCardsController : MasterController
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

            var pageCollectionInfo = await _channelService.FindSalaryCardsByFilterInPageAsync(
                jQueryDataTablesModel.sSearch,
                0,
                int.MaxValue,
                GetServiceHeader()
            );

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(salaryCardDTO => salaryCardDTO.CreatedDate)
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
                items: new List<SalaryCardDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
        );
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            // Find the SalaryCard
            var salaryCardDTO = await _channelService.FindSalaryCardAsync(id, GetServiceHeader());
            if (salaryCardDTO == null)
            {
                MessageBox.Show(
                    "Salary Card not found!",
                    "Salary Card",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification
                );
                return RedirectToAction("Index");
            }

            // Store the SalaryGroupId in TempData
            if (salaryCardDTO.SalaryGroupId != Guid.Empty)
            {
                TempData["SalaryGroupId"] = salaryCardDTO.SalaryGroupId;

                // Find the SalaryGroupEntries associated with the SalaryGroupId
                var salaryGroupEntries = await _channelService.FindSalaryGroupEntriesBySalaryGroupIdAsync(salaryCardDTO.SalaryGroupId, GetServiceHeader());

                if (salaryGroupEntries == null || !salaryGroupEntries.Any())
                {
                    MessageBox.Show(
                        "No Salary Group Entries found for the provided Salary Group ID!",
                        "Salary Group Entry",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.ServiceNotification
                    );
                }
                else
                {
                    ViewBag.SalaryGroupEntries = salaryGroupEntries;
                }
            }

            // Return the SalaryCard to the view
            return View(salaryCardDTO);
        }

        [HttpGet]
        public async Task<ActionResult> GetEmployeeDetails(Guid employeeId)
        {
            try
            {
                var employee = await _channelService.FindEmployeeAsync(employeeId, GetServiceHeader());

                if (employee == null)
                {
                    return Json(new { success = false, message = "Employee not found." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        EmployeeCustomerFullName = employee.Customer.FullName,
                        EmployeeId = employee.Id,
                        EmployeeBloodGroupDescription = employee.BloodGroupDescription,
                        EmployeeNationalHospitalInsuranceFundNumber = employee.NationalHospitalInsuranceFundNumber,
                        EmployeeNationalSocialSecurityFundNumber = employee.NationalSocialSecurityFundNumber,
                        EmployeeCustomerPersonalIdentificationNumber = employee.CustomerPersonalIdentificationNumber,
                        EmployeeEmployeeTypeCategoryDescription = employee.EmployeeTypeCategoryDescription,
                        EmployeeEmployeeTypeDescription = employee.EmployeeTypeDescription,
                        EmployeeDepartmentDescription = employee.DepartmentDescription,
                        EmployeeDepartmentId = employee.DepartmentId,
                        EmployeeDesignationDescription = employee.DesignationDescription,
                        EmployeeDesignationId = employee.DesignationId,
                        EmployeeBranchDescription = employee.BranchDescription,
                        EmployeeBranchId = employee.BranchId,
                        EmployeeCustomerIndividualGenderDescription = employee.CustomerIndividualGenderDescription,
                        EmployeeCustomerIndividualPayrollNumbers = employee.CustomerIndividualPayrollNumbers,
                        EmployeeEmployeeTypeId = employee.EmployeeTypeId,
                        EmployeeEmployeeTypeChartOfAccountId = employee.EmployeeTypeChartOfAccountId,
                        EmployeeCustomerAddressAddressLine1 = employee.CustomerAddressAddressLine1,
                        EmployeeCustomerAddressAddressLine2 = employee.CustomerAddressAddressLine2,
                        EmployeeCustomerAddressCity = employee.CustomerAddressCity,
                        EmployeeCustomerAddressEmail = employee.CustomerAddressEmail,
                        EmployeeCustomerAddressLandLine = employee.CustomerAddressLandLine,
                        EmployeeCustomerAddressMobileLine = employee.CustomerAddressMobileLine,
                        EmployeeCustomerAddressPostalCode = employee.CustomerAddressPostalCode,
                        EmployeeCustomerAddressStreet = employee.CustomerAddressStreet,
                        EmployeeCustomerId = employee.CustomerId,
                        EmployeeCustomerIndividualFirstName = employee.CustomerIndividualFirstName,
                        EmployeeCustomerIndividualGender = employee.CustomerIndividualGender,
                        EmployeeCustomerIndividualIdentityCardNumber = employee.CustomerIndividualIdentityCardNumber,






                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the Employee details." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public async Task<ActionResult> GetSalaryGroupDetails(Guid salaryGroupId)
        {
            try
            {
                var salaryGroup= await _channelService.FindSalaryGroupAsync(salaryGroupId, GetServiceHeader());

                if (salaryGroup == null)
                {
                    return Json(new { success = false, message = "Salary Group not found." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        SalaryGroupDescription = salaryGroup.Description,
                        SalaryGroupId = salaryGroup.Id,
                        





                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the SalaryGroupdetails." }, JsonRequestBehavior.AllowGet);
            }
        }
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.ValueTypeSelectList = GetChargeTypeSelectList(string.Empty);
            ViewBag.RoundingTypeSelectList = GetRoundingTypeSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(SalaryCardDTO salaryCardDTO)
        {
            salaryCardDTO.ValidateAll();

            if (!salaryCardDTO.HasErrors)
            {
                try
                {
                    var salaryCard = await _channelService.AddSalaryCardAsync(salaryCardDTO, GetServiceHeader());

                    if (salaryCard == null) 
                    {
                        TempData["AlertMessage"] = "A salary card already exists for the selected employee.";
                        TempData["AlertType"] = "warning";

                        return View(salaryCardDTO);
                    }

                    TempData["AlertMessage"] = "Salary card created successfully!";
                    TempData["AlertType"] = "success";

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);

                    TempData["AlertMessage"] = "An unexpected error occurred while processing the request.";
                    TempData["AlertType"] = "error";

                    return View(salaryCardDTO);
                }
            }
            else
            {
                TempData["AlertMessage"] = "There were validation errors. Please correct them and try again!";
                TempData["AlertType"] = "error";

                return View(salaryCardDTO);
            }
        }


        //[HttpGet]
        //public async Task<ActionResult> GetSalaryGroupEntryDetails(Guid salaryGroupId)
        //{
        //    try
        //    {
        //        var salaryGroupEntries = await _channelService.FindSalaryGroupEntriesBySalaryGroupIdAsync(salaryGroupId, GetServiceHeader());

        //        if (salaryGroupEntries == null || !salaryGroupEntries.Any())
        //        {
        //            return Json(new { success = false, message = "Salary Group entries not found." }, JsonRequestBehavior.AllowGet);
        //        }

        //        // Populate ViewBag
        //        ViewBag.SalaryGroupEntries = salaryGroupEntries;

        //        return PartialView("_SalaryGroupDetailsPartial", salaryGroupEntries); // Use a partial view to render the details dynamically.
        //    }
        //    catch (Exception ex)
        //    {
        //        // Handle error appropriately
        //        return Json(new { success = false, message = "An error occurred while fetching the Salary Group details." }, JsonRequestBehavior.AllowGet);
        //    }
        //}



        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            // Find the SalaryCard
            var salaryCardDTO = await _channelService.FindSalaryCardAsync(id, GetServiceHeader());
            if (salaryCardDTO == null)
            {
                MessageBox.Show(
                    "Salary Card not found!",
                    "Salary Card",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification
                );
                return RedirectToAction("Index");
            }

            // Store the SalaryGroupId in TempData
            if (salaryCardDTO.SalaryGroupId != Guid.Empty)
            {
                TempData["SalaryGroupId"] = salaryCardDTO.SalaryGroupId;

                // Find the SalaryGroupEntries associated with the SalaryGroupId
                var salaryGroupEntries = await _channelService.FindSalaryGroupEntriesBySalaryGroupIdAsync(salaryCardDTO.SalaryGroupId, GetServiceHeader());

                if (salaryGroupEntries == null || !salaryGroupEntries.Any())
                {
                    MessageBox.Show(
                        "No Salary Group Entries found for the provided Salary Group ID!",
                        "Salary Group Entry",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.ServiceNotification
                    );
                }
                else
                {
                    ViewBag.SalaryGroupEntries = salaryGroupEntries;
                }
            }

            // Return the SalaryCard to the view
            return View(salaryCardDTO);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, SalaryCardDTO salaryCardDTO)
        {
            // Validate the SalaryCard DTO (make sure to handle errors)
            salaryCardDTO.ValidateAll();

            if (salaryCardDTO.HasErrors)
            {
                // If there are validation errors, return the view with the error messages
                TempData["AlertMessage"] = "There are validation errors. Please correct them and try again.";
                TempData["AlertType"] = "error";
                return View(salaryCardDTO);
            }

            // Retrieve the SalaryGroupId from TempData (if set)
            if (TempData["SalaryGroupId"] != null)
            {
                salaryCardDTO.SalaryGroupId = (Guid)TempData["SalaryGroupId"];
            }

            // Check if a SalaryGroupId is provided (this is critical)
            if (salaryCardDTO.SalaryGroupId == Guid.Empty)
            {
                ModelState.AddModelError("SalaryGroupId", "Salary Group is required.");
                TempData["AlertMessage"] = "Salary Group is required.";
                TempData["AlertType"] = "error";
                return View(salaryCardDTO);
            }

            try
            {
                // Update the SalaryCard with the selected SalaryGroup
                var updateSuccess = await _channelService.UpdateSalaryCardAsync(salaryCardDTO, GetServiceHeader());

                if (updateSuccess)
                {
                    TempData["AlertMessage"] = "Salary Group updated successfully!";
                    TempData["AlertType"] = "success";
                    return RedirectToAction("Index");
                }
                else
                {
                    // Handle unsuccessful update
                    TempData["AlertMessage"] = "There was an issue updating the Salary Group.";
                    TempData["AlertType"] = "error";
                    return View(salaryCardDTO);
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during the update
                Console.WriteLine(ex.Message);
                TempData["AlertMessage"] = "An error occurred while updating the SalaryCard. Please try again.";
                TempData["AlertType"] = "error";
                return View(salaryCardDTO);
            }
        }







        [HttpPost]
        public async Task<ActionResult> UpdateSalaryCardEntry(SalaryCardEntryDTO salaryCardEntryDTO)
        {
            if (salaryCardEntryDTO == null)
            {
                ModelState.AddModelError("", "Invalid salary card entry data.");
                return View("SalaryDetails"); // Replace "SalaryDetails" with your actual view name
            }

            var serviceHeader = GetServiceHeader(); // Helper to get service header
            var result = await _channelService.UpdateSalaryCardEntryAsync(salaryCardEntryDTO, serviceHeader);

            if (result)
            {
                // Reload the updated data for the view using the provided method
                var updatedEntries = await _channelService.FindSalaryCardEntriesBySalaryCardIdAsync(salaryCardEntryDTO.SalaryCardId, serviceHeader);
                ViewBag.SalaryGroupEntries = updatedEntries;

                ViewBag.Message = "Salary Head updated successfully.";
                return View("SalaryDetails"); // Replace with your actual view name
            }
            else
            {
                ModelState.AddModelError("", "Failed to update Salary Head.");
                return View("SalaryDetails"); // Replace with your actual view name
            }
        }


        [HttpPost]
        public async Task<ActionResult> ResetSalaryCardEntries(SalaryCardDTO salaryCardDTO)
        {
            if (salaryCardDTO == null)
            {
                ModelState.AddModelError("", "Invalid salary card data.");
                return View("SalaryDetails"); // Replace with your actual view name
            }

            var serviceHeader = GetServiceHeader(); // Helper to get service header
            var result = await _channelService.ResetSalaryCardEntriesAsync(salaryCardDTO, serviceHeader);

            if (result)
            {
                // Reload the updated data for the view using the provided method
                var updatedEntries = await _channelService.FindSalaryCardEntriesBySalaryCardIdAsync(salaryCardDTO.Id, serviceHeader);
                ViewBag.SalaryGroupEntries = updatedEntries;

                ViewBag.Message = "Salary details have been reset.";
                return View("SalaryDetails"); // Replace with your actual view name
            }
            else
            {
                ModelState.AddModelError("", "Failed to reset Salary details.");
                return View("SalaryDetails"); // Replace with your actual view name
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
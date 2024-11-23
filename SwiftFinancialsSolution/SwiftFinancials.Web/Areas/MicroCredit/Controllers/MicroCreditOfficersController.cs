using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.MicroCreditModule;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System.Diagnostics;
using System.Windows.Forms;



namespace SwiftFinancials.Web.Areas.MicroCredit.Controllers
{
    public class MicroCreditOfficersController : MasterController
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

            var pageCollectionInfo = await _channelService.FindMicroCreditOfficersByFilterInPageAsync(
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
                    items: new List<MicroCreditOfficerDTO>(),
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var microCreditOfficerDTO = await _channelService.FindMicroCreditOfficerAsync(id, GetServiceHeader());
            if (microCreditOfficerDTO == null)
            {
                TempData["ErrorMessage"] = "MicroCredit Officer not found.";
                return RedirectToAction("Index");
            }

            return View(microCreditOfficerDTO);
        }


        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            

            return View();
        }

        [HttpGet]
        public async Task<ActionResult> GetCustomerDetails(Guid employeeId)
        {
            try
            {
                var employee = await _channelService.FindEmployeeAsync(employeeId, GetServiceHeader());

                if (employee == null)
                {
                    return Json(new { success = false, message = "Customer not found." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        EmployeeCustomerFullName = employee.Customer.FullName,
                        EmployeeId = employee.Id,
                        EmployeeCustomerIndividualNationalityDescription = employee.CustomerIndividualNationalityDescription,
                        EmployeeCustomerIndividualIdentityCardNumber = employee.CustomerIndividualIdentityCardNumber,
                        EmployeeCustomerIndividualGenderDescription = employee.CustomerIndividualGenderDescription,
                        EmployeeCustomerIndividualMaritalStatusDescription = employee.CustomerIndividualMaritalStatusDescription,
                        EmployeeCustomerIndividualPayrollNumbers = employee.CustomerIndividualPayrollNumbers,
                        EmployeeDesignationDescription = employee.DesignationDescription,
                        EmployeeDesignationId = employee.DesignationId,
                        EmployeeBranchDescription = employee.BranchDescription,
                        EmployeeBranchId = employee.BranchId,
                        EmployeeCustomerAddressAddressLine1 = employee.CustomerAddressAddressLine1,
                        EmployeeCustomerAddressAddressLine2 = employee.CustomerAddressAddressLine2,
                        EmployeeCustomerAddressCity = employee.CustomerAddressCity,
                        EmployeeCustomerAddressEmail = employee.CustomerAddressEmail,
                        EmployeeCustomerAddressLandLine = employee.CustomerAddressLandLine,
                        EmployeeCustomerAddressMobileLine = employee.CustomerAddressMobileLine,
                        EmployeeCustomerAddressStreet = employee.CustomerAddressStreet,
                        EmployeeCustomerIndividualFirstName = employee.CustomerIndividualFirstName,
                        EmployeeCustomerIndividualLastName = employee.CustomerIndividualLastName,
                        EmployeeCustomerPersonalIdentificationNumber = employee.CustomerPersonalIdentificationNumber,
                        EmployeeDepartmentId = employee.DepartmentId,
                        EmployeeCustomerId = employee.CustomerId,
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the customer details." }, JsonRequestBehavior.AllowGet);
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(MicroCreditOfficerDTO microCreditOfficerDTO)
        {
            if (!ModelState.IsValid)
            {
                return View(microCreditOfficerDTO);
            }

            try
            {
                var existingOfficers = await _channelService.FindMicroCreditOfficerAsync(microCreditOfficerDTO.EmployeeId); // Assuming the service provides this method

                if (existingOfficers != null)
                {
                    ViewBag.Message = "Sorry, but the selected employee already exists as a microcredit officer!";
                    ViewBag.IsSuccess = false;
                    return View(microCreditOfficerDTO);
                }

                var serviceHeader = GetServiceHeader();
                var createdOfficer = await _channelService.AddMicroCreditOfficerAsync(microCreditOfficerDTO, serviceHeader);

                if (createdOfficer != null)
                {
                    TempData["SuccessMessage"] = "Micro Credit Officer created successfully!";
                    return View("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to create the Micro Credit Officer. Please try again.";
                    return View("Index");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An error occurred: " + ex.Message;
                ViewBag.IsSuccess = false;
                return View("Index");
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var microCreditOfficerDTO = await _channelService.FindMicroCreditOfficerAsync(id, GetServiceHeader());

            return View(microCreditOfficerDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, MicroCreditOfficerDTO microCreditOfficerDTO)
        {
            try
            {
                bool updateSuccess = await _channelService.UpdateMicroCreditOfficerAsync(microCreditOfficerDTO, GetServiceHeader());

                if (updateSuccess)
                {
                    TempData["SuccessMessage"] = "Micro-credit Officer updated successfully.";
                    return View("Index"); 
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update the Micro-credit Officer.";
                    return View("Index");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An error occurred: " + ex.Message;
                ViewBag.IsSuccess = false;
                return View("Index");
            }

            
        }




    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System.IO; 
using System.Web; 


namespace SwiftFinancials.Web.Areas.HumanResource.Controllers
{
    public class EmployeeDocumentsController : MasterController
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

            var pageCollectionInfo = await _channelService.FindEmployeeDocumentsByFilterInPageAsync(
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
                    items: new List<EmployeeDocumentDTO>(),
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
        }
        
        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var employeeDocumentDTO = await _channelService.FindEmployeeDocumentAsync(id, GetServiceHeader());

            return View(employeeDocumentDTO);
        }


        [HttpGet]
        public async Task<ActionResult> GetCustomerDetails(Guid employeeId)
        {
            try
            {
                var customer = await _channelService.FindEmployeeAsync(employeeId, GetServiceHeader());

                if (customer == null)
                {
                    return Json(new { success = false, message = "Customer not found." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        EmployeeCustomerFullName = customer.Customer.FullName,
                        EmployeeCustomerId = customer.CustomerId,
                        EmployeeId = customer.Id,
                        EmployeeBloodGroupDescription = customer. BloodGroupDescription,
                        EmployeeNationalHospitalInsuranceFundNumber = customer.NationalHospitalInsuranceFundNumber,
                        EmployeeNationalSocialSecurityFundNumber = customer.NationalSocialSecurityFundNumber,
                        EmployeeCustomerPersonalIdentificationNumber = customer.CustomerPersonalIdentificationNumber,
                        EmployeeEmployeeTypeCategoryDescription = customer.EmployeeTypeCategoryDescription,
                        EmployeeEmployeeTypeDescription = customer. EmployeeTypeDescription,
                        EmployeeDepartmentDescription = customer. DepartmentDescription,
                        EmployeeDepartmentId = customer.DepartmentId,
                        EmployeeDesignationDescription = customer. DesignationDescription,
                        EmployeeDesignationId = customer.DesignationId,
                        EmployeeBranchDescription = customer.BranchDescription,
                        EmployeeBranchId = customer. BranchId,
                        EmployeeCustomerIndividualGenderDescription = customer.CustomerIndividualGenderDescription,
                        EmployeeCustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers,
                        EmployeeEmployeeTypeId = customer. EmployeeTypeId,
                        EmployeeEmployeeTypeChartOfAccountId = customer.EmployeeTypeChartOfAccountId,





                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the customer details." }, JsonRequestBehavior.AllowGet);
            }
        }
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(EmployeeDocumentDTO employeeDocumentDTO, HttpPostedFileBase uploadedFile)
        {
            if (uploadedFile != null && uploadedFile.ContentLength > 0)
            {
                employeeDocumentDTO.FileMIMEType = uploadedFile.ContentType;

                using (var memoryStream = new MemoryStream())
                {
                    uploadedFile.InputStream.CopyTo(memoryStream);
                    employeeDocumentDTO.FileBuffer = memoryStream.ToArray();
                }

                employeeDocumentDTO.FileName = Path.GetFileName(uploadedFile.FileName);
            }
            else
            {
                ModelState.AddModelError("FileName", "Please upload a valid file.");
            }

            employeeDocumentDTO.ValidateAll();

            if (!employeeDocumentDTO.HasErrors)
            {
                await _channelService.AddEmployeeDocumentAsync(employeeDocumentDTO, GetServiceHeader());

                TempData["SuccessMessage"] = "Employee Document created successfully!";
                return RedirectToAction("Index");
            }

            foreach (var error in employeeDocumentDTO.ErrorMessages)
            {
                ModelState.AddModelError("", error);
            }

            return View(employeeDocumentDTO);
        }



        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var employeeDocumentDTO = await _channelService.FindEmployeeDocumentAsync(id, GetServiceHeader());

            return View(employeeDocumentDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(EmployeeDocumentDTO employeeDocumentDTO, HttpPostedFileBase uploadedFile)
        {
            if (uploadedFile != null && uploadedFile.ContentLength > 0)
            {
                employeeDocumentDTO.FileMIMEType = uploadedFile.ContentType;

                using (var memoryStream = new MemoryStream())
                {
                    uploadedFile.InputStream.CopyTo(memoryStream);
                    employeeDocumentDTO.FileBuffer = memoryStream.ToArray();
                }

                employeeDocumentDTO.FileName = Path.GetFileName(uploadedFile.FileName);
            }

            employeeDocumentDTO.ValidateAll();

            if (!employeeDocumentDTO.HasErrors)
            {
                bool success = await _channelService.UpdateEmployeeDocumentAsync(employeeDocumentDTO, GetServiceHeader());

                if (success)
                {
                    TempData["SuccessMessage"] = "Employee document updated successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "An error occurred while updating the document.");
                }
            }

            foreach (var error in employeeDocumentDTO.ErrorMessages)
            {
                ModelState.AddModelError("", error);
            }

            return View(employeeDocumentDTO);
        }



    }
}
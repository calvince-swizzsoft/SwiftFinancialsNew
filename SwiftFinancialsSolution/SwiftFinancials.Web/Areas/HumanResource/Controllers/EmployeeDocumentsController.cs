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
using System.Windows.Forms;


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

            bool sortDescending = jQueryDataTablesModel.sSortDir_.First() == "desc";
            var sortedColumns = jQueryDataTablesModel.GetSortedColumns().Select(s => s.PropertyName).ToList();

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;

            var pageCollectionInfo = await _channelService.FindEmployeeDocumentsByFilterInPageAsync(
                jQueryDataTablesModel.sSearch,
                0,
                int.MaxValue,
                GetServiceHeader() 
            );

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(employeeDocumentDTO => employeeDocumentDTO.CreatedDate)
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
               items: new List<EmployeeDocumentDTO>(),
               totalRecords: totalRecordCount,
               totalDisplayRecords: searchRecordCount,
               sEcho: jQueryDataTablesModel.sEcho
               );
        }
        
        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var employeeDocumentDTO = await _channelService.FindEmployeeDocumentAsync(id, GetServiceHeader());

            return View(employeeDocumentDTO);
        }
        


        //public async Task<ActionResult> Details(Guid id)
        //{
        //    await ServeNavigationMenus();

        //    var employeeDocumentDTO = await _channelService.FindEmployeeDocumentAsync(id, GetServiceHeader());

        //    // Add file information to the ViewBag or Model for rendering in the view
        //    if (employeeDocumentDTO != null)
        //    {
        //        ViewBag.FileDownloadUrl = Url.Action("Download", "EmployeeDocument", new { id = employeeDocumentDTO.Id });
        //    }

        //    return View(employeeDocumentDTO);
        //}

        //public async Task<ActionResult> Download(Guid id)
        //{
        //    var employeeDocumentDTO = await _channelService.FindEmployeeDocumentAsync(id, GetServiceHeader());

        //    if (employeeDocumentDTO != null && employeeDocumentDTO.FileBuffer != null)
        //    {
        //        return File(
        //            employeeDocumentDTO.FileBuffer,
        //            employeeDocumentDTO.FileMIMEType,
        //            employeeDocumentDTO.FileName
        //        );
        //    }

        //    // If file is not found, show an error or redirect
        //    return HttpNotFound("File not found or has been deleted.");
        //}


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
        public async Task<ActionResult> Create(EmployeeDocumentDTO employeeDocumentDTO, HttpPostedFileBase[] uploadedFiles)
        {
            if (uploadedFiles != null && uploadedFiles.Any(f => f != null && f.ContentLength > 0))
            {
                foreach (var file in uploadedFiles)
                {
                    if (file != null && file.ContentLength > 0)
                    {
                        var individualDocument = employeeDocumentDTO;

                        individualDocument.FileMIMEType = file.ContentType; 
                        individualDocument.FileName = Path.GetFileName(file.FileName);

                        using (var memoryStream = new MemoryStream())
                        {
                            file.InputStream.CopyTo(memoryStream);
                            individualDocument.FileBuffer = memoryStream.ToArray();
                        }

                        individualDocument.ValidateAll();

                        if (!individualDocument.HasErrors)
                        {
                            await _channelService.AddEmployeeDocumentAsync(individualDocument, GetServiceHeader());
                        }
                        else
                        {
                            foreach (var error in individualDocument.ErrorMessages)
                            {
                                ModelState.AddModelError("", error);
                            }
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("FileName", "One or more files are invalid.");
                    }
                }
                TempData["Message"] = "Operation Success: Employee Documents Created Successfully!";
                TempData["MessageType"] = "Success";

               

                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("FileName", "Please upload at least one valid file.");
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
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, EmployeeDocumentDTO employeeDocumentDTO, HttpPostedFileBase uploadedFile)
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
                TempData["Message"] = "Operation Success: Employee Documents updated Successfully!";
                TempData["MessageType"] = "Success";
                

                if (success)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Message"] = "An error occurred while updating the document.";
                    TempData["MessageType"] = "error";
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
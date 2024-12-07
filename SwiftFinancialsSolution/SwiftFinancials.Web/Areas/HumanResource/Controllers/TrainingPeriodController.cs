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
    public class TrainingPeriodController : MasterController
    {

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Index(JQueryDataTablesModel jQueryDataTablesModel, DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue)
                startDate = DateTime.MinValue;

            if (!endDate.HasValue)
                endDate = DateTime.MaxValue;

            int totalRecordCount = 0;
            int searchRecordCount = 0;

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;

            bool sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc";


            try
            {
                var pageCollectionInfo = await _channelService.FindTrainingPeriodsFilterInPageAsync(
                    startDate.Value,
                    endDate.Value,
                    jQueryDataTablesModel.sSearch,
                    0,
                    int.MaxValue,
                    GetServiceHeader()
                );

                if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
                {

                    var sortedData = pageCollectionInfo.PageCollection
                        .OrderByDescending(trainingPeriodDTO => trainingPeriodDTO.CreatedDate)
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
                    items: new List<TrainingPeriodDTO>(),
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
            );
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var trainingPeriodDTO = await _channelService.FindTrainingPeriodAsync(id, GetServiceHeader());

            return View(trainingPeriodDTO);
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
                        EmployeeCustomerId = employee.CustomerId,
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
        public async Task<ActionResult> Create(TrainingPeriodDTO trainingPeriodDTO, HttpPostedFileBase[] uploadedFiles)
        {
            if (uploadedFiles != null && uploadedFiles.Any(f => f != null && f.ContentLength > 0))
            {
                foreach (var file in uploadedFiles)
                {
                    if (file != null && file.ContentLength > 0)
                    {
                        var individualDocument = trainingPeriodDTO;

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
                            await _channelService.AddNewTrainingPeriodAsync(trainingPeriodDTO, GetServiceHeader());
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

                MessageBox.Show(
                    "Operation Success",
                    "TrainigPeriod Documents",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification
                );

                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("FileName", "Please upload at least one valid file.");
            }

            return View(trainingPeriodDTO);
        }


        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var trainingPeriodDTO = await _channelService.FindTrainingPeriodAsync(id, GetServiceHeader());

            return View(trainingPeriodDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(TrainingPeriodDTO trainingPeriodDTO, HttpPostedFileBase[] uploadedFiles)
        {
            if (uploadedFiles != null && uploadedFiles.Any(f => f != null && f.ContentLength > 0))
            {
                foreach (var file in uploadedFiles)
                {
                    if (file != null && file.ContentLength > 0)
                    {
                        var individualDocument = trainingPeriodDTO;

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
                            await _channelService.UpdateTrainingPeriodAsync(trainingPeriodDTO, GetServiceHeader());
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

                MessageBox.Show(
                    "TrainigPeriod Updated Successfuly",
                    "TrainigPeriod Documents",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification
                );

                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("FileName", "Please upload at least one valid file.");
            }

            return View(trainingPeriodDTO);
        }

        [HttpGet]
        public async Task<JsonResult> GetTrainingPeriodsAsync()
        {
            var trainingPeriodDTOs = await _channelService.FindCompaniesAsync(GetServiceHeader());

            return Json(trainingPeriodDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
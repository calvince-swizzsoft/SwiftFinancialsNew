using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.AdministrationModule;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Windows.Forms;

namespace SwiftFinancials.Web.Areas.HumanResource.Controllers
{
    public class SalaryController : MasterController
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

            bool includeProductDescription = true;

            var pageCollectionInfo = await _channelService.FindSalaryHeadsByFilterInPageAsync(
                jQueryDataTablesModel.sSearch,
                0,
                int.MaxValue,
                includeProductDescription,
                GetServiceHeader()
            );

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(salaryHeadDTO => salaryHeadDTO.CreatedDate)
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
                items: new List<SalaryHeadDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
        );
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var salaryHeadDTO = await _channelService.FindSalaryHeadAsync(id, GetServiceHeader());

            return View(salaryHeadDTO);
        }

        [HttpGet]
        public async Task<ActionResult> GetCSavingProductDetails(Guid savingsProductId)
        {
            try
            {
                var savingsProduct = await _channelService.FindSavingsProductAsync(savingsProductId, GetServiceHeader());
               

                if (savingsProduct == null)
                {
                    return Json(new { success = false, message = "SavingsProduct not found." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        ProductDescription = savingsProduct.Description,
                        CustomerAccountTypeTargetProductId = savingsProduct.Id,
                        CustomerAccountTypeProductCodeDescription = savingsProduct.Code,
                        ProductChartOfAccountId = savingsProduct.ChartOfAccountId,
                        ProductChartOfAccountCode = savingsProduct.ChartOfAccountAccountCode,
                        ProductChartOfAccountName = savingsProduct.ChartOfAccountAccountName,
                        CustomerAccountTypeProductCode = savingsProduct.Code,
                        CustomerAccountTypeTargetProductCode = savingsProduct.Code,
                        ChartOfAccountCostCenterId = savingsProduct.ChartOfAccountCostCenterId,
                        ChartOfAccountCostCenterDescription = savingsProduct.ChartOfAccountCostCenterDescription,








                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the saving Product details." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public async Task<ActionResult> GetChartOfAccountDetails(Guid chartOfAccountId)
        {
            try
            {
                var chartOfAccount = await _channelService.FindChartOfAccountAsync(chartOfAccountId, GetServiceHeader());
                


                if (chartOfAccount == null)
                {
                    return Json(new { success = false, message = "ChartOfAccount not found." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        ChartOfAccountAccountName = chartOfAccount.AccountName,
                        ChartOfAccountId = chartOfAccount.Id,
                        

                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the chartOfAccount details." }, JsonRequestBehavior.AllowGet);
            }
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.SalaryHeadTypeSelectList = GetSalaryHeadTypeSelectList(string.Empty);
            ViewBag.SalaryHeadCategorySelectList = GetSalaryHeadCategorySelectList(string.Empty);


            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Create(SalaryHeadDTO salaryHeadDTO)
        {
            salaryHeadDTO.ValidateAll();

            if (salaryHeadDTO.HasErrors)
            {
                TempData["AlertMessage"] = "Validation failed. Please correct the highlighted errors.";
                TempData["AlertType"] = "error";

                ViewBag.SalaryHeadTypeSelectList = GetSalaryHeadTypeSelectList(salaryHeadDTO.Type.ToString());
                return View(salaryHeadDTO);
            }

            try
            {
                var salaryHead = await _channelService.AddSalaryHeadAsync(salaryHeadDTO, GetServiceHeader());

                TempData["AlertMessage"] = "Salary Head added successfully.";
                TempData["AlertType"] = "success";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message); 

                TempData["AlertMessage"] = "An unexpected error occurred. Please try again.";
                TempData["AlertType"] = "error";

                ViewBag.SalaryHeadTypeSelectList = GetSalaryHeadTypeSelectList(salaryHeadDTO.Type.ToString());
                return View(salaryHeadDTO);
            }
        }


        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.SalaryHeadTypeSelectList = GetSalaryHeadTypeSelectList(string.Empty);

            var salaryHeadDTO = await _channelService.FindSalaryHeadAsync(id, GetServiceHeader());

            return View(salaryHeadDTO);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, SalaryHeadDTO salaryHeadDTO)
        {
            salaryHeadDTO.ValidateAll();

            if (salaryHeadDTO.HasErrors)
            {
                // Validation failed, show error message using SweetAlert
                TempData["AlertMessage"] = "Validation failed. Please correct the highlighted errors.";
                TempData["AlertType"] = "error";

                ViewBag.SalaryHeadTypeSelectList = GetSalaryHeadTypeSelectList(salaryHeadDTO.Type.ToString());
                return View(salaryHeadDTO);
            }

            try
            {
                var salaryHead = await _channelService.UpdateSalaryHeadAsync(salaryHeadDTO, GetServiceHeader());

                // Show success message using SweetAlert
                TempData["AlertMessage"] = "Salary Head Updated successfully.";
                TempData["AlertType"] = "success";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Log the error if necessary, and show error message using SweetAlert
                Console.WriteLine(ex.Message); // Log the exception

                TempData["AlertMessage"] = "An unexpected error occurred. Please try again.";
                TempData["AlertType"] = "error";

                // Repopulate dropdown and return the same view
                ViewBag.SalaryHeadTypeSelectList = GetSalaryHeadTypeSelectList(salaryHeadDTO.Type.ToString());
                return View(salaryHeadDTO);
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
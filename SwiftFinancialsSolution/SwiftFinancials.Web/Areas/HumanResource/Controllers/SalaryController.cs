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

            // Assuming you want to include product descriptions for now. Adjust as necessary.
            bool includeProductDescription = true;

            var pageCollectionInfo = await _channelService.FindSalaryHeadsByFilterInPageAsync(
                jQueryDataTablesModel.sSearch,
                pageIndex,
                pageSize,
                includeProductDescription,
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
                    items: new List<SalaryHeadDTO>(),
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var salaryHeadDTO = await _channelService.FindSalaryHeadAsync(id, GetServiceHeader());
            //var chartOfAccount = await _channelService.FindGeneralLedgerAsync(id, GetServiceHeader());

            return View(salaryHeadDTO);
        }

        [HttpGet]
        public async Task<ActionResult> GetCSavingProductDetails(Guid savingsProductId)
        {
            try
            {
                var savingsProduct = await _channelService.FindSavingsProductAsync(savingsProductId, GetServiceHeader());
                MessageBox.Show(
                                                             "Operation Success",
                                                             "Customer Receipts",
                                                             MessageBoxButtons.OK,
                                                             MessageBoxIcon.Information,
                                                             MessageBoxDefaultButton.Button1,
                                                             MessageBoxOptions.ServiceNotification
                                                         );


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
                MessageBox.Show(
                                                             "Operation Success",
                                                             "Customer Receipts",
                                                             MessageBoxButtons.OK,
                                                             MessageBoxIcon.Information,
                                                             MessageBoxDefaultButton.Button1,
                                                             MessageBoxOptions.ServiceNotification
                                                         );


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


            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Create(SalaryHeadDTO salaryHeadDTO)
        {
           
            salaryHeadDTO.ValidateAll();

            if (!salaryHeadDTO.HasErrors)
            {
                try
                {
                    var salaryHead = await _channelService.AddSalaryHeadAsync(salaryHeadDTO, GetServiceHeader());
                    MessageBox.Show(
                                                             "Operation Success",
                                                             "Customer Receipts",
                                                             MessageBoxButtons.OK,
                                                             MessageBoxIcon.Information,
                                                             MessageBoxDefaultButton.Button1,
                                                             MessageBoxOptions.ServiceNotification
                                                         );

                    TempData["AlertMessage"] = "Salary Head created successfully.";

                    ViewBag.SalaryHeadTypeSelectList = GetSalaryHeadTypeSelectList(salaryHeadDTO.Type.ToString());

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["AlertError"] = "An error occurred while creating the Salary Head: " + ex.Message;

                    ViewBag.SalaryHeadTypeSelectList = GetSalaryHeadTypeSelectList(salaryHeadDTO.Type.ToString());

                    return View(salaryHeadDTO);
                }
            }
            else
            {
                var errorMessages = salaryHeadDTO.ErrorMessages;
                TempData["AlertError"] = "Create Salary Head failed. Please review the form.";

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
        public async Task<ActionResult> Edit(SalaryHeadDTO salaryHeadDTO)
        {

            salaryHeadDTO.ValidateAll();

            if (!salaryHeadDTO.HasErrors)
            {
                try
                {
                    await _channelService.UpdateSalaryHeadAsync(salaryHeadDTO, GetServiceHeader());
                    MessageBox.Show(
                                                             "Operation Success",
                                                             "Customer Receipts",
                                                             MessageBoxButtons.OK,
                                                             MessageBoxIcon.Information,
                                                             MessageBoxDefaultButton.Button1,
                                                             MessageBoxOptions.ServiceNotification
                                                         );

                    TempData["AlertMessage"] = "Salary Head Edited Successfully";

                    ViewBag.SalaryHeadTypeSelectList = GetSalaryHeadTypeSelectList(salaryHeadDTO.Type.ToString());

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["AlertError"] = "An error occurred while updating the Salary Head: " + ex.Message;

                    ViewBag.SalaryHeadTypeSelectList = GetSalaryHeadTypeSelectList(salaryHeadDTO.Type.ToString());

                    return View(salaryHeadDTO);
                }
            }
            else
            {
                var errorMessages = salaryHeadDTO.ErrorMessages;
                TempData["AlertError"] = "Create Salary Head failed. Please review the form.";

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
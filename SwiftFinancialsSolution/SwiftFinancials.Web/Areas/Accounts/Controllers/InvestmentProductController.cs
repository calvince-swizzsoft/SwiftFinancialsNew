using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class InvestmentProductController : MasterController
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

            var pageCollectionInfo = await _channelService.FindInvestmentProductsByFilterInPageAsync(jQueryDataTablesModel.sSearch, 0, int.MaxValue, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(i => i.CreatedDate)
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
                items: new List<InvestmentProductDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var investmentProductDTO = await _channelService.FindInvestmentProductAsync(id, GetServiceHeader());

            var findParentProduct = await _channelService.FindInvestmentProductAsync((Guid)investmentProductDTO.ParentId, GetServiceHeader());
            investmentProductDTO.ParentChartOfAccountNameDescription = findParentProduct.Description;

            return View(investmentProductDTO);
        }


        public async Task<ActionResult> Parent(InvestmentProductDTO investmentProductDTO, Guid? id)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            var parentGL = await _channelService.FindChartOfAccountAsync(parseId, GetServiceHeader());

            if (parentGL != null)
            {
                investmentProductDTO.ParentId = parentGL.ParentId;
                investmentProductDTO.ParentChartOfAccountNameDescription = parentGL.ParentAccountName;
            }


            return View("Create", investmentProductDTO);
        }


        [HttpPost]
        public async Task<JsonResult> ParentChartOfAccountsIndex(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;

            var pageCollectionInfo = await _channelService.
                FindChartOfAccountsByFilterInPageAsync(jQueryDataTablesModel.sSearch, 0, int.MaxValue, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                var filteredData = pageCollectionInfo.PageCollection
                    .Where(c => c.AccountCategory != (int)ChartOfAccountCategory.HeaderAccount)
                    .ToList();

                var sortedData = filteredData
                    .OrderByDescending(c => c.CreatedDate)
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
                items: new List<ChartOfAccountDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }

        public async Task<ActionResult> ChartOfAccountLookUp(Guid? id, InvestmentProductDTO investmentProductDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var chartOfAccount = await _channelService.FindChartOfAccountAsync(parseId, GetServiceHeader());


            if (chartOfAccount != null)
            {
                investmentProductDTO.ChartOfAccountId = chartOfAccount.Id;
                investmentProductDTO.ChartOfAccountAccountName = chartOfAccount.AccountName;


                return Json(new
                {
                    success = true,
                    data = new
                    {
                        ChartOfAccountId = investmentProductDTO.ChartOfAccountId,
                        ChartOfAccountAccountName = investmentProductDTO.ChartOfAccountAccountName
                    }
                });
            }
            return Json(new { success = false, message = "Product Not Found!" });
        }


        public async Task<ActionResult> ParentProductLookUp(Guid? id, InvestmentProductDTO investmentProductDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var parentProduct = await _channelService.FindInvestmentProductAsync(parseId, GetServiceHeader());


            if (parentProduct != null)
            {
                investmentProductDTO.ParentId = parentProduct.Id;
                investmentProductDTO.ParentChartOfAccountNameDescription = parentProduct.Description;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        ParentId = investmentProductDTO.ParentId,
                        ParentChartOfAccountNameDescription = investmentProductDTO.ParentChartOfAccountNameDescription
                    }
                });
            }
            return Json(new { success = false, message = "Product Not Found!" });
        }


        public async Task<ActionResult> Create(Guid? id, InvestmentProductDTO investmentProductDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var parentGL = await _channelService.FindChartOfAccountAsync(parseId, GetServiceHeader());

            if (parentGL != null)
            {
                investmentProductDTO.ParentId = parentGL.ParentId;
                investmentProductDTO.ChartOfAccountAccountName = parentGL.ParentAccountName;
            }

            ViewBag.RecoveryPriority = GetRecoveryPrioritySelectList(string.Empty);

            return View(investmentProductDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(InvestmentProductDTO investmentProductDTO, string selectedItem)
        {
            if (string.IsNullOrEmpty(selectedItem))
            {
                TempData["SelectRecoveryPriority"] = "Recovery Priority Required!";
                return View(investmentProductDTO);
            }

            if (selectedItem == "0")
            {
                investmentProductDTO.Priority = 0;
            }
            else if (selectedItem == "1")
            {
                investmentProductDTO.Priority = 1;
            }
            else if (selectedItem == "2")
            {
                investmentProductDTO.Priority = 2;
            }
            else if (selectedItem == "3")
            {
                investmentProductDTO.Priority = 3;
            }

            investmentProductDTO.ValidateAll();

            if (!investmentProductDTO.HasErrors)
            {
                await _channelService.AddInvestmentProductAsync(investmentProductDTO, GetServiceHeader());

                TempData["AlertMessage"] = "Investment Product created successfully";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = investmentProductDTO.ErrorMessages;

                TempData["Error"] = "Failed to create Investments Product";

                ViewBag.RecoveryPrioritySelectList = GetRecoveryPrioritySelectList(investmentProductDTO.Priority.ToString());
                return View(investmentProductDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.RecoveryPrioritySelectList = GetRecoveryPrioritySelectList(string.Empty);
            var investmentProductDTO = await _channelService.FindInvestmentProductAsync(id, GetServiceHeader());

            return View(investmentProductDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, InvestmentProductDTO investmentProductBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateInvestmentProductAsync(investmentProductBindingModel, GetServiceHeader());

                TempData["Edit"] = "Edited Invetsments Product successfully";


                ViewBag.RecoveryPrioritySelectList = GetRecoveryPrioritySelectList(investmentProductBindingModel.Priority.ToString());
                return RedirectToAction("Index");
            }
            else
            {
                return View(investmentProductBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetInvestmentProductsAsync()
        {
            var investmentProductDTOs = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());

            return Json(investmentProductDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}

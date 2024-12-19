using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class InsurersController : MasterController
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

            var pageCollectionInfo = await _channelService.FindInsuranceCompaniesByFilterInPageAsync(
                jQueryDataTablesModel.sSearch,
                0,
                int.MaxValue,
                GetServiceHeader()
            );

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(loanCase => loanCase.CreatedDate)
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
                items: new List<InsuranceCompanyDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();
            var insuranceCompanyLinkageDTO = await _channelService.FindInsuranceCompanyAsync(id, GetServiceHeader());
            return View(insuranceCompanyLinkageDTO);
        }


        public async Task<ActionResult> ChartOfAccountLookUp(Guid? id, InsuranceCompanyDTO insuranceCompanyDTO)
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
                insuranceCompanyDTO.ChartOfAccountId = chartOfAccount.Id;
                insuranceCompanyDTO.ChartOfAccountAccountName = chartOfAccount.AccountName;


                return Json(new
                {
                    success = true,
                    data = new
                    {
                        ChartOfAccountId = insuranceCompanyDTO.ChartOfAccountId,
                        ChartOfAccountAccountName = insuranceCompanyDTO.ChartOfAccountAccountName
                    }
                });
            }
            return Json(new { success = false, message = "Product Not Found!" });
        }


        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();

        }


        [HttpPost]
        public async Task<ActionResult> Create(InsuranceCompanyDTO insuranceCompanyDTO)
        {
            await ServeNavigationMenus();

            insuranceCompanyDTO.ValidateAll();

            if (!insuranceCompanyDTO.HasErrors)
            {
                await _channelService.AddInsuranceCompanyAsync(insuranceCompanyDTO, GetServiceHeader());

                TempData["AlertMessage"] = "Insurance Company created successfully";

                return RedirectToAction("Index");
            }
            else
            {
                await ServeNavigationMenus();
                var errorMessages = insuranceCompanyDTO.ErrorMessages;
                string errorMessage = string.Join("\n", errorMessages.Where(msg => !string.IsNullOrWhiteSpace(msg)));

                TempData["CreateError"] = "Operation Failed: " + errorMessage;
                return View(insuranceCompanyDTO);
            }
        }



        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var insuranceCompanyDTO = await _channelService.FindInsuranceCompanyAsync(id, GetServiceHeader());

            return View(insuranceCompanyDTO);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, InsuranceCompanyDTO insuranceCompanyDTO)
        {
            await ServeNavigationMenus();
            if (!insuranceCompanyDTO.HasErrors)
            {
                await _channelService.UpdateInsuranceCompanyAsync(insuranceCompanyDTO, GetServiceHeader());

                TempData["edit"] = "Successfully updated Insurace Company ";

                return RedirectToAction("Index");
            }
            else
            {
                await ServeNavigationMenus();
                var errorMessages = insuranceCompanyDTO.ErrorMessages;
                string errorMessage = string.Join("\n", errorMessages.Where(msg => !string.IsNullOrWhiteSpace(msg)));

                TempData["editError"] = "Operation Failed: " + errorMessage;

                return View(insuranceCompanyDTO);
            }
        }
    }
}

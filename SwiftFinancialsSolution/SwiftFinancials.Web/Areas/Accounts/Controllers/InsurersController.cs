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

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc";
            var sortedColumns = jQueryDataTablesModel.GetSortedColumns().Select(s => s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindInsuranceCompaniesByFilterInPageAsync(
                jQueryDataTablesModel.sSearch,
                jQueryDataTablesModel.iDisplayStart,
                jQueryDataTablesModel.iDisplayLength,
                GetServiceHeader()
            );

            var data = new List<InsuranceCompanyDTO>();

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                var sortedPageCollection = sortAscending
                    ? pageCollectionInfo.PageCollection.OrderBy(bankLinkage => bankLinkage.CreatedDate)
                    : pageCollectionInfo.PageCollection.OrderByDescending(bankLinkage => bankLinkage.CreatedDate);

                data = sortedPageCollection.ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? data.Count
                    : totalRecordCount;
            }

            return Json(new
            {
                draw = jQueryDataTablesModel.sEcho,
                recordsTotal = totalRecordCount,
                recordsFiltered = searchRecordCount,
                data
            });
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();
            var insuranceCompanyLinkageDTO = await _channelService.FindInsuranceCompanyAsync(id, GetServiceHeader());
            return View(insuranceCompanyLinkageDTO);
        }



        public async Task<ActionResult> Create(Guid? id, InsuranceCompanyDTO insuranceCompanyDTO)
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

                Session["chartOfAccountId"] = insuranceCompanyDTO.ChartOfAccountId;
                Session["chartOfAccountName"] = insuranceCompanyDTO.ChartOfAccountAccountName;

                
            }

            return View(insuranceCompanyDTO);
            
        }


        [HttpPost]
        public async Task<ActionResult> Create(InsuranceCompanyDTO insuranceCompanyDTO)
        {
            insuranceCompanyDTO.ValidateAll();

            if (!insuranceCompanyDTO.HasErrors)
            {
                await _channelService.AddInsuranceCompanyAsync(insuranceCompanyDTO, GetServiceHeader());

                TempData["AlertMessage"] = "Insurance Company created successfully";

                TempData["RefreshPage"] = true;

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = insuranceCompanyDTO.ErrorMessages;

                TempData["RefreshPage"] = true;

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
            if (ModelState.IsValid)
            {
                await _channelService.UpdateInsuranceCompanyAsync(insuranceCompanyDTO, GetServiceHeader());

                TempData["edit"] = "Successfully updated Insurace Company ";

                return RedirectToAction("Index");
            }
            else
            {
                TempData["editError"] = "Failed to Update Insurace Company ";

                return View(insuranceCompanyDTO);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetInsuranceCompanyAsync()
        {
            var insuranceCompanyDTOs = await _channelService.FindInsuranceCompaniesAsync(GetServiceHeader());

            return Json(insuranceCompanyDTOs, JsonRequestBehavior.AllowGet);
        }



    }
}

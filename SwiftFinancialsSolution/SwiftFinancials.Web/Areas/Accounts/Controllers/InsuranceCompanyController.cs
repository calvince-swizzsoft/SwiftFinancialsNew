
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class InsuranceCompanyController : MasterController
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

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindInsuranceCompaniesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<InsuranceCompanyDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var insuranceCompanyDTO = await _channelService.FindInsuranceCompanyAsync(id, GetServiceHeader());

            return View(insuranceCompanyDTO);
        }
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(InsuranceCompanyDTO insuranceCompanyDTO)
        {
            insuranceCompanyDTO.ValidateAll();

            if (!insuranceCompanyDTO.HasErrors)
            {
                await _channelService.AddInsuranceCompanyAsync(insuranceCompanyDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = insuranceCompanyDTO.ErrorMessages;

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
        public async Task<ActionResult> Edit(Guid id, InsuranceCompanyDTO insuranceCompanyBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateInsuranceCompanyAsync(insuranceCompanyBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(insuranceCompanyBindingModel);
            }
        }

       /* [HttpGet]
        public async Task<JsonResult> GetInsuranceCompanysAsync()
        {
            var insuranceCompanyDTOs = await _channelService.FindInsuranceCompanysAsync(GetServiceHeader());

            return Json(insuranceCompanyDTOs, JsonRequestBehavior.AllowGet);
        }*/
    }
}

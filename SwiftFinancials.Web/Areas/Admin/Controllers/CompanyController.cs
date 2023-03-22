using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AdministrationModule;
using SwiftFinancials.Web.Attributes;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using SwiftFinancials.Presentation.Infrastructure.Util;

namespace SwiftFinancials.Web.Areas.Admin.Controllers
{
    [RoleBasedAccessControl]
    public class CompanyController : MasterController
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

            var pageCollectionInfo = await _channelService.FindCompaniesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CompanyDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var CompanyDTO = await _channelService.FindCompanyAsync(id, GetServiceHeader());

            return View(CompanyDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(CompanyBindingModel companyBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.AddCompanyAsync(companyBindingModel.MapTo<CompanyDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(companyBindingModel);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var CompanyDTO = await _channelService.FindCompanyAsync(id, GetServiceHeader());

            return View(CompanyDTO.MapTo<CompanyBindingModel>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, CompanyBindingModel companyBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateCompanyAsync(companyBindingModel.MapTo<CompanyDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(companyBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetCompaniesAsync()
        {
            var companiesDTOs = await _channelService.FindCompaniesAsync(GetServiceHeader());

            return Json(companiesDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
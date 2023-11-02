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

namespace SwiftFinancials.Web.Areas.HumanResource.Controllers
{
    public class SalaryCardsController : MasterController
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

            var pageCollectionInfo = await _channelService.FindSalaryCardsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<SalaryCardDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var salaryCardDTO = await _channelService.FindSalaryHeadAsync(id, GetServiceHeader());
            //var chartOfAccount = await _channelService.FindGeneralLedgerAsync(id, GetServiceHeader());

            return View(salaryCardDTO);
        }

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            //ViewBag.SalaryHeadTypeSelectList = GetSalaryHeadTypeSelectList(string.Empty);

            Guid parseId;
            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            //var savingproducts = await _channelService.FindSavingsProductAsync(parseId, GetServiceHeader());

            SalaryCardDTO salaryCardDTO = new SalaryCardDTO();

            //if (savingproducts != null)
            //{
            //    salaryHeadDTO.CustomerAccountTypeTargetProductId = savingproducts.Id;
            //    salaryHeadDTO.ProductDescription = savingproducts.Description;

            //}

            return View(salaryCardDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(SalaryCardDTO salaryCardDTO, Guid? id)
        {
            salaryCardDTO.ValidateAll();

            if (!salaryCardDTO.HasErrors)
            {
                var salarycard = await _channelService.AddSalaryCardAsync(salaryCardDTO, GetServiceHeader());

                //ViewBag.SalaryHeadTypeSelectList = GetSalaryHeadTypeSelectList(salaryCardDTO.Type.ToString());

                //await GetChartOfAccountsAsync(id);
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = salaryCardDTO.ErrorMessages;
                return View(salaryCardDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var salaryCardTO = await _channelService.FindSalaryCardAsync(id, GetServiceHeader());

            return View(salaryCardTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, SalaryCardDTO salaryCardDTO)
        {
            salaryCardDTO.ValidateAll();

            if (!salaryCardDTO.HasErrors)
            {
                await _channelService.UpdateSalaryCardAsync(salaryCardDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(salaryCardDTO);
            }
        }
    }
}
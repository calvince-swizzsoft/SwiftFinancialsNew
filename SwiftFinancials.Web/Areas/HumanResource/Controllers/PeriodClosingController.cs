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
    public class PeriodClosingsController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, DateTime startDate, DateTime endDate, int status, int index)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindSalaryPeriodsByFilterInPageAsync(status, startDate, endDate, "", jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<SalaryProcessingDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
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

            ViewBag.MonthTypeSelectList = GetMonthsAsync(string.Empty);
            ViewBag.EmployeeTypeSelectList = GetEmployeeCategorySelectList(string.Empty);

            Guid parseId;
            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            //var savingproducts = await _channelService.FindSavingsProductAsync(parseId, GetServiceHeader());

            SalaryProcessingDTO salaryPeriodDTO = new SalaryProcessingDTO();

            //if (savingproducts != null)
            //{
            //    salaryHeadDTO.CustomerAccountTypeTargetProductId = savingproducts.Id;
            //    salaryHeadDTO.ProductDescription = savingproducts.Description;

            //}

            return View(salaryPeriodDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(SalaryProcessingDTO salaryPeriodDTO, Guid? id)
        {
            salaryPeriodDTO.ValidateAll();

            if (!salaryPeriodDTO.HasErrors)
            {
                var salaryPeriod = await _channelService.AddSalaryPeriodAsync(salaryPeriodDTO, GetServiceHeader());

                ViewBag.MonthTypeSelectList = GetMonthsAsync(salaryPeriodDTO.Month.ToString());
                ViewBag.EmployeeTypeSelectList = GetEmployeeCategorySelectList(salaryPeriodDTO.EmployeeCategoryDescription.ToString());
                //ViewBag.SalaryHeadTypeSelectList = GetSalaryHeadTypeSelectList(salaryCardDTO.Type.ToString());

                //await GetChartOfAccountsAsync(id);
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = salaryPeriodDTO.ErrorMessages;
                return View(salaryPeriodDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var salaryPeriod = await _channelService.FindSalaryPeriodAsync(id, GetServiceHeader());

            return View(salaryPeriod);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, SalaryProcessingDTO salaryPeriodDTO)
        {
            salaryPeriodDTO.ValidateAll();

            if (!salaryPeriodDTO.HasErrors)
            {
                await _channelService.UpdateSalaryPeriodAsync(salaryPeriodDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(salaryPeriodDTO);
            }
        }
    }
}
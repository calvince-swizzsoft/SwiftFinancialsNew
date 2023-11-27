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
    public class SalaryPeriodsController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, int status, DateTime startDate, DateTime EndDate)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindSalaryPeriodsByFilterInPageAsync(status, startDate, EndDate, jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<SalaryPeriodDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var salaryPeriodDTO = await _channelService.FindSalaryPeriodAsync(id, GetServiceHeader());
            //var chartOfAccount = await _channelService.FindGeneralLedgerAsync(id, GetServiceHeader());

            return View(salaryPeriodDTO);
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

            PostingPeriodDTO postingPeriodDTO = new PostingPeriodDTO();
            var postingPeriod = await _channelService.FindPostingPeriodAsync(parseId, GetServiceHeader());

            if (postingPeriod != null)
            {
                postingPeriodDTO.Id = postingPeriod.Id;
                postingPeriodDTO.Description = postingPeriod.Description;
            }

            return View(postingPeriodDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(SalaryPeriodDTO salaryPeriodDTO, Guid? id)
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
        public async Task<ActionResult> Edit(Guid id, SalaryPeriodDTO salaryPeriodDTO)
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
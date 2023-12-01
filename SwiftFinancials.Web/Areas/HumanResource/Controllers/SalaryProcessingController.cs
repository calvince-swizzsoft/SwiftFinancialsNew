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
    public class SalaryProcessingController : MasterController
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
            else return this.DataTablesJson(items: new List<SalaryProcessingDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var salaryProcessingDTO = await _channelService.FindSalaryPeriodAsync(id, GetServiceHeader());

            return View(salaryProcessingDTO);
        }

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            //ViewBag.MonthTypeSelectList = GetMonthsAsync(string.Empty);
            //ViewBag.EmployeeTypeSelectList = GetEmployeeCategorySelectList(string.Empty);

            Guid parseId;
            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            //var savingproducts = await _channelService.FindSavingsProductAsync(parseId, GetServiceHeader());

            SalaryProcessingDTO salaryProcessingDTO = new SalaryProcessingDTO();

            //if (savingproducts != null)
            //{
            //    salaryHeadDTO.CustomerAccountTypeTargetProductId = savingproducts.Id;
            //    salaryHeadDTO.ProductDescription = savingproducts.Description;

            //}

            return View(salaryProcessingDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(SalaryProcessingDTO salaryProcessingDTO, Guid? id)
        {
            salaryProcessingDTO.ValidateAll();

            if (!salaryProcessingDTO.HasErrors)
            {
                var salaryPeriod = await _channelService.AddSalaryPeriodAsync(salaryProcessingDTO, GetServiceHeader());

                //ViewBag.MonthTypeSelectList = GetMonthsAsync(salaryProcessingDTO.Month.ToString());
                //ViewBag.EmployeeTypeSelectList = GetEmployeeCategorySelectList(salaryProcessingDTO.EmployeeCategoryDescription.ToString());
                //ViewBag.SalaryHeadTypeSelectList = GetSalaryHeadTypeSelectList(salaryCardDTO.Type.ToString());

                //await GetChartOfAccountsAsync(id);
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = salaryProcessingDTO.ErrorMessages;
                return View(salaryProcessingDTO);
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
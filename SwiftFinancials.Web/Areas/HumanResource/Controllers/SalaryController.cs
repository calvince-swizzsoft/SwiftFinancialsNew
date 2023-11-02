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

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindSalaryHeadsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, true, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<SalaryHeadDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var salaryHeadDTO = await _channelService.FindSalaryHeadAsync(id, GetServiceHeader());
            //var chartOfAccount = await _channelService.FindGeneralLedgerAsync(id, GetServiceHeader());

            return View(salaryHeadDTO);
        }

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            ViewBag.SalaryHeadTypeSelectList = GetSalaryHeadTypeSelectList(string.Empty);

            Guid parseId;
            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var savingproducts = await _channelService.FindSavingsProductAsync(parseId,GetServiceHeader());

            SalaryHeadDTO salaryHeadDTO = new SalaryHeadDTO();
            
            if (savingproducts != null)
            {
                salaryHeadDTO.CustomerAccountTypeTargetProductId = savingproducts.Id;
                salaryHeadDTO.ProductDescription = savingproducts.Description;

            }

            return View(salaryHeadDTO);
        }


        public async Task<ActionResult> GetChartOfAccountsAsync(Guid? id)
        {
            await ServeNavigationMenus();

            Guid parseId;
            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            ChartOfAccountDTO chartOfAccountDTO = new ChartOfAccountDTO();
            var chartOfAccounts = await _channelService.FindChartOfAccountAsync(parseId, GetServiceHeader());

            if(chartOfAccounts!=null)
            {
                chartOfAccountDTO.Id = chartOfAccounts.Id;
            }

            return View(chartOfAccountDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(SalaryHeadDTO salaryHeadDTO, Guid? id, ChartOfAccountDTO chartOfAccountDTO)
        {
            salaryHeadDTO.ValidateAll();
           // chartOfAccountDTO.ValidateAll();

            if (!salaryHeadDTO.HasErrors)
            {
                var salaryHead = await _channelService.AddSalaryHeadAsync(salaryHeadDTO, GetServiceHeader());

                ViewBag.SalaryHeadTypeSelectList = GetSalaryHeadTypeSelectList(salaryHeadDTO.Type.ToString());

                //await GetChartOfAccountsAsync(id);
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = salaryHeadDTO.ErrorMessages;
                ViewBag.SalaryHeadTypeSelectList = GetSalaryHeadTypeSelectList(salaryHeadDTO.Type.ToString());
                return View(salaryHeadDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var salaryHeadDTO = await _channelService.FindSalaryHeadAsync(id, GetServiceHeader());

            return View(salaryHeadDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, SalaryHeadDTO salaryHeadDTO)
        {
            salaryHeadDTO.ValidateAll();

            if (!salaryHeadDTO.HasErrors)
            {
                await _channelService.UpdateSalaryHeadAsync(salaryHeadDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(salaryHeadDTO);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetSalaryHeadsAsync(Guid id)
        {
            var salaryHeadsDTO = await _channelService.FindSalaryHeadAsync(id, GetServiceHeader());

            return Json(salaryHeadsDTO, JsonRequestBehavior.AllowGet);
        }

        //[HttpGet]
        //public async Task<ActionResult> GetChartOfAccountsAsync(Guid? id)
        //{
        //    await ServeNavigationMenus();

        //    Guid parseId;

        //    if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
        //    {
        //        return View();
        //    }

        //    ChartOfAccountDTO chartOfAccountDTO = new ChartOfAccountDTO();
        //    var chartOfAccounts = await _channelService.FindChartOfAccountAsync(parseId, GetServiceHeader());

        //    if (chartOfAccounts != null)
        //    {
        //        chartOfAccountDTO.AccountCode = Convert.ToInt32(chartOfAccounts.Id);
        //        chartOfAccountDTO.AccountName = chartOfAccounts.AccountName;
        //    }
        //    return View(chartOfAccountDTO);
        //}
    }
}
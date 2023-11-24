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
    public class SalaryGroupsController : MasterController
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

            var pageCollectionInfo = await _channelService.FindSalaryGroupsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<SalaryGroupDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var salaryGroupDTO = await _channelService.FindSalaryGroupAsync(id, GetServiceHeader());
            //var chartOfAccount = await _channelService.FindGeneralLedgerAsync(id, GetServiceHeader());

            return View(salaryGroupDTO);
        }

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            //ViewBag.SalaryHeadTypeSelectList = GetSalaryHeadTypeSelectList(string.Empty);
            ViewBag.ValueTypeSelectList = GetValueGroupTypeSelectList(string.Empty);
            ViewBag.RoundingTypeSelectList = GetRoundingTypeSelectList(string.Empty);

            Guid parseId;
            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var savingproducts = await _channelService.FindSavingsProductAsync(parseId, GetServiceHeader());

            SalaryGroupDTO salaryHeadDTO = new SalaryGroupDTO();

            if (savingproducts != null)
            {
                //salaryHeadDTO.CustomerAccountTypeTargetProductId = savingproducts.Id;
                //salaryHeadDTO.ProductDescription = savingproducts.Description;

            }

            return View(salaryHeadDTO);
        }


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
        //        chartOfAccountDTO.Id = chartOfAccounts.Id;
        //    }

        //    return View(chartOfAccountDTO);
        //}


        [HttpPost]
        public async Task<ActionResult> Create(SalaryGroupDTO salaryGroupDTO, Guid? id, SalaryGroupEntryDTO salaryGroupEntryDTO)
        {
            salaryGroupDTO.ValidateAll();

            if (!salaryGroupDTO.HasErrors)
            {
                var salaryHead = await _channelService.AddSalaryGroupAsync(salaryGroupDTO, GetServiceHeader());

                ViewBag.ValueTypeSelectList = GetValueGroupTypeSelectList(salaryGroupEntryDTO.ChargeType.ToString());
                ViewBag.RoundingTypeSelectList = GetRoundingTypeSelectList(salaryGroupEntryDTO.RoundingType.ToString());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = salaryGroupDTO.ErrorMessages;
                return View(salaryGroupDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var salaryGroupDTO = await _channelService.FindSalaryGroupAsync(id, GetServiceHeader());

            return View(salaryGroupDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, SalaryGroupDTO salaryGroupDTO)
        {
            salaryGroupDTO.ValidateAll();

            if (!salaryGroupDTO.HasErrors)
            {
                await _channelService.UpdateSalaryGroupAsync(salaryGroupDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(salaryGroupDTO);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetSalaryHeadsAsync(Guid id)
        {
            var salaryHeadsDTO = await _channelService.FindSalaryGroupAsync(id, GetServiceHeader());

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
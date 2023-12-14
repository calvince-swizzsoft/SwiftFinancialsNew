using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System.Windows;


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


                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(expensePayable => expensePayable.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<SalaryGroupDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var salaryGroupDTO = await _channelService.FindSalaryGroupAsync(id, GetServiceHeader());

            return View(salaryGroupDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            ViewBag.ValueTypeSelectList = GetChargeTypeSelectList(string.Empty);
            ViewBag.RoundingTypeSelectList = GetRoundingTypeSelectList(string.Empty);
            ViewBag.SalaryGroupsDTO = null;

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Add(SalaryGroupDTO salaryGroupDTO)
        {
            await ServeNavigationMenus();

            SalaryGroupEntryDTOs = TempData["SalaryGroupEntryDTO"] as ObservableCollection<SalaryGroupEntryDTO>;

            if (SalaryGroupEntryDTOs == null)
                SalaryGroupEntryDTOs = new ObservableCollection<SalaryGroupEntryDTO>();

            foreach (var salaryGroupEntryDTO in salaryGroupDTO.SalaryGroupEntries)
            {

                salaryGroupEntryDTO.SalaryGroupId = salaryGroupEntryDTO.SalaryGroupId;
                salaryGroupEntryDTO.SalaryHeadDescription = salaryGroupEntryDTO.SalaryHeadDescription;
                salaryGroupEntryDTO.ChargeType = salaryGroupEntryDTO.ChargeType;
                salaryGroupEntryDTO.MinimumValue = salaryGroupEntryDTO.MinimumValue;
                salaryGroupEntryDTO.RoundingType = salaryGroupEntryDTO.RoundingType;
            };

            TempData["SalaryGroupEntryDTO"] = SalaryGroupEntryDTOs;
            TempData["SalaryGroupDTO"] = salaryGroupDTO;

            ViewBag.SalaryGroupEntryDTOs = SalaryGroupEntryDTOs;

            ViewBag.RoundingTypeSelectList = GetRoundingTypeSelectList(salaryGroupDTO.ToString());
            ViewBag.ValueTypeSelectList = GetChargeTypeSelectList(salaryGroupDTO.ToString());
            return View("Create", salaryGroupDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(SalaryGroupDTO salaryGroupDTO)
        {
            salaryGroupDTO = TempData["salaryGroupDTO"] as SalaryGroupDTO;

            Guid salaryGroupSalaryHeadID = salaryGroupDTO.Id;

            salaryGroupDTO.ValidateAll();

            if (!salaryGroupDTO.HasErrors)
            {

                var salaryGroup = await _channelService.AddSalaryGroupAsync(salaryGroupDTO, GetServiceHeader());

                if (salaryGroup != null)
                {
                    var salaryGroupEntries = new ObservableCollection<SalaryGroupEntryDTO>();


                    foreach (var salaryGroupEntry in salaryGroupDTO.SalaryGroupEntries)
                    {
                        salaryGroupEntry.SalaryGroupId = salaryGroupEntry.SalaryGroupId;
                        salaryGroupEntry.SalaryHeadDescription = salaryGroupEntry.SalaryHeadDescription;
                        salaryGroupEntry.ChargeType = salaryGroupEntry.ChargeType;
                        salaryGroupEntry.MinimumValue = salaryGroupEntry.MinimumValue;
                        salaryGroupEntry.RoundingType = salaryGroupEntry.RoundingType;

                        salaryGroupEntry.SalaryGroupDescription = salaryGroup.Description;


                        salaryGroupEntries.Add(salaryGroupEntry);
                    };

                    if (salaryGroupEntries.Any())

                        await _channelService.UpdateSalaryGroupEntriesBySalaryGroupIdAsync(salaryGroup.Id, salaryGroupEntries, GetServiceHeader());
                }

                ViewBag.RoundingTypeSelectList = GetRoundingTypeSelectList(salaryGroupDTO.ToString());
                ViewBag.ValueTypeSelectList = GetChargeTypeSelectList(salaryGroupDTO.ToString());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = salaryGroupDTO.ErrorMessages;
                ViewBag.RoundingTypeSelectList = GetRoundingTypeSelectList(salaryGroupDTO.ToString());
                ViewBag.ValueTypeSelectList = GetChargeTypeSelectList(salaryGroupDTO.ToString());

                return View(salaryGroupDTO);
            }
        }


        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var salaryGroupDTO = await _channelService.FindSalaryGroupAsync(id, GetServiceHeader());

            ViewBag.ValueTypeSelectList = GetChargeTypeSelectList(string.Empty);
            ViewBag.RoundingTypeSelectList = GetRoundingTypeSelectList(string.Empty);

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

                ViewBag.RoundingTypeSelectList = GetRoundingTypeSelectList(salaryGroupDTO.ToString());
                ViewBag.ValueTypeSelectList = GetChargeTypeSelectList(salaryGroupDTO.ToString());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = salaryGroupDTO.ErrorMessages;

                ViewBag.RoundingTypeSelectList = GetRoundingTypeSelectList(salaryGroupDTO.ToString());
                ViewBag.ValueTypeSelectList = GetChargeTypeSelectList(salaryGroupDTO.ToString());

                return View(salaryGroupDTO);
            }
        }
    }
}




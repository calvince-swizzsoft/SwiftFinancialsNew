using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Areas.Accounts.Models;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class LevyController : MasterController
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

            var pageCollectionInfo = await _channelService.FindLeviesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;
                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(levy => levy.CreatedDate).ToList();
                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LevyDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var levyDTO = await _channelService.FindLevyAsync(id, GetServiceHeader());

            return View(levyDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);

            ViewBag.LevySplitDTOs = null;

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Add(LevyDTO levyDTO)
        {
            await ServeNavigationMenus();

            LevySplitDTOs = TempData["LevySplitDTO"] as ObservableCollection<LevySplitDTO>;

            if (LevySplitDTOs == null)
                LevySplitDTOs = new ObservableCollection<LevySplitDTO>();

            foreach (var levySplitDTO  in levyDTO.LevySplits)
            {
                levySplitDTO.Description = levySplitDTO.Description;
                levySplitDTO.ChartOfAccountId = levyDTO.Id;//Temporary 
                levySplitDTO.Percentage = levySplitDTO.Percentage;
                LevySplitDTOs.Add(levySplitDTO);
            };

            TempData["LevySplitDTO"] = LevySplitDTOs;

            TempData["LevyDTO"] = levyDTO;

            ViewBag.LevySplitDTOs = LevySplitDTOs;

            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(levyDTO.ChargeType.ToString());

            return View("Create", levyDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(LevyDTO levyDTO)
        {
            levyDTO = TempData["LevyDTO"] as LevyDTO;

            levyDTO.LevySplitsTotalPercentage = 100;

            Guid levySplitChartOfAccountId = levyDTO.Id;

            switch ((ChargeType)levyDTO.ChargeType)
            {
                case ChargeType.FixedAmount:
                    levyDTO.ChargeFixedAmount = (decimal)levyDTO.ChargeValue;
                    break;

                case ChargeType.Percentage:
                    levyDTO.ChargePercentage = (double)levyDTO.ChargeValue;
                    break;

                default:
                    break;
            }

            levyDTO.ValidateAll();

            if (!levyDTO.HasErrors)
            {
                var levy = await _channelService.AddLevyAsync(levyDTO, GetServiceHeader());
                TempData["SuccessMessage"] = "Create successful.";


                if (levy != null)
                {
                    var levySplits = new ObservableCollection<LevySplitDTO>();

                    foreach (var levySplitDTO in levyDTO.LevySplits)
                    {
                        levySplitDTO.LevyId = levy.Id;
                        levySplitDTO.Description = levySplitDTO.Description;
                        levySplitDTO.ChartOfAccountId = levySplitChartOfAccountId;
                        levySplitDTO.Percentage = levySplitDTO.Percentage;
                        levySplits.Add(levySplitDTO);
                    };

                    if (levySplits.Any())
                        await _channelService.UpdateLevySplitsByLevyIdAsync(levy.Id, levySplits, GetServiceHeader());
                }

                ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(levyDTO.ChargeType.ToString());

                ViewBag.LevySplits = await _channelService.FindLevySplitsByLevyIdAsync(levy.Id, GetServiceHeader());
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = levyDTO.ErrorMessages;
                ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(levyDTO.ChargeType.ToString());

                return View(levyDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var levyViewModel = await _channelService.FindLevyAsync(id, GetServiceHeader());

            return View(levyViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(LevyViewModel levyViewModel)
        {
            levyViewModel.ValidateAll();

            if (!levyViewModel.HasErrors)
            {
                var levyDTO = new LevyDTO()
                {
                    Description = levyViewModel.LevyDescription,
                    ChargeType = levyViewModel.ChargeType,
                    IsLocked = levyViewModel.LevyIsLocked,
                };

                var levy = await _channelService.AddLevyAsync(levyDTO, GetServiceHeader());

                if (levy != null)
                {
                    var levySplitDTO = new LevySplitDTO()
                    {
                        LevyId = levy.Id,
                        Description = levyViewModel.LevySplitDescription,
                        ChartOfAccountId = levyViewModel.LevySplitChartOfAccountId,
                        Percentage = levyViewModel.LevySplitPercentage,
                    };
                    var levySplits = new ObservableCollection<LevySplitDTO>();

                    levySplits.Add(levySplitDTO);

                    await _channelService.UpdateLevySplitsByLevyIdAsync(levy.Id, levySplits, GetServiceHeader());
                }

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = levyViewModel.ErrorMessages;

                return View(levyViewModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetLevysAsync()
        {
            var levyDTOs = await _channelService.FindLeviesAsync(GetServiceHeader());

            return Json(levyDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}

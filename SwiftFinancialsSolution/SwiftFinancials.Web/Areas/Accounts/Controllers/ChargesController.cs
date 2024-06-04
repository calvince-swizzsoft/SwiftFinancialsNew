using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class ChargesController : MasterController
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

            var pageCollectionInfo = await _channelService.FindCommissionsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CommissionDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var commissionDTO = await _channelService.FindCommissionAsync(id, GetServiceHeader());

            return View(commissionDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.chargeType = GetChargeTypeSelectList(string.Empty);

            return View();
        }


        public async Task<ActionResult> Search(Guid? id, CommissionDTO commissionDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var GLAccountChartOfAccount = await _channelService.FindChartOfAccountAsync(parseId, GetServiceHeader());

            if (GLAccountChartOfAccount != null)
            {
                //commissionDTO.chartOfAccount = GLAccountChartOfAccount;

                //Session["GLAccount"] = commissionDTO.chartOfAccount;
            }

            return View("Create", commissionDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Add(Guid? id, CommissionDTO commissionDTO)
        {
            await ServeNavigationMenus();

            ChargeSplitDTOs = TempData["ChargeSplitDTOs"] as ObservableCollection<CommissionSplitDTO>;

            if (ChargeSplitDTOs == null)
                ChargeSplitDTOs = new ObservableCollection<CommissionSplitDTO>();

            foreach (var chargeSplitDTO in commissionDTO.chargeSplit)
            {
                chargeSplitDTO.Description = chargeSplitDTO.Description;
                chargeSplitDTO.MaximumCharge = chargeSplitDTO.MaximumCharge;
                chargeSplitDTO.ChartOfAccountId = chargeSplitDTO.ChartOfAccountId;
                chargeSplitDTO.ChartOfAccountAccountName = chargeSplitDTO.ChartOfAccountAccountName;
                chargeSplitDTO.Percentage = chargeSplitDTO.Percentage;
                chargeSplitDTO.Leviable = chargeSplitDTO.Leviable;

                ChargeSplitDTOs.Add(chargeSplitDTO);
            };

            TempData["ChargeSplitDTOs"] = ChargeSplitDTOs;

            TempData["ChargeDTO"] = commissionDTO;

            ViewBag.ChargeSplitDTOs = ChargeSplitDTOs;

            return View("Create", commissionDTO);
        }



        [HttpPost]
        public async Task<ActionResult> Create(CommissionDTO commissionDTO)
        {
            commissionDTO.ValidateAll();

            if (!commissionDTO.ErrorMessages.Any())
            {
                var result = await _channelService.AddCommissionAsync(commissionDTO, GetServiceHeader());

                //await _channelService.UpdateCommissionSplitsByCommissionIdAsync(result.Id, commissionDTO.CommissionsplitSplits, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                TempData["CreateError"] = "Failed to Create Charge";

                ViewBag.chargeType = GetChargeTypeSelectList(commissionDTO.ChargeTypeDescription.ToString());

                return View(commissionDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var commissionDTO = await _channelService.FindCommissionAsync(id, GetServiceHeader());

            return View(commissionDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, CommissionDTO commissionDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateCommissionAsync(commissionDTO, GetServiceHeader());

                TempData["Edit"] = "Successfully Edit Charge";

                return RedirectToAction("Index");
            }
            else
            {
                TempData["EditError"] = "Failed to Edit Charge";

                return View(commissionDTO);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetCommissionsAsync()
        {
            var commissionsDTOs = await _channelService.FindCommissionsAsync(GetServiceHeader());

            return Json(commissionsDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
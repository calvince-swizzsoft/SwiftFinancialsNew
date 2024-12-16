using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
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
    public class UnPayReasonController : MasterController
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

            var pageCollectionInfo = await _channelService.FindUnPayReasonsByFilterInPageAsync(jQueryDataTablesModel.sSearch,
                0, int.MaxValue, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(loanCase => loanCase.CreatedDate)
                    .ToList();

                totalRecordCount = sortedData.Count;

                var paginatedData = sortedData
                    .Skip(jQueryDataTablesModel.iDisplayStart)
                    .Take(jQueryDataTablesModel.iDisplayLength)
                    .ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? sortedData.Count
                    : totalRecordCount;

                return this.DataTablesJson(
                    items: paginatedData,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }

            return this.DataTablesJson(
                items: new List<UnPayReasonDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }

        [HttpPost]
        public async Task<JsonResult> CommissionsIndex(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;

            var pageCollectionInfo = await _channelService.
                FindCommissionsByFilterInPageAsync
                (jQueryDataTablesModel.sSearch, 0, int.MaxValue, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(loanCase => loanCase.CreatedDate)
                    .ToList();

                totalRecordCount = sortedData.Count;

                var paginatedData = sortedData
                    .Skip(jQueryDataTablesModel.iDisplayStart)
                    .Take(jQueryDataTablesModel.iDisplayLength)
                    .ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? sortedData.Count
                    : totalRecordCount;

                return this.DataTablesJson(
                    items: paginatedData,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }

            return this.DataTablesJson(
                items: new List<CommissionDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var unPayReasonDTO = await _channelService.FindUnPayReasonAsync(id, GetServiceHeader());

            var applicableCharges = await _channelService.FindCommissionsByUnPayReasonIdAsync(id, GetServiceHeader());
            var commissions = await _channelService.FindCommissionsAsync(GetServiceHeader());

            var applicableChargeIds = new HashSet<Guid>(applicableCharges.Select(ac => ac.Id));

            ViewBag.CheckedStates = commissions.ToDictionary(
                c => c.Id,
                c => applicableChargeIds.Contains(c.Id) 
            );

            ViewBag.Commissions = commissions;
            return View(unPayReasonDTO);
        }


        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(UnPayReasonDTO unPayReasonDTO, string SelectedIds)
        {
            var commissions = new ObservableCollection<CommissionDTO>();

            var ids = SelectedIds.Split(',').Select(Guid.Parse).ToList();

            if (ids != null)
            {
                foreach (var commission in ids)
                {
                    var foundCommission = await _channelService.FindCommissionAsync(commission, GetServiceHeader());
                    commissions.Add(foundCommission);
                }
            }

            unPayReasonDTO.ValidateAll();

            if (!unPayReasonDTO.HasErrors)
            {
                var result = await _channelService.AddUnPayReasonAsync(unPayReasonDTO, GetServiceHeader());

                var myId = result.Id;

                if (result.ErrorMessageResult != null)
                {
                    TempData["ErrorMessageResult"] = "Operation Failed: " + result.ErrorMessageResult;
                    return View(unPayReasonDTO);
                }

                if (commissions != null)
                    await _channelService.UpdateCommissionsByUnPayReasonIdAsync(result.Id, commissions, GetServiceHeader());

                TempData["Create"] = "Unpay Reason Created Successfully";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = unPayReasonDTO.ErrorMessages;
                string errorMessage = string.Join("\n", errorMessages.Where(msg => !string.IsNullOrWhiteSpace(msg)));

                TempData["CreateError"] = "Operation Failed: " + errorMessage;

                return View(unPayReasonDTO);
            }
        }



        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var unPayReasonDTO = await _channelService.FindUnPayReasonAsync(id, GetServiceHeader());

            return View(unPayReasonDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(UnPayReasonDTO unpayReasonDTO, ObservableCollection<CommissionDTO> selectedRows)
        {
            if (!unpayReasonDTO.HasErrors)
            {
                await _channelService.UpdateUnPayReasonAsync(unpayReasonDTO, GetServiceHeader());

                await _channelService.UpdateCommissionsByUnPayReasonIdAsync(unpayReasonDTO.Id, selectedRows);

                TempData["Edit"] = "Unpay Reason Edited Successfully";

                return RedirectToAction("Index");
            }
            else
            {
                TempData["EditError"] = "Failed to Edit Unpay Reason";

                return View(unpayReasonDTO);
            }
        }


    }
}


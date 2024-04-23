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
    public class TiersController : MasterController
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

        [HttpPost]
        public async Task<ActionResult> Create(CommissionDTO commissionDTO)
        {
            commissionDTO.ValidateAll();

            if (!commissionDTO.ErrorMessages.Any())
            {
                await _channelService.AddCommissionAsync(commissionDTO.MapTo<CommissionDTO>(), GetServiceHeader());

                if (commissionDTO != null)
                {
                    //Update CommissionSplits

                    var commissionSplits = new ObservableCollection<CommissionSplitDTO>();

                    if (commissionDTO.CommissionSplits.Any())
                    {
                        foreach (var commissionSplitDTO in commissionDTO.CommissionSplits)
                        {
                            commissionSplitDTO.CommissionId = commissionDTO.Id;
                            commissionDTO.CommissionSplitChartOfAccountId = commissionSplitDTO.ChartOfAccountId;
                            commissionSplitDTO.ChartOfAccountCostCenterId = commissionDTO.CommissionSplit.ChartOfAccountId;
                            commissionSplits.Add(commissionSplitDTO);
                        }
                        if (commissionSplits.Any())
                            await _channelService.UpdateCommissionSplitsByCommissionIdAsync(commissionDTO.Id, commissionSplits, GetServiceHeader());
                    }

                    //Update CommissionLevies

                    //var commissionLevies = new ObservableCollection<CommissionLevyDTO>();

                    //if (commissionDTO.CommissionLevies.Any())
                    //{
                    //    foreach (var commissionLevyDTO in commissionDTO.CommissionLevies)
                    //    {
                    //        commissionLevyDTO.CommissionId = commissionDTO.Id;
                    //        commissionDTO.CommissionSplitChartOfAccountId = commissionDTO.Id;

                    //        commissionLevies.Add(commissionLevyDTO);
                    //    }

                    //    await _channelService.UpdateCommissionLeviesByCommissionIdAsync(commissionDTO.Id, commissionLevies, GetServiceHeader());
                    //}
                }

                return RedirectToAction("Index");
            }
            else
            {
                IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

                ViewBag.chargeType = GetChargeTypeSelectList(commissionDTO.ToString());

                TempData["Error"] = string.Join(",", allErrors);

                return View(commissionDTO);
            }
        }
    }
}
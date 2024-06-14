using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Newtonsoft.Json;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class Charges_LeviesController : MasterController
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


        public async Task<ActionResult> Levies(Guid id)
        {
            await ServeNavigationMenus();

            var commission = await _channelService.FindCommissionAsync(id, GetServiceHeader());

            var chargeLevies = await _channelService.FindLeviesByCommissionIdAsync(id, GetServiceHeader());

            ViewBag.chargeLevies = chargeLevies;

            return View(commission);
        }



        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.chargeType = GetChargeTypeSelectList(string.Empty);

            return View();
        }


        public async Task<ActionResult>search(Guid? id, CommissionLevyDTO commissionLevyDTO)
        {
            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var charges = await _channelService.FindCommissionAsync(parseId, GetServiceHeader());
            if(charges != null)
            {
                commissionLevyDTO.CommissionId = charges.Id;
                commissionLevyDTO.CommissionDescription = charges.Description;

                commissionLevyDTO.maximumCharge = charges.MaximumCharge;

                Session["chargeDescription"] = commissionLevyDTO.CommissionDescription;
                Session["chargeId"] = commissionLevyDTO.CommissionId;
                Session["maximumCharge"] = commissionLevyDTO.maximumCharge;
            }

            return View("Create", commissionLevyDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(CommissionLevyDTO commissionLevyDTO, ObservableCollection<CommissionLevyDTO> selectedRows)
        {
            commissionLevyDTO.CommissionId = (Guid)Session["chargeId"];
            commissionLevyDTO.CommissionDescription = Session["chargeDescription"].ToString();
            commissionLevyDTO.maximumCharge = Convert.ToDecimal(Session["maximumCharge"].ToString());

            commissionLevyDTO.ValidateAll();

            if (!commissionLevyDTO.ErrorMessages.Any())
            {
                await _channelService.UpdateCommissionLeviesByCommissionIdAsync(commissionLevyDTO.CommissionId, selectedRows);

                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.chargeType = GetChargeTypeSelectList(commissionLevyDTO.ToString());

                return View(commissionLevyDTO);
            }
        }
    }
}
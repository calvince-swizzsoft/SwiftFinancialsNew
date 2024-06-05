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


        public async Task<ActionResult> Charge(CommissionDTO commissionDTO)
        {
            Session["Description"] = commissionDTO.Description;
            Session["MaximumCharge"] = commissionDTO.MaximumCharge;

            return View("Create", commissionDTO);
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
                commissionDTO.chartOfAccount = GLAccountChartOfAccount;

                Session["GLAccount"] = commissionDTO.chargeSplits;
            }

            return View("Create", commissionDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Add(Guid? id, CommissionDTO commissionDTO)
        {
            await ServeNavigationMenus();

            ChargeSplitDTOs = TempData["ChargeSplitDTOs"] as ObservableCollection<CommissionSplitDTO>;

            var glAccount = commissionDTO.chargeSplits;

            if (ChargeSplitDTOs == null)
                ChargeSplitDTOs = new ObservableCollection<CommissionSplitDTO>();

            foreach (var chargeSplitDTO in commissionDTO.chargeSplit)
            {
                chargeSplitDTO.Description = chargeSplitDTO.Description;
                chargeSplitDTO.ChartOfAccountId = commissionDTO.Id;
                chargeSplitDTO.ChartOfAccountAccountName = glAccount.ChartOfAccountAccountName;
                chargeSplitDTO.Percentage = chargeSplitDTO.Percentage;
                chargeSplitDTO.Leviable = chargeSplitDTO.Leviable;

                ChargeSplitDTOs.Add(chargeSplitDTO);

                Session["Description"] = chargeSplitDTO.Description;
                Session["ChartOfAccountId"] = chargeSplitDTO.ChartOfAccountId;
                Session["ChartOfAccountAccountName"] = chargeSplitDTO.ChartOfAccountAccountName;
                Session["Percentage"] = chargeSplitDTO.Percentage;
                Session["Leviable"] = chargeSplitDTO.Leviable;
            };

            TempData["ChargeSplitDTOs"] = ChargeSplitDTOs;

            TempData["ChargeDTO"] = commissionDTO;

            ViewBag.ChargeSplitDTOs = ChargeSplitDTOs;

            return View("Create", commissionDTO);
        }



        [HttpPost]
        public async Task<ActionResult> Create(CommissionDTO commissionDTO)
        {
            commissionDTO = TempData["ChargeDTO"] as CommissionDTO;

            commissionDTO.chargeSplit[0].Description = Session["Description"].ToString();
            commissionDTO.chargeSplit[0].ChartOfAccountId = (Guid)Session["ChartOfAccountId"];
            commissionDTO.chargeSplit[0].ChartOfAccountAccountName = Session["ChartOfAccountAccountName"].ToString();
            commissionDTO.chargeSplit[0].Percentage = Convert.ToDouble(Session["Percentage"].ToString());
            commissionDTO.chargeSplit[0].Leviable = (bool)Session["Leviable"];

            //commissionDTO.ChargeSplitsTotalPercentage = 100;

            commissionDTO.ValidateAll();

            if (!commissionDTO.HasErrors)
            {
                var charge = await _channelService.AddCommissionAsync(commissionDTO, GetServiceHeader());

                TempData["SuccessMessage"] = "Successfully Created Charge";
                TempData["ChargeDTO"] = "";

                if (charge != null)
                {
                    var chargeSplits = new ObservableCollection<CommissionSplitDTO>();

                    foreach (var chargeSplitDTO in commissionDTO.chargeSplit)
                    {
                        chargeSplitDTO.Description = chargeSplitDTO.Description;
                        chargeSplitDTO.MaximumCharge = chargeSplitDTO.MaximumCharge;
                        chargeSplitDTO.ChartOfAccountId = chargeSplitDTO.ChartOfAccountId;
                        chargeSplitDTO.ChartOfAccountAccountName = chargeSplitDTO.ChartOfAccountAccountName;
                        chargeSplitDTO.Percentage = chargeSplitDTO.Percentage;
                        chargeSplitDTO.Leviable = chargeSplitDTO.Leviable;

                        chargeSplits.Add(chargeSplitDTO);
                    };

                    if (chargeSplits.Any())
                        await _channelService.UpdateCommissionSplitsByCommissionIdAsync(charge.Id, chargeSplits, GetServiceHeader());
                    TempData["ChargeSplitDTOs"] = "";
                }

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = commissionDTO.ErrorMessages;

                TempData["CreateError"] = "Failed to Create Charge";

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
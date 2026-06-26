using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.AdministrationModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class TreasuriesController : MasterController
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

            var pageCollectionInfo = await _channelService.FindTreasuriesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, false, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(treasuries => treasuries.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<TreasuryDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var treasuryDTO = await _channelService.FindTreasuryAsync(id, false, GetServiceHeader());

            return View(treasuryDTO);
        }


        public async Task<ActionResult> Search(Guid? id, TreasuryDTO treasuryDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            if (Session["chartOfAccountId"] != null)
            {
                treasuryDTO.ChartOfAccountId = (Guid)Session["chartOfAccountId"];
                treasuryDTO.ChartOfAccountAccountName = Session["chartOfAccountName"].ToString();
            }

            if (Session["treasuryName"] != null)
            {
                treasuryDTO.Description = Session["treasuryName"].ToString();
            }

            if (Session["lowerLimit"] != null)
            {
                treasuryDTO.RangeLowerLimit = Convert.ToDecimal(Session["lowerLimit"]);
            }

            if (Session["upperLimit"] != null)
            {
                treasuryDTO.RangeUpperLimit = Convert.ToDecimal(Session["upperLimit"]);
            }


            var branch = await _channelService.FindBranchAsync(parseId, GetServiceHeader());

            if (branch != null)
            {
                treasuryDTO.BranchId = branch.Id;
                treasuryDTO.BranchDescription = branch.Description;

                Session["BranchId"] = treasuryDTO.BranchId;
                Session["BranchDescription"] = treasuryDTO.BranchDescription;


                Session["Description"] = treasuryDTO.Description;
                Session["RangeLowerLimit"] = treasuryDTO.RangeLowerLimit;
                Session["RangeUpperLimit"] = treasuryDTO.RangeUpperLimit;
            }

            return View("Create", treasuryDTO);
        }


        public async Task<ActionResult> Create(Guid? id, TreasuryDTO treasuryDTO)
        {
            await ServeNavigationMenus();

            if (Session["BranchId"] != null)
            {
                treasuryDTO.BranchId = (Guid)Session["BranchId"];
                treasuryDTO.BranchDescription = Session["BranchDescription"].ToString();
            }

            if (Session["Description"] != null)
            {
                treasuryDTO.Description = Session["Description"].ToString();
            }

            if (Session["RangeLowerLimit"] != null)
            {
                treasuryDTO.RangeLowerLimit = Convert.ToDecimal(Session["RangeLowerLimit"]);
            }

            if (Session["RangeUpperLimit"] != null)
            {
                treasuryDTO.RangeUpperLimit = Convert.ToDecimal(Session["RangeUpperLimit"]);
            }


            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var chartOfAccount = await _channelService.FindChartOfAccountAsync(parseId, GetServiceHeader());


            if (chartOfAccount != null)
            {
                treasuryDTO.ChartOfAccountId = chartOfAccount.Id;
                treasuryDTO.ChartOfAccountAccountName = chartOfAccount.AccountName;

                Session["chartOfAccountId"] = treasuryDTO.ChartOfAccountId;
                Session["chartOfAccountName"] = treasuryDTO.ChartOfAccountAccountName;

                Session["treasuryName"] = treasuryDTO.Description;
                Session["lowerLimit"] = treasuryDTO.RangeLowerLimit;
                Session["upperLimit"] = treasuryDTO.RangeUpperLimit;
            }

            return View(treasuryDTO);
        }



        [HttpPost]
        public async Task<ActionResult> Create(TreasuryDTO treasuryDTO)
        {
            treasuryDTO.ValidateAll();

            if (!treasuryDTO.HasErrors)
            {
                await _channelService.AddTreasuryAsync(treasuryDTO, GetServiceHeader());

                TempData["AlertMessage"] = "Treasury created successfully";

                return View("Index");
            }
            else
            {
                var errorMessages = treasuryDTO.ErrorMessages;

                TempData["RefreshPage"] = true;

                return View(treasuryDTO);
            }
        }


        public async Task<ActionResult> Search2(Guid? id, TreasuryDTO treasuryDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            if (Session["chartOfAccountIdEdit"] != null)
            {
                treasuryDTO.ChartOfAccountId = (Guid)Session["chartOfAccountIdEdit"];
                treasuryDTO.ChartOfAccountAccountName = Session["chartOfAccountAccountNameEdit"].ToString();
            }


            var branch = await _channelService.FindBranchAsync(parseId, GetServiceHeader());

            if (branch != null)
            {
                treasuryDTO.BranchId = branch.Id;
                treasuryDTO.BranchDescription = branch.Description;

                Session["BranchIdEdit"] = treasuryDTO.BranchId;
                Session["BranchDescriptionEdit"] = treasuryDTO.BranchDescription;
            }

            return View("Edit", treasuryDTO);
        }




        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var treasuryDTO = await _channelService.FindTreasuryAsync(id, false, GetServiceHeader());

            return View(treasuryDTO);
        }


        public async Task<ActionResult> chartOfAccount(Guid? id, TreasuryDTO treasuryDTO)
        {
            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            if (Session["BranchIdEdit"] != null)
            {
                treasuryDTO.BranchId = (Guid)Session["BranchIdEdit"];
                treasuryDTO.BranchDescription = Session["BranchDescriptionEdit"].ToString();
            }

            var chartOfAccount = await _channelService.FindChartOfAccountAsync(parseId, GetServiceHeader());


            if (chartOfAccount != null)
            {
                treasuryDTO.ChartOfAccountId = chartOfAccount.Id;
                treasuryDTO.ChartOfAccountAccountName = chartOfAccount.AccountName;

                Session["chartOfAccountIdEdit"] = treasuryDTO.ChartOfAccountId;
                Session["chartOfAccountAccountNameEdit"] = treasuryDTO.ChartOfAccountAccountName;
            }

            return View("create", treasuryDTO);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, TreasuryDTO treasuryDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateTreasuryAsync(treasuryDTO, GetServiceHeader());

                TempData["Edit"] = "Successfully Edited Treasury";

                return RedirectToAction("Index");
            }
            else
            {
                return View(treasuryDTO);
            }
        }
    }
}

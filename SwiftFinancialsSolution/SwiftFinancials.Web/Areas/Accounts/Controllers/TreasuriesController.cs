using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
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

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(savingsProduct => savingsProduct.CreatedDate).ToList();

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
        public async Task<ActionResult> Create(Guid? id, TreasuryDTO treasuryDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            if (Session["BranchDescription"] != null)
            {
                treasuryDTO.BranchDescription = Session["BranchDescription"].ToString();
            }



            var glAccount = await _channelService.FindChartOfAccountAsync(parseId, GetServiceHeader());

            if (glAccount != null)
            {
                treasuryDTO.ChartOfAccountId = glAccount.Id;

                Session["ChartOfAccountId"] = treasuryDTO.ChartOfAccountId;
            }

            return View(treasuryDTO);
        }


        public async Task<ActionResult> Branch(Guid? id, TreasuryDTO treasuryDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            if (Session["ChartOfAccountId"] != null)
            {
                treasuryDTO.ChartOfAccountId = (Guid)Session["ChartOfAccountId"];
            }


            var branches = await _channelService.FindBranchAsync(parseId, GetServiceHeader());

            if(branches != null)
            {
                treasuryDTO.BranchId = branches.Id;
                treasuryDTO.BranchDescription = branches.Description;

                Session["BranchDescription"] = treasuryDTO.BranchDescription;
            }

            return View("Create", treasuryDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(TreasuryDTO treasuryDTO)
        {
            treasuryDTO.ValidateAll();

            if (!treasuryDTO.HasErrors)
            {
                await _channelService.AddTreasuryAsync(treasuryDTO, GetServiceHeader());

                TempData["AlertMessage"] = "Treasury created successfully";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = treasuryDTO.ErrorMessages;

                TempData["Error"] = "Failed to create Treasury";

                return View(treasuryDTO);
            }
        }



        public async Task<ActionResult> Branch2(Guid? id, TreasuryDTO treasuryDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            if (Session["ChartOfAccountId"] != null)
            {
                treasuryDTO.ChartOfAccountId = (Guid)Session["ChartOfAccountId"];
            }


            var branches = await _channelService.FindBranchAsync(parseId, GetServiceHeader());

            if (branches != null)
            {
                treasuryDTO.BranchId = branches.Id;
                treasuryDTO.BranchDescription = branches.Description;

                Session["BranchDescription"] = treasuryDTO.BranchDescription;
            }

            return View("Create", treasuryDTO);
        }



        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var treasuryDTO = await _channelService.FindTreasuryAsync(id, false, GetServiceHeader());

            return View(treasuryDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, TreasuryDTO treasuryDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateTreasuryAsync(treasuryDTO, GetServiceHeader());

                TempData["Edit"] = "Edited Treasury successfully";

                return RedirectToAction("Index");
            }
            else
            {
                return View(treasuryDTO);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetSavingsProductsAsync()
        {
            var savingsProductDTOs = await _channelService.FindSavingsProductsAsync(GetServiceHeader());

            return Json(savingsProductDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}

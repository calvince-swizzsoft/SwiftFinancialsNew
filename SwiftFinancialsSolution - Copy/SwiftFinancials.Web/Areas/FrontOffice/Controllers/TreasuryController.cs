using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class TreasuryController : MasterController
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

            bool includeBalances = false;

             var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

             var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

             var pageCollectionInfo = await _channelService.FindTreasuriesByFilterInPageAsync(jQueryDataTablesModel.sSearch, 0, int.MaxValue, includeBalances, GetServiceHeader());

             if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
             {

                var sortedData = pageCollectionInfo.PageCollection.OrderByDescending(gl => gl.CreatedDate).ToList();

                totalRecordCount = pageCollectionInfo.ItemsCount;

                //pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(l => l.JournalCreatedDate).ToList();


                var paginatedData = sortedData.Skip(jQueryDataTablesModel.iDisplayStart).Take(jQueryDataTablesModel.iDisplayLength).ToList();


                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                 return this.DataTablesJson(items: paginatedData, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
             }
             else return this.DataTablesJson(items: new List<TreasuryDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
         }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var treasuryDTO = await _channelService.FindTreasuryAsync(id, true);

            return View(treasuryDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(TreasuryDTO treasuryDTO)
        {
            treasuryDTO.ValidateAll();

            if (!treasuryDTO.HasErrors)
            {
                await _channelService.AddTreasuryAsync(treasuryDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = treasuryDTO.ErrorMessages;

                return View(treasuryDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var treasuryDTO = await _channelService.FindTreasuryAsync(id, true);

            return View(treasuryDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, TreasuryDTO treasuryBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateTreasuryAsync(treasuryBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(treasuryBindingModel);
            }
        }

        //[HttpGet]
        //public async Task<JsonResult> GetTreasuriesAsync()
        //{
        //    var treasuriesDTOs = await _channelService.FindTreasuriesAsync(GetServiceHeader());

        //    return Json(treasuriesDTOs, JsonRequestBehavior.AllowGet);
        //}
    }
}
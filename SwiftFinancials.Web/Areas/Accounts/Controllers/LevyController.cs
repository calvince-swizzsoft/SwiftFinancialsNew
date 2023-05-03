using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Areas.Accounts.Models;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
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

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LevyDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        /*public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var levyDTO = await _channelService.FindLeviesAsync(id, GetServiceHeader());

            return View(levyDTO );
        }*/
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(LevyViewModel levyViewModel)
        {
            levyViewModel.ValidateAll();

            if (!levyViewModel.HasErrors)
            {
                var levyDTO = new LevyDTO()
                {
                    Description = levyViewModel.LevyDescription,
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

        /*public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var levyDTO  = await _channelService.FindLevyAsync(id, GetServiceHeader());

            return View(levyDTO );
        }*/

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, LevyDTO levyDTOBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateLevyAsync(levyDTOBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(levyDTOBindingModel);
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

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

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class TellerController : MasterController
    {

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
       /* public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
         {
             int totalRecordCount = 0;

             int searchRecordCount = 0;

             var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

             var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

             var pageCollectionInfo = await _channelService.FindTellersByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, 1);

             if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
             {
                 totalRecordCount = pageCollectionInfo.ItemsCount;

                 searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                 return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
             }
             else return this.DataTablesJson(items: new List<TellerDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
         }*/

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var tellerDTO = await _channelService.FindTellerAsync(id, true);

            return View(tellerDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(TellerDTO tellerDTO)
        {
            tellerDTO.ValidateAll();

            if (!tellerDTO.HasErrors)
            {
                await _channelService.AddTellerAsync(tellerDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = tellerDTO.ErrorMessages;

                return View(tellerDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var tellerDTO = await _channelService.FindTellerAsync(id, true);

            return View(tellerDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, TellerDTO tellerBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateTellerAsync(tellerBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(tellerBindingModel);
            }
        }

        /*[HttpGet]
        public async Task<JsonResult> GetTellersAsync()
        {
            var tellersDTOs = await _channelService.FindTellersAsync(GetServiceHeader());

            return Json(tellersDTOs, JsonRequestBehavior.AllowGet);
        }*/
    }
}
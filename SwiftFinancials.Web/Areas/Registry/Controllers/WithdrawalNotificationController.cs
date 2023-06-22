using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.RegistryModule;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Registry.Controllers
{
    public class WithdrawalNotificationController : MasterController
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

            var pageCollectionInfo = await _channelService.FindWithdrawalNotificationsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, 1);

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<WithdrawalNotificationDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var withdrawalNotificationDTO = await _channelService.FindWithdrawalNotificationAsync(id, GetServiceHeader());

            return View(withdrawalNotificationDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(WithdrawalNotificationDTO withdrawalNotificationDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.AddWithdrawalNotificationAsync(withdrawalNotificationDTO.MapTo<WithdrawalNotificationDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

                TempData["Error"] = string.Join(",", allErrors);

                return View(withdrawalNotificationDTO);
            }
        }


        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var withdrawalNotificationDTO = await _channelService.FindWithdrawalNotificationAsync(id, GetServiceHeader());

            return View(withdrawalNotificationDTO);
        }

        /*[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, WithdrawalNotificationDTO withdrawalNotificationDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateWithdrawalNotificationAsync(withdrawalNotificationDTO.MapTo<WithdrawalNotificationDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(withdrawalNotificationDTO);
            }
        }*

       /* [HttpGet]
        public async Task<JsonResult> GetWithdrawalNotificationsAsync()
        {
            var withdrawalNotificationDTOs = await _channelService.FindWithdrawalNotificationsAsync(false, true, GetServiceHeader());

            return Json(withdrawalNotificationDTOs, JsonRequestBehavior.AllowGet);
        }*/
    }
}
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

            var pageCollectionInfo = await _channelService.FindWithdrawalNotificationsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength,1, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(creditBatch => creditBatch.CreatedDate).ToList();

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

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();
            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            WithdrawalNotificationDTO withdrawalNotificationDTO = new WithdrawalNotificationDTO();

            if (customer != null)
            {

                withdrawalNotificationDTO.CustomerId = customer.Id;
                withdrawalNotificationDTO.CustomerFullName = customer.FullName;
                withdrawalNotificationDTO.CustomerIndividualPayrollNumbers = customer.IndividualPayrollNumbers;
                withdrawalNotificationDTO.CustomerSerialNumber = customer.SerialNumber;
                withdrawalNotificationDTO.CustomerIndividualIdentityCardNumber = customer.IndividualIdentityCardNumber;
                withdrawalNotificationDTO.CustomerStationDescription = customer.StationDescription;
                withdrawalNotificationDTO.CustomerStationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
            }

            return View(withdrawalNotificationDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(WithdrawalNotificationDTO withdrawalNotificationDTO)
        {
            withdrawalNotificationDTO.ValidateAll();

            if (!withdrawalNotificationDTO.HasErrors)
            {
                await _channelService.AddWithdrawalNotificationAsync(withdrawalNotificationDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = withdrawalNotificationDTO.ErrorMessages;
                ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(withdrawalNotificationDTO.Category.ToString());
                return View(withdrawalNotificationDTO);
            }
        }



        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var withdrawalNotificationDTO = await _channelService.FindWithdrawalNotificationAsync(id, GetServiceHeader());

            return View(withdrawalNotificationDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, WithdrawalNotificationDTO withdrawalNotificationDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateWithdrawalNotificationAsync(withdrawalNotificationDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(withdrawalNotificationDTO);
            }
        }

       /* [HttpGet]
        public async Task<JsonResult> GetWithdrawalNotificationsAsync()
        {
            var withdrawalNotificationDTOs = await _channelService.FindWithdrawalNotificationsAsync(false, true, GetServiceHeader());

            return Json(withdrawalNotificationDTOs, JsonRequestBehavior.AllowGet);
        }*/
    }
}
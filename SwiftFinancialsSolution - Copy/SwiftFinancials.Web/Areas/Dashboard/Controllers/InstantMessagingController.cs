using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Windows.Forms;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.MessagingModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
using OfficeOpenXml;
using SwiftFinancials.TextAlertDispatcher.Celcom.Configuration;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using SwiftFinancials.Web.PDF;

namespace SwiftFinancials.Web.Areas.Dashboard.Controllers
{
    public class InstantMessagingController : MasterController
    {

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, int? status, DateTime? startDate, DateTime? endDate, string filterValue)
        {
            await ServeNavigationMenus();

            var result = await _channelService.FindTextAlertsByDateRangeAndFilterInPageAsync(
                status ?? 0,
                startDate ?? DateTime.MinValue,
                endDate ?? DateTime.MaxValue,
                filterValue,
                0,
                int.MaxValue,
                GetServiceHeader()
            );

            var messages = result?.PageCollection?.OrderByDescending(x => x.CreatedDate).ToList()
                           ?? new List<TextAlertDTO>();

            return Json(messages, JsonRequestBehavior.AllowGet);
        }




        [HttpPost]
        public async Task<JsonResult> InstantMessagesIndex(JQueryDataTablesModel jQueryDataTablesModel,int?dlrstatus,string search,int?dayscap)
        {

            int totalRecordCount = 0;
            int searchRecordCount = 0;
            

            var pageCollectionInfo = await _channelService.FindTextAlertsByFilterInPageAsync((int)dlrstatus, search, int.MaxValue, int.MaxValue,(int)dayscap, GetServiceHeader());


            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;
                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? pageCollectionInfo.PageCollection.Count
                    : totalRecordCount;

                var orderedPageCollection = pageCollectionInfo.PageCollection
                    .OrderByDescending(item => item.CreatedDate)
                    .ToList();

                return this.DataTablesJson(
                    items: orderedPageCollection,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
            else
            {
                return this.DataTablesJson(
                    items: new List<InstantMessageDTO> { },
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
        }


        [HttpPost]
        public async Task<ActionResult> Index(QuickTextAlertDTO quickTextAlertDTO)
        {
            await ServeNavigationMenus();

            if (ModelState.IsValid)
            {
                await _channelService.AddQuickTextAlertAsync(quickTextAlertDTO, GetServiceHeader());
                TempData["Success"] = "Message sent successfully.";
                return RedirectToAction("Index");
            }

            // Reload messages if validation fails
            var result = await _channelService.FindTextAlertsByFilterInPageAsync(0, "", int.MaxValue, int.MaxValue, 7, GetServiceHeader());
            ViewBag.FilteredMessages = result?.PageCollection?.ToList() ?? new List<TextAlertDTO>();

            return View(quickTextAlertDTO);
        }


    }
}
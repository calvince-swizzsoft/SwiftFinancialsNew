using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.MessagingModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using Infrastructure.Crosscutting.Framework.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using SwiftFinancials.Presentation.Infrastructure.Util;
using System.Collections.ObjectModel;

namespace SwiftFinancials.Web.Areas.Messaging.Controllers
{
    public class TextAlertController : MasterController
    {
        // GET: Messaging/TextAlert
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

            int daysCap = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindTextAlertsByFilterInPageAsync((int)DLRStatus.Delivered, jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, daysCap, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<TextAlertDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        // GET: TextAlert/Create
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        // POST: TextAlert/Create
        [HttpPost]
        public async Task<ActionResult> Create(TextAlertDTO textAlertBindingModel)
        {
            textAlertBindingModel.TextMessageSecurityCritical = true;
            textAlertBindingModel.TextMessagePriority =(int) QueuePriority.High;
            textAlertBindingModel.TextMessageDLRStatus = (int)DLRStatus.Pending;
            textAlertBindingModel.TextMessageOrigin = (int)MessageOrigin.Within;
            textAlertBindingModel.TextMessageSendRetry = 0;
            textAlertBindingModel.ValidateAll();

            if (textAlertBindingModel.HasErrors)
            {
                await ServeNavigationMenus();

                await _channelService.AddTextAlertsAsync(new System.Collections.ObjectModel.ObservableCollection<TextAlertDTO> { textAlertBindingModel }, GetServiceHeader());

                TempData["Error"] = textAlertBindingModel.ErrorMessages; 

                return View();
            }
           

            if (textAlertBindingModel != null)
            {
                return RedirectToAction("Index", "TextAlert", new { Area = "Messaging" });
            }

            return View();
        }
    }
}
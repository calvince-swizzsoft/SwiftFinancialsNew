using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.MessagingModule;
using Fedhaplus.DashboardApplication.Controllers;
using Fedhaplus.DashboardApplication.Helpers;
using Fedhaplus.Presentation.Infrastructure.Util;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Fedhaplus.DashboardApplication.Areas.Messaging.Controllers
{
    public class EmailBatchController : MasterController
    {
        // GET: Messaging/EmailAlert
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

            var pageCollectionInfo = await _channelService.FindEmailBatchesInPageAsync(jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<EmailBatchDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(EmailBatchBindingModel emailBatchBindingModel, string[] file)
        {
            emailBatchBindingModel.ValidateAll();

            if (emailBatchBindingModel.HasErrors)
            {
                await ServeNavigationMenus();

                TempData["Error"] = emailBatchBindingModel.ErrorMessages;

                return View(emailBatchBindingModel);
            }

            var emailBatchDTO = await _channelService.AddEmailBatchAsync(emailBatchBindingModel.MapTo<EmailBatchDTO>(), GetServiceHeader());

            if (emailBatchDTO != null)
            {
                return RedirectToAction("Index", "EmailBatch", new { Area = "Messaging" });
            }

            await ServeNavigationMenus();

            return View(emailBatchBindingModel);
        }
    }
}
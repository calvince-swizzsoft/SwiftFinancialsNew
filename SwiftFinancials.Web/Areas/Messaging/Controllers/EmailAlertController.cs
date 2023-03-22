using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.MessagingModule;
using SwiftFinancials.Web.Attributes;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using Infrastructure.Crosscutting.Framework.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Messaging.Controllers
{
    [RoleBasedAccessControl]
    public class EmailAlertController : MasterController
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

            var pageCollectionInfo = await _channelService.FindEmailAlertsByFilterInPageAsync((int)DLRStatus.Delivered, jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength,30, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<EmailAlertDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        // GET: SystemUser/Create
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        // POST: SystemUser/Create
        [HttpPost]
        public async Task<ActionResult> Create(EmailAlertDTO emailAlertBindingModel)
        {
            emailAlertBindingModel.MailMessageFrom = GetDashboardAppConfiguration().DashboardAppSettingsItems.SmtpUsername;
            emailAlertBindingModel.MailMessageIsBodyHtml = true;
            emailAlertBindingModel.MailMessageDLRStatus = (int)DLRStatus.Delivered;
            emailAlertBindingModel.MailMessageOrigin = (int)MessageOrigin.Within;
            emailAlertBindingModel.ValidateAll();

            if (emailAlertBindingModel.HasErrors)
            {
                await ServeNavigationMenus();

                TempData["Error"] = emailAlertBindingModel.ErrorMessages;

                return View();
            }

            var emailAlertDTO = await _channelService.AddEmailAlertAsync(emailAlertBindingModel, GetServiceHeader());

            if (emailAlertDTO != null)
            {
                return RedirectToAction("Index", "EmailAlert", new { Area = "Messaging" });
            }

            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetCompaniesAsync()
        {
            var companiesDTOs = await _channelService.FindCompaniesAsync(GetServiceHeader());

            return Json(companiesDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
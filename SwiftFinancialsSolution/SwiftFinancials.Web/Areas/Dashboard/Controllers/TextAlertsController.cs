using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using SwiftFinancials.Web.PDF;

namespace SwiftFinancials.Web.Areas.Dashboard.Controllers
{
    public class TextAlertsController : MasterController
    {

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            ViewBag.TextAlertStatusFilterSelectList = GetTextAlertStatusFilterSelectList(string.Empty);

            return View();
        }


        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, int status, DateTime? startDate, DateTime? endDate, string filterValue)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;
            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            bool sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc";

            var pageCollectionInfo = startDate.HasValue && endDate.HasValue
                ? await _channelService.FindTextAlertsByDateRangeAndFilterInPageAsync(status, startDate.Value, endDate.Value, filterValue, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader())
                : await _channelService.FindTextAlertsInPageAsync(pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;
                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(alert => alert.CreatedDate).ToList();
                searchRecordCount = string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? totalRecordCount : pageCollectionInfo.PageCollection.Count;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            return this.DataTablesJson(items: new List<TextAlertDTO>(), totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }
    }
}
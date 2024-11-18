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
    public class MessagingController : MasterController
    {

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            ViewBag.TextAlertStatusFilterSelectList = GetTextAlertStatusFilterSelectList(string.Empty);
            ViewBag.LoanCaseStatusSelectList = GetLoanCaseStatusSelectList(string.Empty);

            return View();
        }


        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, int loanCaseStatus, string filterValue, int filterType)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindTextAlertsInPageAsync(pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            var page = pageCollectionInfo.PageCollection;

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(LoanCase => LoanCase.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<TextAlertDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


    }
}
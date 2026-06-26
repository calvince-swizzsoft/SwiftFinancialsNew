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
    public class EmailAlertsController : MasterController
    {

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            ViewBag.EmailalertStatusFilterSelectList = GetEmailAlertStatusFilterSelectList(string.Empty);

            return View();
        }


        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, int? status, DateTime? startDate, DateTime? endDate, string filterValue)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;
            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            bool sortAscending = jQueryDataTablesModel.sSortDir_.FirstOrDefault() == "asc";

            var pageCollectionInfo = new PageCollectionInfo<EmailAlertDTO>();

            if (startDate == null && endDate == null && string.IsNullOrWhiteSpace(filterValue))
            {
                pageCollectionInfo = await _channelService.FindEmailAlertsByFilterInPageAsync(8, filterValue,
                    pageIndex, int.MaxValue, int.MaxValue,
                    GetServiceHeader()
                );
            }
            else
            {
                pageCollectionInfo = await _channelService.FindEmailAlertsByDateRangeAndFilterInPageAsync((int)status, (DateTime)startDate, (DateTime)endDate, filterValue, pageIndex, int.MaxValue, GetServiceHeader());
            }

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;
                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection
                    .OrderBy(alert => sortAscending ? alert.CreatedDate : alert.CreatedDate)
                    .ToList();
                searchRecordCount = string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? totalRecordCount
                    : pageCollectionInfo.PageCollection.Count;
            }

            return Json(new
            {
                sEcho = jQueryDataTablesModel.sEcho,
                iTotalRecords = totalRecordCount,
                iTotalDisplayRecords = searchRecordCount,
                aaData = pageCollectionInfo.PageCollection
            });
        }




        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Create(EmailAlertDTO emailAlertDTO)
        {
            await ServeNavigationMenus();

            emailAlertDTO.ValidateAll();

            if (!emailAlertDTO.HasErrors)
            {
                await _channelService.AddEmailAlertAsync(emailAlertDTO, GetServiceHeader());
            }

            return View();
        }
    }
}
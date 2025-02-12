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
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, int? status, DateTime? startDate, DateTime? endDate, string filterValue)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;

            var pageCollectionInfo = new PageCollectionInfo<TextAlertDTO>();

            if (startDate == null && endDate == null && string.IsNullOrWhiteSpace(filterValue) && !status.HasValue)
            {
                pageCollectionInfo = await _channelService.FindTextAlertsInPageAsync(
                    0,
                    int.MaxValue,
                    GetServiceHeader()
                );
            }
            else
            {
                pageCollectionInfo = await _channelService.FindTextAlertsByDateRangeAndFilterInPageAsync(
                    status ?? 0,
                    startDate ?? DateTime.MinValue,
                    endDate ?? DateTime.MaxValue,
                    filterValue,
                    0,
                    int.MaxValue,
                    GetServiceHeader()
                );
            }

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(t => t.CreatedDate)
                    .ToList();

                totalRecordCount = sortedData.Count;

                var paginatedData = sortedData
                    .Skip(jQueryDataTablesModel.iDisplayStart)
                    .Take(jQueryDataTablesModel.iDisplayLength)
                    .ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? sortedData.Count
                    : totalRecordCount;

                return this.DataTablesJson(
                    items: paginatedData,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }

            return this.DataTablesJson(
                items: new List<TextAlertDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(QuickTextAlertDTO quickTextAlertDTO, string recipient1, string recipient2, string recipient3)
        {
            await ServeNavigationMenus();

            quickTextAlertDTO.ValidateAll();
            var userDTO = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
            if (userDTO.BranchId != null)
            {
                quickTextAlertDTO.BranchId = (Guid)userDTO.BranchId;
            }

            var companyBranch = await _channelService.FindBranchAsync(quickTextAlertDTO.BranchId, GetServiceHeader());
            var company = await _channelService.FindCompanyAsync(companyBranch.CompanyId, GetServiceHeader());
            quickTextAlertDTO.CompanyDescription = company.Description;
            quickTextAlertDTO.CompanyId = company.Id;

            if (recipient1 != string.Empty && recipient2 != string.Empty && recipient3 != string.Empty)
            {
                string recipients = recipient1 + "," + recipient2 + "," + recipient3;
            }

            if (!quickTextAlertDTO.HasErrors)
            {
                await _channelService.AddQuickTextAlertAsync(quickTextAlertDTO, GetServiceHeader());
                TempData["Success"] = "Operation Completed Successfully";
                return RedirectToAction("Index");
            }

            return View();
        }
    }
}
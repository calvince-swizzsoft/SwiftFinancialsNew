using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

using System.Data.SqlClient;
using System.Data;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class LoanRestructuringController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            ViewBag.LoanCaseFilterSelectList = GetLoanCaseFilterTypeSelectList(string.Empty);
            ViewBag.LoanCaseStatusSelectList = GetLoanCaseStatusSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, string filterValue, int filterType)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;

            var pageCollectionInfo = await _channelService.FindLoanCasesByStatusAndFilterInPageAsync(
                (int)LoanCaseStatus.Restructured,
                filterValue,
                filterType,
                0,
                int.MaxValue,
                includeBatchStatus: true,
                GetServiceHeader()
            );

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(loanCase => loanCase.CreatedDate)
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
                items: new List<LoanCaseDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }

        [HttpPost]
        public async Task<JsonResult> CustomerAccountIndex(JQueryDataTablesModel jQueryDataTablesModel, string text, int customerFilter)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;
            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;

            var pageCollectionInfo = await _channelService.FindCustomerAccountsByProductCodeAndRecordStatusAndFilterInPageAsync((int)ProductCode.Loan, (int)CustomerAccountStatus.Normal, text,
                customerFilter, pageIndex, pageSize, true, true, true, true, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(customer => customer.CreatedDate)
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
                items: new List<CustomerAccountDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }




        public async Task<ActionResult> CustomerAccountLookUp(Guid? id, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("create");
            }


            return Json(new { success = false, message = "Customer not found" });
        }



        public async Task<ActionResult> Create(Guid? Id)
        {
            await ServeNavigationMenus();

            ViewBag.LoanCaseFilterSelectList = GetLoanCaseFilterTypeSelectList(string.Empty);
            ViewBag.LoanCaseStatusSelectList = GetLoanCaseStatusSelectList(string.Empty);

            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);

            ViewBag.CustomerFilter = GetCustomerFilterSelectList(string.Empty);

            ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(string.Empty);
            ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(string.Empty);
            ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(string.Empty);

            ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);

            ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(string.Empty);

            return View();
        }



        [HttpPost]
        public async Task<ActionResult> Create(CustomerAccountDTO customerAccountDTO, double PaymentPerPeriod, double NumberOfPeriods, string Reference)
        {
            customerAccountDTO.CustomerAccountTypeTargetProductId = (Guid)Session["productId"];
            customerAccountDTO = Session["customerAccountDTO"] as CustomerAccountDTO;

            customerAccountDTO.NumberOfPeriods = NumberOfPeriods;
            customerAccountDTO.PaymentPerPeriod = PaymentPerPeriod;
            customerAccountDTO.Reference = Reference;

            var findbranchId = await _channelService.FindCustomerAccountsByCustomerIdAsync(customerAccountDTO.CustomerId, true, true, true, true, GetServiceHeader());

            Guid branchId = Guid.Empty;
            foreach (var pickBranchId in findbranchId)
            {
                branchId = pickBranchId.BranchId;
            }

            customerAccountDTO.BranchId = branchId;

            customerAccountDTO.ValidateAll();

            await ServeNavigationMenus();

            if (!customerAccountDTO.HasErrors)
            {
                var submit = await _channelService.RestructureLoanAsync(customerAccountDTO.BranchId, customerAccountDTO.Id, customerAccountDTO.NumberOfPeriods, customerAccountDTO.PaymentPerPeriod,
                    customerAccountDTO.Reference, 1234, GetServiceHeader());

                if (submit == false)
                {
                    TempData["ExistingLoanCase"] = "Sorry, but selected customer has a loan case for the selected product currently undergoing processing!";
                    return View();
                }

                TempData["Create"] = "Loan Restructuring Successful";

                Session.Remove("productId");
                Session.Remove("customerAccountDTO");

                Session.Clear();

                return View();
            }
            else
            {
                var errorMessages = customerAccountDTO.ErrorMessages;
                return View();
            }
        }
    }
}
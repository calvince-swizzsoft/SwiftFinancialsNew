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

            return View();
        }


        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, string text, int pageIndex, int pageSize, LoanCaseDTO loancaseDTO)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindLoanCasesByFilterInPageAsync(jQueryDataTablesModel.sSearch, 10, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, true, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(LoanCase => LoanCase.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanCaseDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var loanCaseDTO = await _channelService.FindLoanCaseAsync(id, GetServiceHeader());

            var loanGuarantors = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(id, GetServiceHeader());

            ViewBag.LoanGuarantors = loanGuarantors;

            return View(loanCaseDTO);
        }


        public async Task<ActionResult> Create(Guid? Id, CustomerAccountDTO customerAccountDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (Id == Guid.Empty || !Guid.TryParse(Id.ToString(), out parseId))
            {
                return View();
            }

            var customerAccount = await _channelService.FindCustomerAccountAsync(parseId, true, true, true, true, GetServiceHeader());

            if (customerAccount.Status != (int)CustomerAccountStatus.Normal)
            {
                TempData["AccountStatus"] = "Sorry. The selected customer account \"" + customerAccount.FullAccountNumber + "\". Account status is: " + customerAccount.StatusDescription;
                return View();
            }


            if (customerAccount.CustomerAccountTypeProductCode != (int)ProductCode.Loan)
            {
                TempData["AccountProductCode"] = "The selected account is not a loan account. You can only restructure a loans account.";
                return View();
            }


            if (customerAccount != null)
            {
                customerAccountDTO.CustomerIndividualIdentityCardNumber = customerAccount.FullAccountNumber;
                customerAccountDTO.Id = customerAccount.Id;
                customerAccountDTO.Status = customerAccount.Status;
                customerAccountDTO.ModifiedBy = customerAccount.StatusDescription;
                customerAccountDTO.PrincipalBalance = customerAccount.AvailableBalance;
                customerAccountDTO.InterestBalance = customerAccount.InterestBalance;
                customerAccountDTO.CustomerId = customerAccount.CustomerId;
                customerAccountDTO.CustomerIndividualFirstName = customerAccount.CustomerIndividualSalutationDescription + " " + customerAccount.CustomerIndividualFirstName + " " +
                    customerAccount.CustomerIndividualLastName;
                customerAccountDTO.CustomerIndividualPayrollNumbers = customerAccount.CustomerIndividualPayrollNumbers;
                customerAccountDTO.CustomerPersonalIdentificationNumber = customerAccount.CustomerPersonalIdentificationNumber;
                customerAccountDTO.CustomerReference1 = customerAccount.CustomerReference1;
                customerAccountDTO.CustomerReference2 = customerAccount.CustomerReference2;
                customerAccountDTO.CustomerReference3 = customerAccount.CustomerReference3;

                var loanProductId = customerAccount.CustomerAccountTypeTargetProductId;

                var loanProduct = await _channelService.FindLoanProductAsync(loanProductId, GetServiceHeader());
                if (loanProduct != null)
                {
                    customerAccountDTO.LoanProductsDTO = loanProduct as LoanProductDTO;

                    // Calculate Number of Periods
                }

                Session["customerAccountDTO"] = customerAccountDTO;
                Session["productId"] = customerAccountDTO.LoanProductsDTO.Id;
            }
            return View(customerAccountDTO);
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
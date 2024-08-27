using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Microsoft.AspNet.Identity;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Attributes;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Registry.Controllers
{
    [RoleBasedAccessControl]
    public class ConditionalLendingController : MasterController
    {
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

            var pageCollectionInfo = await _channelService.FindCustomersByFilterInPageAsync(jQueryDataTablesModel.sSearch, 2, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<ConditionalLendingDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var ConditionalLendingDTO = await _channelService.FindConditionalLendingEntriesByConditionalLendingIdAsync(id, GetServiceHeader());

            return View(ConditionalLendingDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
            ViewBag.IndividualTypeSelectList = GetIndividualTypeSelectList(string.Empty);
            ViewBag.IdentityCardSelectList = GetIdentityCardTypeSelectList(string.Empty);
            ViewBag.SalutationSelectList = GetSalutationSelectList(string.Empty);
            ViewBag.GenderSelectList = GetGenderSelectList(string.Empty);
            ViewBag.MaritalStatusSelectList = GetMaritalStatusSelectList(string.Empty);
            ViewBag.IndividualNationalitySelectList = GetNationalitySelectList(string.Empty);
            ViewBag.IndividualEmploymentTermsOfServiceSelectList = GetTermsOfServiceSelectList(string.Empty);
            ViewBag.IndividualClassificationSelectList = GetCustomerClassificationSelectList(string.Empty);

            var debitTypes = await _channelService.FindMandatoryDebitTypesAsync(true, GetServiceHeader());
            var investmentProducts = await _channelService.FindMandatoryInvestmentProductsAsync(true, GetServiceHeader());
            var savingsProducts = await _channelService.FindMandatorySavingsProductsAsync(false, GetServiceHeader());
            ViewBag.investment = investmentProducts;
            ViewBag.savings = savingsProducts;
            ViewBag.debit = debitTypes;

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(ConditionalLendingBindingModel ConditionalLendingBindingModel)
        {
            var registrationdate = Request["registrationdate"];
            var birthdate = Request["birthdate"];
            var employmentdate = Request["employmentdate"];

            //customerBindingModel.IndividualBirthDate = DateTime.ParseExact((Request["birthdate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            //customerBindingModel.RegistrationDate = DateTime.ParseExact((Request["registrationdate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            //customerBindingModel.IndividualEmploymentDate = DateTime.ParseExact((Request["employmentdate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            var userDTO = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());

            //if (userDTO.BranchId != null)
            //{

            //    customerBindingModel.BranchId = (Guid)userDTO.BranchId;

            //}


            ConditionalLendingBindingModel.ValidateAll();

            //cheat
            var mandatoryInvestmentProducts = new List<InvestmentProductDTO>();
            var mandatorySavingsProducts = new List<SavingsProductDTO>();
            var mandatoryDebitTypes = new List<DebitTypeDTO>();
            var mandatoryProducts = new ProductCollectionInfo();


            var debitTypes = await _channelService.FindMandatoryDebitTypesAsync(true, GetServiceHeader());
            var investmentProducts = await _channelService.FindMandatoryInvestmentProductsAsync(true, GetServiceHeader());
            var savingsProducts = await _channelService.FindMandatorySavingsProductsAsync(false, GetServiceHeader());

            mandatoryProducts.InvestmentProductCollection = investmentProducts.ToList();
            mandatoryProducts.SavingsProductCollection = savingsProducts.ToList();

            if (!ConditionalLendingBindingModel.HasErrors)
            {

                var result = await _channelService.AddCustomerAsync(ConditionalLendingBindingModel.MapTo<CustomerDTO>(), debitTypes.ToList(), investmentProducts.ToList(), savingsProducts.ToList(), mandatoryProducts, 1, GetServiceHeader());
                if (result.ErrorMessageResult != null)
                {
                    TempData["Error"] = result.ErrorMessageResult + result.ErrorMessages;
                    await ServeNavigationMenus();
                    ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(ConditionalLendingBindingModel.LoanProductId.ToString());
                    ViewBag.IndividualTypeSelectList = GetIndividualTypeSelectList(ConditionalLendingBindingModel.LoanProductDescription.ToString());
                    ViewBag.IdentityCardSelectList = GetIdentityCardTypeSelectList(ConditionalLendingBindingModel.Description.ToString());
                    ViewBag.SalutationSelectList = GetSalutationSelectList(ConditionalLendingBindingModel.IsLocked.ToString());
                    ViewBag.GenderSelectList = GetGenderSelectList(ConditionalLendingBindingModel.CreatedBy.ToString());
                    ViewBag.MaritalStatusSelectList = GetMaritalStatusSelectList(ConditionalLendingBindingModel.CreatedDate.ToString());
                    
                    return View("create", ConditionalLendingBindingModel);
                }
                TempData["SuccessMessage"] = "Successfully Created New Conditional Lending " + ConditionalLendingBindingModel;
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = ConditionalLendingBindingModel.ErrorMessages;
                TempData["Error2"] = ConditionalLendingBindingModel.ErrorMessages;
                ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(ConditionalLendingBindingModel.LoanProductId.ToString());
                ViewBag.IndividualTypeSelectList = GetIndividualTypeSelectList(ConditionalLendingBindingModel.LoanProductDescription.ToString());
                ViewBag.IdentityCardSelectList = GetIdentityCardTypeSelectList(ConditionalLendingBindingModel.Description.ToString());
                ViewBag.SalutationSelectList = GetSalutationSelectList(ConditionalLendingBindingModel.IsLocked.ToString());
                ViewBag.GenderSelectList = GetGenderSelectList(ConditionalLendingBindingModel.CreatedBy.ToString());
                ViewBag.MaritalStatusSelectList = GetMaritalStatusSelectList(ConditionalLendingBindingModel.CreatedDate.ToString());

                return View("Create", ConditionalLendingBindingModel);
            }
        }



        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
            var ConditionalLendingDTO = await _channelService.FindCustomerAsync(id, GetServiceHeader());
            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
            ViewBag.IndividualTypeSelectList = GetIndividualTypeSelectList(string.Empty);
            ViewBag.IdentityCardSelectList = GetIdentityCardTypeSelectList(string.Empty);
            ViewBag.SalutationSelectList = GetSalutationSelectList(string.Empty);
            ViewBag.GenderSelectList = GetGenderSelectList(string.Empty);
            ViewBag.MaritalStatusSelectList = GetMaritalStatusSelectList(string.Empty);
            ViewBag.IndividualNationalitySelectList = GetNationalitySelectList(string.Empty);
            ViewBag.IndividualEmploymentTermsOfServiceSelectList = GetTermsOfServiceSelectList(string.Empty);
            ViewBag.IndividualClassificationSelectList = GetCustomerClassificationSelectList(string.Empty);
            ViewBag.recordstatus = GetRecordStatusSelectList(string.Empty);
            return View(ConditionalLendingDTO.MapTo<CustomerBindingModel>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, ConditionalLendingBindingModel ConditionalLendingBindingModel)
        {
            ConditionalLendingBindingModel.ValidateAll();

            if (!ConditionalLendingBindingModel.HasErrors)
            {
                await _channelService.UpdateCustomerAsync(ConditionalLendingBindingModel.MapTo<CustomerDTO>(), GetServiceHeader());
                TempData["SuccessMessage"] = $"Successfully Edited {ConditionalLendingBindingModel} ConditionalLending {ConditionalLendingBindingModel.Id}";
                ViewBag.recordstatus = GetRecordStatusSelectList(ConditionalLendingBindingModel.ToString());
                ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(ConditionalLendingBindingModel.LoanProductId.ToString());
                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(ConditionalLendingBindingModel.LoanProductId.ToString());
                ViewBag.IndividualTypeSelectList = GetIndividualTypeSelectList(ConditionalLendingBindingModel.LoanProductDescription.ToString());
                ViewBag.IdentityCardSelectList = GetIdentityCardTypeSelectList(ConditionalLendingBindingModel.Description.ToString());
                ViewBag.SalutationSelectList = GetSalutationSelectList(ConditionalLendingBindingModel.IsLocked.ToString());
                ViewBag.GenderSelectList = GetGenderSelectList(ConditionalLendingBindingModel.CreatedBy.ToString());
                ViewBag.MaritalStatusSelectList = GetMaritalStatusSelectList(ConditionalLendingBindingModel.CreatedDate.ToString());

                return View(ConditionalLendingBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetCustomersAsync()
        {
            var customersDTOs = await _channelService.FindCustomersAsync(GetServiceHeader());

            return Json(customersDTOs, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<JsonResult> GetCustomerByIdentityNumberAsync(string individualIdentityCardNumber, bool matchExtact)
        {
            var customersDTOs = await _channelService.FindCustomersByIdentityCardNumberAsync(individualIdentityCardNumber, matchExtact, GetServiceHeader());

            return Json(customersDTOs, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<JsonResult> GetCustomersByIDyNumberAsync(string individualIdentityCardNumber)
        {
            var customersDTOs = await _channelService.FindCustomersByIDNumberAsync(individualIdentityCardNumber, GetServiceHeader());

            return Json(customersDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
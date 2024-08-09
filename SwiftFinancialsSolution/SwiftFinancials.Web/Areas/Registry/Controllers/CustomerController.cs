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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Registry.Controllers
{
    [RoleBasedAccessControl]
    public class CustomerController : MasterController
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

            var pageCollectionInfo = await _channelService.FindCustomersByFilterInPageAsync(jQueryDataTablesModel.sSearch, 1, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CustomerDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var customerDTO = await _channelService.FindCustomerAsync(id, GetServiceHeader());

            return View(customerDTO);
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

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(CustomerBindingModel customerBindingModel)
        {
            var registrationdate = Request["registrationdate"];
            var birthdate = Request["birthdate"];
            var employmentdate = Request["employmentdate"];

            customerBindingModel.IndividualBirthDate = DateTime.ParseExact((Request["birthdate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            customerBindingModel.RegistrationDate = DateTime.ParseExact((Request["registrationdate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            customerBindingModel.IndividualEmploymentDate = DateTime.ParseExact((Request["employmentdate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            var userDTO = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());

            customerBindingModel.BranchId =(Guid) userDTO.BranchId;

            customerBindingModel.ValidateAll();

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

            if (!customerBindingModel.HasErrors)
            {

                var result = await _channelService.AddCustomerAsync(customerBindingModel.MapTo<CustomerDTO>(), debitTypes.ToList(), investmentProducts.ToList(), savingsProducts.ToList(), mandatoryProducts, 1, GetServiceHeader());
                if (result.ErrorMessageResult != null)
                {
                    TempData["Error"] = result.ErrorMessageResult + result.ErrorMessages;
                    await ServeNavigationMenus();
                    ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(customerBindingModel.Type.ToString());
                    ViewBag.IndividualTypeSelectList = GetIndividualTypeSelectList(customerBindingModel.IndividualType.ToString());
                    ViewBag.IdentityCardSelectList = GetIdentityCardTypeSelectList(customerBindingModel.IndividualIdentityCardType.ToString());
                    ViewBag.SalutationSelectList = GetSalutationSelectList(customerBindingModel.IndividualSalutation.ToString());
                    ViewBag.GenderSelectList = GetGenderSelectList(customerBindingModel.IndividualGender.ToString());
                    ViewBag.MaritalStatusSelectList = GetMaritalStatusSelectList(customerBindingModel.IndividualMaritalStatus.ToString());
                    ViewBag.IndividualNationalitySelectList = GetNationalitySelectList(customerBindingModel.IndividualNationality.ToString());
                    ViewBag.IndividualEmploymentTermsOfServiceSelectList = GetTermsOfServiceSelectList(customerBindingModel.IndividualEmploymentTermsOfService.ToString());
                    ViewBag.IndividualClassificationSelectList = GetCustomerClassificationSelectList(customerBindingModel.IndividualClassification.ToString());

               

                    return View("index", customerBindingModel);
                }
                TempData["SuccessMessage"] = "Successfully Created Customer " + customerBindingModel.FullName;
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = customerBindingModel.ErrorMessages;
                TempData["Error"] = customerBindingModel.ErrorMessages;

                ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(customerBindingModel.Type.ToString());
                ViewBag.IndividualTypeSelectList = GetIndividualTypeSelectList(customerBindingModel.IndividualType.ToString());
                ViewBag.IdentityCardSelectList = GetIdentityCardTypeSelectList(customerBindingModel.IndividualIdentityCardType.ToString());
                ViewBag.SalutationSelectList = GetSalutationSelectList(customerBindingModel.IndividualSalutation.ToString());
                ViewBag.GenderSelectList = GetGenderSelectList(customerBindingModel.IndividualGender.ToString());
                ViewBag.MaritalStatusSelectList = GetMaritalStatusSelectList(customerBindingModel.IndividualMaritalStatus.ToString());
                ViewBag.IndividualNationalitySelectList = GetNationalitySelectList(customerBindingModel.IndividualNationality.ToString());
                ViewBag.IndividualEmploymentTermsOfServiceSelectList = GetTermsOfServiceSelectList(customerBindingModel.IndividualEmploymentTermsOfService.ToString());
                ViewBag.IndividualClassificationSelectList = GetCustomerClassificationSelectList(customerBindingModel.IndividualClassification.ToString());

                return View("Create", customerBindingModel);
            }
        }




        public async Task<ActionResult> Directors()
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

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Directors(CustomerBindingModel customerBindingModel)
        {
            var registrationdate = Request["registrationdate"];
            var birthdate = Request["birthdate"];
            var employmentdate = Request["employmentdate"];

            customerBindingModel.IndividualBirthDate = DateTime.ParseExact((Request["birthdate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            customerBindingModel.RegistrationDate = DateTime.ParseExact((Request["registrationdate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            customerBindingModel.IndividualEmploymentDate = DateTime.ParseExact((Request["employmentdate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            customerBindingModel.ValidateAll();


            //cheat
            var mandatoryInvestmentProducts = new List<InvestmentProductDTO>();
            var mandatorySavingsProducts = new List<SavingsProductDTO>();
            var mandatoryDebitTypes = new List<DebitTypeDTO>();
            var mandatoryProducts = new ProductCollectionInfo();


            var debitTypes = await _channelService.FindMandatoryDebitTypesAsync(true, GetServiceHeader());
            var investmentProducts = await _channelService.FindMandatoryInvestmentProductsAsync(true, GetServiceHeader());
            var savingsProducts = await _channelService.FindMandatorySavingsProductsAsync(true, GetServiceHeader());

            mandatoryProducts.InvestmentProductCollection = investmentProducts.ToList();
            mandatoryProducts.SavingsProductCollection = savingsProducts.ToList();

            if (!customerBindingModel.HasErrors)
            {
                await _channelService.AddCustomerAsync(customerBindingModel.MapTo<CustomerDTO>(), debitTypes.ToList(), investmentProducts.ToList(), savingsProducts.ToList(), mandatoryProducts, 1, GetServiceHeader());
                TempData["SuccessMessage"] = "Successfully Created Customer " + customerBindingModel.FullName;

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = customerBindingModel.ErrorMessages;

                ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(customerBindingModel.Type.ToString());
                ViewBag.IndividualTypeSelectList = GetIndividualTypeSelectList(customerBindingModel.IndividualType.ToString());
                ViewBag.IdentityCardSelectList = GetIdentityCardTypeSelectList(customerBindingModel.IndividualIdentityCardType.ToString());
                ViewBag.SalutationSelectList = GetSalutationSelectList(customerBindingModel.IndividualSalutation.ToString());
                ViewBag.GenderSelectList = GetGenderSelectList(customerBindingModel.IndividualGender.ToString());
                ViewBag.MaritalStatusSelectList = GetMaritalStatusSelectList(customerBindingModel.IndividualMaritalStatus.ToString());
                ViewBag.IndividualNationalitySelectList = GetNationalitySelectList(customerBindingModel.IndividualNationality.ToString());
                ViewBag.IndividualEmploymentTermsOfServiceSelectList = GetTermsOfServiceSelectList(customerBindingModel.IndividualEmploymentTermsOfService.ToString());
                ViewBag.IndividualClassificationSelectList = GetCustomerClassificationSelectList(customerBindingModel.IndividualClassification.ToString());

                return View(customerBindingModel);
            }
        }





        public async Task<ActionResult> Delegates(Guid id)
        {
            await ServeNavigationMenus();
            var customer = await _channelService.FindCustomerAsync(id, GetServiceHeader());

            DelegateDTO withdrawalNotificationDTO = new DelegateDTO();

            if (customer != null)
            {

                withdrawalNotificationDTO.CustomerId = customer.Id;
                withdrawalNotificationDTO.CustomerIndividualFirstName = customer.FullName;
                withdrawalNotificationDTO.CustomerIndividualPayrollNumbers = customer.IndividualPayrollNumbers;
                withdrawalNotificationDTO.CustomerSerialNumber = customer.SerialNumber;
                withdrawalNotificationDTO.CustomerIndividualIdentityCardNumber = customer.IndividualIdentityCardNumber;
                withdrawalNotificationDTO.CustomerStationZoneDescription = customer.StationZoneDescription;
                withdrawalNotificationDTO.ZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
            }

            return View(withdrawalNotificationDTO);

        }

        [HttpPost]
        public async Task<ActionResult> Delegates(DelegateDTO customerBindingModel)
        {
            if (!customerBindingModel.HasErrors)
            {
                await _channelService.AddDelegateAsync(customerBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = customerBindingModel.ErrorMessages;
                return View(customerBindingModel);
            }
        }




        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
            var customerDTO = await _channelService.FindCustomerAsync(id, GetServiceHeader());
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
            return View(customerDTO.MapTo<CustomerBindingModel>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, CustomerBindingModel customerBindingModel)
        {
            customerBindingModel.ValidateAll();

            if (!customerBindingModel.HasErrors)
            {
                await _channelService.UpdateCustomerAsync(customerBindingModel.MapTo<CustomerDTO>(), GetServiceHeader());
                TempData["SuccessMessage"] = $"Successfully {customerBindingModel.RecordStatusDescription} Customer {customerBindingModel.FullName}";
                ViewBag.recordstatus = GetRecordStatusSelectList(customerBindingModel.RecordStatus.ToString());
                ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(customerBindingModel.Type.ToString());
                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.recordstatus = GetRecordStatusSelectList(customerBindingModel.RecordStatus.ToString());
                ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(customerBindingModel.Type.ToString());
                ViewBag.IndividualTypeSelectList = GetIndividualTypeSelectList(customerBindingModel.IndividualType.ToString());
                ViewBag.IdentityCardSelectList = GetIdentityCardTypeSelectList(customerBindingModel.IndividualIdentityCardType.ToString());
                ViewBag.SalutationSelectList = GetSalutationSelectList(customerBindingModel.IndividualSalutation.ToString());
                ViewBag.GenderSelectList = GetGenderSelectList(customerBindingModel.IndividualGender.ToString());
                ViewBag.MaritalStatusSelectList = GetMaritalStatusSelectList(customerBindingModel.IndividualMaritalStatus.ToString());
                ViewBag.IndividualNationalitySelectList = GetNationalitySelectList(customerBindingModel.IndividualNationality.ToString());
                ViewBag.IndividualEmploymentTermsOfServiceSelectList = GetTermsOfServiceSelectList(customerBindingModel.IndividualEmploymentTermsOfService.ToString());
                ViewBag.IndividualClassificationSelectList = GetCustomerClassificationSelectList(customerBindingModel.IndividualClassification.ToString());

                return View(customerBindingModel);
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
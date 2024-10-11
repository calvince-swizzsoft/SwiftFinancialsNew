using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AdministrationModule;
using SwiftFinancials.Web.Attributes;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using SwiftFinancials.Presentation.Infrastructure.Util;
using Application.MainBoundedContext.DTO.AccountsModule;
using System.Collections.ObjectModel;

namespace SwiftFinancials.Web.Areas.Admin.Controllers
{
    [RoleBasedAccessControl]
    public class CompanyController : MasterController
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

            var pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var pageCollectionInfo = await _channelService.FindCompaniesByFilterInPageAsync(jQueryDataTablesModel.sSearch, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;
                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(company => company.CreatedDate).ToList();
                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CompanyDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var CompanyDTO = await _channelService.FindCompanyAsync(id, GetServiceHeader());

            // Retrieve all available debit, investment, and savings products
            var debitTypes = await _channelService.FindMandatoryDebitTypesAsync(true, GetServiceHeader());
            var creditTypes = await _channelService.FindCreditTypesAsync(GetServiceHeader());
            var allInvestmentProducts = await _channelService.FindMandatoryInvestmentProductsAsync(true, GetServiceHeader());
            var allSavingsProducts = await _channelService.FindMandatorySavingsProductsAsync(true, GetServiceHeader());

            // Retrieve company and its linked products
            var debitTypeDTOs = await _channelService.FindDebitTypesByCompanyIdAsync(CompanyDTO.Id, GetServiceHeader());
            var linkedProducts = await _channelService.FindAttachedProductsByCompanyIdAsync(CompanyDTO.Id, GetServiceHeader());

            // Pass the full list and linked items to ViewBag
            ViewBag.SelectedDebitTypes = debitTypeDTOs.Select(d => d.Id).ToList();
            if (linkedProducts != null)
            {
                ViewBag.SelectedInvestmentProducts = linkedProducts.InvestmentProductCollection.Select(i => i.Id).ToList();
                ViewBag.SelectedSavingsProducts = linkedProducts.SavingsProductCollection.Select(s => s.Id).ToList();
            }
            // Pass all products to the view
            ViewBag.allInvestments = allInvestmentProducts;
            ViewBag.allSavings = allSavingsProducts;
            ViewBag.debit = debitTypes;
            return View(CompanyDTO.MapTo<CompanyBindingModel>());
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            var debitTypes = await _channelService.FindMandatoryDebitTypesAsync(true, GetServiceHeader());
            var creditTypes = await _channelService.FindCreditTypesAsync(GetServiceHeader());
            var investmentProducts = await _channelService.FindMandatoryInvestmentProductsAsync(true, GetServiceHeader());
            var savingsProducts = await _channelService.FindMandatorySavingsProductsAsync(true, GetServiceHeader());


            var savingsProductDTOs = await _channelService.FindSavingsProductsAsync(GetServiceHeader());
            var investment = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());
            var debitypes = await _channelService.FindDebitTypesAsync(GetServiceHeader());
            ViewBag.investment = investment;
            ViewBag.savings = savingsProductDTOs;
            ViewBag.debit = debitypes;
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(CompanyBindingModel companyBindingModel, string[] debittypes, string[] savingsproducts, string[] investmentproducts)
        {
            //cheat
            var mandatoryInvestmentProducts = new List<InvestmentProductDTO>();
            var mandatorySavingsProducts = new List<SavingsProductDTO>();
            var mandatoryDebitTypes = new ObservableCollection<DebitTypeDTO>();
            var mandatoryProducts = new ProductCollectionInfo();

            SavingsProductDTO j = new SavingsProductDTO();
            foreach (var k in savingsproducts)
            {
                j.Id = Guid.Parse(k);
                mandatorySavingsProducts.Add(j);
            }
            mandatoryProducts.SavingsProductCollection = mandatorySavingsProducts;



            InvestmentProductDTO investmentProductDTO = new InvestmentProductDTO();
            foreach (var invest in investmentproducts)
            {
                investmentProductDTO.Id = Guid.Parse(invest);
                mandatoryInvestmentProducts.Add(investmentProductDTO);
            }
            mandatoryProducts.InvestmentProductCollection = mandatoryInvestmentProducts;


            DebitTypeDTO debitTypeDTO = new DebitTypeDTO();
            foreach (var debit in debittypes)
            {
                debitTypeDTO.Id = Guid.Parse(debit);
                mandatoryDebitTypes.Add(debitTypeDTO);
            }


            companyBindingModel.ValidateAll();
            companyBindingModel.RecoveryPriority = "DirectDebits";
            if (!companyBindingModel.HasErrors)
            {
                var companies = await _channelService.AddCompanyAsync(companyBindingModel.MapTo<CompanyDTO>(), GetServiceHeader());
                await _channelService.UpdateAttachedProductsByCompanyIdAsync(companies.Id, mandatoryProducts, GetServiceHeader());

                await _channelService.UpdateDebitTypesByCompanyIdAsync(companies.Id, mandatoryDebitTypes, GetServiceHeader());


                TempData["Success"] = "Company Registered Successfully";
                await ServeNavigationMenus();

                return View("Index");

            }
            else
            {
                var errorMessages = companyBindingModel.ErrorMessages;

                return View(companyBindingModel);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            // Retrieve all available debit, investment, and savings products
            var debitTypes = await _channelService.FindMandatoryDebitTypesAsync(true, GetServiceHeader());
            var creditTypes = await _channelService.FindCreditTypesAsync(GetServiceHeader());
            var allInvestmentProducts = await _channelService.FindMandatoryInvestmentProductsAsync(true, GetServiceHeader());
            var allSavingsProducts = await _channelService.FindMandatorySavingsProductsAsync(true, GetServiceHeader());

            // Retrieve company and its linked products
            var CompanyDTO = await _channelService.FindCompanyAsync(id, GetServiceHeader());
            var debitTypeDTOs = await _channelService.FindDebitTypesByCompanyIdAsync(CompanyDTO.Id, GetServiceHeader());
            var linkedProducts = await _channelService.FindAttachedProductsByCompanyIdAsync(CompanyDTO.Id, GetServiceHeader());

            // Pass the full list and linked items to ViewBag
            ViewBag.SelectedDebitTypes = debitTypeDTOs.Select(d => d.Id).ToList();
            ViewBag.SelectedInvestmentProducts = linkedProducts.InvestmentProductCollection.Select(i => i.Id).ToList();
            ViewBag.SelectedSavingsProducts = linkedProducts.SavingsProductCollection.Select(s => s.Id).ToList();

            // Pass all products to the view
            ViewBag.allInvestments = allInvestmentProducts;
            ViewBag.allSavings = allSavingsProducts;
            ViewBag.debit = debitTypes;

            return View(CompanyDTO.MapTo<CompanyBindingModel>());
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, CompanyBindingModel companyBindingModel, string[] debittypes, string[] savingsproducts, string[] investmentproducts)
        {
            //cheat
            var mandatoryInvestmentProducts = new List<InvestmentProductDTO>();
            var mandatorySavingsProducts = new List<SavingsProductDTO>();
            var mandatoryDebitTypes = new ObservableCollection<DebitTypeDTO>();
            var mandatoryProducts = new ProductCollectionInfo();

            SavingsProductDTO j = new SavingsProductDTO();
            foreach (var k in savingsproducts)
            {
                j.Id = Guid.Parse(k);
                mandatorySavingsProducts.Add(j);
            }
            mandatoryProducts.SavingsProductCollection = mandatorySavingsProducts;



            InvestmentProductDTO investmentProductDTO = new InvestmentProductDTO();
            foreach (var invest in investmentproducts)
            {
                investmentProductDTO.Id = Guid.Parse(invest);
                mandatoryInvestmentProducts.Add(investmentProductDTO);
            }
            mandatoryProducts.InvestmentProductCollection = mandatoryInvestmentProducts;


            DebitTypeDTO debitTypeDTO = new DebitTypeDTO();
            foreach (var debit in debittypes)
            {
                debitTypeDTO.Id = Guid.Parse(debit);
                mandatoryDebitTypes.Add(debitTypeDTO);
            }

            if (ModelState.IsValid)
            {
                await _channelService.UpdateCompanyAsync(companyBindingModel.MapTo<CompanyDTO>(), GetServiceHeader());
                await _channelService.UpdateAttachedProductsByCompanyIdAsync(id, mandatoryProducts, GetServiceHeader());

                await _channelService.UpdateDebitTypesByCompanyIdAsync(id, mandatoryDebitTypes, GetServiceHeader());

                TempData["Success"] = "Company Successfully Edited";

                return RedirectToAction("Index");
            }
            else
            {
                return View(companyBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetCompaniesAsync()
        {
            var companiesDTOs = await _channelService.FindCompaniesAsync(GetServiceHeader());

            return Json(companiesDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class CreditTypeController : MasterController
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

            var pageCollectionInfo = await _channelService.
                FindCreditTypesByFilterInPageAsync(jQueryDataTablesModel.sSearch, 0, int.MaxValue, GetServiceHeader());

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
                items: new List<CreditTypeDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }


        [HttpPost]
        public async Task<JsonResult> ChartOfAccountsIndex(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;

            var pageCollectionInfo = await _channelService.
                FindChartOfAccountsByFilterInPageAsync(jQueryDataTablesModel.sSearch, 0, int.MaxValue, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                var filteredData = pageCollectionInfo.PageCollection
                    .Where(c => c.AccountCategory != (int)ChartOfAccountCategory.HeaderAccount)
                    .ToList();

                var sortedData = filteredData
                    .OrderByDescending(c => c.CreatedDate)
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
                items: new List<ChartOfAccountDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }


        public async Task<ActionResult> GetApplicableCharges()
        {
            var applicableCharges = await _channelService.FindCommissionsAsync(GetServiceHeader());
            return Json(applicableCharges);
        }

        public async Task<ActionResult> GetApplicableDirectDebits()
        {
            var directDebits = await _channelService.FindDirectDebitsAsync(GetServiceHeader());
            return Json(directDebits);
        }

        public async Task<ActionResult> GetAttachedLoanProducts()
        {
            var loanProducts = await _channelService.FindLoanProductsAsync(GetServiceHeader());
            return Json(loanProducts);
        }

        public async Task<ActionResult> GetConcessionExemptLoanProducts()
        {
            var loanProducts = await _channelService.FindLoanProductsAsync(GetServiceHeader());
            return Json(loanProducts);
        }

        public async Task<ActionResult> GetAttachedInvestmentsProducts()
        {
            var investmentsProducts = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());
            return Json(investmentsProducts);
        }

        public async Task<ActionResult> GetAttachedSavingsProducts()
        {
            var savingsProducts = await _channelService.FindSavingsProductsAsync(GetServiceHeader());
            return Json(savingsProducts);
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var creditTypeDTO = await _channelService.FindCreditTypeAsync(id, GetServiceHeader());

            var ApplicableCharges = await _channelService.FindCommissionsByCreditTypeIdAsync(id, GetServiceHeader());
            var ApplicableDirectDebits = await _channelService.FindDirectDebitsByCreditTypeIdAsync(id, GetServiceHeader());
            var findAttachedProducts = await _channelService.FindAttachedProductsByCreditTypeIdAsync(id, GetServiceHeader());

            var findConcessionExemptProducts = await _channelService.FindConcessionExemptProductsByCreditTypeIdAsync(id, GetServiceHeader());

            var AttachedLoanProducts = findAttachedProducts.LoanProductCollection;
            var AttachedSavingsProducts = findAttachedProducts.SavingsProductCollection;
            var AttachedInvestmentsProducts = findAttachedProducts.InvestmentProductCollection;

            var ConcessionExemptLoanProducts = findConcessionExemptProducts.LoanProductCollection;

            return View(creditTypeDTO);
        }


        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            ViewBag.TransactionOwnershipSelectList = GetTransactionOwnershipSelectList(string.Empty);
            return View();
        }


        public async Task<ActionResult> ChartOfAccountLookUp(Guid? id, CreditTypeDTO creditTypeDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var chartOfAccount = await _channelService.FindChartOfAccountAsync(parseId, GetServiceHeader());


            if (chartOfAccount != null)
            {
                creditTypeDTO.ChartOfAccountId = chartOfAccount.Id;
                creditTypeDTO.ChartOfAccountAccountName = chartOfAccount.AccountName;


                return Json(new
                {
                    success = true,
                    data = new
                    {
                        ChartOfAccountId = creditTypeDTO.ChartOfAccountId,
                        ChartOfAccountAccountName = creditTypeDTO.ChartOfAccountAccountName
                    }
                });
            }
            return Json(new { success = false, message = "Product Not Found!" });
        }

        [HttpPost]
        public async Task<ActionResult> Create
            (CreditTypeDTO creditTypeDTO, string SelectedApplicableChargesIds, string SelectedApplicableDirectDebitsIds,
            string SelectedAttachedLoanProductsIds, string SelectedCELoanProductsIds, string SelectedAttachedInvestmentsProductsIds, string SelectedAttachedSavingsProductsIds)
        {
            await ServeNavigationMenus();

            var applicableChargesIds = new List<Guid>();
            var applicableDirectDebitIds = new List<Guid>();
            var attachedLoanProducts = new List<Guid>();
            var cELoanProducts = new List<Guid>();
            var attachedInvestmentsProducts = new List<Guid>();
            var attachedSavingsProducts = new List<Guid>();

            var ApplicableCharges = new ObservableCollection<CommissionDTO>();
            var ApplicableDirectDebits = new ObservableCollection<DirectDebitDTO>();

            var AttachedLoanProducts = new List<LoanProductDTO>();
            var ConcessionExemptLoanProducts = new List<LoanProductDTO>();
            var AttachedInvestmentsProducts = new List<InvestmentProductDTO>();
            var AttachedSavingsProducts = new List<SavingsProductDTO>();

            // Applicable Charges
            if (SelectedApplicableChargesIds != string.Empty || SelectedApplicableChargesIds != "")
            {
                applicableChargesIds = SelectedApplicableChargesIds.Split(',').Select(Guid.Parse).ToList();

                foreach (var id in applicableChargesIds)
                {
                    ApplicableCharges.Add(new CommissionDTO { Id = id });
                }
            }

            // Applicable Direct Debits
            if (SelectedApplicableDirectDebitsIds != string.Empty || SelectedApplicableDirectDebitsIds != "")
            {
                applicableDirectDebitIds = SelectedApplicableDirectDebitsIds.Split(',').Select(Guid.Parse).ToList();

                foreach (var id in applicableDirectDebitIds)
                {
                    ApplicableDirectDebits.Add(new DirectDebitDTO { Id = id });
                }
            }

            // Attached Loan Products
            if (SelectedAttachedLoanProductsIds != string.Empty || SelectedAttachedLoanProductsIds != "")
            {
                attachedLoanProducts = SelectedAttachedLoanProductsIds.Split(',').Select(Guid.Parse).ToList();

                foreach (var productId in attachedLoanProducts)
                {
                    var AttachedLoanProduct = await _channelService.FindLoanProductAsync(productId, GetServiceHeader());
                    AttachedLoanProducts.Add(AttachedLoanProduct);
                }
            }

            // Concession-Exempt Loan Products
            if (SelectedCELoanProductsIds != string.Empty || SelectedCELoanProductsIds != "")
            {
                cELoanProducts = SelectedCELoanProductsIds.Split(',').Select(Guid.Parse).ToList();

                foreach (var productId in cELoanProducts)
                {
                    var ConcessionExemptLoanProduct = await _channelService.FindLoanProductAsync(productId, GetServiceHeader());
                    ConcessionExemptLoanProducts.Add(ConcessionExemptLoanProduct);
                }
            }

            // Attached Investments Products
            if (SelectedAttachedInvestmentsProductsIds != string.Empty || SelectedAttachedInvestmentsProductsIds != "")
            {
                attachedInvestmentsProducts = SelectedAttachedInvestmentsProductsIds.Split(',').Select(Guid.Parse).ToList();

                foreach (var productId in attachedInvestmentsProducts)
                {
                    var attachedInvestmentProduct = await _channelService.FindInvestmentProductAsync(productId, GetServiceHeader());
                    AttachedInvestmentsProducts.Add(attachedInvestmentProduct);
                }
            }

            // Attached Savings Products
            if (SelectedAttachedSavingsProductsIds != string.Empty || SelectedAttachedSavingsProductsIds != "")
            {
                attachedSavingsProducts = SelectedAttachedSavingsProductsIds.Split(',').Select(Guid.Parse).ToList();

                foreach (var productId in attachedSavingsProducts)
                {
                    var attachedSavingsProduct = await _channelService.FindSavingsProductAsync(productId, GetServiceHeader());
                    AttachedSavingsProducts.Add(attachedSavingsProduct);
                }
            }

            creditTypeDTO.ValidateAll();

            if (!creditTypeDTO.HasErrors)
            {
                var productCollectionInfo = new ProductCollectionInfo
                {
                    InvestmentProductCollection = AttachedInvestmentsProducts,
                    SavingsProductCollection = AttachedSavingsProducts,
                    LoanProductCollection = AttachedLoanProducts
                };
                var concessionExemptLoanProductCollectionInfo = new ProductCollectionInfo
                {
                    LoanProductCollection = ConcessionExemptLoanProducts
                };

                await ServeNavigationMenus();
                var submit = await _channelService.AddCreditTypeAsync(creditTypeDTO, GetServiceHeader());

                if (submit == null)
                {
                    await ServeNavigationMenus();
                    ViewBag.TransactionOwnershipSelectList = GetTransactionOwnershipSelectList(creditTypeDTO.TransactionOwnershipDescription.ToString());
                    TempData["ErrorMessageResult"] = "Failed to Create Credit Type";
                    return View(creditTypeDTO);
                }

                if (ApplicableCharges != null)
                    await _channelService.UpdateCommissionsByCreditTypeIdAsync(submit.Id, ApplicableCharges, GetServiceHeader());
                if (ApplicableDirectDebits != null)
                    await _channelService.UpdateDirectDebitsByCreditTypeIdAsync(submit.Id, ApplicableDirectDebits, GetServiceHeader());
                if (productCollectionInfo != null)
                    await _channelService.UpdateAttachedProductsByCreditTypeIdAsync(submit.Id, productCollectionInfo, GetServiceHeader());
                if (ConcessionExemptLoanProducts != null)
                    await _channelService.UpdateConcessionExemptProductsByCreditTypeIdAsync(submit.Id, concessionExemptLoanProductCollectionInfo, GetServiceHeader());

                TempData["SuccessMessage"] = "Create successful.";
                return RedirectToAction("Index");
            }
            else
            {
                await ServeNavigationMenus();
                var errorMessages = creditTypeDTO.ErrorMessages;
                ViewBag.TransactionOwnershipSelectList = GetTransactionOwnershipSelectList(creditTypeDTO.TransactionOwnershipDescription.ToString());
                return View(creditTypeDTO);
            }
        }





        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.TransactionOwnershipSelectList = GetTransactionOwnershipSelectList(string.Empty);

            var creditTypeDTO = await _channelService.FindCreditTypeAsync(id, GetServiceHeader());

            return View(creditTypeDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(CreditTypeDTO creditTypeDTOBindingModel)
        {

            creditTypeDTOBindingModel.ValidateAll();

            if (!creditTypeDTOBindingModel.HasErrors)
            {
                await _channelService.UpdateCreditTypeAsync(creditTypeDTOBindingModel, GetServiceHeader());

                ViewBag.TransactionOwnershipSelectList = GetTransactionOwnershipSelectList(creditTypeDTOBindingModel.TransactionOwnershipDescription.ToString());

                return RedirectToAction("Index");
            }
            else
            {
                return View(creditTypeDTOBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetCreditTypesAsync()
        {
            var creditTypeDTOs = await _channelService.FindCreditTypesAsync(GetServiceHeader());

            return Json(creditTypeDTOs, JsonRequestBehavior.AllowGet);
        }
    }

}

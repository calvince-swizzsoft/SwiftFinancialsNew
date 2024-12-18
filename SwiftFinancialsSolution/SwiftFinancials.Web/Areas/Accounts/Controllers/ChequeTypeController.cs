using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class ChequeTypeController : MasterController
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

            var pageCollectionInfo = await _channelService.FindChequeTypesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;
                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(levy => levy.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<ChequeTypeDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var chequeTypeDTO = await _channelService.FindChequeTypeAsync(id, GetServiceHeader());

            return View(chequeTypeDTO);
        }


        [HttpPost]
        public ActionResult StoreSelectedApplicableCharges(List<CommissionDTO> selectedCharges)
        {
            // If selectedCharges is null or empty, clear the session
            if (selectedCharges != null && selectedCharges.Count > 0)
            {
                var observableCharges = new ObservableCollection<CommissionDTO>(selectedCharges);
                Session["selectedCharges"] = observableCharges;
            }
            else
            {
                // Optionally clear session if no charges are selected
                Session.Remove("selectedCharges");
            }

            return Json(new { success = true });
        }
        [HttpPost]
        public ActionResult StoreSelectedLoanProducts(List<Guid> selectedProducts)
        {
            if (selectedProducts != null && selectedProducts.Count > 0)
            {
                var selectedProductList = new ObservableCollection<Guid>(selectedProducts);
                Session["selectedLoanProducts"] = selectedProductList;
            }
            else
            {
                Session.Remove("selectedLoanProducts");
            }

            return Json(new { success = true });
        }


        [HttpPost]
        public JsonResult StoreSelectedInvestmentProducts(List<InvestmentProductDTO> selectedProducts)
        {

            Session["SelectedInvestmentProducts"] = selectedProducts;

            return Json(new { success = true });
        }





        public async Task<ActionResult> Create(ProductCollectionInfo productCollectionInfo)
        {
            await ServeNavigationMenus();

            ViewBag.ChequeTypeChargeRecoveryModeSelectList = GetChequeTypeChargeRecoveryModeSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(ChequeTypeDTO chequeTypeDTO)
        {
            var selectedCharges = Session["selectedCharges"] as ObservableCollection<CommissionDTO>;

            if (selectedCharges == null || selectedCharges.Count == 0)
            {
                return Json(new { success = false, message = "No charges selected" });
            }

            var selectedLoanProducts = Session["selectedLoanProducts"] as ObservableCollection<Guid>;

            var selectedInvestmentProducts = Session["SelectedInvestmentProducts"] as List<InvestmentProductDTO>;

            if ((selectedLoanProducts == null || selectedLoanProducts.Count == 0) &&
                (selectedInvestmentProducts == null || selectedInvestmentProducts.Count == 0))
            {
                return Json(new { success = false, message = "No products selected" });
            }

            var newChequeType = await _channelService.AddChequeTypeAsync(chequeTypeDTO);
            if (newChequeType == null)
            {
                return Json(new { success = false, message = "Error adding cheque type" });
            }

            var chequeTypeId = newChequeType.Id;
            var updateCommissionsSuccess = await _channelService.UpdateCommissionsByChequeTypeIdAsync(chequeTypeId, selectedCharges);

            var attachedProductsTuple = new ProductCollectionInfo
            {
                InvestmentProductCollection = selectedInvestmentProducts ?? new List<InvestmentProductDTO>(),

                LoanProductCollection = selectedLoanProducts?.Select(p => new LoanProductDTO
                {
                    Id = p, 
                    Description = "Loan Product" 
                }).ToList() ?? new List<LoanProductDTO>()
            };

            var updateProductsSuccess = await _channelService.UpdateAttachedProductsByChequeTypeIdAsync(chequeTypeId, attachedProductsTuple);

            if (updateCommissionsSuccess && updateProductsSuccess)
            {
                return Json(new { success = true, message = "Cheque type, commissions, and attached products successfully created/updated" });

            }
            else
            {
                return Json(new { success = false, message = "Error updating commissions or attached products" });
            }

        }


        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.ChequeTypeChargeRecoveryModeSelectList = GetChequeTypeChargeRecoveryModeSelectList(string.Empty);
            var chequeTypeDTO = await _channelService.FindChequeTypeAsync(id, GetServiceHeader());

            return View(chequeTypeDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, ChequeTypeDTO chequeTypeBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateChequeTypeAsync(chequeTypeBindingModel, GetServiceHeader());
                ViewBag.ChequeTypeChargeRecoveryModeSelectList = GetChequeTypeChargeRecoveryModeSelectList(chequeTypeBindingModel.ChargeRecoveryMode.ToString());
                return RedirectToAction("Index");
            }
            else
            {
                return View(chequeTypeBindingModel);
            }
        }

        /*[HttpGet]
        public async Task<JsonResult> GetChequeTypesAsync()
        {
            var chequeTypeDTOs = await _channelService.FindChequeTypesAsync(GetServiceHeader());

            return Json(chequeTypeDTOs, JsonRequestBehavior.AllowGet);
        }*/
    }
}

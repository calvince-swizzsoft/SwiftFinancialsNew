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


        public async Task<ActionResult> SavingsProduct(ObservableCollection<SavingsProductDTO> savingProductRowData)
        {
            Session["savingsProductIds"] = savingProductRowData;
            

            return View("Create", savingProductRowData);
        }

        public async Task<ActionResult> LoansProduct(ObservableCollection<LoanProductDTO> loansProductRowData)
        {
            Session["loansProductIds"] = loansProductRowData;

            return View("Create", loansProductRowData);
        }

        public async Task<ActionResult> InvestmentsProduct(ObservableCollection<InvestmentProductDTO> investmentProductRowData)
        {
            Session["investmentsProductIds"] = investmentProductRowData;

            return View("Create", investmentProductRowData);
        }



        public async Task<ActionResult> Create(ProductCollectionInfo productCollectionInfo)
        {
            await ServeNavigationMenus();

            ViewBag.ChequeTypeChargeRecoveryModeSelectList = GetChequeTypeChargeRecoveryModeSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(ChequeTypeDTO chequeTypeDTO,ProductCollectionInfo  productCollectionInfo)
        {
            chequeTypeDTO.ValidateAll();

            if (!chequeTypeDTO.HasErrors)

            {
                await _channelService.AddChequeTypeAsync(chequeTypeDTO, GetServiceHeader());
                if (Session["savingsProductIds"] != null)
                {
                    ObservableCollection<SavingsProductDTO> sRowData = Session["savingsProductIds"] as ObservableCollection<SavingsProductDTO>;
                     
                }

                ObservableCollection<CommissionDTO> commissionDTOs = new ObservableCollection<CommissionDTO>();
               await _channelService.UpdateCommissionsByChequeTypeIdAsync(chequeTypeDTO.Id, commissionDTOs, GetServiceHeader());

                

                await _channelService.UpdateAttachedProductsByChequeTypeIdAsync(chequeTypeDTO.Id, productCollectionInfo,GetServiceHeader());
              
                TempData["SuccessMessage"] = "Create successful.";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = chequeTypeDTO.ErrorMessages;
                ViewBag.ChequeTypeChargeRecoveryModeSelectList = GetChequeTypeChargeRecoveryModeSelectList(chequeTypeDTO.ChargeRecoveryMode.ToString());
                return View(chequeTypeDTO);
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

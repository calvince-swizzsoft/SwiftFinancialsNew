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
    public class SavingsProductController : MasterController
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

            var pageCollectionInfo = await _channelService.FindSavingsProductsByFilterInPageAsync(jQueryDataTablesModel.sSearch, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(savingsProduct => savingsProduct.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<SavingsProductDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var savingsProductDTO = await _channelService.FindSavingsProductAsync(id, GetServiceHeader());

            return View(savingsProductDTO);
        }
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            var commissionDTOs = await _channelService.FindCommissionsAsync(GetServiceHeader());
            ViewBag.Commisions = commissionDTOs;
            ViewBag.QueuePrioritySelectList = GetAlternateChannelKnownChargeTypeSelectList(string.Empty);
            ViewBag.AlternateChannelType = GetAlternateChannelTypeSelectList(string.Empty);
            ViewBag.ChargeBenefactor = GetChargeBenefactorSelectList(string.Empty);
            ViewBag.SystemTransactionType = GetSystemTransactionTypeList(string.Empty);
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(SavingsProductDTO savingsProductDTO, string[] commisionIds, string[] ExcemptedommisionId)
        {
            ObservableCollection<CommissionDTO> ExcemptedcommissionDTOs = new ObservableCollection<CommissionDTO>();

            if (ExcemptedommisionId != null && ExcemptedommisionId.Any())
            {
                var selectedIds = ExcemptedommisionId.Select(Guid.Parse).ToList();

                foreach (var commisionid in selectedIds)
                {
                    var commission = await _channelService.FindCommissionAsync(commisionid, GetServiceHeader());
                    ExcemptedcommissionDTOs.Add(commission);
                }
                // Process the selected IDs as needed
            }



            ObservableCollection<CommissionDTO> commissionDTOs = new ObservableCollection<CommissionDTO>();
            if (commisionIds != null && commisionIds.Any())
            {
                var selectedIds = commisionIds.Select(Guid.Parse).ToList();
              
                foreach (var commisionid in selectedIds)
                {
                    var commission = await _channelService.FindCommissionAsync(commisionid, GetServiceHeader());
                    commissionDTOs.Add(commission);
                }
                // Process the selected IDs as needed
            }




            savingsProductDTO.ValidateAll();

            if (!savingsProductDTO.HasErrors)
            {
            var results=  await _channelService.AddSavingsProductAsync(savingsProductDTO, GetServiceHeader());

                await _channelService.UpdateCommissionsBySavingsProductIdAsync(results.Id, commissionDTOs,savingsProductDTO.ChargeType,savingsProductDTO.ChargeBenefactor,GetServiceHeader());

               //await _channelService.UpdateSavingsProductExemptionsBySavingsProductIdAsync(results.Id, ExcemptedcommissionDTOs, GetServiceHeader());
                TempData["AlertMessage"] = "Savings Product created successfully";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = savingsProductDTO.ErrorMessages;

                TempData["Error"] = "Failed to create Savings Product";

                TempData["BackEnd"] = errorMessages;

                return View(savingsProductDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var savingsProductDTO = await _channelService.FindSavingsProductAsync(id, GetServiceHeader());

            return View(savingsProductDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, SavingsProductDTO savingsProductBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateSavingsProductAsync(savingsProductBindingModel, GetServiceHeader());

                TempData["Edit"] = "Edited Savings Product successfully";

                return RedirectToAction("Index");
            }
            else
            {
                return View(savingsProductBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetSavingsProductsAsync()
        {
            var savingsProductDTOs = await _channelService.FindSavingsProductsAsync(GetServiceHeader());

            return Json(savingsProductDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}

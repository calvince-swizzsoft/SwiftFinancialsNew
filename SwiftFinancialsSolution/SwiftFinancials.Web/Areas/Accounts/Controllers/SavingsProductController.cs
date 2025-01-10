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

            var pageCollectionInfo = await _channelService.FindSavingsProductsByFilterInPageAsync
                (jQueryDataTablesModel.sSearch, 0, int.MaxValue, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(i => i.CreatedDate)
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
                items: new List<SavingsProductDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var savingsProductDTO = await _channelService.FindSavingsProductAsync(id, GetServiceHeader());

            var rPriority = savingsProductDTO.Priority;

            string savings = "Savings", loans = "Loans", investments = "Investments", directDebits = "Direct Debits";

            var mapping = new Dictionary<string, int>
            {
                { "Loans", 0 },
                { "Investments", 1 },
                { "Savings", 2 },
                { "Direct Debits", 3 }
            };

            if (rPriority == 0)
            {
                savingsProductDTO.PriorityDescription = "Loans";
            }

            if (rPriority == 1)
            {
                savingsProductDTO.PriorityDescription = "Investments";
            }

            if (rPriority == 2)
            {
                savingsProductDTO.PriorityDescription = "Savings";
            }

            if (rPriority == 3)
            {
                savingsProductDTO.PriorityDescription = "Direct Debits";
            }


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
            ViewBag.RecoveryPriority = GetRecoveryPrioritySelectList(string.Empty);
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
                var results = await _channelService.AddSavingsProductAsync(savingsProductDTO, GetServiceHeader());

                await _channelService.UpdateCommissionsBySavingsProductIdAsync(results.Id, commissionDTOs, savingsProductDTO.ChargeType, savingsProductDTO.ChargeBenefactor, GetServiceHeader());

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System.Collections.ObjectModel;


namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class ChequesController : MasterController
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

            var pageCollectionInfo = await _channelService.FindExternalChequesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(postingPeriod => postingPeriod.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<ExternalChequeDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var externalChequeDTO = await _channelService.FindExternalChequePayablesByExternalChequeIdAsync(id, GetServiceHeader());

            return View(externalChequeDTO);
        }

        

        public async Task<ActionResult> Banking(Guid? id)
        {
            await ServeNavigationMenus();

            var externalChequeDTO = new ExternalChequeDTO();

            if (id.HasValue && id != Guid.Empty)
            {
                var parseId = id.Value;

                var bankLinkageDTO = await _channelService.FindBankLinkageAsync(parseId, GetServiceHeader());

                if (bankLinkageDTO != null)
                {
                    externalChequeDTO.BankName = bankLinkageDTO.BankName;
                    externalChequeDTO.BranchDescription = bankLinkageDTO.BankBranchName;
                    externalChequeDTO.ChartOfAccountAccountName = bankLinkageDTO.ChartOfAccountAccountName;

                    return View(externalChequeDTO);
                }
                else
                {
                    TempData["ErrorMessage"] = "Bank details could not be found.";
                    return Json(new { Error = "Bank details could not be found." });
                }
            }

            return View();
        }


        [HttpPost]
        public async Task<JsonResult> BankCheques(JQueryDataTablesModel jQueryDataTablesModel)
         {
            int totalRecordCount = 0;

            int searchRecordCount = 0;


            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindUnBankedExternalChequesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(postingPeriod => postingPeriod.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<ExternalChequeDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        [HttpPost]
        public async Task<ActionResult> BankSelectedCheques(List<Guid> selectedChequeIds, BankLinkageDTO bankLinkageDTO, int moduleNavigationItemCode)
        {
            var serviceHeader = GetServiceHeader();

            if (serviceHeader == null)
            {
                return Json(new { success = false, message = "Service header is missing." });
            }

            if (selectedChequeIds == null || !selectedChequeIds.Any())
            {
                return Json(new { success = false, message = "No cheques selected for banking." });
            }

            try
            {
                var chequesToBank = selectedChequeIds.Select(id => new ExternalChequeDTO { Id = id }).ToList();

                if (!chequesToBank.Any())
                {
                    return Json(new { success = false, message = "Selected cheques could not be found." });
                }

                var result = await _channelService.BankExternalChequesAsync(
                    new ObservableCollection<ExternalChequeDTO>(chequesToBank),
                    bankLinkageDTO,
                    moduleNavigationItemCode,
                    serviceHeader
                );

                if (result)
                {
                    return Json(new { success = true, message = "Cheques successfully banked." });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to bank the cheques. Ensure valid data and try again." });
                }
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while banking the cheques: " + ex.Message });
            }
        }


        public async Task<ActionResult> ChequeClearance()
        {
            await ServeNavigationMenus();

            return View();
        }
        [HttpPost]
        public async Task<JsonResult> ChequeClearance(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;


            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindUnClearedExternalChequesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(postingPeriod => postingPeriod.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<ExternalChequeDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }



        [HttpPost]
        public async Task<ActionResult> ClearSelectedCheques(List<Guid> selectedChequeIds, string actionType)
        {
            if (selectedChequeIds == null || selectedChequeIds.Count == 0)
            {
                TempData["ErrorMessage"] = "No cheques selected.";
                return RedirectToAction("Index"); // Or the relevant action/view you want to redirect to
            }

            bool result = true;
            var message = string.Empty;

            try
            {
                int clearingOption;
                switch (actionType)
                {
                    case "clear":
                        clearingOption = 1;
                        break;
                    case "unpay":
                        clearingOption = 2;
                        break;
                    default:
                        TempData["ErrorMessage"] = "Invalid action type.";
                        return RedirectToAction("Index");
                }

                foreach (var chequeId in selectedChequeIds)
                {
                    var externalChequeDTO = new ExternalChequeDTO { Id = chequeId };

                    // Fetch additional details if necessary
                    // var fetchedCheque = await _channelService.GetChequeDetailsAsync(chequeId);

                    var serviceHeader = GetServiceHeader();
                    if (serviceHeader == null)
                    {
                        TempData["ErrorMessage"] = "Service header is null.";
                        return RedirectToAction("Index");
                    }

                    result = await _channelService.ClearExternalChequeAsync(
                        externalChequeDTO,
                        clearingOption,
                        moduleNavigationItemCode: 123,
                        unPayReasonDTO: null,
                        serviceHeader: serviceHeader
                    );

                    if (!result)
                    {
                        message = $"Failed to update cheque with ID {chequeId}.";
                        TempData["ErrorMessage"] = message;
                        return RedirectToAction("ChequeClearance");
                    }
                }

                if (result)
                {
                    TempData["SuccessMessage"] = "Cheques updated successfully.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                result = false;
            }

            return RedirectToAction("Index"); // Redirect to the same view or any other relevant view
        }
















    }
}



















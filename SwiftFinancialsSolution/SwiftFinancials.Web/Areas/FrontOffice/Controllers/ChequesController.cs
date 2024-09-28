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
        public async Task<JsonResult> ClearSelectedCheques(List<Guid> selectedChequeIds, int clearingOption, string actionType, UnPayReasonDTO unPayReasonDTO = null)
        {
            if (selectedChequeIds == null || !selectedChequeIds.Any())
            {
                return Json(new { success = false, message = "No cheques selected." });
            }

            var serviceHeader = GetServiceHeader();
            bool isSuccess = true;
            string errorMessage = string.Empty;

            try
            {
                // Fetch all uncleared cheques (using filter or without, as needed)
                var pageCollectionInfo = await _channelService.FindUnClearedExternalChequesByFilterInPageAsync(
                    string.Empty, 
                    0, 
                    int.MaxValue, 
                    serviceHeader
                );

                if (pageCollectionInfo == null || !pageCollectionInfo.PageCollection.Any())
                {
                    return Json(new { success = false, message = "No uncleared cheques found." });
                }

                // Filter the collection to only include the selected cheques
                var selectedCheques = pageCollectionInfo.PageCollection
                    .Where(cheque => selectedChequeIds.Contains(cheque.Id))
                    .ToList();

                if (!selectedCheques.Any())
                {
                    return Json(new { success = false, message = "Selected cheques not found in uncleared cheques." });
                }

                foreach (var cheque in selectedCheques)
                {
                    // Clear or UnPay the cheque based on actionType
                    if (actionType.ToLower() == "clear")
                    {
                        var result = await _channelService.ClearExternalChequeAsync(cheque, clearingOption, /* ModuleNavigationItemCode */ 1, null, serviceHeader);

                        if (!result)
                        {
                            isSuccess = false;
                            errorMessage += $"Failed to clear cheque with ID {cheque.Id}. ";
                        }
                    }
                    else if (actionType.ToLower() == "unpay")
                    {
                        if (unPayReasonDTO == null)
                        {
                            return Json(new { success = false, message = "UnPay reason is required." });
                        }

                        var result = await _channelService.ClearExternalChequeAsync(cheque, clearingOption, /* ModuleNavigationItemCode */ 1, unPayReasonDTO, serviceHeader);

                        if (!result)
                        {
                            isSuccess = false;
                            errorMessage += $"Failed to unpay cheque with ID {cheque.Id}. ";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error occurred: {ex.Message}" });
            }

            return Json(new { success = isSuccess, message = isSuccess ? "Cheques processed successfully." : errorMessage });
        }



    }
}
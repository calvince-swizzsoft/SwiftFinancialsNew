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

        public async Task<JsonResult> LoadUnbankedCheques(JQueryDataTablesModel jQueryDataTablesModel)
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

        public async Task<ActionResult> Banking(Guid? id)
        {
            await ServeNavigationMenus();

            JQueryDataTablesModel jQueryDataTablesModel = new JQueryDataTablesModel
            {
                sEcho = 1
            };

            await LoadUnbankedCheques(jQueryDataTablesModel);
            var externalChequeDTO = new ExternalChequeDTO();

            if (id.HasValue && id != Guid.Empty)
            {
                var parseId = id.Value;

                // Fetch bank linkage data using service
                var bankLinkageDTO = await _channelService.FindBankLinkageAsync(parseId, GetServiceHeader());

                if (bankLinkageDTO != null)
                {
                    // Populate the externalChequeDTO with details from bankLinkageDTO
                    externalChequeDTO.BankName = bankLinkageDTO.BankName;
                    externalChequeDTO.BranchDescription = bankLinkageDTO.BankBranchName;
                    externalChequeDTO.ChartOfAccountAccountName = bankLinkageDTO.ChartOfAccountAccountName;

                    // Return the data as JSON for AJAX
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
        public async Task<ActionResult> BankSelectedCheques(List<Guid> selectedChequeIds, BankLinkageDTO bankLinkageDTO, int moduleNavigationItemCode)
        {
            var serviceHeader = GetServiceHeader();

            if (serviceHeader == null)
            {
                TempData["ErrorMessage"] = "Service header is missing.";
                return Json(new { success = false, message = "Service header is missing." });
            }

            if (selectedChequeIds == null || !selectedChequeIds.Any())
            {
                TempData["ErrorMessage"] = "No cheques selected for banking.";
                return Json(new { success = false, message = "No cheques selected for banking." });
            }

            try
            {
                // Fetch unbanked cheques with a high page size to include all possible matches
                var unbankedCheques = await _channelService.FindUnBankedExternalChequesByFilterInPageAsync(
                    string.Empty,
                    1,            
                    int.MaxValue, 
                    serviceHeader
                );

                if (unbankedCheques == null || !unbankedCheques.PageCollection.Any())
                {
                    TempData["ErrorMessage"] = "No unbanked cheques found.";
                    return Json(new { success = false, message = "No unbanked cheques found." });
                }

                // Filter the fetched cheques based on selectedChequeIds
                var externalChequeDTOs = unbankedCheques.PageCollection
                    .Where(cheque => selectedChequeIds.Contains(cheque.Id))
                    .ToList();

                if (!externalChequeDTOs.Any())
                {
                    TempData["ErrorMessage"] = "Selected cheques could not be found.";
                    return Json(new { success = false, message = "Selected cheques could not be found." });
                }

                // Bank the external cheques
                var result = await _channelService.BankExternalChequesAsync(
                    new ObservableCollection<ExternalChequeDTO>(externalChequeDTOs),
                    bankLinkageDTO,
                    moduleNavigationItemCode,
                    serviceHeader
                );

                if (result)
                {
                    TempData["SuccessMessage"] = "Cheques successfully banked.";
                    return Json(new { success = true, message = "Cheques successfully banked." });
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to bank the cheques. Ensure valid data and try again.";
                    return Json(new { success = false, message = "Failed to bank the cheques. Ensure valid data and try again." });
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while banking the cheques: " + ex.Message;
                return Json(new { success = false, message = "An error occurred while banking the cheques: " + ex.Message });
            }
        }













    }
}



















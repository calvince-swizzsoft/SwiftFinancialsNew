using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.AdministrationModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Infrastructure.Crosscutting.Framework.Utils;
using Org.BouncyCastle.Operators.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using TestApis.Controllers;


namespace TestApis.Controllers
{
    [RoutePrefix("api/cheques")]
    public class ChequesController : MasterController
    {


        //[HttpPost]
        //public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        //{
        //    int totalRecordCount = 0;

        //    int searchRecordCount = 0;

        //    int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;



        //    var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

        //    var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();
        //    var pageCollectionInfo = await _channelService.FindExternalChequesByFilterInPageAsync(jQueryDataTablesModel.sSearch, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

        //    if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
        //    {
        //        totalRecordCount = pageCollectionInfo.ItemsCount;

        //        pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(postingPeriod => postingPeriod.CreatedDate).ToList();

        //        searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

        //        return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        //    }
        //    else return this.DataTablesJson(items: new List<ExternalChequeDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        //}

        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> Index()
        {
            try
            {

                var cheques = await _channelService.FindExternalChequesAsync(GetServiceHeader());

                return Ok(cheques);
            }

            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        //[HttpPost]
        //public async Task<IHttpActionResult> FindExternalChequesByDate(JQueryDataTablesModel jQueryDataTablesModel, DateTime startDate, DateTime endDate)
        //{
        //    int totalRecordCount = 0;
        //    int searchRecordCount = 0;


        //    int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
        //    int pageSize = jQueryDataTablesModel.iDisplayLength;

        //    var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc";

        //    var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

        //    var pageCollectionInfo = await _channelService.FindExternalChequesByDateRangeAndFilterInPageAsync(
        //        startDate,
        //        endDate,
        //        jQueryDataTablesModel.sSearch,
        //        0,
        //        int.MaxValue,
        //        GetServiceHeader()
        //    );

        //    if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
        //    {

        //        var sortedData = pageCollectionInfo.PageCollection
        //            .OrderByDescending(externalChequeDTO => externalChequeDTO.CreatedDate)
        //            .ToList();

        //        totalRecordCount = sortedData.Count;

        //        var paginatedData = sortedData
        //            .Skip(jQueryDataTablesModel.iDisplayStart)
        //            .Take(jQueryDataTablesModel.iDisplayLength)
        //            .ToList();

        //        searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
        //            ? sortedData.Count
        //            : totalRecordCount;

        //        return this.DataTablesJson(
        //            items: paginatedData,
        //            totalRecords: totalRecordCount,
        //            totalDisplayRecords: searchRecordCount,
        //            sEcho: jQueryDataTablesModel.sEcho
        //        );
        //    }

        //    return this.DataTablesJson(
        //        items: new List<ExternalChequeDTO>(),
        //        totalRecords: totalRecordCount,
        //        totalDisplayRecords: searchRecordCount,
        //        sEcho: jQueryDataTablesModel.sEcho
        //);
        //}




        //[HttpPost]
        //public async Task<JsonResult> Cheques(JQueryDataTablesModel jQueryDataTablesModel)
        //{
        //    int totalRecordCount = 0;

        //    int searchRecordCount = 0;


        //    var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

        //    var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

        //    var pageCollectionInfo = await _channelService.FindExternalChequesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

        //    if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
        //    {
        //        totalRecordCount = pageCollectionInfo.ItemsCount;

        //        pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(postingPeriod => postingPeriod.CreatedDate).ToList();

        //        searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

        //        return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        //    }
        //    else return this.DataTablesJson(items: new List<ExternalChequeDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        //}
        //public async Task<ActionResult> Details(Guid id)
        //{
        //    await ServeNavigationMenus();

        //    var externalChequeDTO = await _channelService.FindExternalChequePayablesByExternalChequeIdAsync(id, GetServiceHeader());

        //    return View(externalChequeDTO);
        //}



        //public async Task<ActionResult> Banking(Guid? id)
        //{
        //    await ServeNavigationMenus();

        //    var externalChequeDTO = new ExternalChequeDTO();

        //    if (id.HasValue && id != Guid.Empty)
        //    {
        //        var parseId = id.Value;

        //        var bankLinkageDTO = await _channelService.FindBankLinkageAsync(parseId, GetServiceHeader());

        //        if (bankLinkageDTO != null)
        //        {
        //            externalChequeDTO.BankName = bankLinkageDTO.BankName;
        //            externalChequeDTO.BranchDescription = bankLinkageDTO.BranchDescription;
        //            externalChequeDTO.BankLinkageChartOfAccountAccountName = bankLinkageDTO.ChartOfAccountAccountName;
        //            externalChequeDTO.BankLinkageChartOfAccountId = bankLinkageDTO.ChartOfAccountId;
        //            externalChequeDTO.ChartOfAccountAccountName = bankLinkageDTO.ChartOfAccountAccountName;
        //            externalChequeDTO.BankLinkageChartOfAccountCostCenterId = bankLinkageDTO.ChartOfAccountCostCenterId;
        //            externalChequeDTO.BankLinkageChartOfAccountCostCenterDescription = bankLinkageDTO.ChartOfAccountCostCenterDescription;
        //            externalChequeDTO.BankLinkageChartOfAccountAccountCode = bankLinkageDTO.ChartOfAccountAccountCode;
        //            externalChequeDTO.BankLinkageChartOfAccountAccountType = bankLinkageDTO.ChartOfAccountAccountType;

        //            TempData["BankLinkageChartOfAccountId"] = externalChequeDTO.BankLinkageChartOfAccountId;
        //            TempData["ChartOfAccountAccountName"] = externalChequeDTO.ChartOfAccountAccountName;

        //            TempData["BankLinkageDTO"] = bankLinkageDTO;
        //            TempData["ExternalChequeDTO"] = (externalChequeDTO);

        //            return View(externalChequeDTO);
        //        }
        //        else
        //        {
        //            TempData["ErrorMessage"] = "Bank details could not be found.";
        //            return Json(new { Error = "Bank details could not be found." });
        //        }
        //    }
        //    return View(externalChequeDTO);

        //}


        //[HttpPost]
        //public async Task<JsonResult> BankCheques(JQueryDataTablesModel jQueryDataTablesModel)
        //{
        //    int totalRecordCount = 0;

        //    int searchRecordCount = 0;



        //    var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

        //    var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

        //    var pageCollectionInfo = await _channelService.FindUnBankedExternalChequesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

        //    if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
        //    {
        //        totalRecordCount = pageCollectionInfo.ItemsCount;

        //        pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(postingPeriod => postingPeriod.CreatedDate).ToList();

        //        searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

        //        return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        //    }
        //    else return this.DataTablesJson(items: new List<ExternalChequeDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        //}

        [HttpPost]
        [Route("bank")]
        //public async Task<IHttpActionResult> BankSelectedCheques(List<Guid> selectedChequeIds, BankLinkageDTO bankLinkageDTO)
        public async Task<IHttpActionResult> BankSelectedCheques([FromBody] ChequeBankingRequest chequeBankingRequest)
        {
            var selectedChequeIds = chequeBankingRequest.selectedChequeIds;
            var bankLinkageDTO = chequeBankingRequest.bankLinkageDTO;

            if (selectedChequeIds == null || !selectedChequeIds.Any())
            {
                return Json(new { success = false, message = "No cheques selected." });
            }

            if (bankLinkageDTO == null || bankLinkageDTO.Id == Guid.Empty)
            {
                return Json(new
                {
                    success = false,
                    message = "Bank linkage is required. Please enter the bank linkage details and try again."
                });
            }

            var serviceHeader = GetServiceHeader();
            bool isSuccess = true;
            string errorMessage = string.Empty;

            try
            {
                var pageCollectionInfo = await _channelService.FindUnBankedExternalChequesByFilterInPageAsync(
                    string.Empty,
                    0,
                    int.MaxValue,
                    serviceHeader
                );

                if (pageCollectionInfo == null || !pageCollectionInfo.PageCollection.Any())
                {
                    return Json(new { success = false, message = "No uncleared cheques found." });
                }

                var selectedCheques = pageCollectionInfo.PageCollection
                    .Where(cheque => selectedChequeIds.Contains(cheque.Id))
                    .ToList();

                if (!selectedCheques.Any())
                {
                    return Json(new { success = false, message = "Selected cheques not found in unbanked cheques." });
                }

                //foreach (var cheque in selectedCheques)
                //{
                //    cheque.BankLinkageChartOfAccountId = (Guid)TempData["BankLinkageChartOfAccountId"];
                //    cheque.ChartOfAccountAccountName = TempData["ChartOfAccountAccountName"].ToString();
                //}

                var externalChequeDTOs = new ObservableCollection<ExternalChequeDTO>(selectedCheques.Select(cheque => new ExternalChequeDTO
                {
                    Id = cheque.Id,
                    Number = cheque.Number,
                    Amount = cheque.Amount,
                    BankLinkageChartOfAccountId = cheque.BankLinkageChartOfAccountId,
                    BankLinkageChartOfAccountAccountName = cheque.ChartOfAccountAccountName,
                }).ToList());

                var result = await _channelService.BankExternalChequesAsync(
                    externalChequeDTOs,
                    bankLinkageDTO,
                    moduleNavigationItemCode: 123,
                    serviceHeader
                );

                if (!result)
                {
                    isSuccess = false;
                    errorMessage = "Failed to bank the cheques. Ensure valid data and try again.";
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error occurred: {ex.Message}" });
            }

            return Json(new { success = isSuccess, message = isSuccess ? "Cheques processed successfully." : errorMessage });
        }

        //public async Task<ActionResult> ChequeClearance()
        //{
        //    await ServeNavigationMenus();

        //    return View();
        //}
        //[HttpPost]
        //public async Task<JsonResult> ChequeClearance(JQueryDataTablesModel jQueryDataTablesModel)
        //{
        //    int totalRecordCount = 0;

        //    int searchRecordCount = 0;


        //    var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

        //    var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

        //    var pageCollectionInfo = await _channelService.FindUnClearedExternalChequesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

        //    if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
        //    {
        //        totalRecordCount = pageCollectionInfo.ItemsCount;

        //        pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(postingPeriod => postingPeriod.CreatedDate).ToList();

        //        searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

        //        return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        //    }
        //    else return this.DataTablesJson(items: new List<ExternalChequeDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        //}


        [HttpPost]
        [Route("clear")]
        //public async Task<IHttpActionResult> ClearSelectedCheques(List<Guid> selectedChequeIds, int clearingOption, string actionType, UnPayReasonDTO unPayReasonDTO = null)
        public async Task<IHttpActionResult> ClearSelectedCheques(ChequeClearingRequest chequeClearingRequest)
        {
            var selectedChequeIds = chequeClearingRequest.selectedChequeIds;
            var clearingOption = chequeClearingRequest.clearingOption;
            var actionType = chequeClearingRequest.actionType;
            var unPayReasonDTO = chequeClearingRequest.unPayReasonDTO;


            if (selectedChequeIds == null || !selectedChequeIds.Any())
            {
                return Json(new { success = false, message = "No cheques selected." });
            }

            var serviceHeader = GetServiceHeader();
            bool isSuccess = true;
            string errorMessage = string.Empty;

            try
            {
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

                var selectedCheques = pageCollectionInfo.PageCollection
                    .Where(cheque => selectedChequeIds.Contains(cheque.Id))
                    .ToList();

                if (!selectedCheques.Any())
                {
                    return Json(new { success = false, message = "Selected cheques not found in uncleared cheques." });
                }

                foreach (var cheque in selectedCheques)
                {
                    bool chequeProcessed = false;

                    if (actionType.ToLower() == "clear")
                    {
                        var result = await _channelService.ClearExternalChequeAsync(
                            cheque,
                            clearingOption,
                            /* ModuleNavigationItemCode */ 1,
                            null,
                            serviceHeader
                        );

                        if (result)
                        {
                            var markClearedResult = await _channelService.MarkExternalChequeClearedAsync(cheque.Id, serviceHeader);
                            if (markClearedResult)
                            {
                                chequeProcessed = true;
                            }
                            else
                            {
                                isSuccess = false;
                                errorMessage += $"Failed to mark cheque with ID {cheque.ChequeTypeDescription} as cleared. ";
                            }
                        }
                        else
                        {
                            isSuccess = false;
                            errorMessage += $"Failed to clear cheque with ID {cheque.ChequeTypeDescription}. ";
                        }
                    }
                    else if (actionType.ToLower() == "unpay")
                    {
                        if (unPayReasonDTO == null)
                        {
                            return Json(new { success = false, message = "UnPay reason is required." });
                        }

                        var result = await _channelService.ClearExternalChequeAsync(
                            cheque,
                            clearingOption,
                            /* ModuleNavigationItemCode */ 1,
                            unPayReasonDTO,
                            serviceHeader
                        );

                        if (result)
                        {
                            var markClearedResult = await _channelService.MarkExternalChequeClearedAsync(cheque.Id, serviceHeader);
                            if (markClearedResult)
                            {
                                chequeProcessed = true;
                            }
                            else
                            {
                                isSuccess = false;
                                errorMessage += $"Failed to mark cheque with ID {cheque.ChequeTypeDescription} as cleared. ";
                            }
                        }
                        else
                        {
                            isSuccess = false;
                            errorMessage += $"Failed to unpay cheque with ID {cheque.ChequeTypeDescription}. ";
                        }
                    }

                    if (chequeProcessed)
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error occurred: {ex.Message}" });
            }

            return Json(new { success = isSuccess, message = isSuccess ? "Cheques processed successfully." : errorMessage });
        }

        [Route("untransfered")]
        public async Task<IHttpActionResult> GetTellerUntransferredCheques(Guid teller)
        {

            // Guid parseId;

            if (teller == Guid.Empty)
            {
                return BadRequest("teller Id cannot be null");
            }

            try
            {
                var cheques = await _channelService.FindUnTransferredExternalChequesByTellerId(teller, "", GetServiceHeader());
                return Ok(cheques);
            }

            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

        }
    }


    public class ChequeBankingRequest
    {
        public List<Guid> selectedChequeIds { get; set; } = new List<Guid>();
        public BankLinkageDTO bankLinkageDTO { get; set; }
    }

    public class ChequeClearingRequest
    {
        public List<Guid> selectedChequeIds { get; set; } = new List<Guid>();

        public int clearingOption { get; set; }

        public string actionType { get; set; }

        public UnPayReasonDTO unPayReasonDTO { get; set; }
    }
}
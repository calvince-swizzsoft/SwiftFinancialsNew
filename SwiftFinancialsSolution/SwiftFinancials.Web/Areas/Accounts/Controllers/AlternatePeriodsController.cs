using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Attributes;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class AlternatePeriodsController : MasterController
    {
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Create(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();
            DateTime startDate = DateTime.Now.AddDays(-30);
            DateTime endDate = DateTime.Now.AddDays(+30);
            var pageCollectionInfo = await _channelService.FindAlternateChannelReconciliationPeriodsByFilterInPageAsync(1, startDate, endDate, jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(alternatechannel => alternatechannel.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<AlternateChannelReconciliationPeriodDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }



        public async Task<ActionResult> Clos()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Clos(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();
            DateTime startDate = DateTime.Now.AddDays(-30);
            DateTime endDate = DateTime.Now.AddDays(+30);
            var pageCollectionInfo = await _channelService.FindAlternateChannelReconciliationPeriodsByFilterInPageAsync(2, startDate, endDate, jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(postingPeriod => postingPeriod.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<AlternateChannelReconciliationPeriodDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }




        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var debitBatchDTO = await _channelService.FindAlternateChannelAsync(id, true, GetServiceHeader());

            return View(debitBatchDTO);
        }

        public async Task<ActionResult> Index(Guid? id, AlternateChannelReconciliationPeriodDTO alternateChannelDTO1)
        {

            await ServeNavigationMenus();

            Guid parseId;

            ViewBag.alternateChannelType = GetAlternateChannelTypeSelectList(string.Empty);

            ViewBag.RecordStatusSelectList = GetRecordStatusSelectList(string.Empty);

            ViewBag.SetDifferenceMode = GetSetDifferenceModeSelectList(string.Empty);

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }
            //var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            bool includeBalances = false;
            bool includeProductDescription = false;
            bool includeInterestBalanceForLoanAccounts = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;


            var customer = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            AlternateChannelReconciliationPeriodDTO alternateChannelDTO = new AlternateChannelReconciliationPeriodDTO();

            if (customer != null)
            {
                //alternateChannelDTO.CustomerAccountCustomerId = customer.Id;
                //alternateChannelDTO.CustomerAccountId = customer.Id;
                //alternateChannelDTO.CustomerAccountCustomerIndividualFirstName = customer.CustomerIndividualFirstName;
                //alternateChannelDTO.CustomerAccountCustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers;
                //alternateChannelDTO.CustomerAccountCustomerSerialNumber = customer.CustomerSerialNumber;
                //alternateChannelDTO.CustomerAccountCustomerReference1 = customer.CustomerReference1;
                //alternateChannelDTO.CustomerAccountCustomerReference2 = customer.CustomerReference2;
                //alternateChannelDTO.CustomerAccountCustomerReference3 = customer.CustomerReference3;
                //alternateChannelDTO.CustomerAccountCustomerIndividualIdentityCardNumber = customer.CustomerIdentificationNumber;
                //alternateChannelDTO.Remarks = customer.Remarks;


            }

            ViewBag.SystemTransactionType = GetSystemTransactionTypeList(string.Empty);
            ViewBag.alternateChannelType = GetAlternateChannelTypeSelectList(string.Empty);
            ViewBag.ChargeBenefactor = GetChargeBenefactorSelectList(string.Empty);
            ViewBag.Chargetype = GetChargeTypeSelectList(string.Empty);

            return View(alternateChannelDTO);
        }


        public async Task<ActionResult> Search(Guid? id, AlternateChannelDTO alternateChannelDTO1)
        {
            await ServeNavigationMenus();

            Guid parseId;
            ViewBag.alternateChannelType = GetAlternateChannelTypeSelectList(string.Empty);
            ViewBag.RecordStatusSelectList = GetRecordStatusSelectList(string.Empty);

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            //var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            bool includeBalances = false;
            bool includeProductDescription = false;
            bool includeInterestBalanceForLoanAccounts = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;


            var customer = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            AlternateChannelDTO alternateChannelDTO = new AlternateChannelDTO();

            if (customer != null)
            {
                alternateChannelDTO.CustomerAccountCustomerId = customer.Id;
                alternateChannelDTO.CustomerAccountId = customer.Id;
                alternateChannelDTO.CustomerAccountCustomerIndividualFirstName = customer.CustomerIndividualFirstName;
                alternateChannelDTO.CustomerAccountCustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers;
                alternateChannelDTO.CustomerAccountCustomerSerialNumber = customer.CustomerSerialNumber;
                alternateChannelDTO.CustomerAccountCustomerReference1 = customer.CustomerReference1;
                alternateChannelDTO.CustomerAccountCustomerReference2 = customer.CustomerReference2;
                alternateChannelDTO.CustomerAccountCustomerReference3 = customer.CustomerReference3;
                alternateChannelDTO.CustomerAccountCustomerIndividualIdentityCardNumber = customer.CustomerIdentificationNumber;
                alternateChannelDTO.Remarks = customer.Remarks;




            }
            Session["DailyLimit"] = alternateChannelDTO1.DailyLimit;
            Session["CardNumber"] = alternateChannelDTO1.CardNumber;



            return View("Create", alternateChannelDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Index(AlternateChannelReconciliationPeriodDTO alternateChannelDTO, AlternateChannelReconciliationEntryDTO alternateChannelReconciliationEntryDTO)
        {
            var startDate = Request["startDate"];

            var endDate = Request["endDate"];

            alternateChannelDTO.DurationStartDate = DateTime.Parse(startDate).Date;

            alternateChannelDTO.DurationEndDate = DateTime.Parse(endDate).Date;

            alternateChannelDTO.AlternateChannelReconciliationEntryDTO = alternateChannelReconciliationEntryDTO;

            alternateChannelDTO.ValidateAll();

            if (!alternateChannelDTO.HasErrors)
            {
                var customer = await _channelService.AddAlternateChannelReconciliationPeriodAsync(alternateChannelDTO, GetServiceHeader());

                TempData["SuccessMessage"] = "Create successful. for alternatechannel with Remarks   " + alternateChannelDTO.Remarks;
                //if (result.ErrorMessageResult != null)
                //{
                //    TempData["ErrorMsg"] = result.ErrorMessageResult;
                //    await ServeNavigationMenus();
                //    return View();
                //}
                // = new AlternateChannelReconciliationEntryDTO();

                //if (customer != null)
                //{
                //    alternateChannelReconciliationEntryDTO.Id = customer.Id;

                //    alternateChannelReconciliationEntryDTO.Remarks = customer.Remarks;
                //    alternateChannelReconciliationEntryDTO.Status = customer.Status;                    
                //}

                //await _channelService.AddAlternateChannelReconciliationEntryAsync(alternateChannelReconciliationEntryDTO, GetServiceHeader());

                ViewBag.alternateChannelType = GetAlternateChannelTypeSelectList(alternateChannelDTO.AlternateChannelType.ToString());
                ViewBag.RecordStatusSelectList = GetRecordStatusSelectList(alternateChannelDTO.SetDifferenceMode.ToString());
                ViewBag.SetDifferenceMode = GetSetDifferenceModeSelectList(alternateChannelDTO.SetDifferenceMode.ToString());




                ViewBag.alternateChannelType = GetAlternateChannelTypeSelectList(alternateChannelDTO.AlternateChannelType.ToString());

                return RedirectToAction("Create");
            }
            else
            {
                var errorMessages = alternateChannelDTO.ErrorMessages;
                ViewBag.alternateChannelType = GetAlternateChannelTypeSelectList(alternateChannelDTO.AlternateChannelType.ToString());
                ViewBag.RecordStatusSelectList = GetRecordStatusSelectList(alternateChannelDTO.SetDifferenceMode.ToString());
                ViewBag.SetDifferenceMode = GetSetDifferenceModeSelectList(alternateChannelDTO.SetDifferenceMode.ToString());
                return View(alternateChannelDTO);
            }
        }
       
        public async Task<ActionResult> Processing(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.alternateChannelType = GetAlternateChannelTypeSelectList(string.Empty);

            ViewBag.RecordStatusSelectList = GetRecordStatusSelectList(string.Empty);

            ViewBag.SetDifferenceMode = GetSetDifferenceModeSelectList(string.Empty);
            var bankReconciliationPeriodDTO = await _channelService.FindAlternateChannelReconciliationPeriodAsync(id, GetServiceHeader());
           var i= await _channelService.FindAlternateChannelsByTypeAndFilterInPageAsync(64, 3, null, 2, 0, 10, true, GetServiceHeader());
                var k = await _channelService.FindAlternateChannelReconciliationEntriesByAlternateChannelReconciliationPeriodIdAndFilterInPageAsync(bankReconciliationPeriodDTO.Id, 1,"", 0,10, GetServiceHeader());
            ViewBag.history = k;

            return View(bankReconciliationPeriodDTO);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Processing(AlternateChannelReconciliationPeriodDTO alternateChannelReconciliationPeriodDTO, AlternateChannelReconciliationEntryDTO alternateChannelReconciliationEntry)
        {
            alternateChannelReconciliationPeriodDTO.AlternateChannelReconciliationEntryDTO = alternateChannelReconciliationEntry;
            if (!alternateChannelReconciliationPeriodDTO.HasErrors)
            {
                alternateChannelReconciliationEntry.AlternateChannelReconciliationPeriodId = alternateChannelReconciliationPeriodDTO.Id;
                alternateChannelReconciliationEntry.Amount = alternateChannelReconciliationPeriodDTO.Amount;
                //alternateChannelReconciliationEntry. = alternateChannelReconciliationPeriodDTO.;
                //alternateChannelReconciliationEntry.ChequeNumber = alternateChannelReconciliationPeriodDTO.ChequeNumber;
                //alternateChannelReconciliationEntry.ChequeDrawee = alternateChannelReconciliationPeriodDTO.ChequeDrawee;
                //alternateChannelReconciliationEntry.Value = alternateChannelReconciliationPeriodDTO.Value;



                await _channelService.AddAlternateChannelReconciliationEntryAsync(alternateChannelReconciliationEntry, GetServiceHeader());

                TempData["Edit"] = "Successfully Processed Bank Reconciliation Period";

                return RedirectToAction("Create");
            }
            else
            {
                return View(alternateChannelReconciliationPeriodDTO);
            }
        }

        public async Task<ActionResult> Closing(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.alternateChannelType = GetAlternateChannelTypeSelectList(string.Empty);

            ViewBag.RecordStatusSelectList = GetRecordStatusSelectList(string.Empty);

            ViewBag.SetDifferenceMode = GetSetDifferenceModeSelectList(string.Empty);
            var bankReconciliationPeriodDTO = await _channelService.FindAlternateChannelReconciliationPeriodAsync(id,GetServiceHeader());

            return View(bankReconciliationPeriodDTO);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Closing(AlternateChannelReconciliationPeriodDTO alternateChannelReconciliationPeriodDTO, AlternateChannelReconciliationEntryDTO alternateChannelReconciliationEntry)
        {
            alternateChannelReconciliationPeriodDTO.AlternateChannelReconciliationEntryDTO = alternateChannelReconciliationEntry;
            if (!alternateChannelReconciliationPeriodDTO.HasErrors)
            {
                alternateChannelReconciliationEntry.AlternateChannelReconciliationPeriodId = alternateChannelReconciliationPeriodDTO.Id;
                alternateChannelReconciliationEntry.Amount = alternateChannelReconciliationPeriodDTO.Amount;
                //alternateChannelReconciliationEntry. = alternateChannelReconciliationPeriodDTO.;
                //alternateChannelReconciliationEntry.ChequeNumber = alternateChannelReconciliationPeriodDTO.ChequeNumber;
                //alternateChannelReconciliationEntry.ChequeDrawee = alternateChannelReconciliationPeriodDTO.ChequeDrawee;
                //alternateChannelReconciliationEntry.Value = alternateChannelReconciliationPeriodDTO.Value;



                await _channelService.CloseAlternateChannelReconciliationPeriodAsync(alternateChannelReconciliationPeriodDTO, 1 ,GetServiceHeader());

                TempData["Edit"] = "Successfully Processed Bank Reconciliation Period";

                return RedirectToAction("Create");
            }
            else
            {
                return View(alternateChannelReconciliationPeriodDTO);
            }
        }

    }

}
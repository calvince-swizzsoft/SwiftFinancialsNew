

using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class BankReconciliation_PeriodsController : MasterController
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

            var pageCollectionInfo = await _channelService.FindBankReconciliationPeriodsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(bankLinkage => bankLinkage.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<BankReconciliationPeriodDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var bankReconciliationPeriodDTO = await _channelService.FindBankReconciliationPeriodAsync(id, GetServiceHeader());

           var k= await _channelService.FindBankReconciliationEntriesByBankReconciliationPeriodIdAndFilterInPageAsync(bankReconciliationPeriodDTO.Id, "", 0, 10, GetServiceHeader());
            ViewBag.history = k;
            bankReconciliationPeriodDTO.Value = k.PageCollection[0].Value;
            bankReconciliationPeriodDTO.Remarks = k.PageCollection[0].Remarks;
            return View(bankReconciliationPeriodDTO);
        }



        public async Task<ActionResult> PostingPeriod(Guid? id, BankReconciliationPeriodDTO bankReconciliationPeriodDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            var postingPeriodDetails = await _channelService.FindPostingPeriodAsync(parseId, GetServiceHeader());

            if (postingPeriodDetails != null)
            {
                bankReconciliationPeriodDTO.PostingPeriodId = postingPeriodDetails.Id;
                bankReconciliationPeriodDTO.PostingPeriodDescription = postingPeriodDetails.Description;

                Session["postingPeriodId"] = bankReconciliationPeriodDTO.PostingPeriodId;
                Session["postingPeriodDescription"] = bankReconciliationPeriodDTO.PostingPeriodDescription;
            }

            return View("Create", bankReconciliationPeriodDTO);
        }




        public async Task<ActionResult> Bank(Guid? id, BankReconciliationPeriodDTO bankReconciliationPeriodDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            if (Session["postingPeriodId"] != null)
            {
                bankReconciliationPeriodDTO.PostingPeriodId = (Guid)Session["postingPeriodId"];
            }

            if (Session["postingPeriodDescription"] != null)
            {
                bankReconciliationPeriodDTO.PostingPeriodDescription = Session["postingPeriodDescription"].ToString();
            }


            var bankDetails = await _channelService.FindBankLinkageAsync(parseId, GetServiceHeader());

            if (bankDetails != null)
            {
                bankReconciliationPeriodDTO.BankLinkageId = bankDetails.Id;
                bankReconciliationPeriodDTO.BankLinkageBankName = bankDetails.BankName;
                bankReconciliationPeriodDTO.BranchId = bankDetails.BranchId;
                bankReconciliationPeriodDTO.ChartOfAccountId = bankDetails.ChartOfAccountId;
                bankReconciliationPeriodDTO.BankAccountNumber = bankDetails.BankAccountNumber;
                Session["BankId"] = bankReconciliationPeriodDTO.BankLinkageId;

                var k = await _channelService.FindBankLinkageAsync(bankReconciliationPeriodDTO.BankLinkageId, GetServiceHeader());
                bankReconciliationPeriodDTO.BankAccountNumber = k.BankAccountNumber;
                bankReconciliationPeriodDTO.BranchId = k.BranchId;
                bankReconciliationPeriodDTO.ChartOfAccountId = k.ChartOfAccountId;
                var j = await _channelService.FindGeneralLedgerAccountAsync(k.ChartOfAccountId, true, GetServiceHeader());
                ViewBag.j = j.Balance;
                bankReconciliationPeriodDTO.GeneralLedgerAccountBalance = j.Balance;
                Session["BankName"] = bankReconciliationPeriodDTO.BankLinkageBankName;
            }
            
           
            return View("Create", bankReconciliationPeriodDTO);
        }


        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Create(BankReconciliationPeriodDTO bankReconciliationPeriodDTO)
        {
            bankReconciliationPeriodDTO.BankLinkageId = (Guid)Session["BankId"];
            bankReconciliationPeriodDTO.BankLinkageBankName = Session["BankName"].ToString();

            bankReconciliationPeriodDTO.PostingPeriodId = (Guid)Session["postingPeriodId"];
            bankReconciliationPeriodDTO.PostingPeriodDescription = Session["postingPeriodDescription"].ToString();
            var k = await _channelService.FindBankLinkageAsync(bankReconciliationPeriodDTO.BankLinkageId, GetServiceHeader());
            bankReconciliationPeriodDTO.BankAccountNumber = k.BankAccountNumber;
            bankReconciliationPeriodDTO.BranchId = k.BranchId;
            bankReconciliationPeriodDTO.ChartOfAccountId = k.ChartOfAccountId;
            var j = await _channelService.FindGeneralLedgerAccountAsync(k.ChartOfAccountId, true, GetServiceHeader());
            ViewBag.j = j.Balance;
            bankReconciliationPeriodDTO.ChartOfAccountAccountName = k.ChartOfAccountAccountName;
           
            bankReconciliationPeriodDTO.DurationEndDate = DateTime.Now;

            bankReconciliationPeriodDTO.ValidateAll();

            if (!bankReconciliationPeriodDTO.HasErrors)
            {
                await _channelService.AddBankReconciliationPeriodAsync(bankReconciliationPeriodDTO, GetServiceHeader());

                TempData["AlertMessage"] = "Bank Reconciliation Period Created Successfully";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = bankReconciliationPeriodDTO.ErrorMessages;

                TempData["Error"] = "Failed to create Bank Linkage";

                return View(bankReconciliationPeriodDTO);
            }
        }


        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var bankReconciliationPeriodDTO = await _channelService.FindBankReconciliationPeriodAsync(id, GetServiceHeader());

            return View(bankReconciliationPeriodDTO);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid? id, BankReconciliationPeriodDTO bankReconciliationPeriodDTO)
        {
            if (!bankReconciliationPeriodDTO.HasErrors)
            {
                await _channelService.UpdateBankReconciliationPeriodAsync(bankReconciliationPeriodDTO, GetServiceHeader());

                TempData["Edit"] = "Successfully edited Bank Reconciliation Period";

                return RedirectToAction("Index");
            }
            else
            {
                return View(bankReconciliationPeriodDTO);
            }
        }


        public async Task<ActionResult> Processing(Guid id)
        {
            await ServeNavigationMenus();

            var bankReconciliationPeriodDTO = await _channelService.FindBankReconciliationPeriodAsync(id, GetServiceHeader());
            var k = await _channelService.FindBankReconciliationEntriesByBankReconciliationPeriodIdAndFilterInPageAsync(bankReconciliationPeriodDTO.Id, "", 0, 10, GetServiceHeader());
            if (k.ItemsCount !=0)
            {
                ViewBag.history = k;

                // var k = await _channelService.FindBankReconciliationEntriesByBankReconciliationPeriodIdAndFilterInPageAsync(bankReconciliationPeriodDTO.Id, "", 0, 10, GetServiceHeader());
                ViewBag.history = k;
                bankReconciliationPeriodDTO.Value = k.PageCollection[0].Value;
                bankReconciliationPeriodDTO.Remarks = k.PageCollection[0].Remarks;
            }
            return View(bankReconciliationPeriodDTO);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Processing(BankReconciliationPeriodDTO bankReconciliationPeriodDTO,BankReconciliationEntryDTO bankReconciliationEntryDTO)
        {
            bankReconciliationPeriodDTO.bankReconciliationEntryDTOs = bankReconciliationEntryDTO;
            if (!bankReconciliationPeriodDTO.HasErrors)
            {
                 bankReconciliationEntryDTO.BankReconciliationPeriodId= bankReconciliationPeriodDTO.Id ;
                bankReconciliationEntryDTO.ChartOfAccountId = bankReconciliationPeriodDTO.ChartOfAccountId;
                bankReconciliationEntryDTO.ChartOfAccountId = bankReconciliationPeriodDTO.ChartOfAccountId;
                bankReconciliationEntryDTO.ChequeNumber = bankReconciliationPeriodDTO.ChequeNumber;
                bankReconciliationEntryDTO.ChequeDrawee = bankReconciliationPeriodDTO.ChequeDrawee;
                bankReconciliationEntryDTO.Value = bankReconciliationPeriodDTO.Value;
                bankReconciliationEntryDTO.Remarks = bankReconciliationPeriodDTO.Remarks;


                await _channelService.AddBankReconciliationEntryAsync(bankReconciliationEntryDTO, GetServiceHeader());

                TempData["Edit"] = "Successfully Processed Bank Reconciliation Period";

                return RedirectToAction("Index");
            }
            else
            {
                return View(bankReconciliationPeriodDTO);
            }
        }

        public async Task<ActionResult> Closing(Guid id)
        {
            await ServeNavigationMenus();

            var bankReconciliationPeriodDTO = await _channelService.FindBankReconciliationPeriodAsync(id, GetServiceHeader());
            var k = await _channelService.FindBankReconciliationEntriesByBankReconciliationPeriodIdAndFilterInPageAsync(bankReconciliationPeriodDTO.Id, "", 0, 10, GetServiceHeader());
            if (k.ItemsCount != 0)
            {
                ViewBag.history = k;

                // var k = await _channelService.FindBankReconciliationEntriesByBankReconciliationPeriodIdAndFilterInPageAsync(bankReconciliationPeriodDTO.Id, "", 0, 10, GetServiceHeader());
                ViewBag.history = k;
                bankReconciliationPeriodDTO.Value = k.PageCollection[0].Value;
                bankReconciliationPeriodDTO.Remarks = k.PageCollection[0].Remarks;
            }
            return View(bankReconciliationPeriodDTO);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Closing(Guid? id, BankReconciliationPeriodDTO bankReconciliationPeriodDTO)
        {
            if (!bankReconciliationPeriodDTO.HasErrors)
            {
                

                var k = await _channelService.FindBankLinkageAsync(bankReconciliationPeriodDTO.BankLinkageId, GetServiceHeader());
                bankReconciliationPeriodDTO.BankAccountNumber = k.BankAccountNumber;
                bankReconciliationPeriodDTO.BranchId = k.BranchId;
                bankReconciliationPeriodDTO.ChartOfAccountId = k.ChartOfAccountId;
                var j = await _channelService.FindGeneralLedgerAccountAsync(k.ChartOfAccountId, true, GetServiceHeader());
                ViewBag.j = j.Balance;
                bankReconciliationPeriodDTO.ChartOfAccountAccountName = k.ChartOfAccountAccountName;

                bankReconciliationPeriodDTO.DurationEndDate = DateTime.Now;

                await _channelService.CloseBankReconciliationPeriodAsync(bankReconciliationPeriodDTO,1,2,GetServiceHeader());

                TempData["Edit"] = "Successfully edited Bank Reconciliation Period";

                return RedirectToAction("Index");
            }
            else
            {
                return View(bankReconciliationPeriodDTO);
            }
        }

    }



  
}



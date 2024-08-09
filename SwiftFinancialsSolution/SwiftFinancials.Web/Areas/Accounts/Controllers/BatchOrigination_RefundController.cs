using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class BatchOrigination_RefundController : MasterController
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
            DateTime startDate = DateTime.Now.AddDays(-30);
            DateTime endDate = DateTime.Now.AddDays(+30);
            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindOverDeductionBatchesByStatusAndFilterInPageAsync(1,startDate,endDate,jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(debitBatch => debitBatch.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<OverDeductionBatchDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var overDeductionBatchDTO = await _channelService.FindOverDeductionBatchAsync(id, GetServiceHeader());
            var batchentries = await _channelService.FindOverDeductionBatchEntriesByOverDeductionBatchIdAsync(id,true,GetServiceHeader());

            ViewBag.batchEntries = batchentries;

            return View(overDeductionBatchDTO);
        }
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(OverDeductionBatchDTO overDeductionBatchDTO)
        {
            overDeductionBatchDTO.ValidateAll();

            if (!overDeductionBatchDTO.HasErrors)
            {
                await _channelService.AddOverDeductionBatchAsync(overDeductionBatchDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = overDeductionBatchDTO.ErrorMessages;
                
                return View(overDeductionBatchDTO);
            }
        }


        public async Task<ActionResult> RefundEntries(Guid id)
        {
            await ServeNavigationMenus();

            var k = await _channelService.FindOverDeductionBatchAsync(id ,GetServiceHeader());
            return View(k);
        }



        [HttpPost]
        public async Task<ActionResult> Add(Guid? id, OverDeductionBatchDTO overDeductionBatchDTO)
        {
            await ServeNavigationMenus();

            OverDeductionBatchEntryDTOs = TempData["OverDeductionBatchEntryDTOs"] as ObservableCollection<OverDeductionBatchEntryDTO>;

            

            var creditCustomerAccount = overDeductionBatchDTO.overDeductionBatchEntries;
            var debitCustomerAccount = overDeductionBatchDTO.overDeductionBatchEntries;

            if (OverDeductionBatchEntryDTOs == null)
                OverDeductionBatchEntryDTOs = new ObservableCollection<OverDeductionBatchEntryDTO>();

            if (overDeductionBatchDTO.BranchDescription == null || overDeductionBatchDTO.TotalValue == 0)
            {
                TempData["tPercentage"] = "Branch  and TotalValue are required to proceed.";
            }
            else
            {
                Guid parseId;

                if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
                {
                    TempData["tPercentage"] = "Could not add refundBatchEntries Choose Batch";

                    await ServeNavigationMenus();

                    return View("Create", overDeductionBatchDTO);
                }


                foreach (var overDeductionBatchEntryDTO in overDeductionBatchDTO.overDeductionBatchEntry)
                {
                    overDeductionBatchEntryDTO.Id = parseId;
                    overDeductionBatchEntryDTO.CreditCustomerAccountId = creditCustomerAccount.CreditCustomerAccountId;
                    overDeductionBatchEntryDTO.CreditCustomerAccountBranchId= creditCustomerAccount.DebitCustomerAccountBranchId;
                    overDeductionBatchEntryDTO.CreditCustomerAccountCustomerIndividualFirstName= creditCustomerAccount.DebitCustomerAccountCustomerIndividualFirstName;
                    overDeductionBatchEntryDTO.DebitCustomerAccountId = debitCustomerAccount.CreditCustomerAccountId;
                    overDeductionBatchEntryDTO.DebitCustomerAccountBranchId = debitCustomerAccount.CreditCustomerAccountBranchId;
                    overDeductionBatchEntryDTO.DebitCustomerAccountCustomerIndividualFirstName = debitCustomerAccount.DebitCustomerAccountCustomerIndividualFirstName;


                    TempData["tPercentage"] = "";


                    if (overDeductionBatchEntryDTO.CreditFullAccountNumber == null || overDeductionBatchEntryDTO.DebitFullAccountNumber == null)
                    {
                        TempData["tPercentage"] = "Provide Both the Credit and Debit accounts to create refund .";
                    }
                    else
                    {
                        OverDeductionBatchEntryDTOs.Add(overDeductionBatchEntryDTO);

                       
                    }
                };
            }

            ViewBag.OverDeductionBatchEntryDTOs = OverDeductionBatchEntryDTOs;


            TempData["OverDeductionBatchEntryDTOs"] = OverDeductionBatchEntryDTOs;


            //TempData["ChargeDTO"] = commissionDTO;

            return View("Create");
        }


        [HttpPost]
        public async Task<ActionResult> RefundEntries(OverDeductionBatchEntryDTO overDeductionBatchEntryDTO)
        {
            overDeductionBatchEntryDTO.ValidateAll();

            if (!overDeductionBatchEntryDTO.HasErrors)
            {
                await _channelService.AddOverDeductionBatchEntryAsync(overDeductionBatchEntryDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = overDeductionBatchEntryDTO.ErrorMessages;

                return View(overDeductionBatchEntryDTO);
            }
        }



        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
          
            var overDeductionBatchDTO = await _channelService.FindOverDeductionBatchAsync(id, GetServiceHeader());

            return View(overDeductionBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, OverDeductionBatchDTO overDeductionBatchDTO)
        {
            overDeductionBatchDTO.ValidateAll();

            if (!overDeductionBatchDTO.HasErrors)
            {
                await _channelService.UpdateOverDeductionBatchAsync(overDeductionBatchDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = overDeductionBatchDTO.ErrorMessages;
                //ViewBag.QueuePrioritySelectList = GetQueuePrioritySelectList(debitBatchDTO.Priority.ToString());
                return View(overDeductionBatchDTO);
            }
        }

        public async Task<ActionResult> Verify(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);

            var debitBatchDTO = await _channelService.FindOverDeductionBatchAsync(id, GetServiceHeader());

            return View(debitBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(Guid id, OverDeductionBatchDTO debitBatchDTO)
        {
            debitBatchDTO.ValidateAll();

            if (!debitBatchDTO.HasErrors)
            {
                await _channelService.AuditOverDeductionBatchAsync(debitBatchDTO, 1, GetServiceHeader());
                
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = debitBatchDTO.ErrorMessages;
                
                return View(debitBatchDTO);
            }
        }

        public async Task<ActionResult> Authorize(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);

            var debitBatchDTO = await _channelService.FindOverDeductionBatchAsync(id, GetServiceHeader());

            return View(debitBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Authorize(Guid id, OverDeductionBatchDTO debitBatchDTO)
        {
            //var batchAuthOption = debitBatchDTO.BatchAuthOption;
            debitBatchDTO.ValidateAll();

            if (!debitBatchDTO.HasErrors)
            {
                await _channelService.AuthorizeOverDeductionBatchAsync(debitBatchDTO, 1, 1, GetServiceHeader());
                //ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(debitBatchDTO.BatchAuthOption.ToString());
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = debitBatchDTO.ErrorMessages;
                //ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(debitBatchDTO.BatchAuthOption.ToString());
                return View(debitBatchDTO);
            }
        }

        



        [HttpGet]
        public async Task<JsonResult> GetDebitBatchesAsync()
        {
            var debitBatchDTOs = await _channelService.FindDebitBatchesAsync(GetServiceHeader());

            return Json(debitBatchDTOs, JsonRequestBehavior.AllowGet);
        }
    }

}

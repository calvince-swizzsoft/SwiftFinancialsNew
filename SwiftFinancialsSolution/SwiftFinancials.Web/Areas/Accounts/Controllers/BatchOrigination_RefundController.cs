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

            var pageCollectionInfo = await _channelService.FindOverDeductionBatchesByStatusAndFilterInPageAsync(1, startDate, endDate, jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

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
            var batchentries = await _channelService.FindOverDeductionBatchEntriesByOverDeductionBatchIdAsync(id, true, GetServiceHeader());

            ViewBag.OverDeductionBatchEntryDTOs = batchentries;

            return View(overDeductionBatchDTO);
        }


        [HttpPost]
        public async Task<JsonResult> CreditCustomerAccountLookUp(Guid id)
        {
            if (id == Guid.Empty)
            {
                return Json(new { success = false, message = "Invalid ID" });
            }

            // Retrieve session data
            var overDeductionBatchDTO = Session["BatchDTO"] as OverDeductionBatchDTO;
            if (overDeductionBatchDTO == null)
            {
                overDeductionBatchDTO = new OverDeductionBatchDTO();
                Session["BatchDTO"] = overDeductionBatchDTO;
            }

            // Ensure DTO's entry collection is initialized
            if (overDeductionBatchDTO.overDeductionBatchEntries == null)
            {
                overDeductionBatchDTO.overDeductionBatchEntries = new ObservableCollection<OverDeductionBatchEntryDTO>();
            }

            if (overDeductionBatchDTO.overDeductionBatchEntries.Count == 0)
            {
                overDeductionBatchDTO.overDeductionBatchEntries.Add(new OverDeductionBatchEntryDTO());
            }

            var entry = overDeductionBatchDTO.overDeductionBatchEntries[0];

            // Find customer account
            var creditCustomerAccount = await _channelService.FindCustomerAccountAsync(id, true, true, true, false, GetServiceHeader());

            if (creditCustomerAccount != null)
            {
                // Populate entry with the retrieved data
                entry.CreditCustomerAccountFullAccountNumber = creditCustomerAccount.FullAccountNumber;
                entry.CreditCustomerAccountId = creditCustomerAccount.Id;
                entry.CreditProductDescription = creditCustomerAccount.CustomerAccountTypeProductCodeDescription;
                entry.CreditCustomerAccountFullName = creditCustomerAccount.CustomerFullName;
                entry.CreditCustomerIdentificationNumber = creditCustomerAccount.CustomerIdentificationNumber;

                // Update session values
                Session["CreditCustomerAccountFullAccountNumber"] = entry.CreditCustomerAccountFullAccountNumber;
                Session["CreditProductDescription"] = entry.CreditProductDescription;
                Session["CreditCustomerAccountFullName"] = entry.CreditCustomerAccountFullName;
                Session["CreditCustomerAccountId"] = entry.CreditCustomerAccountId;

                // Return JSON result
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        CreditCustomerAccountFullAccountNumber = entry.CreditCustomerAccountFullAccountNumber,
                        CreditCustomerAccountId = entry.CreditCustomerAccountId,
                        CreditProductDescription = entry.CreditProductDescription,
                        CreditCustomerAccountFullName = entry.CreditCustomerAccountFullName,
                        CreditCustomerIdentificationNumber = entry.CreditCustomerIdentificationNumber,
                        
                    }
                });
            }

            return Json(new { success = false, message = "Customer account not found" });
        }


        [HttpPost]
        public async Task<ActionResult> DebitCustomerAccountLookup(Guid id)
        {
            if (id == Guid.Empty)
            {
                return Json(new { success = false, message = "Invalid ID" });
            }

            // Retrieve session data if necessary
            OverDeductionBatchDTO overDeductionBatchDTO = Session["BatchDTO"] as OverDeductionBatchDTO;
            if (overDeductionBatchDTO == null)
            {
                overDeductionBatchDTO = new OverDeductionBatchDTO();
                Session["BatchDTO"] = overDeductionBatchDTO;
            }

            // Fetch data based on ID
            var debitCustomerAccount = await _channelService.FindCustomerAccountAsync(id, true, true, true, false, GetServiceHeader());

            if (debitCustomerAccount != null)
            {
                var debitEntry = new OverDeductionBatchEntryDTO
                {
                    DebitCustomerAccountFullAccountNumber = debitCustomerAccount.FullAccountNumber,
                    DebitCustomerAccountId = debitCustomerAccount.Id,
                    DebitProductDescription = debitCustomerAccount.CustomerAccountTypeProductCodeDescription,
                    DebitCustomerAccountFullName = debitCustomerAccount.CustomerFullName,
                    DebitCustomerIdentificationNumber = debitCustomerAccount.CustomerIdentificationNumber
                };

                return Json(new { success = true, data = debitEntry });
            }

            return Json(new { success = false, message = "Customer account not found" });
        }







        public async Task<ActionResult> Create()
        {


            await ServeNavigationMenus();


            return View();
        }



        [HttpPost]
        public async Task<ActionResult> Create(OverDeductionBatchDTO overDeductionBatchDTO)
        {
            // Retrieve the DTO stored in session, this will contain your batch data
            var storedOverDeductionBatchDTO = Session["overDeductionBatchDTO"] as OverDeductionBatchDTO;

            // If there's no stored batch data, initialize a new DTO
            if (storedOverDeductionBatchDTO == null)
            {
                storedOverDeductionBatchDTO = new OverDeductionBatchDTO();
            }

            // Ensure the entries are retrieved from the session
            var overDeductionBatchEntryDTOs = Session["OverDeductionBatchEntryDTO"] as ObservableCollection<OverDeductionBatchEntryDTO>;

            if (overDeductionBatchEntryDTOs != null)
            {
                storedOverDeductionBatchDTO.overDeductionBatchEntries = overDeductionBatchEntryDTOs;
            }

            // Perform validation
            storedOverDeductionBatchDTO.ValidateAll();
            if (storedOverDeductionBatchDTO.ErrorMessageResult != null)
            {
                await ServeNavigationMenus();
                TempData["ErrorMsg"] = storedOverDeductionBatchDTO.ErrorMessageResult;
                return View(storedOverDeductionBatchDTO);
            }

            // Save the batch data
            var refundBatch = await _channelService.AddOverDeductionBatchAsync(storedOverDeductionBatchDTO, GetServiceHeader());
            if (refundBatch.HasErrors)
            {
                await ServeNavigationMenus();
                TempData["ErrorMsg"] = refundBatch.ErrorMessages;
                return View(storedOverDeductionBatchDTO);
            }

            // Save each batch entry
            foreach (var overdeductiontBatchEntry in overDeductionBatchEntryDTOs)
            {
                overdeductiontBatchEntry.OverDeductionBatchId = refundBatch.Id;
                await _channelService.AddOverDeductionBatchEntryAsync(overdeductiontBatchEntry, GetServiceHeader());
            }

            // Clear session data after successful creation
            Session["OverDeductionBatchDTO"] = null;
            Session["OverDeductionBatchEntryDTO"] = null;

            TempData["SuccessMessage"] = "Successfully Created Refund Batch";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<JsonResult> Add(OverDeductionBatchDTO overDeductionBatchDTO)
        {
            await ServeNavigationMenus();

            /*if (Session["HeaderDetails"] != null)
            {
                overDeductionBatchDTO = Session["HeaderDetails"] as OverDeductionBatchDTO;
            }*/


            var overDeductionBatchEntryDTOs = Session["OverDeductionBatchEntryDTO"] as ObservableCollection<OverDeductionBatchEntryDTO>;
            if (overDeductionBatchEntryDTOs == null)
            {
                overDeductionBatchEntryDTOs = new ObservableCollection<OverDeductionBatchEntryDTO>();
            }

            foreach (var overDeductionBatchEntryDTO in overDeductionBatchDTO.overDeductionBatchEntries)
            {
                overDeductionBatchEntryDTOs.Add(overDeductionBatchEntryDTO);
            }

            Session["OverDeductionBatchEntryDTO"] = overDeductionBatchEntryDTOs;
            Session["overDeductionBatchDTO"] = overDeductionBatchDTO;

            // Return updated entries as JSON
            return Json(new { success = true, data = overDeductionBatchEntryDTOs });
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

            var overDeductionBatchDTO = await _channelService.FindOverDeductionBatchAsync(id, GetServiceHeader());

            var overdeductionEntries = await _channelService.FindOverDeductionBatchEntriesByOverDeductionBatchIdAsync(id, true, GetServiceHeader());


            TempData["overDeductionBatchDTO"] = overDeductionBatchDTO;

            ViewBag.OverDeductionBatchEntryDTOs = overdeductionEntries;
            TempData["overdeductionEntries"] = overdeductionEntries;

            return View(overDeductionBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(Guid id, OverDeductionBatchDTO  overDeductionBatchDTO)
        {
            overDeductionBatchDTO.ValidateAll();


            var auth = overDeductionBatchDTO.RefundAuthOption;


            if (!overDeductionBatchDTO.HasErrors)
            {
                await _channelService.AuditOverDeductionBatchAsync(overDeductionBatchDTO, auth, GetServiceHeader());

                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(overDeductionBatchDTO.RefundAuthOption.ToString());

                TempData["VerifySuccess"] = "Verification Successiful";

                return RedirectToAction("Index");
            }
            else
            {
                TempData["VerificationFail"] = "Verification Failed!. Review all conditions.";
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);
                var errorMessages = overDeductionBatchDTO.ErrorMessages;

                return View(overDeductionBatchDTO);
            }
        }

        public async Task<ActionResult> Authorize(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);

            var debitBatchDTO = await _channelService.FindOverDeductionBatchAsync(id, GetServiceHeader());
            var overdeductionEntries = await _channelService.FindOverDeductionBatchEntriesByOverDeductionBatchIdAsync(id, true, GetServiceHeader());
            ViewBag.OverDeductionBatchEntryDTOs = overdeductionEntries;

            return View(debitBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Authorize(Guid id, OverDeductionBatchDTO  overDeductionBatchDTO)
        {
            var batchAuthOption = overDeductionBatchDTO.RefundAuthOption;
            overDeductionBatchDTO.ValidateAll();

            if (!overDeductionBatchDTO.HasErrors)
            {
                await _channelService.AuthorizeOverDeductionBatchAsync(overDeductionBatchDTO, batchAuthOption, 1, GetServiceHeader());
               

                    ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(overDeductionBatchDTO.RefundAuthOption.ToString());

                

                TempData["AuthorizationSuccess"] = "Authorization Successiful";
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = overDeductionBatchDTO.ErrorMessages;
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(overDeductionBatchDTO.RefundAuthOption.ToString());
                TempData["AuthorizationFail"] = "Authorization Failed!. Review all conditions.";

                return View(overDeductionBatchDTO);
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
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
            ViewBag.BatchStatus = GetBatchStatusTypeSelectList(string.Empty);


            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, int status, DateTime startDate, DateTime endDate)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;

            var pageCollectionInfo = await _channelService.FindOverDeductionBatchesByStatusAndFilterInPageAsync(
                status, 
                startDate, 
                endDate,
                jQueryDataTablesModel.sSearch, 
                0, 
                int.MaxValue, 
                GetServiceHeader()
                );

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(k => k.CreatedDate)
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
                items: new List<OverDeductionBatchDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
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

            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);
            return View();
        }

        [HttpPost]
        public JsonResult CheckSumAmount()
        {
            var storedOverDeductionBatchDTO = Session["overDeductionBatchDTO"] as OverDeductionBatchDTO;
            if (storedOverDeductionBatchDTO == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Batch cannot be null."
                });
            }
            var overDeductionBatchEntryDTOs = Session["OverDeductionBatchEntryDTO"] as ObservableCollection<OverDeductionBatchEntryDTO>;

            decimal totalPrincipalAndInterest = overDeductionBatchEntryDTOs.Sum(e => e.Principal + e.Interest);
            decimal totalValue = storedOverDeductionBatchDTO?.TotalValue ?? 0;

            if (totalPrincipalAndInterest != totalValue)
            {
                var balance = totalValue - totalPrincipalAndInterest;
                return Json(new { success = false, message = $"The total value ({totalValue}) should be equal to the sum of the entries ({totalPrincipalAndInterest}). Balance: {balance}" });
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<ActionResult> Create(OverDeductionBatchDTO overDeductionBatchDTO)
        {
            // Retrieve the DTO stored in session
            var storedOverDeductionBatchDTO = Session["overDeductionBatchDTO"] as OverDeductionBatchDTO;

            if (storedOverDeductionBatchDTO == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Batch cannot be null."
                });
            }

            // If there are no entries, initialize a new list
            var overDeductionBatchEntryDTOs = Session["OverDeductionBatchEntryDTO"] as ObservableCollection<OverDeductionBatchEntryDTO>;

            if (overDeductionBatchEntryDTOs != null)
            {
                storedOverDeductionBatchDTO.overDeductionBatchEntries = overDeductionBatchEntryDTOs;
            }
            decimal totalPrincipalAndInterest = overDeductionBatchEntryDTOs.Sum(e => e.Principal + e.Interest);
            decimal totalValue = storedOverDeductionBatchDTO?.TotalValue ?? 0;

            if (totalPrincipalAndInterest != totalValue)
            {
                var balance = totalValue - totalPrincipalAndInterest;
                return Json(new { success = false, message = $"The total value ({totalValue}) should be equal to the sum of the entries ({totalPrincipalAndInterest}). Balance: {balance}" });
            }
            // Validate the DTO
            storedOverDeductionBatchDTO.ValidateAll();
            if (storedOverDeductionBatchDTO.ErrorMessageResult != null)
            {
                return Json(new
                {
                    success = false,
                    message = storedOverDeductionBatchDTO.ErrorMessageResult
                });
            }

            // Save the batch data
            var refundBatch = await _channelService.AddOverDeductionBatchAsync(storedOverDeductionBatchDTO, GetServiceHeader());
            if (refundBatch.HasErrors)
            {
                return Json(new
                {
                    success = false,
                    message = refundBatch.ErrorMessages
                });
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

            // Return success message in JSON
            return Json(new
            {
                success = true,
                message = "Successfully created refund batch."
            });
        }








        [HttpPost]
        public async Task<JsonResult> Add(OverDeductionBatchDTO overDeductionBatchDTO)
        {
            await ServeNavigationMenus();

            // Retrieve the batch entries from the session
            var overDeductionBatchEntryDTOs = Session["OverDeductionBatchEntryDTO"] as ObservableCollection<OverDeductionBatchEntryDTO>;
            if (overDeductionBatchEntryDTOs == null)
            {
                overDeductionBatchEntryDTOs = new ObservableCollection<OverDeductionBatchEntryDTO>();
            }

            foreach (var entry in overDeductionBatchDTO.overDeductionBatchEntries)
            {
                // Check if an entry with the same Debit and Credit account numbers already exists
                var existingEntry = overDeductionBatchEntryDTOs
                    .FirstOrDefault(e => e.DebitCustomerAccountFullAccountNumber == entry.DebitCustomerAccountFullAccountNumber
                                      && e.CreditCustomerAccountFullAccountNumber == entry.CreditCustomerAccountFullAccountNumber);

                if (existingEntry != null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "A refund with the same Debit and Credit account numbers already exists."
                    });
                }

                if (entry.Id == Guid.Empty)
                {
                    entry.Id = Guid.NewGuid();
                }
                overDeductionBatchEntryDTOs.Add(entry);

                // Calculate the sum of Principal and Interest
                decimal totalPrincipalAndInterest = overDeductionBatchEntryDTOs.Sum(e => e.Principal + e.Interest);

                // Check if the total value is exceeded
                if (totalPrincipalAndInterest > overDeductionBatchDTO.TotalValue)
                {
                    overDeductionBatchEntryDTOs.Remove(entry);
                    decimal exceededAmount = totalPrincipalAndInterest - overDeductionBatchDTO.TotalValue;
                    return Json(new
                    {
                        success = false,
                        message = $"The total of principal and interest has exceeded the total value by {exceededAmount}."
                    });
                }
            }

            // Save the updated entries back to the session
            Session["OverDeductionBatchEntryDTO"] = overDeductionBatchEntryDTOs;
            Session["overDeductionBatchDTO"] = overDeductionBatchDTO;
            
            return Json(new { success = true, data = overDeductionBatchEntryDTOs });
        }





        [HttpPost]
        public async Task<JsonResult> Remove(Guid id)
        {
            await ServeNavigationMenus();

            var overDeductionBatchEntryDTOs = Session["OverDeductionBatchEntryDTO"] as ObservableCollection<OverDeductionBatchEntryDTO>;


            decimal totalPrincipalAndInterest = overDeductionBatchEntryDTOs.Sum(e => e.Principal + e.Interest);

            if (overDeductionBatchEntryDTOs != null)
            {
                var entryToRemove = overDeductionBatchEntryDTOs.FirstOrDefault(e => e.Id == id);
                if (entryToRemove != null)
                {
                    overDeductionBatchEntryDTOs.Remove(entryToRemove);

                    totalPrincipalAndInterest -= entryToRemove.Principal + entryToRemove.Interest;

                    Session["OverDeductionBatchEntryDTO"] = overDeductionBatchEntryDTOs;
                }
            }

           

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




        [HttpGet]
        public async Task<JsonResult> GetDebitBatchesAsync()
        {
            var debitBatchDTOs = await _channelService.FindDebitBatchesAsync(GetServiceHeader());

            return Json(debitBatchDTOs, JsonRequestBehavior.AllowGet);
        }
    }

}
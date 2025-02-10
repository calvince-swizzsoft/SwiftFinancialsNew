using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class AddGeneralLedgerController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            ViewBag.BatchStatus = GetBatchStatusTypeSelectList(string.Empty);

            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Index(JQueryDataTablesModel jQueryDataTablesModel, int status, DateTime startDate, DateTime endDate)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;

            var pageCollectionInfo = await _channelService.FindGeneralLedgersByStatusAndFilterInPageAsync(
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
                items: new List<GeneralLedgerDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var generalLedgerDTO = await _channelService.FindGeneralLedgerAsync(id, GetServiceHeader());
            var batchentries = await _channelService.FindGeneralLedgerEntriesByGeneralLedgerIdAsync(id, GetServiceHeader());
            ViewBag.GeneralLedgerEntries = batchentries;
            return View(generalLedgerDTO);
        }


        [HttpPost]
        public async Task<JsonResult> CreditCustomerAccountLookUp(Guid id)
        {
            if (id == Guid.Empty)
            {
                return Json(new { success = false, message = "Invalid ID" });
            }

            

            // Find customer account
            var creditCustomerAccount = await _channelService.FindCustomerAccountAsync(id, true, true, true, false, GetServiceHeader());

            if (creditCustomerAccount != null)
            {
                var creditEntry = new GeneralLedgerEntryDTO
                {
                    CreditFullAccountNumber = creditCustomerAccount.FullAccountNumber,
                    CustomerAccountId = creditCustomerAccount.Id,
                    CustomerAccountCustomerId = creditCustomerAccount.CustomerId,
                    CustomerAccountAccountTypeTargetProductDescription = creditCustomerAccount.CustomerAccountTypeProductCodeDescription,
                    CustomerAccountCustomerAccountTypeTargetProductId = creditCustomerAccount.CustomerAccountTypeTargetProductId,
                    ChartOfAccountId = creditCustomerAccount.CustomerAccountTypeTargetProductChartOfAccountId,
                    CustomerAccountCustomerIndividualFirstName = creditCustomerAccount.CustomerFullName,

                };

                return Json(new { success = true, data = creditEntry });
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

            

            // Fetch data based on ID
            var debitCustomerAccount = await _channelService.FindCustomerAccountAsync(id, true, true, true, false, GetServiceHeader());

            if (debitCustomerAccount != null)
            {
                var debitEntry = new GeneralLedgerEntryDTO
                {
                    DebitFullAccountNumber = debitCustomerAccount.FullAccountNumber,
                    ContraCustomerAccountId = debitCustomerAccount.Id,
                    ContraCustomerAccountCustomerId = debitCustomerAccount.CustomerId,
                    ContraCustomerAccountAccountTypeTargetProductDescription = debitCustomerAccount.CustomerAccountTypeProductCodeDescription,
                    ContraCustomerAccountCustomerAccountTypeTargetProductId = debitCustomerAccount.CustomerAccountTypeTargetProductId,
                    ContraChartOfAccountId = debitCustomerAccount.CustomerAccountTypeTargetProductChartOfAccountId,
                    ContraCustomerAccountCustomerIndividualFirstName = debitCustomerAccount.CustomerFullName,
                    
                };

                return Json(new { success = true, data = debitEntry });
            }

            return Json(new { success = false, message = "Customer account not found" });
        }

        [HttpPost]
        public async Task<JsonResult> Add(GeneralLedgerDTO  generalLedgerDTO)
        {
            await ServeNavigationMenus();

            // Retrieve the batch entries from the session
            var generalLedgerBatchEntryDTOs = Session["generalLedgerBatchEntryDTOs"] as ObservableCollection<GeneralLedgerEntryDTO>;
            if (generalLedgerBatchEntryDTOs == null)
            {
                generalLedgerBatchEntryDTOs = new ObservableCollection<GeneralLedgerEntryDTO>();
            }

            foreach (var entry in generalLedgerDTO.GeneralLedgerEntries)
            {
                // Check if an entry with the same Debit and Credit account numbers already exists
                var existingEntry = generalLedgerBatchEntryDTOs
                .FirstOrDefault(e =>
                    (e.DebitFullAccountNumber == entry.DebitFullAccountNumber && e.CreditFullAccountNumber == entry.CreditFullAccountNumber) ||
                    (e.DebitFullAccountNumber == entry.DebitFullAccountNumber && e.ChartOfAccountId == entry.ChartOfAccountId) ||
                    (e.ContraChartOfAccountId == entry.ContraChartOfAccountId && e.DebitFullAccountNumber == entry.DebitFullAccountNumber) ||
                    (e.ContraChartOfAccountId == entry.ContraChartOfAccountId && e.ChartOfAccountId == entry.ChartOfAccountId));


                if (existingEntry != null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "A general Ledger with the same Debit and Credit transaction already exists."
                    });
                }

                if (entry.Id == Guid.Empty)
                {
                    entry.Id = Guid.NewGuid();
                }
                if (entry.ChartOfAccountId == Guid.Empty)
                {
                    entry.ChartOfAccountId = (Guid)entry.CustomerAccountCustomerAccountTypeTargetProductId;
                }
                if (entry.ContraChartOfAccountId == Guid.Empty)
                {
                    entry.ContraChartOfAccountId = (Guid)entry.ContraCustomerAccountCustomerAccountTypeTargetProductId;
                }
               
                generalLedgerBatchEntryDTOs.Add(entry);

                // Calculate the sum of Principal and Interest
                decimal sumAmount = generalLedgerBatchEntryDTOs.Sum(e => e.Amount);

                // Check if the total value is exceeded
                if (sumAmount > generalLedgerDTO.TotalValue)
                {
                    generalLedgerBatchEntryDTOs.Remove(entry);
                    decimal exceededAmount = sumAmount - generalLedgerDTO.TotalValue;
                    return Json(new
                    {
                        success = false,
                        message = $"The total of principal and interest has exceeded the total value by {exceededAmount}."
                    });
                }
            }

            // Save the updated entries back to the session
            Session["generalLedgerBatchEntryDTOs"] = generalLedgerBatchEntryDTOs;
            Session["generalLedgerDTO"] = generalLedgerDTO;

            return Json(new { success = true, data = generalLedgerBatchEntryDTOs });
        }

        [HttpPost]
        public async Task<JsonResult> Remove(Guid id)
        {
            await ServeNavigationMenus();

            var generalLedgerBatchEntryDTOs = Session["generalLedgerBatchEntryDTOs"] as ObservableCollection<GeneralLedgerEntryDTO>;


            decimal sumAmount = generalLedgerBatchEntryDTOs.Sum(e => e.Amount);

            if (generalLedgerBatchEntryDTOs != null)
            {
                var entryToRemove = generalLedgerBatchEntryDTOs.FirstOrDefault(e => e.Id == id);
                if (entryToRemove != null)
                {
                    generalLedgerBatchEntryDTOs.Remove(entryToRemove);

                    sumAmount -= entryToRemove.Amount;

                    Session["generalLedgerBatchEntryDTOs"] = generalLedgerBatchEntryDTOs;
                }
            }



            return Json(new { success = true, data = generalLedgerBatchEntryDTOs });
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);

            ViewBag.SystemGeneralLedgerAccountCodeSelectList = GetSystemGeneralLedgerAccountCodeSelectList(string.Empty);
            ViewBag.GeneralLedgerEntryTypeSelectList = GetGeneralLedgerEntryTypeSelectList(string.Empty);
            return View();
        }



        [HttpPost]
        public async Task<ActionResult> Create(GeneralLedgerDTO  generalLedgerDTO)
        {
            // Retrieve the DTO stored in session
             generalLedgerDTO = Session["generalLedgerDTO"] as GeneralLedgerDTO;

            if (generalLedgerDTO == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Batch cannot be null."
                });
            }



            // If there are no entries, initialize a new list
            var generalLedgerBatchEntryDTOs = Session["generalLedgerBatchEntryDTOs"] as ObservableCollection<GeneralLedgerEntryDTO>;

            if (generalLedgerBatchEntryDTOs != null)
            {
                generalLedgerDTO.GeneralLedgerEntries = generalLedgerBatchEntryDTOs;
            }

            decimal SumAmount = generalLedgerBatchEntryDTOs.Sum(e => e.Amount);
            decimal totalValue = generalLedgerDTO?.TotalValue ?? 0;

            if (SumAmount != totalValue)
            {
                var balance = totalValue - SumAmount;
                return Json(new { success = false, message = $"The total value ({totalValue}) should be equal to the sum of the entries ({SumAmount}). Balance: {balance}" });
            }

            // Validate the DTO
            generalLedgerDTO.ValidateAll();
            if (generalLedgerDTO.ErrorMessages.Count != 0)
            {
                return Json(new
                {
                    success = false,
                    message = generalLedgerDTO.ErrorMessages
                });
            }

            // Save the batch data
            var generalLedgerdBatch = await _channelService.AddGeneralLedgerAsync(generalLedgerDTO, GetServiceHeader());
            if (generalLedgerdBatch.HasErrors)
            {
                return Json(new
                {
                    success = false,
                    message = generalLedgerdBatch.ErrorMessages
                });
            }

            // Save each batch entry
            foreach (var generalLedgerBatchEntry in generalLedgerBatchEntryDTOs)
            {
                generalLedgerBatchEntry.GeneralLedgerId = generalLedgerdBatch.Id;
                generalLedgerBatchEntry.BranchId = generalLedgerdBatch.BranchId;
                generalLedgerBatchEntry.BranchDescription = generalLedgerdBatch.BranchDescription;
                await _channelService.AddGeneralLedgerEntryAsync(generalLedgerBatchEntry, GetServiceHeader());
            }

            // Clear session data after successful creation
            Session["generalLedgerBatchEntryDTOs"] = null;
            Session["generalLedgerDTO"] = null;

            // Return success message in JSON
            return Json(new
            {
                success = true,
                message = "Successfully created refund batch."
            });
        }


        



        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var postingPeriodDTO = await _channelService.FindGeneralLedgerAsync(id, GetServiceHeader());

            return View(postingPeriodDTO.MapTo<GeneralLedgerDTO>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, GeneralLedgerDTO generalLedgerDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateGeneralLedgerAsync(generalLedgerDTO.MapTo<GeneralLedgerDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(generalLedgerDTO);
            }
        }


        public async Task<ActionResult> Verify(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);
            var generalLedgerDTO = await _channelService.FindGeneralLedgerAsync(id, GetServiceHeader());
            var batchentries = await _channelService.FindGeneralLedgerEntriesByGeneralLedgerIdAsync(id, GetServiceHeader());

            ViewBag.GeneralLedgerEntries = batchentries;
            return View(generalLedgerDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(Guid id, GeneralLedgerDTO  generalLedgerDTO)
        {
            generalLedgerDTO.ValidateAll();


            var auth = generalLedgerDTO.GeneralLedgerAuthOption;


            if (!generalLedgerDTO.HasErrors)
            {
                await _channelService.AuditGeneralLedgerAsync(generalLedgerDTO, auth, GetServiceHeader());

                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(generalLedgerDTO.GeneralLedgerAuthOption.ToString());

                TempData["VerifySuccess"] = "Verification Successiful";

                return RedirectToAction("Index");
            }
            else
            {
                TempData["VerificationFail"] = "Verification Failed!. Review all conditions.";
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);
                var errorMessages = generalLedgerDTO.ErrorMessages;

                return View(generalLedgerDTO);
            }
        }

        public async Task<ActionResult> Authorize(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);


            var generalLedgerDTO = await _channelService.FindGeneralLedgerAsync(id, GetServiceHeader());
            var batchentries = await _channelService.FindGeneralLedgerEntriesByGeneralLedgerIdAsync(id, GetServiceHeader());
            ViewBag.GeneralLedgerEntries = batchentries;
            return View(generalLedgerDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Authorize(Guid id, GeneralLedgerDTO  generalLedgerDTO)
        {
            var batchAuthOption = generalLedgerDTO.GeneralLedgerAuthOption;
            generalLedgerDTO.ValidateAll();

            if (!generalLedgerDTO.HasErrors)
            {
                await _channelService.AuthorizeGeneralLedgerAsync(generalLedgerDTO, batchAuthOption, 1, GetServiceHeader());


                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(generalLedgerDTO.GeneralLedgerAuthOption.ToString());



                TempData["AuthorizationSuccess"] = "Authorization Successiful";
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = generalLedgerDTO.ErrorMessages;
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(generalLedgerDTO.GeneralLedgerAuthOption.ToString());
                TempData["AuthorizationFail"] = "Authorization Failed!. Review all conditions.";

                return View(generalLedgerDTO);
            }
        }






        [HttpGet]
        public async Task<JsonResult> GetPostingPeriodsAsync()
        {
            var postingPeriodsDTOs = await _channelService.FindPostingPeriodsAsync(GetServiceHeader());

            return Json(postingPeriodsDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
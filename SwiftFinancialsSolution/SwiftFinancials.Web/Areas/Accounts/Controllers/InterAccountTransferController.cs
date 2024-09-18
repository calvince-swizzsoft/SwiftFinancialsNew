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
    public class InterAccountTransferController : MasterController
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
            

           

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;


            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindInterAccountTransferBatchesByStatusAndFilterInPageAsync(status,startDate, endDate, jQueryDataTablesModel.sSearch, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<InterAccountTransferBatchDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }








        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var WireTransferBatch = await _channelService.FindInterAccountTransferBatchAsync(id, GetServiceHeader());
            var batchentries = await _channelService.FindInterAccountTransferBatchEntriesByInterAccountTransferBatchIdAsync(id, GetServiceHeader());
            ViewBag.InterAccountTransferEntries = batchentries;
            return View(WireTransferBatch);
        }










        [HttpPost]
        public async Task<JsonResult> EntryCreditCustomerAccountLookUp(Guid id)
        {

            if (id == Guid.Empty)
            {
                return Json(new { success = false, message = "Invalid ID" });
            }
            var creditcustomerAccount = await _channelService.FindCustomerAccountAsync(id, true, true, true, false, GetServiceHeader());

            if (creditcustomerAccount != null)
            {
                if(creditcustomerAccount.CustomerAccountTypeProductCode == 2)
                {
                    var creditEntry = new InterAccountTransferBatchEntryDTO
                    {
                        InterAccountTransferBatchBranchId = creditcustomerAccount.BranchId,
                        CustomerFullName = creditcustomerAccount.CustomerFullName,
                        AccountNumber = creditcustomerAccount.FullAccountNumber,
                        CustomerAccountCustomerReference2 = creditcustomerAccount.CustomerReference2,
                        CustomerAccountCustomerReference3 = creditcustomerAccount.CustomerReference3,
                        CustomerPersonalIdentificationNumber = creditcustomerAccount.CustomerIndividualIdentityCardNumber,
                        BookBalance = creditcustomerAccount.BookBalance,
                        AccountStatusDescription = creditcustomerAccount.StatusDescription,
                        Remarks = creditcustomerAccount.Remarks,
                        CustomerAccountId = creditcustomerAccount.Id,
                        CustomerType = creditcustomerAccount.CustomerType,
                        CustomerTypeDescription = creditcustomerAccount.CustomerTypeDescription,
                        CustomerIndividualPayrollNumbers = creditcustomerAccount.CustomerIndividualPayrollNumbers,
                        CustomerAccountCustomerReference1 = creditcustomerAccount.CustomerReference1,
                        CustomerAccountCustomerAccountTypeProductCode = creditcustomerAccount.CustomerAccountTypeProductCode,
                        Interest = creditcustomerAccount.InterestBalance,
                        Principal = creditcustomerAccount.PrincipalBalance
                        

                    };

                    return Json(new { success = true, data = creditEntry });
                }


               
            }

            return Json(new { success = false, message = "Customer account not found" });
        }
        [HttpPost]
        public async Task<JsonResult> CustomerAccountLookUp(Guid id)
        {

            if (id == Guid.Empty)
            {
                return Json(new { success = false, message = "Invalid ID" });
            }
            var creditcustomerAccount = await _channelService.FindCustomerAccountAsync(id, true, true, true, false, GetServiceHeader());

            if (creditcustomerAccount != null)
            {


                var creditEntry = new InterAccountTransferBatchDTO
                {
                    BranchId = creditcustomerAccount.BranchId,
                    CustomerFullName = creditcustomerAccount.CustomerFullName,
                    AccountNumber = creditcustomerAccount.FullAccountNumber,
                    CustomerAccountCustomerReference2 = creditcustomerAccount.CustomerReference2,
                    CustomerAccountCustomerReference3 = creditcustomerAccount.CustomerReference3,
                    CustomerPersonalIdentificationNumber = creditcustomerAccount.CustomerIndividualIdentityCardNumber,
                    AvailableBalance = creditcustomerAccount.AvailableBalance,
                    AccountStatusDescription = creditcustomerAccount.StatusDescription,
                    CustomerId = creditcustomerAccount.CustomerId,
                    Remarks = creditcustomerAccount.Remarks,
                    CustomerAccountId = creditcustomerAccount.Id,
                    CustomerTypeDescription = creditcustomerAccount.CustomerTypeDescription,
                    CustomerIndividualPayrollNumbers = creditcustomerAccount.CustomerIndividualPayrollNumbers,
                    CustomerAccountCustomerReference1 = creditcustomerAccount.CustomerReference1
                    

                };

                return Json(new { success = true, data = creditEntry });
            }

            return Json(new { success = false, message = "Customer account not found" });
        }

        [HttpPost]
        public async Task<JsonResult> Add(InterAccountTransferBatchDTO  interAccountTransferBatchDTO)
        {
            await ServeNavigationMenus();

            // Retrieve the batch entries from the session
            var interAccountBatchEntryDTOs = Session["interAccountBatchEntryDTOs"] as ObservableCollection<InterAccountTransferBatchEntryDTO>;
            if (interAccountBatchEntryDTOs == null)
            {
                interAccountBatchEntryDTOs = new ObservableCollection<InterAccountTransferBatchEntryDTO>();
            }

            foreach (var entry in interAccountTransferBatchDTO.interAccountBatchEntries)
            {
                // Check if an entry with the same Debit and Credit account numbers already exists
                var existingEntry = interAccountBatchEntryDTOs
                .FirstOrDefault(e =>
                    (e.AccountNumber != null && entry.AccountNumber != null && e.AccountNumber == entry.AccountNumber) ||
                    (e.ChartOfAccountId != null && entry.ChartOfAccountId != null && e.ChartOfAccountId == entry.ChartOfAccountId));



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


                interAccountBatchEntryDTOs.Add(entry);

                // Calculate the sum of Principal and Interest
                decimal sumAmount = interAccountBatchEntryDTOs.Sum(e => e.Principal + e.Interest);

                // Check if the total value is exceeded
                if (sumAmount > interAccountTransferBatchDTO.AvailableBalance)
                {
                    interAccountBatchEntryDTOs.Remove(entry);
                    decimal exceededAmount = sumAmount - interAccountTransferBatchDTO.AvailableBalance;
                    return Json(new
                    {
                        success = false,
                        message = $"The total of principal and interest has exceeded the  AvailableBalance by {exceededAmount}."
                    });
                }
            }

            // Save the updated entries back to the session
            Session["interAccountBatchEntryDTOs"] = interAccountBatchEntryDTOs;
            Session["interAccountTransferBatchDTO"] = interAccountTransferBatchDTO;

            return Json(new { success = true, data = interAccountBatchEntryDTOs });
        }





        [HttpPost]
        public async Task<JsonResult> Remove(Guid id)
        {
            await ServeNavigationMenus();

            var interAccountBatchEntryDTOs = Session["interAccountBatchEntryDTOs"] as ObservableCollection<InterAccountTransferBatchEntryDTO>;


            decimal sumAmount = interAccountBatchEntryDTOs.Sum(e => e.Principal + e.Interest);

            if (interAccountBatchEntryDTOs != null)
            {
                var entryToRemove = interAccountBatchEntryDTOs.FirstOrDefault(e => e.Id == id);
                if (entryToRemove != null)
                {
                    interAccountBatchEntryDTOs.Remove(entryToRemove);

                    sumAmount -= entryToRemove.Principal + entryToRemove.Interest;

                    Session["interAccountBatchEntryDTOs"] = interAccountBatchEntryDTOs;
                }
            }



            return Json(new { success = true, data = interAccountBatchEntryDTOs });
        }





        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);

            ViewBag.SystemGeneralLedgerAccountCodeSelectList = GetSystemGeneralLedgerAccountCodeSelectList(string.Empty);
            ViewBag.GetApportionToSelectList = GetApportionToSelectList(string.Empty);
            return View();
        }



        [HttpPost]
        public async Task<ActionResult> Create(InterAccountTransferBatchDTO  interAccountTransferBatchDTO)
        {
            // Retrieve the DTO stored in session
            interAccountTransferBatchDTO = Session["interAccountTransferBatchDTO"] as InterAccountTransferBatchDTO;

            if (interAccountTransferBatchDTO == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Batch cannot be null."
                });
            }



            // If there are no entries, initialize a new list
            var interAccountBatchEntryDTOs = Session["interAccountBatchEntryDTOs"] as ObservableCollection<InterAccountTransferBatchEntryDTO>;

            if (interAccountBatchEntryDTOs != null)
            {
                interAccountTransferBatchDTO.interAccountBatchEntries = interAccountBatchEntryDTOs;
            }

            decimal SumAmount = interAccountBatchEntryDTOs.Sum(e => e.Principal + e.Interest);
            decimal totalValue = interAccountTransferBatchDTO?.AvailableBalance ?? 0;

            if (SumAmount != totalValue)
            {
                var balance = totalValue - SumAmount;
                return Json(new { success = false, message = $"The total value ({totalValue}) should be equal to the sum of the entries ({SumAmount}). Balance: {balance}" });
            }

            // Validate the DTO
            interAccountTransferBatchDTO.ValidateAll();
            if (interAccountTransferBatchDTO.ErrorMessages.Count != 0)
            {
                return Json(new
                {
                    success = false,
                    message = interAccountTransferBatchDTO.ErrorMessages
                });
            }

            // Save the batch data
            var interAccountTransferBatch = await _channelService.AddInterAccountTransferBatchAsync(interAccountTransferBatchDTO, GetServiceHeader());
            if (interAccountTransferBatchDTO.HasErrors)
            {
                return Json(new
                {
                    success = false,
                    message = interAccountTransferBatchDTO.ErrorMessages
                });
            }

            // Save each batch entry
            foreach (var InterAccountTransferBatchEntry in interAccountBatchEntryDTOs)
            {
                InterAccountTransferBatchEntry.InterAccountTransferBatchId = interAccountTransferBatch.Id;
                await _channelService.AddInterAccountTransferBatchEntryAsync(InterAccountTransferBatchEntry, GetServiceHeader());
            }

            // Clear session data after successful creation
            Session["interAccountBatchEntryDTOs"] = null;
            Session["interAccountTransferBatchDTO"] = null;

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
            var interAccountTransferBatchDTO = await _channelService.FindInterAccountTransferBatchAsync(id, GetServiceHeader());

            CustomerAccountDTO customerAcc= await _channelService.FindCustomerAccountAsync(interAccountTransferBatchDTO.CustomerAccountId, true, true, true, true, GetServiceHeader());

            interAccountTransferBatchDTO.CustomerId = customerAcc.CustomerId;
            
            var batchentries = await _channelService.FindInterAccountTransferBatchEntriesByInterAccountTransferBatchIdAsync(id, GetServiceHeader());

            ViewBag.InterAccountTransferEntries = batchentries;
            return View(interAccountTransferBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(Guid id, InterAccountTransferBatchDTO  interAccountTransferBatchDTO)
        {
            interAccountTransferBatchDTO.ValidateAll();


            var auth = interAccountTransferBatchDTO.WireTransferAuthOption;


            if (!interAccountTransferBatchDTO.HasErrors)
            {
                await _channelService.AuditInterAccountTransferBatchAsync(interAccountTransferBatchDTO, auth, GetServiceHeader());

                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(interAccountTransferBatchDTO.WireTransferAuthOption.ToString());

                TempData["VerifySuccess"] = "Verification Successiful";

                return RedirectToAction("Index");
            }
            else
            {
                TempData["VerificationFail"] = "Verification Failed!. Review all conditions.";
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);
                var errorMessages = interAccountTransferBatchDTO.ErrorMessages;

                return View(interAccountTransferBatchDTO);
            }
        }

        public async Task<ActionResult> Authorize(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);


            var interAccountTransferBatchDTO = await _channelService.FindInterAccountTransferBatchAsync(id, GetServiceHeader());
            var batchentries = await _channelService.FindInterAccountTransferBatchEntriesByInterAccountTransferBatchIdAsync(id, GetServiceHeader());

            CustomerAccountDTO customerAcc = await _channelService.FindCustomerAccountAsync(interAccountTransferBatchDTO.CustomerAccountId, true, true, true, true, GetServiceHeader());

            interAccountTransferBatchDTO.CustomerId = customerAcc.CustomerId;

            ViewBag.InterAccountTransferEntries = batchentries;
            return View(interAccountTransferBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Authorize(Guid id, InterAccountTransferBatchDTO  interAccountTransferBatchDTO)
        {
            var batchAuthOption = interAccountTransferBatchDTO.WireTransferAuthOption;
            interAccountTransferBatchDTO.ValidateAll();

            if (!interAccountTransferBatchDTO.HasErrors)
            {
                await _channelService.AuthorizeInterAccountTransferBatchAsync(interAccountTransferBatchDTO, batchAuthOption, 1, GetServiceHeader());


                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(interAccountTransferBatchDTO.WireTransferAuthOption.ToString());



                TempData["AuthorizationSuccess"] = "Authorization Successiful";
                return RedirectToAction("Index");
            }
            else
            {

                var errorMessages = interAccountTransferBatchDTO.ErrorMessages;
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(interAccountTransferBatchDTO.WireTransferAuthOption.ToString());
                TempData["AuthorizationFail"] = "Authorization Failed!. Review all conditions.";

                return View(interAccountTransferBatchDTO);
            }
        }


        
    }
}
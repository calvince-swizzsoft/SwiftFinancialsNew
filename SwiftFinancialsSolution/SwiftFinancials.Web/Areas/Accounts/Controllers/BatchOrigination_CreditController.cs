using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using AutoMapper.Execution;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class BatchOrigination_CreditController : MasterController
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

            var pageCollectionInfo = await _channelService.FindCreditBatchesByStatusAndFilterInPageAsync(
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
                items: new List<CreditBatchDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }

        public async Task<ActionResult> CreditTypeLookup(Guid? id)
        {
            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(string.Empty);
            ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);

            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("create");
            }

            CreditBatchDTO creditBatch = new CreditBatchDTO();

            var creditType = await _channelService.FindCreditTypeAsync(parseId, GetServiceHeader());
            if (creditType != null)
            {
                creditBatch.CreditTypeId = creditType.Id;
                creditBatch.CreditTypeDescription = creditType.Description;
                
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        CreditTypeId = creditBatch.CreditTypeId,
                        CreditTypeDescription = creditBatch.CreditTypeDescription
                    }
                });
            }
            return Json(new { success = false, message = "Credit Type not found" });
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(string.Empty);
            ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);

            var creditBatchDTO = await _channelService.FindCreditBatchAsync(id, GetServiceHeader());

            var creditBatches = await _channelService.FindCreditBatchEntriesByCreditBatchIdAsync(id, true, GetServiceHeader());

            ViewBag.CreditBatchEntryDTOs = creditBatches;

            return View(creditBatchDTO);
        }


        public void BatchOrigination_Credit(CreditBatchDTO creditBatchDTO)
        {
            
            Session["HeaderDetails"] = creditBatchDTO;

            
            
        }

        [HttpPost]
        public async Task<JsonResult> CreditCustomerAccountLookUp(Guid id)
        {
            if (id == Guid.Empty)
            {
                return Json(new { success = false, message = "Invalid ID" });
            }

            //Viewbags
            ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(string.Empty);
            ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);
             
            

            CreditBatchEntryDTOs = Session["CreditBatchEntryDTO"] as ObservableCollection<CreditBatchEntryDTO>;


            ViewBag.CreditBatchEntryDTOs = CreditBatchEntryDTOs;


            var entry = new CreditBatchEntryDTO();

            var creditcustomerAccount = await _channelService.FindCustomerAccountAsync(id, true, true, true, false, GetServiceHeader());

            if (creditcustomerAccount != null)
            {
                // Populate entry with the retrieved data
                entry.CreditCustomerAccountFullName = creditcustomerAccount.CustomerFullName;
                entry.CreditCustomerAccountFullAccountNumber = creditcustomerAccount.FullAccountNumber;
                entry.CustomerAccountCustomerReference2 = creditcustomerAccount.CustomerReference2;
                entry.CustomerAccountCustomerReference3 = creditcustomerAccount.CustomerReference3;
                entry.CreditCustomerAccountIdentificationNumber = creditcustomerAccount.CustomerIndividualIdentityCardNumber;
                entry.CreditCustomerAccountStatusDescription = creditcustomerAccount.StatusDescription;
                entry.CreditCustomerAccountRemarks = creditcustomerAccount.Remarks;
                entry.ProductDescription = creditcustomerAccount.CustomerAccountTypeProductCodeDescription;
                entry.CustomerAccountCustomerId = creditcustomerAccount.Id;
                entry.CreditCustomerAccountTypeDescription = creditcustomerAccount.TypeDescription;
                entry.CustomerAccountCustomerIndividualPayrollNumbers = creditcustomerAccount.CustomerIndividualPayrollNumbers;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        CreditCustomerAccountFullName=entry.CreditCustomerAccountFullName,
                        CreditCustomerAccountFullAccountNumber=entry.CreditCustomerAccountFullAccountNumber ,
                        CustomerAccountCustomerReference3=entry.CustomerAccountCustomerReference3,
                        CustomerAccountCustomerReference2=entry.CustomerAccountCustomerReference2,
                        CreditCustomerAccountIdentificationNumber =entry.CreditCustomerAccountIdentificationNumber,
                        CreditCustomerAccountStatusDescription=entry.CreditCustomerAccountStatusDescription,
                        CreditCustomerAccountRemarks=entry.CreditCustomerAccountRemarks,
                        ProductDescription=entry.ProductDescription,
                        CustomerAccountCustomerId=entry.CustomerAccountCustomerId ,
                        CreditCustomerAccountTypeDescription=entry.CreditCustomerAccountTypeDescription ,
                        CustomerAccountCustomerIndividualPayrollNumbers=entry.CustomerAccountCustomerIndividualPayrollNumbers 

            }
                });
            }

            return Json(new { success = false, message = "Customer account not found" });
        }


        [HttpPost]
        public async Task<ActionResult> Add(CreditBatchDTO creditBatchDTO)
        {
            await ServeNavigationMenus();

            var creditBatchEntryDTOs = Session["CreditBatchEntryDTO"] as ObservableCollection<CreditBatchEntryDTO>;

            if (creditBatchEntryDTOs == null)
            {
                creditBatchEntryDTOs = new ObservableCollection<CreditBatchEntryDTO>();
            }

            decimal sumAmount = creditBatchEntryDTOs.Sum(cs => cs.Principal + cs.Interest);

            foreach (var creditBatchEntryDTO in creditBatchDTO.CreditBatchEntries)
            {
                var existingEntry = creditBatchEntryDTOs.FirstOrDefault(e => e.CreditCustomerAccountFullAccountNumber == creditBatchEntryDTO.CreditCustomerAccountFullAccountNumber);

                if (existingEntry != null)
                {
                    // If found, return a message indicating the account already exists
                    return Json(new
                    {
                        success = false,
                        message = $"A credit entry with account number {creditBatchEntryDTO.CreditCustomerAccountFullAccountNumber} already exists."
                    });
                }

                sumAmount += creditBatchEntryDTO.Principal + creditBatchEntryDTO.Interest;

                if (creditBatchEntryDTO.Id == Guid.Empty)
                {
                    creditBatchEntryDTO.Id = Guid.NewGuid();
                }

                if (sumAmount > creditBatchDTO.TotalValue)
                {
                    return Json(new { success = false, message = "Failed to add Credit Entry. Total Amount exceeded Total Value." });
                }

                creditBatchEntryDTOs.Add(creditBatchEntryDTO);
            }

            // Update session values
            Session["CreditBatchEntryDTO"] = creditBatchEntryDTOs;
            Session["CreditBatchDTO"] = creditBatchDTO;

            // Return updated entries to the client
            return Json(new { success = true, entries = creditBatchEntryDTOs });
        }


        [HttpPost]
        public async Task<JsonResult> Remove(Guid id)
        {
            await ServeNavigationMenus();

            var creditBatchEntryDTOs = Session["CreditBatchEntryDTO"] as ObservableCollection<CreditBatchEntryDTO>;
            decimal sumAmount = creditBatchEntryDTOs.Sum(cs => cs.Principal + cs.Interest);
            if (creditBatchEntryDTOs != null)
            {
                var entryToRemove = creditBatchEntryDTOs.FirstOrDefault(e => e.Id == id);
                if (entryToRemove != null)
                {
                    creditBatchEntryDTOs.Remove(entryToRemove);

                    sumAmount -= entryToRemove.Principal + entryToRemove.Interest;

                    Session["CreditBatchEntryDTO"] = creditBatchEntryDTOs;
                }
            }



            return Json(new { success = true, data = creditBatchEntryDTOs });
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);


            ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(string.Empty);
            ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(CreditBatchDTO creditBatchDTO)
        {
            try
            {
                // Cheat: In case header details do not hit Add Action
                // Session handling code remains as-is
                Session["RecoverCarryForwards"] = creditBatchDTO.RecoverCarryForwards;
                Session["PreserveAccountBalance"] = creditBatchDTO.PreserveAccountBalance;
                Session["RecoverIndefiniteCharges"] = creditBatchDTO.RecoverIndefiniteCharges;
                Session["RecoverArrearages"] = creditBatchDTO.RecoverArrearages;

                // Retrieve DTO from session
                creditBatchDTO = Session["creditBatchDTO"] as CreditBatchDTO;
                var creditBatchEntryDTOs = Session["CreditBatchEntryDTO"] as ObservableCollection<CreditBatchEntryDTO>;
                if (creditBatchDTO == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Batch cannot be null."
                    });
                }

                // Retrieve CreditBatchEntries from session
                

                if (creditBatchEntryDTOs != null)
                {
                    decimal totalPrincipalAndInterest = creditBatchEntryDTOs.Sum(e => e.Principal + e.Interest);
                    decimal totalValue = creditBatchDTO?.TotalValue ?? 0;

                    if (totalPrincipalAndInterest != totalValue)
                    {
                        var balance = totalValue - totalPrincipalAndInterest;
                        return Json(new { success = false, message = $"The total value ({totalValue}) should be equal to the sum of the entries ({totalPrincipalAndInterest}). Balance: {balance}" });
                    }

                    creditBatchDTO.CreditBatchEntries = creditBatchEntryDTOs;
                }

                // Set additional batch properties from session
                if (Session["RecoverCarryForwards"] != null)
                {
                    creditBatchDTO.RecoverCarryForwards = (bool)Session["RecoverCarryForwards"];
                }

                if (Session["PreserveAccountBalance"] != null)
                {
                    creditBatchDTO.PreserveAccountBalance = (bool)Session["PreserveAccountBalance"];
                }

                if (Session["RecoverIndefiniteCharges"] != null)
                {
                    creditBatchDTO.RecoverIndefiniteCharges = (bool)Session["RecoverIndefiniteCharges"];
                }

                if (Session["RecoverArrearages"] != null)
                {
                    creditBatchDTO.RecoverArrearages = (bool)Session["RecoverArrearages"];
                }

                // Validate the batch DTO
                creditBatchDTO.ValidateAll();

                // If no errors, proceed with batch creation
                if (!creditBatchDTO.HasErrors)
                {
                    var creditBatch = await _channelService.AddCreditBatchAsync(creditBatchDTO, GetServiceHeader());

                    // Check if the service returned any errors
                    if (creditBatch.HasErrors)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Error creating the batch: " + string.Join(", ", creditBatchDTO.ErrorMessages)
                        });
                    }

                    // Save each batch entry
                    foreach (var creditBatchEntry in creditBatchEntryDTOs)
                    {
                        creditBatchEntry.CreditBatchId = creditBatch.Id;
                        await _channelService.AddCreditBatchEntryAsync(creditBatchEntry, GetServiceHeader());
                    }

                    // Clear session after success
                    Session["CreditBatchEntryDTO"] = null;
                    Session["CreditBatchDTO"] = null;
                    Session["sumAmount"] = null;

                    return Json(new
                    {
                        success = true,
                        message = "Successfully Created Credit Batch"
                    });
                }
                else
                {
                    // If validation errors exist, return them in JSON format
                    return Json(new
                    {
                        success = false,
                        message = "Validation errors: " + string.Join(", ", creditBatchDTO.ErrorMessages)
                    });
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions and return a failure response
                return Json(new
                {
                    success = false,
                    message = "An unexpected error occurred: " + ex.Message
                });
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, CreditBatchDTO creditBatchDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateCreditBatchAsync(creditBatchDTO, GetServiceHeader());
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(creditBatchDTO.Type.ToString());
                ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(creditBatchDTO.Type.ToString());
                ViewBag.MonthsSelectList = GetMonthsAsync(creditBatchDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetCreditBatchesAsync(creditBatchDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(creditBatchDTO.Priority.ToString());
                ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(creditBatchDTO.ConcessionType.ToString());
                return RedirectToAction("Index");
            }
            else
            {
                return View(creditBatchDTO);
            }
        }

        public async Task<JsonResult> GetCreditBatchesAsync()
        {
            var creditBatchDTOs = await _channelService.FindCreditBatchesAsync(GetServiceHeader());

            return Json(creditBatchDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}

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
    public class BatchOrigination_WireTransferController : MasterController
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

            var pageCollectionInfo = await _channelService.FindWireTransferBatchesByStatusAndFilterInPageAsync(1, startDate, endDate, jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(x => x.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<WireTransferBatchDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var wireTransferBatch = await _channelService.FindWireTransferBatchAsync(id, GetServiceHeader());


            
            var wireTransferEntries = await _channelService.FindWireTransferBatchEntriesByWireTransferBatchIdAsync(id, true, GetServiceHeader());

            ViewBag.WireTransferEntryDTOs = wireTransferEntries;



            return View(wireTransferBatch);
        }


        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();
            ViewBag.BatchType = GetWireTransferBatchTypeSelectList(string.Empty);
            ViewBag.Priority = GetQueuePriorityAsync(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> WireTransferTypeLookup(Guid? id)
        {
            // Fetch all wire transfer types and filter by the provided id
            var wireTransferTypes = await _channelService.FindWireTransferTypesAsync(GetServiceHeader());
            var wireTransferType = wireTransferTypes.FirstOrDefault(wt => wt.Id == id);

            if (wireTransferType == null)
            {
                return Json(new { success = false, message = "Wire Transfer Type not found." });
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    WireTransferTypeDescription = wireTransferType.Description,
                    WireTransferTypeId = wireTransferType.Id
                }
            });
        }


        

        public async Task<JsonResult> WireTransferCustomerAccountLookUp(Guid id)
        {
            // Initialize the DTO without using Session
            var wireTransferBatchDTO = new WireTransferBatchDTO
            {
                WireTransferEntries = new ObservableCollection<WireTransferBatchEntryDTO>()
            };

            // Validate the provided ID
            if (id == Guid.Empty)
            {
                return Json(new { success = false, message = "Invalid ID provided." });
            }

            // Fetch customer account details
            var creditCustomerAccount = await _channelService.FindCustomerAccountAsync(id, true, true, true, false, GetServiceHeader());

            if (creditCustomerAccount != null)
            {
                // Populate the DTO with the fetched data
                var entry = new WireTransferBatchEntryDTO
                {
                    WireTranferCustomerAccountFullName = creditCustomerAccount.CustomerFullName,
                    CustomerAccountCustomerReference1 = creditCustomerAccount.CustomerReference1,
                    CustomerAccountCustomerReference2 = creditCustomerAccount.CustomerReference2,
                    CustomerAccountCustomerReference3 = creditCustomerAccount.CustomerReference3,
                    WireTransferAccountIdentificationNumber = creditCustomerAccount.CustomerIndividualIdentityCardNumber,
                    WiretransferCustomerAccountFullAccountNumber = creditCustomerAccount.FullAccountNumber,
                    ProductDescription = creditCustomerAccount.CustomerAccountTypeProductCodeDescription,
                    CustomerAccountId = creditCustomerAccount.Id,
                    WireTransferAccountStatusDescription = creditCustomerAccount.StatusDescription,
                    CustomerAccountCustomerIndividualPayrollNumbers = creditCustomerAccount.CustomerIndividualPayrollNumbers
                };

                wireTransferBatchDTO.WireTransferEntries.Add(entry);

                // Return the populated DTO fields as JSON
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        entry.WireTranferCustomerAccountFullName,
                        entry.CustomerAccountCustomerReference1,
                        entry.CustomerAccountCustomerReference2,
                        entry.CustomerAccountCustomerReference3,
                        entry.WireTransferAccountIdentificationNumber,
                        entry.WiretransferCustomerAccountFullAccountNumber,
                        entry.ProductDescription,
                        entry.CustomerAccountId,
                        entry.WireTransferAccountStatusDescription,
                        entry.CustomerAccountCustomerIndividualPayrollNumbers
                    }
                });
            }
            else
            {
                return Json(new { success = false, message = "Customer account not found." });
            }
        }



       


        [HttpPost]
        public async Task<JsonResult> Add(WireTransferBatchDTO wireTransferBatchDTO)
        {
            await ServeNavigationMenus();
            var wireTransferEntryDTOs = Session["WireTransferEntryDTOs"] as ObservableCollection<WireTransferBatchEntryDTO>
                ?? new ObservableCollection<WireTransferBatchEntryDTO>();

            decimal sumAmount = wireTransferEntryDTOs.Sum(cs => cs.Amount);

            foreach (var entry in wireTransferBatchDTO.WireTransferEntries)
            {
                // Check if the account already exists
                var existingEntry = wireTransferEntryDTOs.FirstOrDefault(e =>
                    e.WiretransferCustomerAccountFullAccountNumber == entry.WiretransferCustomerAccountFullAccountNumber);

                if (existingEntry != null)
                {
                    // If the account already exists, return a failure message
                    return Json(new { success = false, message = $"Account {entry.WiretransferCustomerAccountFullAccountNumber} already exists." });
                }

                // Assign a new Guid if the entry doesn't have one
                if (entry.Id == Guid.Empty)
                {
                    entry.Id = Guid.NewGuid();
                }

                // Add the entry
                wireTransferEntryDTOs.Add(entry);

                // Update sum amount and check if it exceeds the total value
                sumAmount += entry.Amount;
                if (sumAmount > wireTransferBatchDTO.TotalValue)
                {
                    wireTransferEntryDTOs.Remove(entry);
                    return Json(new { success = false, message = "Failed to add Wire Transfer Entry. Total Amount exceeded Total Value." });
                }
            }

            // Store entries and DTO back in TempData and Session
            TempData["WireTransferEntryDTOs"] = wireTransferEntryDTOs;
            Session["WireTransferEntryDTOs"] = wireTransferEntryDTOs;
            Session["wireTransferBatchDTO"] = wireTransferBatchDTO;

            return Json(new { success = true, entries = wireTransferEntryDTOs });
        }


        [HttpPost]
        public JsonResult Remove(Guid id)
        {
            var wireTransferEntryDTOs = Session["WireTransferEntryDTOs"] as ObservableCollection<WireTransferBatchEntryDTO>;
            decimal sumAmount = wireTransferEntryDTOs.Sum(cs => cs.Amount);
            if (wireTransferEntryDTOs != null)
            {
                var entryToRemove = wireTransferEntryDTOs.FirstOrDefault(e => e.Id == id);
                if (entryToRemove != null)
                {
                    wireTransferEntryDTOs.Remove(entryToRemove);
                    sumAmount -= entryToRemove.Amount;
                }
            }

            TempData["WireTransferEntryDTOs"] = wireTransferEntryDTOs;
            Session["WireTransferEntryDTOs"] = wireTransferEntryDTOs;
           

            return Json(new { success = true , entries = wireTransferEntryDTOs });
        }



        [HttpPost]
        public JsonResult CheckSumAmount()
        {
            var wireTransferBatchDTO = Session["WireTransferBatchDTO"] as WireTransferBatchDTO;
            var wireTransferEntryDTOs = Session["WireTransferEntryDTOs"] as ObservableCollection<WireTransferBatchEntryDTO> ?? new ObservableCollection<WireTransferBatchEntryDTO>();

            decimal sumAmount = wireTransferEntryDTOs.Sum(e => e.Amount);
            decimal totalValue = wireTransferBatchDTO?.TotalValue ?? 0;

            if (sumAmount != totalValue)
            {
                var balance = totalValue - sumAmount;
                return Json(new { success = false, message = $"The total value ({totalValue}) should be equal to the sum of the entries ({sumAmount}). Balance: {balance}" });
            }

            return Json(new { success = true });
        }


        [HttpPost]
        public async Task<ActionResult> Create(WireTransferBatchDTO wireTransferBatchDTO)
        {
            wireTransferBatchDTO = Session["WireTransferBatchDTO"] as WireTransferBatchDTO;

            if (wireTransferBatchDTO == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Batch cannot be null."
                });
            }

            var WireTransferEntryDTOs = Session["WireTransferEntryDTOs"] as ObservableCollection<WireTransferBatchEntryDTO>;

            if (Session["WireTransferEntryDTOs"] != null)
            {
                wireTransferBatchDTO.WireTransferEntries = WireTransferEntryDTOs;
            }

            wireTransferBatchDTO.ValidateAll();

            if (!wireTransferBatchDTO.HasErrors)
            {
                var wireTransferBatch = await _channelService.AddWireTransferBatchAsync(wireTransferBatchDTO, GetServiceHeader());

                foreach (var wireTransferBatchEntry in WireTransferEntryDTOs)
                {
                    wireTransferBatchEntry.WireTransferBatchId = wireTransferBatch.Id;
                    await _channelService.AddWireTransferBatchEntryAsync(wireTransferBatchEntry, GetServiceHeader());
                }

                Session["WireTransferEntryDTOs"] = null;
                Session["WireTransferBatchDTO"] = null;

                return Json(new
                {
                    success = true,
                    message = "Successfully created WireTransfer Batch."
                });
            }
            else
            {
                var errorMessages = string.Join("\n", wireTransferBatchDTO.ErrorMessages);
                return Json(new
                {
                    success = false,
                    message = errorMessages
                });
            }
        }




        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.BatchType = GetWireTransferBatchTypeSelectList(string.Empty);
            ViewBag.Priority = GetQueuePriorityAsync(string.Empty);

            var debitBatchDTO = await _channelService.FindDebitBatchAsync(id, GetServiceHeader());

            return View(debitBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, WireTransferBatchDTO wireTransferBatchDTO)
        {
            wireTransferBatchDTO.ValidateAll();

            if (!wireTransferBatchDTO.HasErrors)
            {
                await _channelService.UpdateWireTransferBatchAsync(wireTransferBatchDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = wireTransferBatchDTO.ErrorMessages;

                ViewBag.BatchType = GetWireTransferBatchTypeSelectList(wireTransferBatchDTO.Priority.ToString());
                ViewBag.Priority = GetQueuePriorityAsync(wireTransferBatchDTO.Priority.ToString());

                return View(wireTransferBatchDTO);
            }
        }

        public async Task<ActionResult> Verify(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.BatchType = GetWireTransferBatchTypeSelectList(string.Empty);
            ViewBag.Priority = GetQueuePriorityAsync(string.Empty);
            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);
            var wireTransferBatchDTO = await _channelService.FindWireTransferBatchAsync(id, GetServiceHeader());

            TempData["wireTransferBatchDTO"] = wireTransferBatchDTO;

            var wireTransferEntries = await _channelService.FindWireTransferBatchEntriesByWireTransferBatchIdAsync(id, true, GetServiceHeader());


            ViewBag.WireTransferEntryDTOs = wireTransferEntries;


            return View(wireTransferBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(Guid id, WireTransferBatchDTO wireTransferBatchDTO)
        {
            var Auth = wireTransferBatchDTO.BatchAuthOption;
            wireTransferBatchDTO.ValidateAll();

            if (!wireTransferBatchDTO.HasErrors)
            {
                await _channelService.AuditWireTransferBatchAsync(wireTransferBatchDTO, Auth, GetServiceHeader());

                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(wireTransferBatchDTO.BatchAuthOption.ToString());
                ViewBag.BatchType = GetWireTransferBatchTypeSelectList(wireTransferBatchDTO.WireTransferTypeDescription.ToString());
                ViewBag.Priority = GetQueuePriorityAsync(wireTransferBatchDTO.Priority.ToString());

                TempData["VerifySuccess"] = "Verification Successful";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = wireTransferBatchDTO.ErrorMessages;
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(wireTransferBatchDTO.BatchAuthOption.ToString());
                ViewBag.BatchType = GetWireTransferBatchTypeSelectList(wireTransferBatchDTO.Priority.ToString());
                ViewBag.Priority = GetQueuePriorityAsync(wireTransferBatchDTO.Priority.ToString());

                return View(wireTransferBatchDTO);
            }
        }



        public async Task<ActionResult> Authorize(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.BatchType = GetWireTransferBatchTypeSelectList(string.Empty);
            ViewBag.Priority = GetQueuePriorityAsync(string.Empty);
            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);

            var wireTranferBatchDTO = await _channelService.FindWireTransferBatchAsync(id, GetServiceHeader());
            var wireTransferEntries = await _channelService.FindWireTransferBatchEntriesByWireTransferBatchIdAsync(id, true, GetServiceHeader());


            ViewBag.WireTransferEntryDTOs = wireTransferEntries;


            return View(wireTranferBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Authorize(Guid id, WireTransferBatchDTO wireTransferBatchDTO)
        {
            var Auth = wireTransferBatchDTO.BatchAuthOption;
            wireTransferBatchDTO.ValidateAll();



            if (!wireTransferBatchDTO.HasErrors)
            {
                await _channelService.AuthorizeWireTransferBatchAsync(wireTransferBatchDTO, Auth, 1, GetServiceHeader());

                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(wireTransferBatchDTO.BatchAuthOption.ToString());
                ViewBag.BatchType = GetWireTransferBatchTypeSelectList(wireTransferBatchDTO.Priority.ToString());
                ViewBag.Priority = GetQueuePriorityAsync(wireTransferBatchDTO.Priority.ToString());

                TempData["Authorize"] = "Authorization Successful";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = wireTransferBatchDTO.ErrorMessages;
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(wireTransferBatchDTO.BatchAuthOption.ToString());
                ViewBag.BatchType = GetWireTransferBatchTypeSelectList(wireTransferBatchDTO.Priority.ToString());
                ViewBag.Priority = GetQueuePriorityAsync(wireTransferBatchDTO.Priority.ToString());

                return View(wireTransferBatchDTO);
            }
        }



        /*[HttpGet]
        public async Task<JsonResult> GetWireTransferBatchesAsync()
        {
            var debitBatchDTOs = await _channelService.FindDebitBatchesAsync(GetServiceHeader());

            return Json(debitBatchDTOs, JsonRequestBehavior.AllowGet);
        }*/
    }

}

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


        public void BatchOrigination_WireTransfer(WireTransferBatchDTO  wireTransferBatchDTO)
        {

            Session["HeaderDetails"] = wireTransferBatchDTO;

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


        /* public async Task<ActionResult> WireTransferTypeLookup(Guid? id, WireTransferBatchDTO wireTransferBatchDTO)
         {
             await ServeNavigationMenus();
             // Check if the id is null or an empty Guid
             if (id == null || id == Guid.Empty)
             {
                 await ServeNavigationMenus();
                 return View("Create", wireTransferBatchDTO);
             }

             // Fetch all wire transfer types and filter by the provided id
             var wireTransferTypes = await _channelService.FindWireTransferTypesAsync(GetServiceHeader());
             var wireTransferType = wireTransferTypes.FirstOrDefault(wt => wt.Id == id);

             // If the wire transfer type with the specified id is not found, you can handle it accordingly
             if (wireTransferType == null)
             {

                 return View("Create", wireTransferBatchDTO);
             }

             wireTransferBatchDTO.WireTransferTypeDescription = wireTransferType.Description;
             wireTransferBatchDTO.WireTransferTypeId = wireTransferType.Id;

             ViewBag.BatchType = GetWireTransferBatchTypeSelectList(string.Empty);
             ViewBag.Priority = GetQueuePriorityAsync(string.Empty);

             return View("Create", wireTransferBatchDTO);
         }*/





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



        /*[HttpPost]
        public async Task<ActionResult> Add(WireTransferBatchDTO wireTransferBatchDTO)
        {

            await ServeNavigationMenus();
            ViewBag.BatchType = GetWireTransferBatchTypeSelectList(string.Empty);
            ViewBag.Priority = GetQueuePriorityAsync(string.Empty);


           

            WireTransferEntryDTOs = TempData["WireTransferEntryDTOs"] as ObservableCollection<WireTransferBatchEntryDTO>;


            ViewBag.WireTransferEntryDTOs = WireTransferEntryDTOs;



            if (WireTransferEntryDTOs == null)
            {
                WireTransferEntryDTOs = new ObservableCollection<WireTransferBatchEntryDTO>();
            }



            foreach (var wireTransferBatchEntry in wireTransferBatchDTO.WireTransferEntries)
            {
                WireTransferEntryDTOs.Add(wireTransferBatchEntry);

                Session["WireTransferEntries"] = WireTransferEntryDTOs;

            };


            wireTransferBatchDTO.WireTransferEntries = WireTransferEntryDTOs;

            TempData["WireTransferEntryDTOs"] = WireTransferEntryDTOs;
            Session["WireTransferEntryDTOs"] = WireTransferEntryDTOs;
            TempData["WireTransferBatchDTO"] = wireTransferBatchDTO;
            Session["WireTransferBatchDTO"] = wireTransferBatchDTO;

            wireTransferBatchDTO.WireTransferEntries=null;
            Session["debitBatchDTO2"] = null;

            ViewBag.WireTransferEntryDTOs = WireTransferEntryDTOs;

            return View("Create");
        }
*/
        /*
                [HttpPost]
                public JsonResult Add(WireTransferBatchDTO  wireTransferBatchDTO)
                {
                    var wireTransferEntryDTOs = Session["WireTransferEntryDTOs"] as ObservableCollection<WireTransferBatchEntryDTO> ?? new ObservableCollection<WireTransferBatchEntryDTO>();

                    decimal sumAmount = wireTransferEntryDTOs.Sum(cs => cs.Amount);

                    foreach (var entry in wireTransferBatchDTO.WireTransferEntries)
                    {
                        if (entry.Id == Guid.Empty)
                        {
                            entry.Id = Guid.NewGuid();
                        }
                        wireTransferEntryDTOs.Add(entry);

                        sumAmount += entry.Amount;
                        if (sumAmount > wireTransferBatchDTO.TotalValue)
                        {
                            wireTransferEntryDTOs.Remove(entry);
                            return Json(new { success = false, message = "Failed to add Wire Transfer Entry. Total Amount exceeded Total Value." });
                        }

                    }

                    TempData["WireTransferEntryDTOs"] = wireTransferEntryDTOs;
                    Session["WireTransferEntryDTOs"] = wireTransferEntryDTOs;
                    Session["wireTransferBatchDTO"] = wireTransferBatchDTO;

                    return Json(new { success = true, entries = wireTransferEntryDTOs });
                }*/


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

            WireTransferEntryDTOs = Session["WireTransferEntryDTOs"] as ObservableCollection<WireTransferBatchEntryDTO>;



            if (Session["WireTransferEntryDTOs"] != null)
            {
                wireTransferBatchDTO.WireTransferEntries = Session["WireTransferEntryDTOs"] as ObservableCollection<WireTransferBatchEntryDTO>;
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





                //  OverDeductionBatchEntryDTO overDeductionBatch = new OverDeductionBatchEntryDTO();

                Session["WireTransferEntryDTOs"] = null;
                Session["WireTransferBatchDTO"] = null;


                TempData["SuccessMessage"] = "Successfully Created WireTransfer Batch";



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




        





        /*[HttpPost]
        public async Task<ActionResult> Create(WireTransferBatchDTO wireTransferBatchDTO)
        {
            // Retrieve WireTransferBatchDTO and WireTransferEntryDTOs from Session
            wireTransferBatchDTO = Session["WireTransferBatchDTO"] as WireTransferBatchDTO;
            var wireTransferEntryDTOs = Session["WireTransferEntryDTOs"] as ObservableCollection<WireTransferBatchEntryDTO>;

            // Check if there are any entries
            if (wireTransferEntryDTOs != null)
            {
                wireTransferBatchDTO.WireTransferEntries = wireTransferEntryDTOs;

                // Calculate the sum of the amounts in the entries
                decimal sumAmount = wireTransferEntryDTOs.Sum(entry => entry.Amount);

                // Check if the total sum of the entries matches the total value
                if (sumAmount != wireTransferBatchDTO.TotalValue)
                {
                    // Calculate the balance
                    decimal balance = wireTransferBatchDTO.TotalValue - sumAmount;

                    // Return error message and balance to the view
                    ViewBag.BatchType = GetWireTransferBatchTypeSelectList(wireTransferBatchDTO.Priority.ToString());
                    ViewBag.Priority = GetQueuePriorityAsync(wireTransferBatchDTO.Priority.ToString());
                    TempData["validation"]  = $"The total value ({wireTransferBatchDTO.TotalValue}) should be equal to the sum of the entries ({sumAmount}). Balance: {balance}";

                    // Return the current batch DTO back to the view
                    return View("Create",wireTransferBatchDTO);
                }
            }

            // Validate the DTO
            wireTransferBatchDTO.ValidateAll();

            if (!wireTransferBatchDTO.HasErrors)
            {
                // Proceed with creating the batch and entries
                var wireTransferBatch = await _channelService.AddWireTransferBatchAsync(wireTransferBatchDTO, GetServiceHeader());

                foreach (var wireTransferBatchEntry in wireTransferEntryDTOs)
                {
                    wireTransferBatchEntry.WireTransferBatchId = wireTransferBatch.Id;
                    await _channelService.AddWireTransferBatchEntryAsync(wireTransferBatchEntry, GetServiceHeader());
                }

                // Clear the session
                Session["WireTransferEntryDTOs"] = null;
                Session["WireTransferBatchDTO"] = null;

                TempData["SuccessMessage"] = "Successfully Created WireTransfer Batch";
                return RedirectToAction("Index");
            }
            else
            {
                // Handle validation errors
                var errorMessages = wireTransferBatchDTO.ErrorMessages;
                ViewBag.BatchType = GetWireTransferBatchTypeSelectList(wireTransferBatchDTO.Priority.ToString());
                ViewBag.Priority = GetQueuePriorityAsync(wireTransferBatchDTO.Priority.ToString());
                return View(wireTransferBatchDTO);
            }
        }
*/





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

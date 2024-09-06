using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Microsoft.AspNet.Identity;
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
    public class BatchOrigination_DebitController : MasterController
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

            var pageCollectionInfo = await _channelService.FindDebitBatchesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(debitBatch => debitBatch.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<DebitBatchDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }



        public void BatchOrigination_Debit(DebitBatchDTO  debitBatchDTO)
        {

            Session["HeaderDetails"] = debitBatchDTO;

        }




        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var debitBatchDTO = await _channelService.FindDebitBatchAsync(id, GetServiceHeader());
            var debitEntries = await _channelService.FindDebitBatchEntriesByDebitBatchIdAsync(id,true, GetServiceHeader());

            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);
            ViewBag.QueuePrioritySelectList = GetQueuePrioritySelectList(string.Empty);
            ViewBag.DebitBatchEntryDTOs = debitEntries;
            return View(debitBatchDTO);
        }
        public async Task<ActionResult> Create(Guid? id)
        {
            ViewBag.QueuePrioritySelectList = GetQueuePrioritySelectList(string.Empty);
            await ServeNavigationMenus();
            
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> GetDebitTypeDetails(Guid id)
        {
            await ServeNavigationMenus();
            var customer = await _channelService.FindDebitTypeAsync(id, GetServiceHeader());

            if (customer == null)
            {
                return Json(new { success = false, message = "Debit type not found" });
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    DebitTypeDescription = customer.Description,
                    DebitTypeId = customer.Id
                }
            });
        }




        [HttpPost]
        public async Task<JsonResult> DebitCustomerAccountLookUp(Guid id)
        {
            await ServeNavigationMenus();
            var debitcustomerAccount = await _channelService.FindCustomerAccountAsync(id, true, true, true, false, GetServiceHeader());

            if (debitcustomerAccount == null)
            {
                return Json(new { success = false, message = "Customer account not found" });
            }

            var result = new
            {
                CustomerFullName = debitcustomerAccount.CustomerFullName,
                FullAccountNumber = debitcustomerAccount.FullAccountNumber,
                CustomerReference2 = debitcustomerAccount.CustomerReference2,
                CustomerReference3 = debitcustomerAccount.CustomerReference3,
                IdentificationNumber = debitcustomerAccount.CustomerIndividualIdentityCardNumber,
                StatusDescription = debitcustomerAccount.StatusDescription,
                Status = debitcustomerAccount.Status,
                Remarks = debitcustomerAccount.Remarks,
                ProductDescription = debitcustomerAccount.CustomerAccountTypeProductCodeDescription,
                CustomerId = debitcustomerAccount.Id,
                TypeDescription = debitcustomerAccount.TypeDescription,
                IndividualPayrollNumbers = debitcustomerAccount.CustomerIndividualPayrollNumbers
            };

            return Json(result);
        }


        /*[HttpPost]
        public async Task<ActionResult> Add(DebitBatchDTO debitBatchDTO)
        {
            await ServeNavigationMenus();
            
            if(debitBatchDTO.DebitBatchEntries == null)
            {
                TempData["EntryRequired"] = "Please fill the entries dat";
                return View(debitBatchDTO);
            }
            // Retrieve or initialize the collection of debit batch entries from the session
            var debitBatchEntryDTOs = Session["DebitBatchEntryDTO"] as ObservableCollection<DebitBatchEntryDTO>;
            if (debitBatchEntryDTOs == null)
            {
                debitBatchEntryDTOs = new ObservableCollection<DebitBatchEntryDTO>();
            }

            // Add new entries to the collection
            foreach (var debitBatchEntryDTO in debitBatchDTO.DebitBatchEntries)
            {
                debitBatchEntryDTOs.Add(debitBatchEntryDTO);
            }

            // Update session values
            Session["DebitBatchEntryDTO"] = debitBatchEntryDTOs;
            Session["DebitBatchDTO"] = debitBatchDTO;

            // Return JSON response with the updated entries
            return Json(new { success = true, entries = debitBatchEntryDTOs });
        }*/

        [HttpPost]
        public async Task<ActionResult> Add(DebitBatchDTO debitBatchDTO)
        {
            await ServeNavigationMenus();

            // Retrieve or initialize the collection of debit batch entries from the session
            var debitBatchEntryDTOs = Session["DebitBatchEntryDTO"] as ObservableCollection<DebitBatchEntryDTO>;
            if (debitBatchEntryDTOs == null)
            {
                debitBatchEntryDTOs = new ObservableCollection<DebitBatchEntryDTO>();
            }

            // Loop through the new entries
            foreach (var newEntry in debitBatchDTO.DebitBatchEntries)
            {
                // Check if the DebitCustomerAccountFullAccountNumber already exists in the session entries
                var existingEntry = debitBatchEntryDTOs.FirstOrDefault(e => e.DebitCustomerAccountFullAccountNumber == newEntry.DebitCustomerAccountFullAccountNumber);

                if (existingEntry != null)
                {
                    // If found, return a message indicating the account already exists
                    return Json(new
                    {
                        success = false,
                        message = $"A debit entry with account number {newEntry.DebitCustomerAccountFullAccountNumber} already exists."
                    });
                }

                if (newEntry.Id == Guid.Empty)
                {
                    newEntry.Id = Guid.NewGuid();
                }

                // Add new entries to the collection if not already present
                debitBatchEntryDTOs.Add(newEntry);
            }

            // Update session values
            Session["DebitBatchEntryDTO"] = debitBatchEntryDTOs;
            Session["DebitBatchDTO"] = debitBatchDTO;

            // Return JSON response with the updated entries
            return Json(new { success = true, entries = debitBatchEntryDTOs });
        }


        [HttpPost]
        public async Task<JsonResult> Remove(Guid id)
        {
            await ServeNavigationMenus();

            var debitBatchEntryDTOs = Session["DebitBatchEntryDTO"] as ObservableCollection<DebitBatchEntryDTO>;

            if (debitBatchEntryDTOs != null)
            {
                var entryToRemove = debitBatchEntryDTOs.FirstOrDefault(e => e.Id == id);
                if (entryToRemove != null)
                {
                    debitBatchEntryDTOs.Remove(entryToRemove);

                    

                    Session["DebitBatchEntryDTO"] = debitBatchEntryDTOs;
                }
            }



            return Json(new { success = true, data = debitBatchEntryDTOs });
        }



        [HttpPost]
        public async Task<ActionResult> Create(DebitBatchDTO debitBatchDTO)
        {
            ViewBag.QueuePrioritySelectList = GetQueuePrioritySelectList(debitBatchDTO.Priority.ToString());

            debitBatchDTO = Session["DebitBatchDTO"] as DebitBatchDTO;

            DebitBatchEntryDTOs = Session["DebitBatchEntryDTO"] as ObservableCollection<DebitBatchEntryDTO>;


            if (Session["DebitBatchEntryDTO"] != null)
            {
                debitBatchDTO.DebitBatchEntries = Session["DebitBatchEntryDTO"] as ObservableCollection<DebitBatchEntryDTO>;
            }

            debitBatchDTO.ValidateAll();

            if (!debitBatchDTO.HasErrors)
            {
                var debitBatch = await _channelService.AddDebitBatchAsync(debitBatchDTO, GetServiceHeader());

                foreach (var debitBatchEntry in DebitBatchEntryDTOs)
                {
                    debitBatchEntry.DebitBatchId =debitBatch.Id;
                    await _channelService.AddDebitBatchEntryAsync(debitBatchEntry, GetServiceHeader());
                }
            




            //  OverDeductionBatchEntryDTO overDeductionBatch = new OverDeductionBatchEntryDTO();




                TempData["SuccessMessage"] = "Successfully Created Debit Batch";
                TempData["OverDeductionBatchDTO"] = "";
                Session["DebitBatchEntryDTO"] = null;
                Session["DebitBatchDTO"] = null;

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = debitBatchDTO.ErrorMessages;

                return View(debitBatchDTO);
            }
        }



        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.QueuePrioritySelectList = GetQueuePrioritySelectList(string.Empty);
            var debitBatchDTO = await _channelService.FindDebitBatchAsync(id, GetServiceHeader());

            return View(debitBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, DebitBatchDTO debitBatchDTO)
        {
            debitBatchDTO.ValidateAll();

            if (!debitBatchDTO.HasErrors)
            {
                await _channelService.UpdateDebitBatchAsync(debitBatchDTO, GetServiceHeader());

                TempData["edit"] = "Successfully edited ";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = debitBatchDTO.ErrorMessages;
                ViewBag.QueuePrioritySelectList = GetQueuePrioritySelectList(debitBatchDTO.Priority.ToString());
                return View(debitBatchDTO);
            }
        }

        public async Task<ActionResult> Verify(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);
            ViewBag.QueuePrioritySelectList = GetQueuePrioritySelectList(string.Empty);
            /*ViewBag.DebitBatchTypeSelectList = GetDebitBatchesAsync(string.Empty);*/

            var debitBatchDTO = await _channelService.FindDebitBatchAsync(id, GetServiceHeader());

            TempData["debitBatchDTO"] = debitBatchDTO;
            var debitEntries = await _channelService.FindDebitBatchEntriesByDebitBatchIdAsync(id, true, GetServiceHeader());

            
            ViewBag.DebitBatchEntryDTOs = debitEntries;



            return View(debitBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(Guid id, DebitBatchDTO debitBatchDTO)
        {
            TempData["Auth"] = debitBatchDTO.BatchAuthOption;
            debitBatchDTO = TempData["debitBatchDTO"] as DebitBatchDTO;

            debitBatchDTO.BatchAuthOption = (Byte)TempData["Auth"];
            /*var userDTO = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
            debitBatchDTO.BranchId = (Guid)userDTO.BranchId;*/

            ViewBag.QueuePrioritySelectList = GetQueuePrioritySelectList(debitBatchDTO.Priority.ToString());
            var batchAuthOption = debitBatchDTO.BatchAuthOption;
            debitBatchDTO.ValidateAll();

            if (!debitBatchDTO.HasErrors)
            {
                await _channelService.AuditDebitBatchAsync(debitBatchDTO, batchAuthOption, GetServiceHeader());
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(debitBatchDTO.BatchAuthOption.ToString());
                

                TempData["verify"] = "Successfully verified Debit Batch";
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = debitBatchDTO.ErrorMessages;
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(debitBatchDTO.BatchAuthOption.ToString());
                return View(debitBatchDTO);
            }
        }

        public async Task<ActionResult> Authorize(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);

            var debitBatchDTO = await _channelService.FindDebitBatchAsync(id, GetServiceHeader());

            var debitBatches = await _channelService.FindDebitBatchEntriesByDebitBatchIdAsync(id,true, GetServiceHeader());

            ViewBag.DebitBatchEntryDTOs = debitBatches;

            return View(debitBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Authorize(Guid id, DebitBatchDTO debitBatchDTO)
        {
            var batchAuthOption = debitBatchDTO.BatchAuthOption;
            debitBatchDTO.ValidateAll();

            if (!debitBatchDTO.HasErrors)
            {
                await _channelService.AuthorizeDebitBatchAsync(debitBatchDTO, 1, batchAuthOption, GetServiceHeader());
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(debitBatchDTO.BatchAuthOption.ToString());
                ViewBag.QueuePrioritySelectList = GetQueuePrioritySelectList(debitBatchDTO.Priority.ToString());
                TempData["AuthorizeSuccess"] = "Authorization Successiful";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = debitBatchDTO.ErrorMessages;
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(debitBatchDTO.BatchAuthOption.ToString());
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

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

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var debitBatchDTO = await _channelService.FindDebitBatchAsync(id, GetServiceHeader());
            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);
            ViewBag.QueuePrioritySelectList = GetQueuePrioritySelectList(string.Empty);

            return View(debitBatchDTO);
        }
        public async Task<ActionResult> Create(Guid? id)
        {
            ViewBag.QueuePrioritySelectList = GetQueuePrioritySelectList(string.Empty);
            await ServeNavigationMenus();
            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindDebitTypeAsync(parseId, GetServiceHeader());

            DebitBatchDTO customerAccountDTO = new DebitBatchDTO();

            if (customer != null)
            {

                customerAccountDTO.DebitTypeDescription = customer.Description;
                customerAccountDTO.DebitTypeId = customer.Id;
            }

            return View(customerAccountDTO);
        }



        public async Task<ActionResult> DebitCustomerAccountLookUp(Guid? id, DebitBatchDTO  debitBatchDTO)
        {

            ViewBag.QueuePrioritySelectList = GetQueuePrioritySelectList(debitBatchDTO.Priority.ToString());

            Guid parseId;
            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                await ServeNavigationMenus();

                return View("Create", debitBatchDTO);
            }

            
            

            if (Session["debitBatchDTO1"] != null)
            {
                debitBatchDTO = Session["debitBatchDTO1"] as DebitBatchDTO;
            }

            DebitBatchEntryDTOs = Session["DebitBatchEntryDTO"] as ObservableCollection<DebitBatchEntryDTO>;


            ViewBag.DebitBatchEntryDTOs = DebitBatchEntryDTOs;

            if (debitBatchDTO != null && debitBatchDTO.DebitBatchEntries == null)
            {
                debitBatchDTO.DebitBatchEntries = new ObservableCollection<DebitBatchEntryDTO>();
            }

            // Ensure at least one entry exists before trying to access it by index
            if (debitBatchDTO.DebitBatchEntries.Count == 0)
            {
                debitBatchDTO.DebitBatchEntries.Add(new DebitBatchEntryDTO());
            }

            var debitcustomerAccount = await _channelService.FindCustomerAccountAsync(parseId, true, true, true, false, GetServiceHeader());

            if (debitcustomerAccount != null)
            {
                debitBatchDTO.DebitBatchEntries[0].DebitCustomerAccountFullName = debitcustomerAccount.CustomerFullName;
                debitBatchDTO.DebitBatchEntries[0].DebitCustomerAccountFullAccountNumber = debitcustomerAccount.FullAccountNumber;
                debitBatchDTO.DebitBatchEntries[0].CustomerAccountCustomerReference2 = debitcustomerAccount.CustomerReference2;
                debitBatchDTO.DebitBatchEntries[0].CustomerAccountCustomerReference3 = debitcustomerAccount.CustomerReference3;
                debitBatchDTO.DebitBatchEntries[0].DebitCustomerAccountIdentificationNumber = debitcustomerAccount.CustomerIndividualIdentityCardNumber;
                debitBatchDTO.DebitBatchEntries[0].DebitCustomerAccountStatusDescription = debitcustomerAccount.StatusDescription;
                debitBatchDTO.DebitBatchEntries[0].DebitCustomerAccountRemarks = debitcustomerAccount.Remarks;
                debitBatchDTO.DebitBatchEntries[0].ProductDescription = debitcustomerAccount.CustomerAccountTypeProductCodeDescription;
                debitBatchDTO.DebitBatchEntries[0].CustomerAccountCustomerId = debitcustomerAccount.Id;
                debitBatchDTO.DebitBatchEntries[0].DebitCustomerAccountTypeDescription = debitcustomerAccount.TypeDescription;
                debitBatchDTO.DebitBatchEntries[0].CustomerAccountCustomerIndividualPayrollNumbers = debitcustomerAccount.CustomerIndividualPayrollNumbers;



            }

            Session["creditBatchDTO2"] = debitBatchDTO;

            return View("Create", debitBatchDTO);
        }


        [HttpPost]
        public async Task<ActionResult> AddBatch(DebitBatchDTO  debitBatchDTO)
        {
            await ServeNavigationMenus();

            ViewBag.QueuePrioritySelectList = GetQueuePrioritySelectList(debitBatchDTO.Priority.ToString());

            Session["debitBatchDTO1"] = debitBatchDTO;

            TempData["BatchSuccess"] = "Partial Batch Saved Successifully";


            return View("Create");
        }




        [HttpPost]
        public async Task<ActionResult> Add(DebitBatchDTO  debitBatchDTO)
        {

            await ServeNavigationMenus();
            ViewBag.QueuePrioritySelectList = GetQueuePrioritySelectList(debitBatchDTO.Priority.ToString());

            DebitBatchEntryDTOs = Session["DebitBatchEntryDTO"] as ObservableCollection<DebitBatchEntryDTO>;



            ViewBag.DebitBatchEntryDTOs = DebitBatchEntryDTOs;



            if (DebitBatchEntryDTOs == null)
            {
                DebitBatchEntryDTOs = new ObservableCollection<DebitBatchEntryDTO>();
            }



            foreach (var debitBatchEntry in debitBatchDTO.DebitBatchEntries)
            {
                debitBatchEntry.DebitCustomerAccountFullName = debitBatchEntry.DebitCustomerAccountFullName;
                debitBatchEntry.DebitCustomerAccountFullAccountNumber = debitBatchEntry.DebitCustomerAccountFullAccountNumber;
                debitBatchEntry.CustomerAccountCustomerReference2 = debitBatchEntry.CustomerAccountCustomerReference2;
                debitBatchEntry.CustomerAccountCustomerReference3 = debitBatchEntry.CustomerAccountCustomerReference3;
                debitBatchEntry.DebitCustomerAccountIdentificationNumber = debitBatchEntry.DebitCustomerAccountIdentificationNumber;
                debitBatchEntry.DebitCustomerAccountStatusDescription = debitBatchEntry.DebitCustomerAccountStatusDescription;
                debitBatchEntry.DebitCustomerAccountRemarks = debitBatchEntry.DebitCustomerAccountRemarks;
                debitBatchEntry.ProductDescription = debitBatchEntry.ProductDescription;
                debitBatchEntry.CustomerAccountCustomerId = debitBatchEntry.CustomerAccountCustomerId;
                debitBatchEntry.DebitCustomerAccountTypeDescription = debitBatchEntry.DebitCustomerAccountTypeDescription;
                debitBatchEntry.CustomerAccountCustomerIndividualPayrollNumbers = debitBatchEntry.CustomerAccountCustomerIndividualPayrollNumbers;
                debitBatchEntry.Multiplier = debitBatchEntry.Multiplier;
                debitBatchEntry.ProductDescription = debitBatchEntry.ProductDescription;
                debitBatchEntry.DebitCustomerAccountStatusDescription = debitBatchEntry.StatusDescription;
                debitBatchEntry.Reference = debitBatchEntry.Reference;

                DebitBatchEntryDTOs.Add(debitBatchEntry);




                Session["debitBatchEntries"] = DebitBatchEntryDTOs;

            };
            

            debitBatchDTO.DebitBatchEntries = DebitBatchEntryDTOs;

            



            ViewBag.DebitBatchEntryDTOs = DebitBatchEntryDTOs;

            TempData["DebitBatchEntryDTO"] = DebitBatchEntryDTOs;
            Session["DebitBatchEntryDTO"] = DebitBatchEntryDTOs;
            TempData["DebitBatchDTO"] = debitBatchDTO;
            Session["DebitBatchDTO"] = debitBatchDTO;

            Session["creditBatchDTO2"] = null;



            return View("Create", debitBatchDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(DebitBatchDTO debitBatchDTO)
        {

            debitBatchDTO = Session["DebitBatchDTO"] as DebitBatchDTO;

            DebitBatchEntryDTOs = Session["DebitBatchEntryDTO"] as ObservableCollection<DebitBatchEntryDTO>;


            if (Session["DebitBatchEntryDTO"] != null)
            {
                debitBatchDTO.DebitBatchEntries = Session["DebitBatchEntryDTO"] as ObservableCollection<DebitBatchEntryDTO>;
            }

            debitBatchDTO.ValidateAll();

            if (!debitBatchDTO.HasErrors)
            {


                var debitBatchEntries = new ObservableCollection<DebitBatchEntryDTO>();

                foreach (var debitBatchEntry in debitBatchDTO.DebitBatchEntries)
                {
                    debitBatchEntry.DebitCustomerAccountFullName = debitBatchEntry.DebitCustomerAccountFullName;
                    debitBatchEntry.DebitCustomerAccountFullAccountNumber = debitBatchEntry.DebitCustomerAccountFullAccountNumber;
                    debitBatchEntry.CustomerAccountCustomerReference2 = debitBatchEntry.CustomerAccountCustomerReference2;
                    debitBatchEntry.CustomerAccountCustomerReference3 = debitBatchEntry.CustomerAccountCustomerReference3;
                    debitBatchEntry.DebitCustomerAccountIdentificationNumber = debitBatchEntry.DebitCustomerAccountIdentificationNumber;
                    debitBatchEntry.DebitCustomerAccountStatusDescription = debitBatchEntry.DebitCustomerAccountStatusDescription;
                    debitBatchEntry.DebitCustomerAccountRemarks = debitBatchEntry.DebitCustomerAccountRemarks;
                    debitBatchEntry.ProductDescription = debitBatchEntry.ProductDescription;
                    debitBatchEntry.CustomerAccountCustomerId = debitBatchEntry.CustomerAccountCustomerId;
                    debitBatchEntry.DebitCustomerAccountTypeDescription = debitBatchEntry.DebitCustomerAccountTypeDescription;
                    debitBatchEntry.CustomerAccountCustomerIndividualPayrollNumbers = debitBatchEntry.CustomerAccountCustomerIndividualPayrollNumbers;
                    debitBatchEntry.Multiplier = debitBatchEntry.Multiplier;
                    debitBatchEntry.ProductDescription = debitBatchEntry.ProductDescription;
                    debitBatchEntry.DebitCustomerAccountStatusDescription = debitBatchEntry.StatusDescription;
                    debitBatchEntry.Reference = debitBatchEntry.Reference;


                    
                }

                var debitBatch = await _channelService.AddDebitBatchAsync(debitBatchDTO, GetServiceHeader());

                /*await _channelService.AddOverDeductionBatchEntryAsync(OverDeductionBatchEntries, GetServiceHeader());*/

                

                TempData["SuccessMessage"] = "Successfully Created refund Batch";
                TempData["OverDeductionBatchDTO"] = "";

                
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

                TempData["edit"] = "Successfully edited Loan Purpose";

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

            

            return View(debitBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(Guid id, DebitBatchDTO debitBatchDTO)
        {
            debitBatchDTO.ValidateAll();

            if (!debitBatchDTO.HasErrors)
            {
                await _channelService.AuditDebitBatchAsync(debitBatchDTO, 1, GetServiceHeader());
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(debitBatchDTO.BatchAuthOption.ToString());
                ViewBag.QueuePrioritySelectList = GetQueuePrioritySelectList(debitBatchDTO.Priority.ToString());

                TempData["verify"] = "Successfully verified Loan Purpose";
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

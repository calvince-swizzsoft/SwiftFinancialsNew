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



        public async Task<ActionResult> CreditCustomerAccountLookUp(Guid? id, OverDeductionBatchDTO overDeductionBatchDTO)
        {

            
            Guid parseId;
            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                await ServeNavigationMenus();

                return View("Create", overDeductionBatchDTO);
            }
            if (Session["BatchDTO"] != null)
            {
                overDeductionBatchDTO = Session["BatchDTO"] as OverDeductionBatchDTO;
            }

            OverDeductionBatchEntryDTOs = Session["OverDeductionBatchEntryDTO"] as ObservableCollection<OverDeductionBatchEntryDTO>;


            ViewBag.OverDeductionBatchEntryDTOs = OverDeductionBatchEntryDTOs;

            if (overDeductionBatchDTO != null && overDeductionBatchDTO.overDeductionBatchEntries == null)
            {
                overDeductionBatchDTO.overDeductionBatchEntries = new ObservableCollection<OverDeductionBatchEntryDTO>();
            }

            // Ensure at least one entry exists before trying to access it by index
            if (overDeductionBatchDTO.overDeductionBatchEntries.Count == 0)
            {
                overDeductionBatchDTO.overDeductionBatchEntries.Add(new OverDeductionBatchEntryDTO());
            }

            var creditcustomerAccount = await _channelService.FindCustomerAccountAsync(parseId, true, true, true, false, GetServiceHeader());

            if (creditcustomerAccount != null)
            {
                overDeductionBatchDTO.overDeductionBatchEntries[0].CreditCustomerAccountFullAccountNumber = creditcustomerAccount.FullAccountNumber;
                overDeductionBatchDTO.overDeductionBatchEntries[0].CreditCustomerAccountId = creditcustomerAccount.Id;
                overDeductionBatchDTO.overDeductionBatchEntries[0].CreditProductDescription = creditcustomerAccount.CustomerAccountTypeProductCodeDescription;
                overDeductionBatchDTO.overDeductionBatchEntries[0].CreditCustomerAccountFullName = creditcustomerAccount.CustomerFullName;

                Session["CreditCustomerAccountFullAccountNumber"] = overDeductionBatchDTO.overDeductionBatchEntries[0].CreditCustomerAccountFullAccountNumber;
                Session["CreditProductDescription"] = overDeductionBatchDTO.overDeductionBatchEntries[0].CreditProductDescription;
                Session["CreditCustomerAccountFullName"] = overDeductionBatchDTO.overDeductionBatchEntries[0].CreditCustomerAccountFullName;
                Session["CreditCustomerAccountId"] = overDeductionBatchDTO.overDeductionBatchEntries[0].CreditCustomerAccountId;
            }

            return View("Create", overDeductionBatchDTO);
        }





        public async Task<ActionResult> DebitCustomerAccountLookup(Guid? Id, OverDeductionBatchDTO overDeductionBatchDTO)
        {

            Guid parseId;
            if (Id == Guid.Empty || !Guid.TryParse(Id.ToString(), out parseId))
            {
                await ServeNavigationMenus();

                return View("Create", overDeductionBatchDTO);
            }

            if (Session["BatchDTO"] != null)
            {
                overDeductionBatchDTO = Session["BatchDTO"] as OverDeductionBatchDTO;
            }

            OverDeductionBatchEntryDTOs = Session["OverDeductionBatchEntryDTO"] as ObservableCollection<OverDeductionBatchEntryDTO>;



            ViewBag.OverDeductionBatchEntryDTOs = OverDeductionBatchEntryDTOs;

            if (overDeductionBatchDTO != null && overDeductionBatchDTO.overDeductionBatchEntry == null)
            {
                overDeductionBatchDTO.overDeductionBatchEntry = new OverDeductionBatchEntryDTO();
            }

            ViewBag.OverDeductionBatchEntryDTOs = OverDeductionBatchEntryDTOs;

            var creditCustomerAccountFullAccountNumber = Session["CreditCustomerAccountFullAccountNumber"] as string;
            var creditProductDescription = Session["CreditProductDescription"] as string;
            var creditCustomerAccountFullName = Session["CreditCustomerAccountFullName"] as string;

            if (creditCustomerAccountFullAccountNumber != null && creditProductDescription != null && creditCustomerAccountFullName != null)
            {
                // Assign the values back to the DTO or use them as needed
                overDeductionBatchDTO.overDeductionBatchEntry.CreditCustomerAccountFullAccountNumber = creditCustomerAccountFullAccountNumber;
                overDeductionBatchDTO.overDeductionBatchEntry.CreditProductDescription = creditProductDescription;
                overDeductionBatchDTO.overDeductionBatchEntry.CreditCustomerAccountFullName = creditCustomerAccountFullName;
            }



            if (overDeductionBatchDTO != null && overDeductionBatchDTO.overDeductionBatchEntries == null)
            {
                overDeductionBatchDTO.overDeductionBatchEntries = new ObservableCollection<OverDeductionBatchEntryDTO>();
            }

            // Ensure at least one entry exists before trying to access it by index
            if (overDeductionBatchDTO.overDeductionBatchEntries.Count == 0)
            {
                overDeductionBatchDTO.overDeductionBatchEntries.Add(new OverDeductionBatchEntryDTO());
            }

            var debitcustomerAccount = await _channelService.FindCustomerAccountAsync(parseId, true, true, true, false, GetServiceHeader());

            if (debitcustomerAccount != null)
            {
                overDeductionBatchDTO.overDeductionBatchEntries[0].DebitCustomerAccountFullAccountNumber = debitcustomerAccount.FullAccountNumber;
                overDeductionBatchDTO.overDeductionBatchEntries[0].DebitCustomerAccountId = debitcustomerAccount.Id;
                overDeductionBatchDTO.overDeductionBatchEntries[0].DebitProductDescription = debitcustomerAccount.CustomerAccountTypeProductCodeDescription;
                overDeductionBatchDTO.overDeductionBatchEntries[0].DebitCustomerAccountFullName = debitcustomerAccount.CustomerFullName;
                overDeductionBatchDTO.overDeductionBatchEntries[0].Principal = overDeductionBatchDTO.overDeductionBatchEntries[0].Principal;
                overDeductionBatchDTO.overDeductionBatchEntries[0].Interest = overDeductionBatchDTO.overDeductionBatchEntries[0].Interest;


                Session["DebitCustomerAccountFullAccountNumber"] = overDeductionBatchDTO.overDeductionBatchEntries[0].DebitCustomerAccountFullAccountNumber;
                Session["DebitProductDescription"] = overDeductionBatchDTO.overDeductionBatchEntries[0].DebitProductDescription;
                Session["DebitCustomerAccountFullName"] = overDeductionBatchDTO.overDeductionBatchEntries[0].DebitCustomerAccountFullName;
                Session["DebitCustomerAccountId"] = overDeductionBatchDTO.overDeductionBatchEntries[0].DebitCustomerAccountId;
            }



            return View("create", overDeductionBatchDTO);
        }





        public async Task<ActionResult> Create()
        {


            await ServeNavigationMenus();


            return View();
        }



        [HttpPost]
        public async Task<ActionResult> Create(OverDeductionBatchDTO overDeductionBatchDTO)
        {

            overDeductionBatchDTO = Session["OverDeductionBatchDTO"] as OverDeductionBatchDTO;

            OverDeductionBatchEntryDTOs = Session["OverDeductionBatchEntryDTO"] as ObservableCollection<OverDeductionBatchEntryDTO>;



            if (Session["OverDeductionBatchEntryDTO"] != null)
            {
                overDeductionBatchDTO.overDeductionBatchEntries = Session["OverDeductionBatchEntryDTO"] as ObservableCollection<OverDeductionBatchEntryDTO>;
            }


            overDeductionBatchDTO.ValidateAll();
            if (overDeductionBatchDTO.ErrorMessageResult != null)
            {
                await ServeNavigationMenus();

                TempData["ErrorMsg"] = overDeductionBatchDTO.ErrorMessageResult;

                return View();
            }



            var refundBatch = await _channelService.AddOverDeductionBatchAsync(overDeductionBatchDTO, GetServiceHeader());
            if (refundBatch.HasErrors)
            {
                await ServeNavigationMenus();

                TempData["ErrorMsg"] = overDeductionBatchDTO.ErrorMessages;

                return View();
            }

            foreach (var overdeductiontBatchEntry in OverDeductionBatchEntryDTOs)
            {
                overdeductiontBatchEntry.OverDeductionBatchId = refundBatch.Id;
                await _channelService.AddOverDeductionBatchEntryAsync(overdeductiontBatchEntry, GetServiceHeader());
            }

            TempData["SuccessMessage"] = "Successfully Created refund Batch";
            TempData["OverDeductionBatchDTO"] = "";

            return RedirectToAction("Index");






        }
            


        


        public async Task<ActionResult> RefundEntries(Guid id)
        {
            await ServeNavigationMenus();

            var k = await _channelService.FindOverDeductionBatchAsync(id, GetServiceHeader());
            return View(k);
        }

        

        [HttpPost]
        public async Task<ActionResult> AddBatch(OverDeductionBatchDTO overDeductionBatchDTO)
        {
            await ServeNavigationMenus();




            var creditCustomerAccountFullAccountNumber = Session["CreditCustomerAccountFullAccountNumber"] as string;
            var creditProductDescription = Session["CreditProductDescription"] as string;
            var creditCustomerAccountFullName = Session["CreditCustomerAccountFullName"] as string;

            


            Session["BatchDTO"] = overDeductionBatchDTO;
            Session["TotalValue"] = overDeductionBatchDTO.TotalValue;
            Session["Reference"] = overDeductionBatchDTO.Reference;
            Session["Branch"] = overDeductionBatchDTO.BranchDescription;
            Session["BranchId"] = overDeductionBatchDTO.BranchId;



            TempData["BatchSuccess"] = "Partial Batch Saved Successifully";


            return View("Create");
        }


        


        [HttpPost]
        public async Task<ActionResult> Add(OverDeductionBatchDTO overDeductionBatchDTO)
        {
            await ServeNavigationMenus();

            if (Session["TotalValue"] != null)
            {
                overDeductionBatchDTO.TotalValue = (decimal)Session["TotalValue"];
            }
            if (Session["Reference"] != null)
            {
                overDeductionBatchDTO.Reference = (string)Session["Reference"];
            }

            if(Session["Branch"] != null)
            {
                overDeductionBatchDTO.BranchDescription = (string)Session["Branch"];
            }
            if (Session["BranchId"] != null)
            {
                overDeductionBatchDTO.BranchId = (Guid)Session["BranchId"];
            }




            OverDeductionBatchEntryDTOs = Session["OverDeductionBatchEntryDTO"] as ObservableCollection<OverDeductionBatchEntryDTO>;

            

            ViewBag.OverDeductionBatchEntryDTOs = OverDeductionBatchEntryDTOs;



            if (OverDeductionBatchEntryDTOs == null)
            {
                OverDeductionBatchEntryDTOs = new ObservableCollection<OverDeductionBatchEntryDTO>();
            }



            foreach (var overDeductionBatchEntryDTO in overDeductionBatchDTO.overDeductionBatchEntries)
            {
                overDeductionBatchEntryDTO.DebitCustomerAccountFullName = overDeductionBatchEntryDTO.DebitCustomerAccountFullName;
                overDeductionBatchEntryDTO.DebitProductDescription = overDeductionBatchEntryDTO.DebitProductDescription;
                overDeductionBatchEntryDTO.DebitCustomerAccountId = overDeductionBatchEntryDTO.DebitCustomerAccountId;
                overDeductionBatchEntryDTO.DebitCustomerAccountFullAccountNumber = overDeductionBatchEntryDTO.DebitCustomerAccountFullAccountNumber;
                overDeductionBatchEntryDTO.CreditCustomerAccountFullAccountNumber = overDeductionBatchEntryDTO.CreditCustomerAccountFullAccountNumber;
                overDeductionBatchEntryDTO.CreditProductDescription = overDeductionBatchEntryDTO.CreditProductDescription;
                overDeductionBatchEntryDTO.CreditCustomerAccountId = overDeductionBatchEntryDTO.CreditCustomerAccountId;
                overDeductionBatchEntryDTO.CreditCustomerAccountFullName = overDeductionBatchEntryDTO.CreditCustomerAccountFullName;
                overDeductionBatchEntryDTO.Interest = overDeductionBatchDTO.overDeductionBatchEntries[0].Interest;
                overDeductionBatchEntryDTO.Principal = overDeductionBatchDTO.overDeductionBatchEntries[0].Principal;


                OverDeductionBatchEntryDTOs.Add(overDeductionBatchEntryDTO);
                



                Session["overDeductionBatchEntries"] = OverDeductionBatchEntryDTOs;

            };

            Session["CreditCustomerAccountFullAccountNumber"] = null;
            Session["CreditProductDescription"] = null;
            Session["CreditCustomerAccountFullName"] = null;



            ViewBag.OverDeductionBatchEntryDTOs = OverDeductionBatchEntryDTOs;

            TempData["OverDeductionBatchEntryDTO"] = OverDeductionBatchEntryDTOs;
            Session["OverDeductionBatchEntryDTO"] = OverDeductionBatchEntryDTOs;
            TempData["OverDeductionBatchDTO"] = overDeductionBatchDTO;
            Session["OverDeductionBatchDTO"] = overDeductionBatchDTO;



            return View("Create",overDeductionBatchDTO);



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
        public async Task<ActionResult> Verify(Guid id, OverDeductionBatchDTO  overDeductionBatchDTO)
        {
            overDeductionBatchDTO.ValidateAll();

            if (!overDeductionBatchDTO.HasErrors)
            {
                await _channelService.AuditOverDeductionBatchAsync(overDeductionBatchDTO, 1, GetServiceHeader());

                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(overDeductionBatchDTO.RefundAuthOption.ToString());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = overDeductionBatchDTO.ErrorMessages;

                return View(overDeductionBatchDTO);
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
        public async Task<ActionResult> Authorize(Guid id, OverDeductionBatchDTO  overDeductionBatchDTO)
        {
            //var batchAuthOption = debitBatchDTO.BatchAuthOption;
            overDeductionBatchDTO.ValidateAll();

            if (!overDeductionBatchDTO.HasErrors)
            {
                await _channelService.AuthorizeOverDeductionBatchAsync(overDeductionBatchDTO, 1, 1, GetServiceHeader());
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(overDeductionBatchDTO.RefundAuthOption.ToString());
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = overDeductionBatchDTO.ErrorMessages;
                //ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(debitBatchDTO.BatchAuthOption.ToString());
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
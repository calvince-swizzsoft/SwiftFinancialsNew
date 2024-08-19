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

            ViewBag.batchEntries = batchentries;

            return View(overDeductionBatchDTO);
        }



        public async Task<ActionResult> CreditCustomerAccountLookUp(Guid? id, OverDeductionBatchDTO overDeductionBatchDTO)
        {

            var TotalValue = overDeductionBatchDTO.TotalValue;
            var Reference = overDeductionBatchDTO.Reference;

            Session["TotalValue"] = TotalValue;
            Session["Reference"] = Reference;

            Guid parseId;
            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                await ServeNavigationMenus();

                return View("Create", overDeductionBatchDTO);
            }

            if (Session["debitCustomerAccount"] != null)
            {
                overDeductionBatchDTO.DebitCustomerAccountDTO = Session["debitCustomerAccount"] as CustomerAccountDTO;
            }
            Session["totalValue"] = overDeductionBatchDTO.TotalValue;
            Session["branchDescription"] = overDeductionBatchDTO.BranchDescription;
            Session["reference"] = overDeductionBatchDTO.Reference;

            TempData["totalValue"] = overDeductionBatchDTO.TotalValue;
            TempData["branchDescription"] = overDeductionBatchDTO.BranchDescription;
            TempData["reference"] = overDeductionBatchDTO.Reference;
            if (Session["totalValue"]!= null)
            {
                overDeductionBatchDTO.TotalValue = (decimal)Session["totalValue"];
            }
            if (Session["branchDescription"] != null)
            {
                overDeductionBatchDTO.Reference = (string)Session["reference"];
            }

            if (Session["reference"] != null)
            {
                overDeductionBatchDTO.Reference = (string)Session["reference"];
            }


            var creditcustomerAccount = await _channelService.FindCustomerAccountAsync(parseId, true, true, true, false, GetServiceHeader());
            if (creditcustomerAccount != null)
            {
               /* overDeductionBatchDTO.overDeductionBatchEntry.CreditCustomerAccountFullAccountNumber = creditcustomerAccount.FullAccountNumber;
                overDeductionBatchDTO.overDeductionBatchEntry.DebitCustomerAccountFullAccountNumber = overDeductionBatchDTO.overDeductionBatchEntry.DebitFullAccountNumber;
                overDeductionBatchDTO.overDeductionBatchEntry.CreditProductDescription = overDeductionBatchDTO.overDeductionBatchEntry.CreditProductDescription;
                overDeductionBatchDTO.overDeductionBatchEntry.DebitProductDescription = overDeductionBatchDTO.overDeductionBatchEntry.DebitProductDescription;
                overDeductionBatchDTO.overDeductionBatchEntry.CreditCustomerAccountFullName = overDeductionBatchDTO.overDeductionBatchEntry.CreditCustomerFullName;

                overDeductionBatchDTO.overDeductionBatchEntry.DebitCustomerAccountFullName = overDeductionBatchDTO.overDeductionBatchEntry.CreditCustomerFullName;*/

                overDeductionBatchDTO.CreditCustomerAccountDTO = creditcustomerAccount as CustomerAccountDTO;
               
                Session["creditCustomerAcct"] = overDeductionBatchDTO.CreditCustomerAccountDTO;
            }

            return View("create", overDeductionBatchDTO);
        }

        public async Task<ActionResult> DebitCustomerAccountLookup(Guid? Id, OverDeductionBatchDTO overDeductionBatchDTO)
        {

            Guid parseId;
            if (Id == Guid.Empty || !Guid.TryParse(Id.ToString(), out parseId))
            {
                await ServeNavigationMenus();

                return View("Create", overDeductionBatchDTO);
            }


            if (Session["creditCustomerAcct"] != null)
            {
                overDeductionBatchDTO.CreditCustomerAccountDTO = Session["creditCustomerAcct"] as CustomerAccountDTO;
            }

            if (Session["OverDeductionBatchDTO"] != null)
            {
                overDeductionBatchDTO = Session["OverDeductionBatchDTO"] as OverDeductionBatchDTO;
            }

            var debitcustomerAccount = await _channelService.FindCustomerAccountAsync(parseId, true, true, true, false, GetServiceHeader());
            if (debitcustomerAccount != null)
            {
                //overDeductionBatchDTO.overDeductionBatchEntry.CreditCustomerAccountId = creditcustomerAccount.Id;
                //overDeductionBatchDTO.overDeductionBatchEntry.CreditCustomerAccountFullAccountNumber = creditcustomerAccount.FullAccountNumber;
                //overDeductionBatchDTO.overDeductionBatchEntry.CreditProductDescription = creditcustomerAccount.CustomerAccountTypeTargetProductDescription;
                //overDeductionBatchDTO.overDeductionBatchEntry.CreditCustomerAccountFullName = creditcustomerAccount.CustomerIndividualSalutationDescription+" "+creditcustomerAccount.CustomerIndividualFirstName+" "+
                //    " "+creditcustomerAccount.CustomerIndividualLastName;

                overDeductionBatchDTO.DebitCustomerAccountDTO = debitcustomerAccount as CustomerAccountDTO;

                Session["debitCustomerAccount"] = overDeductionBatchDTO.DebitCustomerAccountDTO;
            }

            return View("create", overDeductionBatchDTO);
        }





        public async Task<ActionResult> Create(OverDeductionBatchDTO overDeductionBattchDTO)
        {

            await ServeNavigationMenus();

            var overDeductionBatchDTO = new OverDeductionBatchDTO();

            

            return View(overDeductionBatchDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(Guid id,OverDeductionBatchDTO overDeductionBatchDTO) 
        {

            Session["totalValue"] = overDeductionBatchDTO.TotalValue;
            Session["branchDescription"] = overDeductionBatchDTO.BranchDescription;
            Session["reference"] = overDeductionBatchDTO.Reference;

            TempData["totalValue"] = overDeductionBatchDTO.TotalValue;
            TempData["branchDescription"] = overDeductionBatchDTO.BranchDescription;
            TempData["reference"] = overDeductionBatchDTO.Reference;

            overDeductionBatchDTO = TempData["OverDeductionBatchDTO"] as OverDeductionBatchDTO;
            

            Session["OverDeductionBatchDTO"] = overDeductionBatchDTO;

            overDeductionBatchDTO = TempData["OverDeductionBatchDTO"] as OverDeductionBatchDTO;

            OverDeductionBatchEntryDTOs = TempData["OverDeductionBatchEntryDTOs"] as ObservableCollection<OverDeductionBatchEntryDTO>;
            JournalVoucherEntryDTOs = TempData["JournalVoucherEntryDTOs"] as ObservableCollection<JournalVoucherEntryDTO>;


            if (TempData["OverDeductionBatchDTO"] != null)
            {
                overDeductionBatchDTO.overDeductionBatchEntries = TempData["OverDeductionBatchDTO"] as ObservableCollection<OverDeductionBatchEntryDTO>;
            }


            overDeductionBatchDTO.ValidateAll();




            if (!overDeductionBatchDTO.HasErrors)
            {
                var OverDeductionBatchEntries = new ObservableCollection<OverDeductionBatchEntryDTO>();



                foreach (var overDeductionBatchEntryDTO in overDeductionBatchDTO.overDeductionBatchEntries)
                {
                    overDeductionBatchEntryDTO.CreditCustomerAccountId = overDeductionBatchEntryDTO.CreditCustomerAccountId;
                    overDeductionBatchEntryDTO.CreditCustomerAccountBranchId = overDeductionBatchEntryDTO.DebitCustomerAccountBranchId;
                    overDeductionBatchEntryDTO.CreditCustomerAccountCustomerIndividualFirstName = overDeductionBatchEntryDTO.DebitCustomerAccountCustomerIndividualFirstName;
                    overDeductionBatchEntryDTO.DebitCustomerAccountId = overDeductionBatchEntryDTO.CreditCustomerAccountId;
                    overDeductionBatchEntryDTO.DebitCustomerAccountBranchId = overDeductionBatchEntryDTO.CreditCustomerAccountBranchId;
                    overDeductionBatchEntryDTO.DebitCustomerAccountCustomerIndividualFirstName = overDeductionBatchEntryDTO.DebitCustomerAccountCustomerIndividualFirstName;
                    OverDeductionBatchEntries.Add(overDeductionBatchEntryDTO);
                    /*await _channelService.AddOverDeductionBatchEntryAsync(overDeductionBatchEntryDTO, GetServiceHeader());*/
                }


                var overDeductionBatch = await _channelService.AddOverDeductionBatchAsync(overDeductionBatchDTO, GetServiceHeader());

                if (overDeductionBatchDTO.ErrorMessageResult != null)
                {
                    await ServeNavigationMenus();

                    TempData["ErrorMsg"] = overDeductionBatch.ErrorMessageResult;

                    return View();
                }

                TempData["SuccessMessage"] = "Successfully Created refund Batch";
                TempData["OverDeductionBatchDTO"] = "";



                if (OverDeductionBatchEntries.Any())
                    //Update the OverDeductionBatchEntries

                    /* await _channelService.AddOverDeductionBatchEntryAsync( OverDeductionBatchEntries, GetServiceHeader());*/

                    TempData["OverDeductionBatchDTO"] = "";


            }
            return RedirectToAction("Index");


        }


        public async Task<ActionResult> RefundEntries(Guid id)
        {
            await ServeNavigationMenus();

            var k = await _channelService.FindOverDeductionBatchAsync(id, GetServiceHeader());
            return View(k);
        }

        public async Task<ActionResult> Add(Guid id, OverDeductionBatchDTO overDeductionBatchDTO)
        {
            await ServeNavigationMenus();

            /*var totalValue = overDeductionBatchDTO.TotalValue;
            var branchDescription = overDeductionBatchDTO.BranchDescription;
            var reference = overDeductionBatchDTO.Reference;*/

            Session["totalValue"] = overDeductionBatchDTO.TotalValue;
            Session["branchDescription"] = overDeductionBatchDTO.BranchDescription;
            Session["reference"] = overDeductionBatchDTO.Reference;

            TempData["totalValue"] = overDeductionBatchDTO.TotalValue;
            TempData["branchDescription"] = overDeductionBatchDTO.BranchDescription;
            TempData["reference"] = overDeductionBatchDTO.Reference;

            return View(overDeductionBatchDTO);
        }

        public async Task<ActionResult>AddBatch(OverDeductionBatchDTO overDeductionBatchDTO)
        {
            await ServeNavigationMenus();

            

            await _channelService.AddOverDeductionBatchAsync(overDeductionBatchDTO);

            return View("Create");

        }

        [HttpPost]
        public async Task<ActionResult> Add(OverDeductionBatchDTO overDeductionBatchDTO)
        {
            await ServeNavigationMenus();

            OverDeductionBatchEntryDTOs = TempData["OverDeductionBatchEntryDTOs"] as ObservableCollection<OverDeductionBatchEntryDTO>;

            if (OverDeductionBatchEntryDTOs == null)
                OverDeductionBatchEntryDTOs = new ObservableCollection<OverDeductionBatchEntryDTO>();

            Session["debitCustomerAccount"] = overDeductionBatchDTO.DebitCustomerAccountDTO;


            /*int CreditAccountNumber = int.Parse(overDeductionBatchDTO.CreditCustomerAccountDTO.FullAccountNumber);
            int DebitAccountNumber = int.Parse(overDeductionBatchDTO.DebitCustomerAccountDTO.FullAccountNumber);*/

            foreach (var overDeductionBatchEntryDTO in overDeductionBatchDTO.overDeductionBatchEntries)
            {
                overDeductionBatchEntryDTO.CreditCustomerAccountFullName = overDeductionBatchDTO.CreditCustomerAccountDTO.CustomerFullName;
                overDeductionBatchEntryDTO.DebitCustomerAccountCustomerIndividualFirstName = overDeductionBatchDTO.DebitCustomerAccountDTO.CustomerFullName;
                overDeductionBatchEntryDTO.CreditProductDescription = overDeductionBatchDTO.CreditCustomerAccountDTO.CustomerAccountTypeTargetProductDescription;
                overDeductionBatchEntryDTO.DebitProductDescription = overDeductionBatchDTO.DebitCustomerAccountDTO.CustomerAccountTypeTargetProductDescription;
                overDeductionBatchEntryDTO.DebitCustomerAccountCustomerSerialNumber =int.Parse(overDeductionBatchDTO.DebitCustomerAccountDTO.FullAccountNumber);
                overDeductionBatchEntryDTO.DebitCustomerAccountCustomerSerialNumber = int.Parse(overDeductionBatchDTO.DebitCustomerAccountDTO.FullAccountNumber);

                OverDeductionBatchEntryDTOs.Add(overDeductionBatchEntryDTO);

                Session["overDeductionBatchEntries"] = OverDeductionBatchEntryDTOs;

            };



            ViewBag.OverDeductionBatchEntryDTOs = OverDeductionBatchEntryDTOs;

            TempData["OverDeductionBatchEntryDTO"] = OverDeductionBatchEntryDTOs;
            Session["OverDeductionBatchEntryDTO"] = OverDeductionBatchEntryDTOs;
            TempData["OverDeductionBatchDTO"] = overDeductionBatchDTO;



            return View("Create");



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
        public async Task<ActionResult> Verify(Guid id, OverDeductionBatchDTO debitBatchDTO)
        {
            debitBatchDTO.ValidateAll();

            if (!debitBatchDTO.HasErrors)
            {
                await _channelService.AuditOverDeductionBatchAsync(debitBatchDTO, 1, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = debitBatchDTO.ErrorMessages;

                return View(debitBatchDTO);
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
        public async Task<ActionResult> Authorize(Guid id, OverDeductionBatchDTO debitBatchDTO)
        {
            //var batchAuthOption = debitBatchDTO.BatchAuthOption;
            debitBatchDTO.ValidateAll();

            if (!debitBatchDTO.HasErrors)
            {
                await _channelService.AuthorizeOverDeductionBatchAsync(debitBatchDTO, 1, 1, GetServiceHeader());
                //ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(debitBatchDTO.BatchAuthOption.ToString());
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = debitBatchDTO.ErrorMessages;
                //ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(debitBatchDTO.BatchAuthOption.ToString());
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

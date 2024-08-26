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

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindCreditBatchesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(creditBatch => creditBatch.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CreditBatchDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(string.Empty);
            ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);

            var creditBatchDTO = await _channelService.FindCreditBatchAsync(id, GetServiceHeader());

            var creditBatches = await _channelService.FindCreditBatchEntriesByCreditBatchIdAsync(id,true, GetServiceHeader());

            ViewBag.CrdeitBatchEntryDTOs = creditBatches;

            return View(creditBatchDTO);
        }


        public async Task<ActionResult> CreditCustomerAccountLookUp(Guid? id, CreditBatchDTO  creditBatchDTO)
        {


            ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(string.Empty);
            ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);

            Guid parseId;
            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                await ServeNavigationMenus();

                return View("Create", creditBatchDTO);
            }

            

            if (Session["CreditBatchDTO1"] != null)
            {
                creditBatchDTO = Session["CreditBatchDTO1"] as CreditBatchDTO;
            }

            CreditBatchEntryDTOs = Session["CreditBatchEntryDTO"] as ObservableCollection<CreditBatchEntryDTO>;


            ViewBag.CreditBatchEntryDTOs = CreditBatchEntryDTOs;

            if (creditBatchDTO != null && creditBatchDTO.CreditBatchEntries == null)
            {
                creditBatchDTO.CreditBatchEntries = new ObservableCollection<CreditBatchEntryDTO>();
            }

            // Ensure at least one entry exists before trying to access it by index
            if (creditBatchDTO.CreditBatchEntries.Count == 0)
            {
                creditBatchDTO.CreditBatchEntries.Add(new CreditBatchEntryDTO());
            }

            var creditcustomerAccount = await _channelService.FindCustomerAccountAsync(parseId, true, true, true, false, GetServiceHeader());

            if (creditcustomerAccount != null)
            {
                creditBatchDTO.CreditBatchEntries[0].CreditCustomerAccountFullName = creditcustomerAccount.CustomerFullName;
                creditBatchDTO.CreditBatchEntries[0].CreditCustomerAccountFullAccountNumber = creditcustomerAccount.FullAccountNumber;
                creditBatchDTO.CreditBatchEntries[0].CustomerAccountCustomerReference2 = creditcustomerAccount.CustomerReference2;
                creditBatchDTO.CreditBatchEntries[0].CustomerAccountCustomerReference3 = creditcustomerAccount.CustomerReference3;
                creditBatchDTO.CreditBatchEntries[0].CreditCustomerAccountIdentificationNumber= creditcustomerAccount.CustomerIndividualIdentityCardNumber;
                creditBatchDTO.CreditBatchEntries[0].CreditCustomerAccountStatusDescription= creditcustomerAccount.StatusDescription;
                creditBatchDTO.CreditBatchEntries[0].CreditCustomerAccountRemarks= creditcustomerAccount.Remarks;
                creditBatchDTO.CreditBatchEntries[0].ProductDescription= creditcustomerAccount.CustomerAccountTypeProductCodeDescription;
                creditBatchDTO.CreditBatchEntries[0].CustomerAccountCustomerId = creditcustomerAccount.Id;
                creditBatchDTO.CreditBatchEntries[0].CreditCustomerAccountTypeDescription = creditcustomerAccount.TypeDescription;
                creditBatchDTO.CreditBatchEntries[0].CustomerAccountCustomerIndividualPayrollNumbers = creditcustomerAccount.CustomerIndividualPayrollNumbers;


               
            }

            Session["creditBatchDTO2"] = creditBatchDTO;

            return View("Create", creditBatchDTO);
        }


        [HttpPost]
        public async Task<ActionResult> AddBatch(CreditBatchDTO creditBatchDTO)
        {
            await ServeNavigationMenus();
            ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(string.Empty);
            ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);

            Session["CreditBatchDTO1"] = creditBatchDTO;




            Session["PostingPeriod"] = creditBatchDTO.PostingPeriodDescription;
            Session["PostingPeriodId"] = creditBatchDTO.PostingPeriodId;
            Session["CFixedAmount"] = creditBatchDTO.ConcessionFixedAmount;
            Session["CreditType"] = creditBatchDTO.CreditTypeDescription;
            Session["CreditTypeId"] = creditBatchDTO.CreditTypeId;
            Session["BatchType"] = creditBatchDTO.Type;
            Session["Branch"] = creditBatchDTO.BranchDescription;
            Session["BranchId"] = creditBatchDTO.BranchId;
            Session["Reference"] = creditBatchDTO.Reference;
            Session["Month"] = creditBatchDTO.Month;
            Session["MonthDec"] = creditBatchDTO.MonthDescription;
            Session["ConcesType"] = creditBatchDTO.ConcessionType;
            Session["ConcesTypeDec"] = creditBatchDTO.ConcessionTypeDescription;
            Session["Priority"] = creditBatchDTO.Priority;
            Session["PriorityDec"] = creditBatchDTO.PriorityDescription;
            Session["TotalValue"] = creditBatchDTO.TotalValue;



            return View("Create");
        }



        [HttpPost]
        public async Task<ActionResult> Add(CreditBatchDTO creditBatchDTO)
        {
            await ServeNavigationMenus();
            ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(string.Empty);
            ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);

            


            if (Session["PostingPeriod"] != null)
            {
                creditBatchDTO.PostingPeriodDescription = (string)Session["PostingPeriod"];
            }

            if (Session["PostingPeriodId"] != null)
            {
                creditBatchDTO.PostingPeriodId = (Guid)Session["PostingPeriodId"];
            }
            if (Session["CFixedAmount"] != null)
            {
                creditBatchDTO.ConcessionFixedAmount = (decimal)Session["CFixedAmount"];
            }
            if (Session["CreditType"] != null)
            {
                creditBatchDTO.CreditTypeDescription = (string)Session["CreditType"];
            }

            if (Session["CreditTypeId"] != null)
            {
                creditBatchDTO.CreditTypeId = (Guid)Session["CreditTypeId"];
            }
            if (Session["BatchType"] != null)
            {
                creditBatchDTO.Type = (int)Session["BatchType"];
            }
            if (Session["Branch"] != null)
            {
                creditBatchDTO.BranchDescription = (string)Session["Branch"];
            }
            if (Session["BranchId"] != null)
            {
                creditBatchDTO.BranchId = (Guid)Session["BranchId"];
            }
            if (Session["Reference"] != null)
            {
                creditBatchDTO.Reference = (string)Session["Reference"];
            }
            if (Session["Month"] != null)
            {
                creditBatchDTO.Month = (int)Session["Month"];
            }
            
            if (Session["Month"] != null)
            {
                creditBatchDTO.Month = (int)Session["Month"];
            }
            if (Session["ConcesType"] != null)
            {
                creditBatchDTO.ConcessionType = (int)Session["ConcesType"];
            }

            if (Session["Priority"] != null)
            {
                creditBatchDTO.Priority = (int)Session["Priority"];
            }
            if (Session["TotalValue"] != null)
            {
                creditBatchDTO.TotalValue = (decimal)Session["TotalValue"];
            }




            CreditBatchEntryDTOs = Session["CreditBatchEntryDTO"] as ObservableCollection<CreditBatchEntryDTO>;



            ViewBag.CreditBatchEntryDTOs = CreditBatchEntryDTOs;



            if (CreditBatchEntryDTOs == null)
            {
                CreditBatchEntryDTOs = new ObservableCollection<CreditBatchEntryDTO>();
            }



            foreach (var overDeductionBatchEntryDTO in creditBatchDTO.CreditBatchEntries)
            {
                overDeductionBatchEntryDTO.CreditCustomerAccountFullName = overDeductionBatchEntryDTO.CreditCustomerAccountFullName;
                overDeductionBatchEntryDTO.CreditCustomerAccountFullAccountNumber = overDeductionBatchEntryDTO.CreditCustomerAccountFullAccountNumber;
                overDeductionBatchEntryDTO.CustomerAccountCustomerReference2 = overDeductionBatchEntryDTO.CustomerAccountCustomerReference2;
                overDeductionBatchEntryDTO.CustomerAccountCustomerReference3 = overDeductionBatchEntryDTO.CustomerAccountCustomerReference3;
                overDeductionBatchEntryDTO.CreditCustomerAccountIdentificationNumber = overDeductionBatchEntryDTO.CreditCustomerAccountIdentificationNumber;
                overDeductionBatchEntryDTO.CreditCustomerAccountStatusDescription = overDeductionBatchEntryDTO.CreditCustomerAccountStatusDescription;
                overDeductionBatchEntryDTO.CreditCustomerAccountRemarks = overDeductionBatchEntryDTO.CreditCustomerAccountRemarks;
                overDeductionBatchEntryDTO.ProductDescription = overDeductionBatchEntryDTO.ProductDescription;
                overDeductionBatchEntryDTO.CustomerAccountCustomerId = overDeductionBatchEntryDTO.CustomerAccountCustomerId;
                overDeductionBatchEntryDTO.CreditCustomerAccountTypeDescription = overDeductionBatchEntryDTO.CreditCustomerAccountTypeDescription;
                overDeductionBatchEntryDTO.CustomerAccountCustomerIndividualPayrollNumbers = overDeductionBatchEntryDTO.CustomerAccountCustomerIndividualPayrollNumbers;
                overDeductionBatchEntryDTO.Principal = overDeductionBatchEntryDTO.Principal;
                overDeductionBatchEntryDTO.Beneficiary = overDeductionBatchEntryDTO.Beneficiary;
                overDeductionBatchEntryDTO.Interest = overDeductionBatchEntryDTO.Interest;
                overDeductionBatchEntryDTO.Reference = overDeductionBatchEntryDTO.Reference;

                CreditBatchEntryDTOs.Add(overDeductionBatchEntryDTO);




                

            };

            

            Session["creditBatchDTO"] = creditBatchDTO;

            ViewBag.CreditBatchEntryDTOs = CreditBatchEntryDTOs;

            TempData["CreditBatchEntryDTO"] = CreditBatchEntryDTOs;
            Session["CreditBatchEntryDTO"] = CreditBatchEntryDTOs;
            TempData["CreditBatchDTO"] = creditBatchDTO;
            Session["CreditBatchDTO"] = creditBatchDTO;

            Session["creditBatchDTO2"] = null;





            return View("Create", creditBatchDTO);
        }




        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(string.Empty);
            ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(CreditBatchDTO creditBatchDTO)
        {

            creditBatchDTO = Session["CreditBatchDTO"] as CreditBatchDTO;

            CreditBatchEntryDTOs = Session["CreditBatchEntryDTO"] as ObservableCollection<CreditBatchEntryDTO>;


            if (Session["CreditBatchEntryDTO"] != null)
            {
                creditBatchDTO.CreditBatchEntries = Session["CreditBatchEntryDTO"] as ObservableCollection<CreditBatchEntryDTO>;
            }


            creditBatchDTO.RecoverCarryForwards = creditBatchDTO.RecoverCarryForwards;
            creditBatchDTO.PreserveAccountBalance = creditBatchDTO.PreserveAccountBalance;
            creditBatchDTO.RecoverIndefiniteCharges = creditBatchDTO.RecoverIndefiniteCharges;
            creditBatchDTO.RecoverArrearages = creditBatchDTO.RecoverArrearages;

            creditBatchDTO.ValidateAll();

            if (!creditBatchDTO.HasErrors)
            {
                

                var creditBatchEntries = new ObservableCollection<CreditBatchEntryDTO>();

                foreach (var overDeductionBatchEntryDTO in creditBatchDTO.CreditBatchEntries)
                {
                    overDeductionBatchEntryDTO.CreditCustomerAccountFullName = overDeductionBatchEntryDTO.CreditCustomerAccountFullName;
                    overDeductionBatchEntryDTO.CreditCustomerAccountFullAccountNumber = overDeductionBatchEntryDTO.CreditCustomerAccountFullAccountNumber;
                    overDeductionBatchEntryDTO.CustomerAccountCustomerReference2 = overDeductionBatchEntryDTO.CustomerAccountCustomerReference2;
                    overDeductionBatchEntryDTO.CustomerAccountCustomerReference3 = overDeductionBatchEntryDTO.CustomerAccountCustomerReference3;
                    overDeductionBatchEntryDTO.CreditCustomerAccountIdentificationNumber = overDeductionBatchEntryDTO.CreditCustomerAccountIdentificationNumber;
                    overDeductionBatchEntryDTO.CreditCustomerAccountStatusDescription = overDeductionBatchEntryDTO.CreditCustomerAccountStatusDescription;
                    overDeductionBatchEntryDTO.CreditCustomerAccountRemarks = overDeductionBatchEntryDTO.CreditCustomerAccountRemarks;
                    overDeductionBatchEntryDTO.ProductDescription = overDeductionBatchEntryDTO.ProductDescription;
                    overDeductionBatchEntryDTO.CustomerAccountCustomerId = overDeductionBatchEntryDTO.CustomerAccountCustomerId;
                    overDeductionBatchEntryDTO.CreditCustomerAccountTypeDescription = overDeductionBatchEntryDTO.CreditCustomerAccountTypeDescription;
                    overDeductionBatchEntryDTO.CustomerAccountCustomerIndividualPayrollNumbers = overDeductionBatchEntryDTO.CustomerAccountCustomerIndividualPayrollNumbers;
                    overDeductionBatchEntryDTO.Principal = overDeductionBatchEntryDTO.Principal;
                    overDeductionBatchEntryDTO.Beneficiary = overDeductionBatchEntryDTO.Beneficiary;
                    overDeductionBatchEntryDTO.Interest = overDeductionBatchEntryDTO.Interest;
                    overDeductionBatchEntryDTO.Reference = overDeductionBatchEntryDTO.Reference;


                   /* var creditBatchEntry = await _channelService.AddCreditBatchEntryAsync(overDeductionBatchEntryDTO, GetServiceHeader());*/
                }

                var creditBatch = await _channelService.AddCreditBatchAsync(creditBatchDTO, GetServiceHeader());

                /*await _channelService.AddOverDeductionBatchEntryAsync(OverDeductionBatchEntries, GetServiceHeader());*/

                if (creditBatch.HasErrors)
                {
                    await ServeNavigationMenus();

                    TempData["ErrorMsg"] = creditBatchDTO.ErrorMessages;

                    return View();
                }

                TempData["SuccessMessage"] = "Successfully Created Credit Batch";
                TempData["OverDeductionBatchDTO"] = "";

                ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(creditBatchDTO.Type.ToString());
                ViewBag.MonthsSelectList = GetMonthsAsync(creditBatchDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetCreditBatchesAsync(creditBatchDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(creditBatchDTO.Priority.ToString());
                ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(creditBatchDTO.ConcessionType.ToString());
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = creditBatchDTO.ErrorMessages;

                return View(creditBatchDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
            var creditBatchDTO = await _channelService.FindCreditBatchAsync(id, GetServiceHeader());
            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);
            ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(string.Empty);
            ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);
            return View(creditBatchDTO);
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



        public async Task<ActionResult> Verify(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);


            var creditBatchDTO = await _channelService.FindCreditBatchAsync(id, GetServiceHeader());
            ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(string.Empty);
            ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);


            var creditBatches = await _channelService.FindCreditBatchEntriesByCreditBatchIdAsync(id,true, GetServiceHeader());

            ViewBag.CrdeitBatchEntryDTOs = creditBatches;


            return View(creditBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(Guid id, CreditBatchDTO creditBatchDTO)
        {
            creditBatchDTO.ValidateAll();

            if (!creditBatchDTO.HasErrors)
            {
                await _channelService.AuditCreditBatchAsync(creditBatchDTO, 1, GetServiceHeader());
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
                var errorMessages = creditBatchDTO.ErrorMessages;
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(creditBatchDTO.Type.ToString());
                return View(creditBatchDTO);
            }
        }


        public async Task<ActionResult> Authorize(Guid id)
        {
            await ServeNavigationMenus();


            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);

            var creditBatchDTO = await _channelService.FindCreditBatchAsync(id, GetServiceHeader());

            ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(string.Empty);
            ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);
            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);

            var creditBatches = await _channelService.FindCreditBatchEntriesByCreditBatchIdAsync(id, true, GetServiceHeader());

            ViewBag.CrdeitBatchEntryDTOs = creditBatches;

            return View(creditBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Authorize(Guid id, CreditBatchDTO creditBatchDTO)
        {
            var batchAuthOption = creditBatchDTO.Status;

            creditBatchDTO.ValidateAll();

            if (!creditBatchDTO.HasErrors)
            {
                await _channelService.AuthorizeCreditBatchAsync(creditBatchDTO, 1, batchAuthOption, GetServiceHeader());
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
                var errorMessages = creditBatchDTO.ErrorMessages;
                await _channelService.AuthorizeCreditBatchAsync(creditBatchDTO, 1, batchAuthOption, GetServiceHeader());
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(creditBatchDTO.Type.ToString());
                ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(creditBatchDTO.Type.ToString());
                ViewBag.MonthsSelectList = GetMonthsAsync(creditBatchDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetCreditBatchesAsync(creditBatchDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(creditBatchDTO.Priority.ToString());
                ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(creditBatchDTO.ConcessionType.ToString());
                return View(creditBatchDTO);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetCreditBatchesAsync()
        {
            var creditBatchDTOs = await _channelService.FindCreditBatchesAsync(GetServiceHeader());

            return Json(creditBatchDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}

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

            var creditBatches = await _channelService.FindCreditBatchEntriesByCreditBatchIdAsync(id, true, GetServiceHeader());

            ViewBag.CreditBatchEntryDTOs = creditBatches;

            return View(creditBatchDTO);
        }


        public async Task<ActionResult> CreditCustomerAccountLookUp(Guid? id, CreditBatchDTO creditBatchDTO)
        {

            await ServeNavigationMenus();

            // Check whether header details contain data and proceed to add entries...
            if (Session["HeaderDetails"] != null)
            {
                creditBatchDTO = Session["HeaderDetails"] as CreditBatchDTO;
            }



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
                creditBatchDTO.CreditBatchEntries[0].CreditCustomerAccountIdentificationNumber = creditcustomerAccount.CustomerIndividualIdentityCardNumber;
                creditBatchDTO.CreditBatchEntries[0].CreditCustomerAccountStatusDescription = creditcustomerAccount.StatusDescription;
                creditBatchDTO.CreditBatchEntries[0].CreditCustomerAccountRemarks = creditcustomerAccount.Remarks;
                creditBatchDTO.CreditBatchEntries[0].ProductDescription = creditcustomerAccount.CustomerAccountTypeProductCodeDescription;
                creditBatchDTO.CreditBatchEntries[0].CustomerAccountCustomerId = creditcustomerAccount.Id;
                creditBatchDTO.CreditBatchEntries[0].CreditCustomerAccountTypeDescription = creditcustomerAccount.TypeDescription;
                creditBatchDTO.CreditBatchEntries[0].CustomerAccountCustomerIndividualPayrollNumbers = creditcustomerAccount.CustomerIndividualPayrollNumbers;



            }

            Session["creditBatchDTO2"] = creditBatchDTO;

            return View("Create", creditBatchDTO);
        }


        [HttpPost]
        public async Task<ActionResult> BatchOrigination_Credit(CreditBatchDTO creditBatchDTO)
        {
            await ServeNavigationMenus();
            ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(string.Empty);
            ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);

            Session["HeaderDetails"] = creditBatchDTO;

            return View("Create", creditBatchDTO);
        }



        [HttpPost]
        public async Task<ActionResult> Add(CreditBatchDTO creditBatchDTO)
        {
            await ServeNavigationMenus();
            ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(string.Empty);
            ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);


            // Check whether header details contain data and proceed to add entries...
            if (Session["HeaderDetails"] != null)
            {
                creditBatchDTO = Session["HeaderDetails"] as CreditBatchDTO;
            }



            CreditBatchEntryDTOs = Session["CreditBatchEntryDTO"] as ObservableCollection<CreditBatchEntryDTO>;



            ViewBag.CreditBatchEntryDTOs = CreditBatchEntryDTOs;



            if (CreditBatchEntryDTOs == null)
            {
                CreditBatchEntryDTOs = new ObservableCollection<CreditBatchEntryDTO>();
            }

            decimal sumAmount = 0;

            foreach (var creditBatchEntryDTO in creditBatchDTO.CreditBatchEntries)
            {
                creditBatchEntryDTO.CreditCustomerAccountFullName = creditBatchEntryDTO.CreditCustomerAccountFullName;
                creditBatchEntryDTO.CreditCustomerAccountFullAccountNumber = creditBatchEntryDTO.CreditCustomerAccountFullAccountNumber;
                creditBatchEntryDTO.CustomerAccountCustomerReference2 = creditBatchEntryDTO.CustomerAccountCustomerReference2;
                creditBatchEntryDTO.CustomerAccountCustomerReference3 = creditBatchEntryDTO.CustomerAccountCustomerReference3;
                creditBatchEntryDTO.CreditCustomerAccountIdentificationNumber = creditBatchEntryDTO.CreditCustomerAccountIdentificationNumber;
                creditBatchEntryDTO.CreditCustomerAccountStatusDescription = creditBatchEntryDTO.CreditCustomerAccountStatusDescription;
                creditBatchEntryDTO.CreditCustomerAccountRemarks = creditBatchEntryDTO.CreditCustomerAccountRemarks;
                creditBatchEntryDTO.ProductDescription = creditBatchEntryDTO.ProductDescription;
                creditBatchEntryDTO.CustomerAccountCustomerId = creditBatchEntryDTO.CustomerAccountCustomerId;
                creditBatchEntryDTO.CreditCustomerAccountTypeDescription = creditBatchEntryDTO.CreditCustomerAccountTypeDescription;
                creditBatchEntryDTO.CustomerAccountCustomerIndividualPayrollNumbers = creditBatchEntryDTO.CustomerAccountCustomerIndividualPayrollNumbers;
                creditBatchEntryDTO.Principal = creditBatchEntryDTO.Principal;
                creditBatchEntryDTO.Beneficiary = creditBatchEntryDTO.Beneficiary;
                creditBatchEntryDTO.Interest = creditBatchEntryDTO.Interest;
                creditBatchEntryDTO.Reference = creditBatchEntryDTO.Reference;


                CreditBatchEntryDTOs.Add(creditBatchEntryDTO);


                sumAmount = CreditBatchEntryDTOs.Sum(cs => cs.Principal + cs.Interest);
                if (sumAmount > creditBatchDTO.TotalValue)
                {
                    TempData["tPercentage"] = "Failed to add  Credit  Entry Total Amount exceeded Total Value.";

                    CreditBatchEntryDTOs.Remove(creditBatchEntryDTO);

                }

            };


            Session["creditBatchDTO"] = creditBatchDTO;

            ViewBag.CreditBatchEntryDTOs = CreditBatchEntryDTOs;

            TempData["CreditBatchEntryDTO"] = CreditBatchEntryDTOs;
            Session["CreditBatchEntryDTO"] = CreditBatchEntryDTOs;
            TempData["CreditBatchDTO"] = creditBatchDTO;
            Session["CreditBatchDTO"] = creditBatchDTO;

            Session["sumAmount"] = sumAmount;
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
            // Cheat: In case header details do not hit Add Action
            //if (Session["HeaderDetails"] != null)
            //{
            //    creditBatchDTO = Session["HeaderDetails"] as CreditBatchDTO;
            //}

            Session["RecoverCarryForwards"] = creditBatchDTO.RecoverCarryForwards;
            Session["PreserveAccountBalance"] = creditBatchDTO.PreserveAccountBalance;
            Session["RecoverIndefiniteCharges"] = creditBatchDTO.RecoverIndefiniteCharges;
            Session["RecoverArrearages"] = creditBatchDTO.RecoverArrearages;

            creditBatchDTO = Session["CreditBatchDTO"] as CreditBatchDTO;

            CreditBatchEntryDTOs = Session["CreditBatchEntryDTO"] as ObservableCollection<CreditBatchEntryDTO>;



            if (Session["CreditBatchEntryDTO"] != null)
            {
                creditBatchDTO.CreditBatchEntries = Session["CreditBatchEntryDTO"] as ObservableCollection<CreditBatchEntryDTO>;
            }


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






            creditBatchDTO.ValidateAll();

            if (!creditBatchDTO.HasErrors)
            {
                decimal sumAmount = Convert.ToDecimal(Session["sumAmount"].ToString());

                if (sumAmount != creditBatchDTO.TotalValue)
                {
                    TempData["LessSumAmount"] = "Amount and Interest should be equal to Total Value";
                    ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(creditBatchDTO.Type.ToString());
                    ViewBag.MonthsSelectList = GetMonthsAsync(creditBatchDTO.Type.ToString());
                    ViewBag.QueuePriorityTypeSelectList = GetCreditBatchesAsync(creditBatchDTO.Type.ToString());
                    ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(creditBatchDTO.Priority.ToString());
                    ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(creditBatchDTO.ConcessionType.ToString());
                    ViewBag.CreditBatchEntryDTOs = CreditBatchEntryDTOs;
                    return View(creditBatchDTO);
                }

                var creditBatch = await _channelService.AddCreditBatchAsync(creditBatchDTO, GetServiceHeader());
                if (creditBatch.HasErrors)
                {
                    await ServeNavigationMenus();

                    TempData["ErrorMsg"] = creditBatchDTO.ErrorMessages;

                    return View();
                }

                foreach (var creditBatchEntry in CreditBatchEntryDTOs)
                {
                    creditBatchEntry.CreditBatchId = creditBatch.Id;
                    await _channelService.AddCreditBatchEntryAsync(creditBatchEntry, GetServiceHeader());
                }




                TempData["CreditBatchCreated"] = "Successfully Created Credit Batch";
                Session["CreditBatchEntryDTO"] = null;
                Session["CreditBatchDTO"] = null;

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



            var creditBatches = await _channelService.FindCreditBatchEntriesByCreditBatchIdAsync(id, true, GetServiceHeader());

            ViewBag.CreditBatchEntryDTOs = creditBatches;


            return View(creditBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(Guid id, CreditBatchDTO creditBatchDTO)
        {
            creditBatchDTO.ValidateAll();
            var batchAuthOption = creditBatchDTO.CreditAuthOption;
            if (!creditBatchDTO.HasErrors)
            {
                await _channelService.AuditCreditBatchAsync(creditBatchDTO, batchAuthOption, GetServiceHeader());
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(creditBatchDTO.CreditAuthOption.ToString());
                ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(creditBatchDTO.Type.ToString());
                ViewBag.MonthsSelectList = GetMonthsAsync(creditBatchDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetCreditBatchesAsync(creditBatchDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(creditBatchDTO.Priority.ToString());
                ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(creditBatchDTO.ConcessionType.ToString());

                TempData["VerifySuccess"] = "Verification Successiful";
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


            creditBatchDTO.ValidateAll();
            var batchAuthOption = creditBatchDTO.CreditAuthOption;
            if (!creditBatchDTO.HasErrors)
            {
                await _channelService.AuthorizeCreditBatchAsync(creditBatchDTO, batchAuthOption, 1, GetServiceHeader());
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(creditBatchDTO.Type.ToString());
                ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(creditBatchDTO.Type.ToString());
                ViewBag.MonthsSelectList = GetMonthsAsync(creditBatchDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetCreditBatchesAsync(creditBatchDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(creditBatchDTO.Priority.ToString());
                ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(creditBatchDTO.ConcessionType.ToString());

                TempData["AuthorizeSuccess"] = "Authorization Successiful";

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

                TempData["AuthorizeFail"] = "Authorization Failed";
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

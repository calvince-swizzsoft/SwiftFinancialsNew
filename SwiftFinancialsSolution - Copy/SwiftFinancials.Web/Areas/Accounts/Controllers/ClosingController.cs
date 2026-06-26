using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Attributes;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class ClosingController : MasterController
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

            var pageCollectionInfo = await _channelService.FindAlternateChannelReconciliationPeriodsByFilterInPageAsync(1, DateTime.Now, DateTime.Now, jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(postingPeriod => postingPeriod.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<AlternateChannelReconciliationPeriodDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var debitBatchDTO = await _channelService.FindAlternateChannelAsync(id, true, GetServiceHeader());

            return View(debitBatchDTO);
        }

        public async Task<ActionResult> Create(Guid? id, AlternateChannelReconciliationPeriodDTO alternateChannelDTO1)
        {

            await ServeNavigationMenus();

            Guid parseId;

            ViewBag.alternateChannelType = GetAlternateChannelTypeSelectList(string.Empty);

            ViewBag.RecordStatusSelectList = GetRecordStatusSelectList(string.Empty);

            ViewBag.SetDifferenceMode = GetSetDifferenceModeSelectList(string.Empty);

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }
            //var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());



            //var customer = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());
            var customer = await _channelService.FindAlternateChannelAsync(parseId, true, GetServiceHeader());

            AlternateChannelReconciliationPeriodDTO alternateChannelDTO = new AlternateChannelReconciliationPeriodDTO();

            if (customer != null)
            {
                alternateChannelDTO.DurationStartDate = customer.CreatedDate;
                alternateChannelDTO.Remarks = customer.Remarks;
                alternateChannelDTO.Remarks = customer.Remarks;
                alternateChannelDTO.AlternateChannelType = customer.Type;



            }

            ViewBag.SystemTransactionType = GetSystemTransactionTypeList(string.Empty);
            ViewBag.alternateChannelType = GetAlternateChannelTypeSelectList(string.Empty);
            ViewBag.ChargeBenefactor = GetChargeBenefactorSelectList(string.Empty);
            ViewBag.Chargetype = GetChargeTypeSelectList(string.Empty);

            return View(alternateChannelDTO);
        }


        public async Task<ActionResult> Search(Guid? id, AlternateChannelDTO alternateChannelDTO1)
        {
            await ServeNavigationMenus();

            Guid parseId;
            ViewBag.alternateChannelType = GetAlternateChannelTypeSelectList(string.Empty);
            ViewBag.RecordStatusSelectList = GetRecordStatusSelectList(string.Empty);

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            //var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());




            var customer = await _channelService.FindAlternateChannelAsync(parseId, true,GetServiceHeader());

            AlternateChannelReconciliationPeriodDTO alternateChannelDTO = new AlternateChannelReconciliationPeriodDTO();

            if (customer != null)
            {
                alternateChannelDTO.DurationStartDate = customer.CreatedDate;
                alternateChannelDTO.Remarks = customer.Remarks;
                alternateChannelDTO.Remarks = customer.Remarks;
                alternateChannelDTO.AlternateChannelType = customer.Type;



            }
            Session["DailyLimit"] = alternateChannelDTO1.DailyLimit;
            Session["CardNumber"] = alternateChannelDTO1.CardNumber;



            return View("Create", alternateChannelDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(AlternateChannelReconciliationPeriodDTO alternateChannelDTO)
        {
            var startDate = Request["startDate"];

            var endDate = Request["endDate"];

            alternateChannelDTO.DurationStartDate = DateTime.Parse(startDate).Date;

            alternateChannelDTO.DurationEndDate = DateTime.Parse(endDate).Date;


            alternateChannelDTO.ValidateAll();

            if (!alternateChannelDTO.HasErrors)
            {
                await _channelService.CloseAlternateChannelReconciliationPeriodAsync(alternateChannelDTO, 0,GetServiceHeader());
                TempData["SuccessMessage"] = "Closing successful.";
                //if (result.ErrorMessageResult != null)
                //{
                //    TempData["ErrorMsg"] = result.ErrorMessageResult;
                //    await ServeNavigationMenus();
                //    return View();
                //}
                //AlternateChannelReconciliationEntryDTO alternateChannelReconciliationEntryDTO = new AlternateChannelReconciliationEntryDTO();

                ////if (customer != null)
                ////{
                //    alternateChannelReconciliationEntryDTO.Id = customer.Id;

                //    alternateChannelReconciliationEntryDTO.Remarks = customer.Remarks;
                //    alternateChannelReconciliationEntryDTO.Status = customer.Status;
                //}

                //await _channelService.AddAlternateChannelReconciliationEntryAsync(alternateChannelReconciliationEntryDTO, GetServiceHeader());

                ViewBag.alternateChannelType = GetAlternateChannelTypeSelectList(alternateChannelDTO.AlternateChannelType.ToString());
                ViewBag.RecordStatusSelectList = GetRecordStatusSelectList(alternateChannelDTO.SetDifferenceMode.ToString());
                ViewBag.SetDifferenceMode = GetSetDifferenceModeSelectList(alternateChannelDTO.SetDifferenceMode.ToString());




                ViewBag.alternateChannelType = GetAlternateChannelTypeSelectList(alternateChannelDTO.AlternateChannelType.ToString());

                return RedirectToAction("Create");
            }
            else
            {
                var errorMessages = alternateChannelDTO.ErrorMessages;
                ViewBag.alternateChannelType = GetAlternateChannelTypeSelectList(alternateChannelDTO.AlternateChannelType.ToString());
                ViewBag.RecordStatusSelectList = GetRecordStatusSelectList(alternateChannelDTO.SetDifferenceMode.ToString());
                ViewBag.SetDifferenceMode = GetSetDifferenceModeSelectList(alternateChannelDTO.SetDifferenceMode.ToString());
                return View(alternateChannelDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.QueuePrioritySelectList = GetAlternateChannelTypeSelectList(string.Empty);

            ViewBag.alternateChannelType = GetAlternateChannelTypeSelectList(string.Empty);
            ViewBag.RecordStatusSelectList = GetRecordStatusSelectList(string.Empty);

            var alternateChannelDTO = await _channelService.FindAlternateChannelAsync(id, true, GetServiceHeader());

            return View(alternateChannelDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, AlternateChannelDTO alternateChannelDTO)
        {
            alternateChannelDTO.ValidateAll();

            if (!alternateChannelDTO.HasErrors)
            {
                await _channelService.UpdateAlternateChannelAsync(alternateChannelDTO, GetServiceHeader());
                ViewBag.QueuePrioritySelectList = GetAlternateChannelTypeSelectList(alternateChannelDTO.Type.ToString());
                ViewBag.RecordStatusSelectList = GetRecordStatusSelectList(alternateChannelDTO.RecordStatus.ToString());

                ViewBag.alternateChannelType = GetAlternateChannelTypeSelectList(alternateChannelDTO.Type.ToString());
                TempData["SuccessMessage"] = "Create successful.";
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = alternateChannelDTO.ErrorMessages;
                TempData["Error"] = "Failed to Edit .";
                ViewBag.QueuePrioritySelectList = GetQueuePrioritySelectList(alternateChannelDTO.Type.ToString());
                return View(alternateChannelDTO);
            }
        }

        public async Task<ActionResult> Verify(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);

            var alternateChannelDTO = await _channelService.FindAlternateChannelAsync(id, true, GetServiceHeader());

            return View(alternateChannelDTO);
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

        public async Task<ActionResult> Linking(Guid? id)
        {
            await ServeNavigationMenus();

            Guid parseId;
            ViewBag.alternateChannelType = GetAlternateChannelTypeSelectList(string.Empty);
            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            //var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            bool includeBalances = false;
            bool includeProductDescription = false;
            bool includeInterestBalanceForLoanAccounts = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;


            var customer = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            AlternateChannelDTO alternateChannelDTO = new AlternateChannelDTO();

            if (customer != null)
            {
                alternateChannelDTO.CustomerAccountCustomerId = customer.Id;
                alternateChannelDTO.CustomerAccountId = customer.Id;
                alternateChannelDTO.CustomerAccountCustomerIndividualFirstName = customer.CustomerIndividualFirstName;
                alternateChannelDTO.CustomerAccountCustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers;
                alternateChannelDTO.CustomerAccountCustomerSerialNumber = customer.CustomerSerialNumber;
                alternateChannelDTO.CustomerAccountCustomerReference1 = customer.CustomerReference1;
                alternateChannelDTO.CustomerAccountCustomerReference2 = customer.CustomerReference2;
                alternateChannelDTO.CustomerAccountCustomerReference3 = customer.CustomerReference3;
                alternateChannelDTO.CustomerAccountCustomerIndividualIdentityCardNumber = customer.CustomerIdentificationNumber;
                alternateChannelDTO.Remarks = customer.Remarks;
                alternateChannelDTO.CardNumber = customer.FullAccountNumber;


            }

            ViewBag.SystemTransactionType = GetSystemTransactionTypeList(string.Empty);
            ViewBag.alternateChannelType = GetAlternateChannelTypeSelectList(string.Empty);
            ViewBag.ChargeBenefactor = GetChargeBenefactorSelectList(string.Empty);
            ViewBag.Chargetype = GetChargeTypeSelectList(string.Empty);

            return View(alternateChannelDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Linking(AlternateChannelDTO alternateChannelDTO)
        {
            alternateChannelDTO.ValidateAll();


            if (!alternateChannelDTO.HasErrors)
            {
                await _channelService.AddAlternateChannelAsync(alternateChannelDTO, GetServiceHeader());
                TempData["SuccessMessage"] = "Create successful.";
                ViewBag.QueuePrioritySelectList = GetAlternateChannelTypeSelectList(alternateChannelDTO.Type.ToString());
                //ViewBag.QueuePrioritySelectList = GetAlternateChannelKnownChargeTypeSelectList(levyDTO.ChargeBenefactor.ToString());
                ViewBag.alternateChannelType = GetAlternateChannelTypeSelectList(alternateChannelDTO.Type.ToString());
                //ViewBag.ChargeBenefactor = GetChargeBenefactorSelectList(levyDTO.ChargeBenefactor.ToString());
                //ViewBag.Chargetype = GetChargeTypeSelectList(levyDTO.ChargeBenefactor.ToString());
                // await _channelService.AddDynamicChargeAsync(levyDTO, GetServiceHeader());

                return RedirectToAction("Create");
            }
            else
            {
                var errorMessages = alternateChannelDTO.ErrorMessages;
                ViewBag.alternateChannelType = GetAlternateChannelTypeSelectList(alternateChannelDTO.Type.ToString());
                return View(alternateChannelDTO);
            }
        }


        public async Task<ActionResult> History(Guid? id)
        {
            await ServeNavigationMenus();

            Guid parseId;
            ViewBag.alternateChannelType = GetAlternateChannelTypeSelectList(string.Empty);
            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            //var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            bool includeBalances = false;
            bool includeProductDescription = false;
            bool includeInterestBalanceForLoanAccounts = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;


            var customer = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            AlternateChannelDTO alternateChannelDTO = new AlternateChannelDTO();

            if (customer != null)
            {
                alternateChannelDTO.CustomerAccountCustomerId = customer.Id;
                alternateChannelDTO.CustomerAccountId = customer.Id;
                alternateChannelDTO.CustomerAccountCustomerIndividualFirstName = customer.CustomerIndividualFirstName;
                alternateChannelDTO.CustomerAccountCustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers;
                alternateChannelDTO.CustomerAccountCustomerSerialNumber = customer.CustomerSerialNumber;
                alternateChannelDTO.CustomerAccountCustomerReference1 = customer.CustomerReference1;
                alternateChannelDTO.CustomerAccountCustomerReference2 = customer.CustomerReference2;
                alternateChannelDTO.CustomerAccountCustomerReference3 = customer.CustomerReference3;
                alternateChannelDTO.CustomerAccountCustomerIndividualIdentityCardNumber = customer.CustomerIdentificationNumber;
                alternateChannelDTO.Remarks = customer.Remarks;
                alternateChannelDTO.CardNumber = customer.FullAccountNumber;


            }

            ViewBag.SystemTransactionType = GetSystemTransactionTypeList(string.Empty);
            ViewBag.alternateChannelType = GetAlternateChannelTypeSelectList(string.Empty);
            ViewBag.ChargeBenefactor = GetChargeBenefactorSelectList(string.Empty);
            ViewBag.Chargetype = GetChargeTypeSelectList(string.Empty);

            return View(alternateChannelDTO);
        }


        [HttpPost]
        public async Task<ActionResult> History(AlternateChannelDTO alternateChannelDTO)
        {
            alternateChannelDTO.ValidateAll();


            if (!alternateChannelDTO.HasErrors)
            {
                await _channelService.AddAlternateChannelAsync(alternateChannelDTO, GetServiceHeader());
                TempData["SuccessMessage"] = "Create successful.";
                ViewBag.QueuePrioritySelectList = GetAlternateChannelTypeSelectList(alternateChannelDTO.Type.ToString());
                //ViewBag.QueuePrioritySelectList = GetAlternateChannelKnownChargeTypeSelectList(levyDTO.ChargeBenefactor.ToString());
                ViewBag.alternateChannelType = GetAlternateChannelTypeSelectList(alternateChannelDTO.Type.ToString());
                //ViewBag.ChargeBenefactor = GetChargeBenefactorSelectList(levyDTO.ChargeBenefactor.ToString());
                //ViewBag.Chargetype = GetChargeTypeSelectList(levyDTO.ChargeBenefactor.ToString());
                // await _channelService.AddDynamicChargeAsync(levyDTO, GetServiceHeader());

                return RedirectToAction("Create");
            }
            else
            {
                var errorMessages = alternateChannelDTO.ErrorMessages;
                ViewBag.alternateChannelType = GetAlternateChannelTypeSelectList(alternateChannelDTO.Type.ToString());
                return View(alternateChannelDTO);
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
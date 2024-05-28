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
    public class WellknownchargesController : MasterController
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

            return View(debitBatchDTO);
        }


        public async Task<ActionResult> Wellknowncharges(SystemTransactionTypeInCommissionDTO systemTransactionTypeInCommissionDTO)
        {
            Session["ComplementFixedAmount"] = systemTransactionTypeInCommissionDTO.ComplementFixedAmount;
            Session["SystemTransactionType"] = systemTransactionTypeInCommissionDTO.SystemTransactionType;
            Session["ComplementType"] = systemTransactionTypeInCommissionDTO.ComplementType;

            return View("Create",systemTransactionTypeInCommissionDTO);
        }


        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.QueuePrioritySelectList = GetQueuePrioritySelectList(string.Empty);
            ViewBag.SystemTransactionType = GetSystemTransactionTypeList(string.Empty);
            ViewBag.ChargeBenefactor = GetChargeBenefactorSelectList(string.Empty);
            ViewBag.Chargetype = GetChargeTypeSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(SystemTransactionTypeInCommissionDTO systemTransactionTypeInCommissionDTO, ObservableCollection<CommissionDTO> selectedRows, ChargeDTO chargeDTO)
        {
            systemTransactionTypeInCommissionDTO.ComplementFixedAmount =Convert.ToInt32( Session["ComplementFixedAmount"].ToString());
            systemTransactionTypeInCommissionDTO.SystemTransactionType = Convert.ToInt32(Session["SystemTransactionType"].ToString());
            systemTransactionTypeInCommissionDTO.ComplementType= Convert.ToInt32(Session["ComplementType"].ToString());

            //debitTypeDTO.ProductCode = Convert.ToInt32(Session["ProductCode"].ToString());
            //debitTypeDTO.IsLocked = (bool)Session["isLocked"];


            systemTransactionTypeInCommissionDTO.ValidateAll();
            int SystemTransactionType = systemTransactionTypeInCommissionDTO.SystemTransactionType;
            if (!systemTransactionTypeInCommissionDTO.HasErrors)
            {
                var j=await _channelService.MapSystemTransactionTypeToCommissionsAsync(SystemTransactionType,selectedRows,chargeDTO, GetServiceHeader());

                ViewBag.SystemTransactionType = GetSystemTransactionTypeList(systemTransactionTypeInCommissionDTO.SystemTransactionType.ToString());
                //ViewBag.QueuePrioritySelectList = GetAlternateChannelKnownChargeTypeSelectList(systemTransactionTypeInCommissionDTO.RecoveryMode.ToString());
                //ViewBag.AlternateChannelType = GetAlternateChannelTypeSelectList(systemTransactionTypeInCommissionDTO.RecoveryMode.ToString());
                //ViewBag.ChargeBenefactor = GetChargeBenefactorSelectList(systemTransactionTypeInCommissionDTO.RecoveryMode.ToString());
                ViewBag.Chargetype = GetChargeTypeSelectList(systemTransactionTypeInCommissionDTO.ComplementType.ToString());

                //ViewBag.QueuePrioritySelectList = GetQueuePrioritySelectList(systemTransactionTypeInCommissionDTO.RecoverySource.ToString());
                TempData["Create"] = "Successfully Well known charges";
                return View("Create");
            }
            else
            {
                var errorMessages = systemTransactionTypeInCommissionDTO.ErrorMessages;
                //ViewBag.SystemTransactionType = GetSystemTransactionTypeList(systemTransactionTypeInCommissionDTO.RecoveryMode.ToString());
                //ViewBag.QueuePrioritySelectList = GetAlternateChannelKnownChargeTypeSelectList(systemTransactionTypeInCommissionDTO.RecoveryMode.ToString());
                //ViewBag.AlternateChannelType = GetAlternateChannelTypeSelectList(systemTransactionTypeInCommissionDTO.RecoveryMode.ToString());
                //ViewBag.ChargeBenefactor = GetChargeBenefactorSelectList(systemTransactionTypeInCommissionDTO.RecoveryMode.ToString());
                //ViewBag.Chargetype = GetChargeTypeSelectList(systemTransactionTypeInCommissionDTO.RecoveryMode.ToString());

                //ViewBag.QueuePrioritySelectList = GetQueuePrioritySelectList(systemTransactionTypeInCommissionDTO.RecoverySource.ToString());
                return View(systemTransactionTypeInCommissionDTO);
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



        [HttpGet]
        public async Task<JsonResult> GetDebitBatchesAsync()
        {
            var debitBatchDTOs = await _channelService.FindDebitBatchesAsync(GetServiceHeader());

            return Json(debitBatchDTOs, JsonRequestBehavior.AllowGet);
        }
    }

}

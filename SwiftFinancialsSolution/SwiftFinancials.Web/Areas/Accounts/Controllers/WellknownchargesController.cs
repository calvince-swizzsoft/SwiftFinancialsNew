using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class WellknownchargesController : MasterController
    {

        public async Task<ActionResult> Create(Guid? id, DynamicChargeDTO dynamicChargeDTO)
        {
            await ServeNavigationMenus();
            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            ViewBag.SystemTransactionType = GetSystemTransactionTypeList(string.Empty);
            ViewBag.QueuePrioritySelectList = GetAlternateChannelKnownChargeTypeSelectList(string.Empty);
            ViewBag.AlternateChannelType = GetAlternateChannelTypeSelectList(string.Empty);
            ViewBag.ChargeBenefactor = GetChargeBenefactorSelectList(string.Empty);
            ViewBag.Chargetype = GetChargeTypeSelectList(string.Empty);  
           var J=await  _channelService.FindDynamicChargeAsync(parseId, GetServiceHeader());

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(DynamicChargeDTO levyDTO)
        {
            levyDTO.ValidateAll();

            if (!levyDTO.HasErrors)
            {
                await _channelService.AddDynamicChargeAsync(levyDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = levyDTO.ErrorMessages;
                ViewBag.SystemTransactionType = GetSystemTransactionTypeList(levyDTO.RecoveryMode.ToString());
                ViewBag.QueuePrioritySelectList = GetAlternateChannelKnownChargeTypeSelectList(levyDTO.RecoveryMode.ToString());
                ViewBag.AlternateChannelType = GetAlternateChannelTypeSelectList(levyDTO.RecoveryMode.ToString());
                ViewBag.ChargeBenefactor = GetChargeBenefactorSelectList(levyDTO.RecoveryMode.ToString());
                ViewBag.Chargetype = GetChargeTypeSelectList(levyDTO.RecoveryMode.ToString());

                return View(levyDTO);
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

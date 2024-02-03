using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Loaning
{
    public class LoanVerificationController : MasterController
    {

        public async Task<ActionResult> Verify(Guid id)
        {
            await ServeNavigationMenus();

            var loanCaseDTO = await _channelService.FindLoanCaseAsync(id, GetServiceHeader());
            ViewBag.LoanAuditOptionSelectList = GetLoanAuditOptionSelectList(string.Empty);

            return View(loanCaseDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(LoanCaseDTO loanCaseDTO)
        {
            var loanAuditOption = loanCaseDTO.LoanAuditOption;
            loanCaseDTO.ValidateAll();

            if (!loanCaseDTO.HasErrors)
            {
                await _channelService.AuditLoanCaseAsync(loanCaseDTO, loanAuditOption, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loanCaseDTO.ErrorMessages;
                ViewBag.LoanAuditOptionSelectList = GetLoanAuditOptionSelectList(loanCaseDTO.LoanAuditOption.ToString());
                return View(loanCaseDTO);
            }
        }

    }
}
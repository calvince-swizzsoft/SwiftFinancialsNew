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

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class AppraiseLoanController : MasterController
    {
        public async Task<ActionResult> Appraise()

        {
            await ServeNavigationMenus();
            int caseNumber = 0;
            await _channelService.FindLoanCaseByLoanCaseNumberAsync(caseNumber, GetServiceHeader());

            ViewBag.LoanAppraisalOptionSelectList = GetLoanAppraisalOptionSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Appraise(LoanCaseDTO loanCaseDTO)
        {
            var loanAppraisalOption = loanCaseDTO.LoanAppraisalOption;

            loanCaseDTO.ValidateAll();
            if (!loanCaseDTO.HasErrors)
            {
                await _channelService.AppraiseLoanCaseAsync(loanCaseDTO, loanAppraisalOption, 1, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loanCaseDTO.ErrorMessages;
                ViewBag.LoanAppraisalOptionSelectList = GetLoanAppraisalOptionSelectList(loanCaseDTO.LoanApprovalOption.ToString());
                return View(loanCaseDTO);
            }
        }
    }
}
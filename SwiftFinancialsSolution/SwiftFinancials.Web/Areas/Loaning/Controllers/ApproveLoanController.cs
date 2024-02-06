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
    public class ApproveLoanController : MasterController
    {
        public async Task<ActionResult> Approve(Guid id)
        {
            await ServeNavigationMenus();

            var loanCaseDTO = await _channelService.FindLoanCaseAsync(id, GetServiceHeader());
            ViewBag.LoanApprovalOptionSelectList = GetLoanApprovalOptionSelectList(string.Empty);

            return View(loanCaseDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Approve(LoanCaseDTO loanCaseDTO)
        {
            var loanApprovalOption = loanCaseDTO.LoanApprovalOption;
            loanCaseDTO.ValidateAll();

            if (!loanCaseDTO.HasErrors)
            {
                await _channelService.ApproveLoanCaseAsync(loanCaseDTO, loanApprovalOption, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loanCaseDTO.ErrorMessages;
                ViewBag.LoanApprovalOptionSelectList = GetLoanApprovalOptionSelectList(loanCaseDTO.LoanApprovalOption.ToString());
                return View(loanCaseDTO);
            }
        }
    }
}
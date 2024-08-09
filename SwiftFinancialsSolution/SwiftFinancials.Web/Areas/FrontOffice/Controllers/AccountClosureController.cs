using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class AccountClosureController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        {
            var pageCollectionInfo = await _channelService.FindAccountClosureRequestsByFilterInPageAsync(
                jQueryDataTablesModel.sSearch, 2, jQueryDataTablesModel.iDisplayStart,
                jQueryDataTablesModel.iDisplayLength, false, GetServiceHeader()
            );

            var sortedPageCollection = pageCollectionInfo.PageCollection
                .OrderByDescending(acct => acct.CreatedDate)
                .ToList();

            var totalRecordCount = pageCollectionInfo.ItemsCount;
            var searchRecordCount = string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                ? totalRecordCount
                : sortedPageCollection.Count;

            return this.DataTablesJson(
                items: sortedPageCollection,
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();
            var accountClosureRequestDTO = await _channelService.FindAccountClosureRequestAsync(id, true);
            return View(accountClosureRequestDTO);
        }

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();
            var accountClosureRequestDTO = new AccountClosureRequestDTO();

            if (id.HasValue && id != Guid.Empty)
            {
                var customerAccount = await _channelService.FindCustomerAccountAsync(id.Value, false, false, false, false, GetServiceHeader());
                if (customerAccount != null)
                {
                    accountClosureRequestDTO = PopulateAccountClosureDTO(accountClosureRequestDTO, customerAccount);
                }
            }

            PopulateViewBags(accountClosureRequestDTO);
            return View(accountClosureRequestDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(AccountClosureRequestDTO accountClosureRequestDTO)
        {
            accountClosureRequestDTO.ValidateAll();
            var userDTO = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());

            if (userDTO.BranchId.HasValue)
            {
                accountClosureRequestDTO.BranchId = userDTO.BranchId.Value;
            }
            else
            {
                ModelState.AddModelError("BranchId", "Branch Id is required.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _channelService.AddAccountClosureRequestAsync(accountClosureRequestDTO, GetServiceHeader());
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"An error occurred while saving the data: {ex.Message}");
                }
            }

            PopulateViewBags(accountClosureRequestDTO);
            return View(accountClosureRequestDTO);
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
            var accountClosureRequestDTO = await _channelService.FindAccountClosureRequestAsync(id, true);
            PopulateViewBags(accountClosureRequestDTO);
            return View(accountClosureRequestDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, AccountClosureRequestDTO accountClosureRequestDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateAccountClosureRequestAsync(accountClosureRequestDTO, GetServiceHeader());
                return RedirectToAction("Index");
            }
            PopulateViewBags(accountClosureRequestDTO);
            return View(accountClosureRequestDTO);
        }

        public async Task<ActionResult> Approval(Guid id)
        {
            await ServeNavigationMenus();
            var accountClosureRequestDTO = await _channelService.FindAccountClosureRequestAsync(id, true);
            PopulateViewBags(accountClosureRequestDTO);
            return View(accountClosureRequestDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Approval(Guid id, AccountClosureRequestDTO accountClosureRequestDTO)
        {
            int accountClosureApprovalOption = 0;

            if (ModelState.IsValid)
            {
                await _channelService.ApproveAccountClosureRequestAsync(accountClosureRequestDTO, accountClosureApprovalOption, GetServiceHeader());
                return RedirectToAction("Index");
            }
            PopulateViewBags(accountClosureRequestDTO);
            return View(accountClosureRequestDTO);
        }

        public async Task<ActionResult> Verify(Guid id)
        {
            await ServeNavigationMenus();
            var accountClosureRequestDTO = await _channelService.FindAccountClosureRequestAsync(id, true);
            PopulateViewBags(accountClosureRequestDTO);
            return View(accountClosureRequestDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(Guid id, AccountClosureRequestDTO accountClosureRequestDTO)
        {
            int auditAccountClosureRequestAsync = 0;

            if (ModelState.IsValid)
            {
                await _channelService.AuditAccountClosureRequestAsync(accountClosureRequestDTO, auditAccountClosureRequestAsync, GetServiceHeader());
                return RedirectToAction("Index");
            }
            PopulateViewBags(accountClosureRequestDTO);
            return View(accountClosureRequestDTO);
        }

        private void PopulateViewBags(AccountClosureRequestDTO accountClosureRequestDTO)
        {
            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(accountClosureRequestDTO.CustomerAccountCustomerType.ToString());
        }

        private AccountClosureRequestDTO PopulateAccountClosureDTO(AccountClosureRequestDTO dto, CustomerAccountDTO customerAccount)
        {
            dto.CustomerAccountCustomerId = customerAccount.Id;
            dto.CustomerAccountId = customerAccount.Id;
            dto.CustomerAccountCustomerIndividualFirstName = customerAccount.FullAccountNumber;
            dto.CustomerAccountCustomerIndividualPayrollNumbers = customerAccount.CustomerIndividualPayrollNumbers;
            dto.CustomerAccountCustomerSerialNumber = customerAccount.CustomerSerialNumber;
            dto.CustomerAccountCustomerIndividualIdentityCardNumber = customerAccount.CustomerIndividualIdentityCardNumber;
            dto.CustomerAccountCustomerIndividualLastName = customerAccount.CustomerIndividualFirstName + " " + customerAccount.CustomerIndividualLastName;
            return dto;
        }
    }
}

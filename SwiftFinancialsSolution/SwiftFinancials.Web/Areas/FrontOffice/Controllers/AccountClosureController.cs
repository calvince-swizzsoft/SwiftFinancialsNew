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
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System.Diagnostics;

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




        // GET: FrontOffice/AccountClosureRequest/Create
        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);

            var accountClosureRequestDTO = new AccountClosureRequestDTO();

            if (id != null && id != Guid.Empty && Guid.TryParse(id.ToString(), out Guid parseId))
            {
                // Placeholder for customer details (replace this with actual data as needed)
                // Retrieve customer account details
                var customer = await _channelService.FindCustomerAccountAsync(
                    parseId,
                    includeBalances: false,
                    includeProductDescription: false,
                    includeInterestBalanceForLoanAccounts: false,
                    considerMaturityPeriodForInvestmentAccounts: false,
                    GetServiceHeader()
                );

                if (customer != null)
                {
                    // Populate the DTO with placeholder customer details
                   // accountClosureRequestDTO.CustomerAccountFullAccountNumber = customer.FullAccountNumber;
                    accountClosureRequestDTO.CustomerAccountCustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers;
                    accountClosureRequestDTO.CustomerAccountCustomerIndividualIdentityCardNumber = customer.CustomerIdentificationNumber;
                    accountClosureRequestDTO.CustomerAccountRemarks = customer.Remarks;
                    accountClosureRequestDTO.CustomerAccountTypeTargetProductDescription = customer.CustomerAccountTypeTargetProductDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerNonIndividualDescription = customer.TypeDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerNonIndividualRegistrationNumber = customer.RecordStatusDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerIndividualIdentityCardNumber = customer.CustomerIdentificationNumber;
                    accountClosureRequestDTO.CustomerAccountCustomerReference1 = customer.CustomerReference1;
                    accountClosureRequestDTO.CustomerAccountCustomerReference2 = customer.CustomerReference2;
                    accountClosureRequestDTO.CustomerAccountCustomerReference3 = customer.CustomerReference3;
                    accountClosureRequestDTO.CustomerAccountCustomerReference1 = customer.FullAccountNumber;
                    accountClosureRequestDTO.CustomerAccountCustomerIndividualFirstName = customer.CustomerFullName;
                    accountClosureRequestDTO.CustomerAccountTypeTargetProductDescription = customer.CustomerAccountTypeTargetProductDescription;                   
                    accountClosureRequestDTO.CustomerAccountCustomerId = customer.CustomerId;
                    accountClosureRequestDTO.BranchId = customer.BranchId;
                    accountClosureRequestDTO.CustomerAccountId = customer.Id;
                }

            }

            return View(accountClosureRequestDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(AccountClosureRequestDTO accountClosureRequestDTO)
        {
            // Handle unexpected null DTO
            if (accountClosureRequestDTO == null)
            {
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
                return View("Error");
            }

            // Access the hidden fields
            var branchId = accountClosureRequestDTO.BranchId;
            var customerAccountId = accountClosureRequestDTO.CustomerAccountId;

            // Validate the DTO
            accountClosureRequestDTO.ValidateAll();

            if (!accountClosureRequestDTO.HasErrors)
            {
                // Process the account closure request
                await _channelService.AddAccountClosureRequestAsync(accountClosureRequestDTO, GetServiceHeader());

                TempData["SuccessMessage"] = "Account closure request successfully created.";
                return RedirectToAction("Index");
            }
            else
            {
                // Log errors and return view with validation messages
                foreach (var error in accountClosureRequestDTO.ErrorMessages)
                {
                    Debug.WriteLine($"- {error}");
                }

                TempData["ErrorMessage"] = "There were errors in your submission. Please review the form and try again.";
                return View(accountClosureRequestDTO);
            }
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
            if (!accountClosureRequestDTO.HasErrors)
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
                try
                {
                    await _channelService.ApproveAccountClosureRequestAsync(accountClosureRequestDTO, accountClosureApprovalOption, GetServiceHeader());

                    // Set a success message in TempData
                    TempData["SuccessMessage"] = "Account closure request approved successfully.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Log the exception if needed
                    Debug.WriteLine($"Error approving account closure request: {ex.Message}");

                    // Set an error message in TempData
                    TempData["ErrorMessage"] = "An error occurred while approving the account closure request. Please try again.";
                }
            }
            else
            {
                // Set an error message if the model state is not valid
                TempData["ErrorMessage"] = "There were validation errors. Please review the form and try again.";
            }

            // Repopulate the view bags and return the view with the model
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
            int auditAccountClosureRequestAsync = 1;

            if (!accountClosureRequestDTO.HasErrors)
            {
                try
                {

                    await _channelService.AuditAccountClosureRequestAsync(accountClosureRequestDTO, auditAccountClosureRequestAsync, GetServiceHeader());

                    // Set a success message in TempData
                    TempData["SuccessMessage"] = "Account closure request verified successfully.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Log the exception if needed
                    Debug.WriteLine($"Error verifying account closure request: {ex.Message}");

                    // Set an error message in TempData
                    TempData["ErrorMessage"] = "An error occurred while verifying the account closure request. Please try again.";
                }
            }
            else
            {
                // Set an error message if there are validation errors
                TempData["ErrorMessage"] = "There were validation errors. Please review the form and try again.";
            }

            // Repopulate the view bags and return the view with the model
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


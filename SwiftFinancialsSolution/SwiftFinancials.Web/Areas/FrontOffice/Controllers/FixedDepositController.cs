using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class FixedDepositController : MasterController
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

            bool includeProductDescription = false;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindFixedDepositsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, includeProductDescription, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(postingPeriod => postingPeriod.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<FixedDepositDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var fixedDepositDTO = await _channelService.FindFixedDepositAsync(id, GetServiceHeader());

            return View(fixedDepositDTO);
        }


        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            bool includeBalances = false;
            bool includeProductDescription = false;
            bool includeInterestBalanceForLoanAccounts = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;

            var customer = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            FixedDepositDTO fixedDepositDTO = new FixedDepositDTO();

            if (customer != null)
            {
                // Populate the DTO with placeholder customer details
                // accountClosureRequestDTO.CustomerAccountFullAccountNumber = customer.FullAccountNumber;
                fixedDepositDTO.CustomerAccountCustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers;
                fixedDepositDTO.CustomerAccountCustomerIndividualIdentityCardNumber = customer.CustomerIdentificationNumber;
                fixedDepositDTO.Remarks = customer.Remarks;
                fixedDepositDTO.ProductDescription = customer.CustomerAccountTypeTargetProductDescription;
                fixedDepositDTO.CustomerAccountCustomerNonIndividualDescription = customer.TypeDescription;
                fixedDepositDTO.CustomerAccountCustomerNonIndividualRegistrationNumber = customer.RecordStatusDescription;
                fixedDepositDTO.CustomerAccountCustomerIndividualIdentityCardNumber = customer.CustomerIdentificationNumber;
                fixedDepositDTO.CustomerAccountCustomerReference1 = customer.CustomerReference1;
                fixedDepositDTO.CustomerAccountCustomerReference2 = customer.CustomerReference2;
                fixedDepositDTO.CustomerAccountCustomerReference3 = customer.CustomerReference3;
                fixedDepositDTO.CustomerAccountCustomerReference1 = customer.FullAccountNumber;
                fixedDepositDTO.CustomerAccountCustomerIndividualFirstName = customer.CustomerFullName;
                fixedDepositDTO.CustomerAccountCustomerId = customer.CustomerId;
                fixedDepositDTO.BranchId = customer.BranchId;
                fixedDepositDTO.CustomerAccountId = customer.Id;
                
            }

            var fixedDepositTypeDTO = await _channelService.FindFixedDepositTypeAsync(parseId, GetServiceHeader());

            FixedDepositTypeDTO fixedDepositTypeDTOs = new FixedDepositTypeDTO();

            if (fixedDepositTypeDTO != null)
            {
                fixedDepositTypeDTOs.Id = fixedDepositTypeDTO.Id;
                fixedDepositTypeDTOs.Description = fixedDepositTypeDTO.Description;
                fixedDepositTypeDTOs.Months = fixedDepositTypeDTO.Months;
                fixedDepositTypeDTOs.IsLocked = fixedDepositTypeDTO.IsLocked;
                fixedDepositTypeDTOs.CreatedDate = fixedDepositTypeDTO.CreatedDate;
            }

            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);
            ViewBag.customertypeSelectList = GetCustomerTypeSelectList(string.Empty);

            return View(fixedDepositDTO);
        }



        [HttpPost]
        public async Task<ActionResult> Add(FixedDepositDTO fixedDepositDTO)
        {
            await ServeNavigationMenus();

            FixedDepositPayableDTOs = TempData["FixedDepositPayableDTO"] as ObservableCollection<FixedDepositPayableDTO>;

            if (FixedDepositPayableDTOs == null)
                FixedDepositPayableDTOs = new ObservableCollection<FixedDepositPayableDTO>();

            foreach (var fixedDepositPayableDTO in fixedDepositDTO.FixedDepositPayables)
            {

                fixedDepositPayableDTO.CustomerAccountTypeTargetProductChartOfAccountName = fixedDepositPayableDTO.CustomerAccountTypeTargetProductChartOfAccountName;
                fixedDepositPayableDTO.CustomerAccountTypeTargetProductDescription = fixedDepositPayableDTO.CustomerAccountTypeTargetProductDescription;
                fixedDepositPayableDTO.CustomerAccountTypeTargetProductProductSection = fixedDepositPayableDTO.CustomerAccountTypeTargetProductProductSection;

                FixedDepositPayableDTOs.Add(fixedDepositPayableDTO);
            };

            TempData["FixedDepositPayableDTO"] = FixedDepositPayableDTOs;

            TempData["FixedDepositPayableDTO"] = fixedDepositDTO;

            ViewBag.ExpensePayableEntryDTOs = ExpensePayableEntryDTOs;
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(fixedDepositDTO.CustomerAccountCustomerType.ToString());
            ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(fixedDepositDTO.CustomerAccountCustomerType.ToString());
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(fixedDepositDTO.CustomerAccountCustomerType.ToString());
            return View("Create", fixedDepositDTO);
        }
        [HttpPost]
        public async Task<ActionResult> Create(FixedDepositDTO fixedDepositDTO)
        {
            // Handle unexpected null DTO
            if (fixedDepositDTO == null)
            {
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
                return View("Error");
            }

            // Access the hidden fields
            var branchId = fixedDepositDTO.BranchId;
            var customerAccountId = fixedDepositDTO.CustomerAccountId;

            // Validate the DTO
            fixedDepositDTO.ValidateAll();

            if (!fixedDepositDTO.HasErrors)
            {
                try
                {
                    // Process the fixed deposit creation
                    int moduleNavigationItemCode = 0;
                    await _channelService.PayFixedDepositAsync(fixedDepositDTO, moduleNavigationItemCode, GetServiceHeader());

                    TempData["SuccessMessage"] = "Fixed Deposit created successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    // Log unexpected errors
                    TempData["ErrorMessage"] = "An unexpected error occurred while processing your request. Please try again.";
                    return View("Error");
                }
            }
            else
            {
                // Log validation errors
                foreach (var error in fixedDepositDTO.ErrorMessages)
                {
                }

                TempData["ErrorMessage"] = "There were errors in your submission. Please review the form and try again.";
                return View(fixedDepositDTO);
            }
        }




        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var accountClosureRequestDTO = await _channelService.FindAccountClosureRequestAsync(id, true);

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
            else
            {
                return View(accountClosureRequestDTO);
            }
        }



        public async Task<ActionResult> Approval(Guid id)
        {
            await ServeNavigationMenus();

            var accountClosureRequestDTO = await _channelService.FindAccountClosureRequestAsync(id, true);

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
            else
            {
                return View(accountClosureRequestDTO);
            }
        }





        public async Task<ActionResult> verify(Guid id)
        {
            await ServeNavigationMenus();

            var accountClosureRequestDTO = await _channelService.FindAccountClosureRequestAsync(id, true);

            return View(accountClosureRequestDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> verify(Guid id, AccountClosureRequestDTO accountClosureRequestDTO)
        {
            int AuditAccountClosureRequestAsync = 0;

            if (ModelState.IsValid)

            {
                await _channelService.ApproveAccountClosureRequestAsync(accountClosureRequestDTO, AuditAccountClosureRequestAsync, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(accountClosureRequestDTO);
            }
        }


        public async Task<ActionResult> Search(Guid? id)
        {
            //string Remarks = "";
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            bool includeBalances = false;
            bool includeProductDescription = false;
            bool includeInterestBalanceForLoanAccounts = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;

            var customer = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            FixedDepositDTO fixedDepositDTO = new FixedDepositDTO();

            if (customer != null)
            {
                fixedDepositDTO.CustomerAccountCustomerId = customer.Id;
                fixedDepositDTO.CustomerAccountId = customer.Id;
                fixedDepositDTO.CustomerAccountCustomerIndividualFirstName = customer.CustomerIndividualFirstName;
                fixedDepositDTO.CustomerAccountCustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers;
                fixedDepositDTO.CustomerAccountCustomerSerialNumber = customer.CustomerSerialNumber;
                fixedDepositDTO.CustomerAccountCustomerIndividualIdentityCardNumber = customer.CustomerIndividualIdentityCardNumber;
            }

            var fixedDepositTypeDTO = await _channelService.FindFixedDepositTypeAsync(parseId, GetServiceHeader());

            FixedDepositTypeDTO fixedDepositTypeDTOs = new FixedDepositTypeDTO();

            if (fixedDepositTypeDTO != null)
            {
                fixedDepositTypeDTOs.Id = fixedDepositTypeDTO.Id;
                fixedDepositTypeDTOs.Description = fixedDepositTypeDTO.Description;
                fixedDepositTypeDTOs.Months = fixedDepositTypeDTO.Months;
                fixedDepositTypeDTOs.IsLocked = fixedDepositTypeDTO.IsLocked;
                fixedDepositTypeDTOs.CreatedDate = fixedDepositTypeDTO.CreatedDate;
            }

            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);
            ViewBag.customertypeSelectList = GetCustomerTypeSelectList(string.Empty);

            if (Session["Description"] != null)
            {
                fixedDepositTypeDTOs.Description = Session["Description"].ToString();
            }
            if (Session["Remarks"] != null)
            {
                fixedDepositDTO.Remarks = Session["Remarks"].ToString();
            }
            if (Session["CustomerAccountCustomerIndividualFirstName"] != null)
            {
                fixedDepositDTO.CustomerAccountCustomerIndividualFirstName = Session["CustomerAccountCustomerIndividualFirstName"].ToString();
            }
            //


            //TempData["WithdrawalNotificationDTOs"] = withdrawalNotificationDTO;
            return View("Create", fixedDepositTypeDTOs);
        }

        [HttpPost]
        public ActionResult AssignText(string Remarks, string Description)
        {

            Session["Description"] = Description;
            return null;
        }

    }
}
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

                fixedDepositPayableDTO.CustomerAccountTypeTargetProductChartOfAccountId = fixedDepositDTO.Id;
                fixedDepositPayableDTO.CustomerAccountTypeTargetProductChartOfAccountName = fixedDepositPayableDTO.CustomerAccountTypeTargetProductChartOfAccountName;
                fixedDepositPayableDTO.CustomerAccountBranchId = fixedDepositPayableDTO.CustomerAccountBranchId;
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
            fixedDepositDTO.ValidateAll();

            int moduleNavigationItemCode = 0;

            if (!fixedDepositDTO.HasErrors)
            {
                await _channelService.PayFixedDepositAsync(fixedDepositDTO, moduleNavigationItemCode, GetServiceHeader());

                ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(fixedDepositDTO.CustomerAccountCustomerType.ToString());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = fixedDepositDTO.ErrorMessages;

                return View();
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

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
using System.Diagnostics;



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
            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            bool sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc";
            bool includeProductDescription = true; 

            var pageCollectionInfo = await _channelService.FindFixedDepositsByFilterInPageAsync(
                jQueryDataTablesModel.sSearch,
                pageIndex,
                jQueryDataTablesModel.iDisplayLength,
                includeProductDescription,
                GetServiceHeader()
            );

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection
                    .OrderByDescending(postingPeriod => postingPeriod.CreatedDate)
                    .ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? pageCollectionInfo.PageCollection.Count
                    : totalRecordCount;

                return this.DataTablesJson(
                    items: pageCollectionInfo.PageCollection,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
            else
            {
                return this.DataTablesJson(
                    items: new List<FixedDepositDTO>(),
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
        }


        [HttpPost]
        public async Task<ActionResult> FindExternalChequesByDate(JQueryDataTablesModel jQueryDataTablesModel, DateTime startDate, DateTime endDate)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;
            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;

            bool sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc";
            int status = 1; // Define the status as per your requirements
            bool includeProductDescription = true; // Set this based on your requirements

            // Call the service with adapted parameters
            var pageCollectionInfo = await _channelService.FindFixedDepositsByStatusAndFilterInPageAsync(
                status,
                jQueryDataTablesModel.sSearch,
                0,
                int.MaxValue,
                includeProductDescription,
                GetServiceHeader()
            );

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(fixedDepositDTO => fixedDepositDTO.CreatedDate)
                    .ToList();

                totalRecordCount = sortedData.Count;

                var paginatedData = sortedData
                    .Skip(jQueryDataTablesModel.iDisplayStart)
                    .Take(jQueryDataTablesModel.iDisplayLength)
                    .ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? sortedData.Count
                    : totalRecordCount;

                return this.DataTablesJson(
                    items: paginatedData,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }

            return this.DataTablesJson(
                items: new List<FixedDepositDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
        );
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

            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
            ViewBag.FixedDepositCategorySelectList = GetFixedDepositCategorySelectList(string.Empty);
            ViewBag.FixedDepositMaturityActionSelectList = GetFixedDepositMaturityActionSelectList(string.Empty);
            ViewBag.FixedDepositMaturityActionSelectList = GetFixedDepositMaturityActionSelectList(string.Empty);

            FixedDepositDTO fixedDepositDTO = new FixedDepositDTO();

            if (id.HasValue && id != Guid.Empty)
            {
                var parseId = id.Value;

                var fixedDepositType = await _channelService.FindFixedDepositTypeAsync(parseId, GetServiceHeader());

                if (fixedDepositType != null)
                {
                    fixedDepositDTO.FixedDepositTypeId = fixedDepositType.Id;
                    fixedDepositDTO.FixedDepositTypeDescription = fixedDepositType.Description;
                   


                }
                else
                {
                    TempData["ErrorMessage"] = "Fixed deposit type details could not be found.";
                    return RedirectToAction("Index");
                }
            }

            return View(fixedDepositDTO);
        }



        [HttpGet]
        public async Task<ActionResult> GetCustomerAccountDetails(Guid customerAccountId)
        {
            try
            {
                var customerAccount = await _channelService.FindCustomerAccountAsync(
                    customerAccountId,
                    includeBalances: true,
                    includeProductDescription: true,
                    includeInterestBalanceForLoanAccounts: true,
                    considerMaturityPeriodForInvestmentAccounts: true,
                    GetServiceHeader()
                );

                if (customerAccount == null)
                {
                    return Json(new { success = false, message = "Customer account not found." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        CustomerAccountFullAccountNumber = customerAccount.FullAccountNumber,
                        CustomerAccountStatusDescription = customerAccount.StatusDescription,
                        AvailableBalance = customerAccount.AvailableBalance,
                        CustomerAccountId = customerAccount.Id,
                        CustomerAccountCustomerId = customerAccount.CustomerId,
                        CustomerAccountCustomerFullName = customerAccount.CustomerFullName,
                        CustomerAccountCustomerIndividualPayrollNumbers = customerAccount.CustomerIndividualPayrollNumbers,
                        CustomerAccountCustomerIndividualIdentityCardNumber = customerAccount.CustomerIndividualIdentityCardNumber,
                        CustomerAccountCustomerReference1 = customerAccount.CustomerReference1,
                        CustomerAccountCustomerReference2 = customerAccount.CustomerReference2,
                        CustomerAccountCustomerReference3 = customerAccount.CustomerReference3,
                        CustomerAccountBranchId = customerAccount.BranchId,
                        BranchDescription = customerAccount.BranchDescription,
                        BranchId = customerAccount.BranchId,
                        ProductChartOfAccountId = customerAccount.CustomerAccountTypeTargetProductChartOfAccountId,
                        CustomerAccountCustomerAccountTypeTargetProductId = customerAccount.CustomerAccountTypeTargetProductId,
                        CustomerAccountBranchCode = customerAccount.BranchCode,
                        CustomerAccountCustomerAccountTypeProductCode = customerAccount.CustomerAccountTypeProductCode,
                        CustomerAccountCustomerAccountTypeTargetProductCode = customerAccount.CustomerAccountTypeTargetProductCode,
                        CustomerAccountCustomerType = customerAccount. CustomerType,
                        CustomerAccountCustomerTypeDescription = customerAccount.CustomerTypeDescription,
                        CustomerAccountStatus = customerAccount.Status,
                        CustomerAccountCustomerIndividualSalutationDescription = customerAccount.CustomerIndividualSalutationDescription,
                        CustomerAccountCustomerIndividualFirstName = customerAccount.CustomerIndividualFirstName,
                        CustomerAccountCustomerIndividualLastName = customerAccount.CustomerIndividualLastName,
                        CustomerAccountCustomerNonIndividualDescription = customerAccount.CustomerNonIndividualDescription,
                        CustomerAccountCustomerNonIndividualRegistrationNumber = customerAccount.CustomerNonIndividualRegistrationNumber,
                        CustomerAccountCustomerPersonalIdentificationNumber = customerAccount.CustomerPersonalIdentificationNumber,
                        CustomerAccountCustomerNonIndividualDateEstablished = customerAccount.CustomerNonIndividualDateEstablished,
                        CustomerAccountCustomerIdentificationNumber = customerAccount.CustomerIdentificationNumber,
                        ProductDescription = customerAccount.CustomerAccountTypeTargetProductDescription,
                        ProductChartOfAccountCode = customerAccount.CustomerAccountTypeTargetProductChartOfAccountCode,
                        ProductChartOfAccountName = customerAccount.CustomerAccountTypeTargetProductChartOfAccountName,

                        account1 = customerAccount.BookBalance,
                        account2 = customerAccount.PrincipalBalance,
                        account3 = customerAccount.InterestBalance,

                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while fetching the customer account details." }, JsonRequestBehavior.AllowGet);
            }
        }




        [HttpPost]
        public async Task<ActionResult> Create(FixedDepositDTO fixedDepositDTO)
        {
            if (fixedDepositDTO == null)
            {
                TempData["SweetAlertMessage"] = "An unexpected error occurred. Please try again.";
                TempData["SweetAlertType"] = "error";
                return RedirectToAction("Index");
            }

            fixedDepositDTO.ValidateAll();

            if (!fixedDepositDTO.HasErrors)
            {
                try
                {
                    var createdOfficer = await _channelService.InvokeFixedDepositAsync(fixedDepositDTO, GetServiceHeader());

                    TempData["SweetAlertMessage"] = "Fixed deposit created successfully.";
                    TempData["SweetAlertType"] = "success";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error creating Fixed Deposit: {ex.Message}");
                    TempData["SweetAlertMessage"] = "An error occurred while creating the Fixed Deposit. Please try again.";
                    TempData["SweetAlertType"] = "error";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                foreach (var error in fixedDepositDTO.ErrorMessages)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                TempData["SweetAlertMessage"] = "There were errors in your submission. Please review the form and try again.";
                TempData["SweetAlertType"] = "warning";
                return RedirectToAction("Index");
            }
        }







        [HttpPost]
        public async Task<ActionResult> Add(FixedDepositPayableDTO fixedDepositPayableDTO, FormCollection form)
        {
            await ServeNavigationMenus();

            ObservableCollection<FixedDepositPayableDTO> fixedDepositPayableDTOs = TempData["FixedDepositPayableDTOs"] as ObservableCollection<FixedDepositPayableDTO>
                ?? new ObservableCollection<FixedDepositPayableDTO>();

            var selectedAccounts = form.GetValues("selectedAccounts");

            if (selectedAccounts != null)
            {
                foreach (var accountNumber in selectedAccounts)
                {
                    var accountDetails = form["accountDetails"].Split(',');

                    var newEntry = new FixedDepositPayableDTO
                    {
                        CustomerAccountCustomerNonIndividualRegistrationNumber = accountNumber,
                        CustomerAccountTypeTargetProductDescription = accountDetails[1],
                        BookBalance = Convert.ToDecimal(accountDetails[2]),
                        PrincipalBalance = Convert.ToDecimal(accountDetails[3]),
                        InterestBalance = Convert.ToDecimal(accountDetails[4])
                    };

                    if (string.IsNullOrWhiteSpace(newEntry.FullAccountNumber) ||
                        string.IsNullOrWhiteSpace(newEntry.CustomerAccountTypeTargetProductDescription) ||
                        newEntry.BookBalance <= 0 ||
                        newEntry.PrincipalBalance <= 0 ||
                        newEntry.InterestBalance <= 0)
                    {
                        TempData["ErrorMessage"] = "Could not add Fixed Deposit Payable Entry. Ensure all required fields are entered.";
                    }
                    else
                    {
                        fixedDepositPayableDTOs.Add(newEntry);
                    }
                }
                TempData["FixedDepositPayableDTOs"] = fixedDepositPayableDTOs;
                Session["FixedDepositPayableEntries"] = fixedDepositPayableDTOs;
                TempData["EntryAdded"] = "Successfully added entries.";
            }

            var fixedDepositDTO = new FixedDepositDTO
            {
            };

            ViewBag.FixedDepositPayableDTOs = fixedDepositPayableDTOs;

            return View("Create", fixedDepositDTO);
        }




        public async Task<ActionResult> Verify(Guid id)
        {
            await ServeNavigationMenus();

            var fixedDepositDTO = await _channelService.FindFixedDepositAsync(id, GetServiceHeader());

            return View(fixedDepositDTO);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(Guid id, FixedDepositDTO fixedDepositDTO, string fixedDepositAuth)
        {
            int fixedDepositAuthOption = fixedDepositAuth == "Post"
                ? (int)FixedDepositAuthOption.Post
                : (int)FixedDepositAuthOption.Reject;

            int moduleNavigationItemCode = 1; // Replace with the actual code if necessary

            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _channelService.AuditFixedDepositAsync(
                        fixedDepositDTO,
                        fixedDepositAuthOption,
                        moduleNavigationItemCode,
                        GetServiceHeader()
                    );

                    if (result)
                    {
                        TempData["SweetAlertMessage"] = fixedDepositAuth == "Post"
                            ? "Account fixing posted successfully."
                            : "Account fixing rejected successfully.";
                        TempData["SweetAlertType"] = "success";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["SweetAlertMessage"] = "Failed to process the account fixing request. Please try again.";
                        TempData["SweetAlertType"] = "error";
                        return RedirectToAction("Index");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error processing account fixing: {ex.Message}");
                    TempData["SweetAlertMessage"] = "An error occurred while processing the account fixing. Please try again.";
                    TempData["SweetAlertType"] = "error";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                TempData["SweetAlertMessage"] = "The available balance is below zero.";
                TempData["SweetAlertType"] = "warning";
                return RedirectToAction("Index");
            }
        }









        public async Task<ActionResult> Terminate(Guid id, DateTime? startDate = null, DateTime? endDate = null, string searchText = null)
        {
            await ServeNavigationMenus();

            var fixedDepositDTO = await _channelService.FindFixedDepositAsync(id, GetServiceHeader());

            if (fixedDepositDTO != null)
            {
                if (startDate.HasValue && endDate.HasValue)
                {
                    if (!(fixedDepositDTO.CreatedDate >= startDate.Value && fixedDepositDTO.MaturityDate <= endDate.Value))
                    {
                        fixedDepositDTO = null;
                    }
                }

                if (!string.IsNullOrEmpty(searchText) && !fixedDepositDTO.ProductDescription.Contains(searchText))
                {
                    fixedDepositDTO = null;
                }
            }

            if (Request.IsAjaxRequest())
            {
                return Json(new[] { fixedDepositDTO }, JsonRequestBehavior.AllowGet);
            }

            ViewBag.ModuleNavigationItemCode = 123;
            ViewBag.FixedDeposit = fixedDepositDTO;

            return View(fixedDepositDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Terminate(Guid[] selectedFixedDepositIds, int moduleNavigationItemCode)
        {
            if (selectedFixedDepositIds == null || selectedFixedDepositIds.Length == 0)
            {
                TempData["SweetAlertMessage"] = "No fixed deposit selected for termination.";
                TempData["SweetAlertType"] = "warning";
                return RedirectToAction("Terminate");
            }

            var fixedDeposits = new List<FixedDepositDTO>();
            foreach (var id in selectedFixedDepositIds)
            {
                var fixedDepositDTO = await _channelService.FindFixedDepositAsync(id, GetServiceHeader());
                if (fixedDepositDTO != null)
                {
                    fixedDeposits.Add(fixedDepositDTO);
                }
            }

            if (!fixedDeposits.Any())
            {
                TempData["SweetAlertMessage"] = "The selected fixed deposit(s) could not be found.";
                TempData["SweetAlertType"] = "warning";
                return RedirectToAction("Terminate");
            }

            var selectedFixedDepositObservableCollection = new ObservableCollection<FixedDepositDTO>(fixedDeposits);

            try
            {
                bool result = await _channelService.RevokeFixedDepositsAsync(
                    selectedFixedDepositObservableCollection,
                    moduleNavigationItemCode,
                    GetServiceHeader()
                );

                if (result)
                {
                    TempData["SweetAlertMessage"] = "The selected fixed deposit(s) were successfully terminated.";
                    TempData["SweetAlertType"] = "success";
                    return RedirectToAction("Index", "FixedDeposit", new { Area = "FrontOffice" });
                }
                else
                {
                    TempData["SweetAlertMessage"] = "An error occurred while terminating the selected fixed deposit(s).";
                    TempData["SweetAlertType"] = "error";
                    return RedirectToAction("Terminate");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error terminating fixed deposits: {ex.Message}");
                TempData["SweetAlertMessage"] = "An unexpected error occurred while terminating the fixed deposit(s). Please try again.";
                TempData["SweetAlertType"] = "error";
                return RedirectToAction("Terminate");
            }
        }





        public async Task<ActionResult> Liquidation(Guid id, DateTime? startDate = null, DateTime? endDate = null, string searchText = null)
        {
            await ServeNavigationMenus();

            var fixedDepositDTO = await _channelService.FindFixedDepositAsync(id, GetServiceHeader());

            if (fixedDepositDTO != null)
            {
                if (startDate.HasValue && endDate.HasValue)
                {
                    if (!(fixedDepositDTO.CreatedDate >= startDate.Value && fixedDepositDTO.MaturityDate <= endDate.Value))
                    {
                        fixedDepositDTO = null;
                    }
                }

                if (!string.IsNullOrEmpty(searchText) && !fixedDepositDTO.ProductDescription.Contains(searchText))
                {
                    fixedDepositDTO = null;
                }
            }

            if (Request.IsAjaxRequest())
            {
                return Json(new[] { fixedDepositDTO }, JsonRequestBehavior.AllowGet);
            }
            ViewBag.ModuleNavigationItemCode = 123;


            ViewBag.FixedDeposit = fixedDepositDTO;
            return View(fixedDepositDTO);
        }



        [HttpPost]
        public async Task<ActionResult> Liquidation(Guid[] selectedFixedDepositIds, int moduleNavigationItemCode)
        {
            if (selectedFixedDepositIds == null || selectedFixedDepositIds.Length == 0)
            {
                TempData["SweetAlertMessage"] = "No fixed deposit selected for liquidation.";
                TempData["SweetAlertType"] = "warning";
                return RedirectToAction("Liquidation");
            }

            var fixedDeposits = new List<FixedDepositDTO>();
            foreach (var id in selectedFixedDepositIds)
            {
                var fixedDepositDTO = await _channelService.FindFixedDepositAsync(id, GetServiceHeader());
                if (fixedDepositDTO != null)
                {
                    if (fixedDepositDTO.MaturityDate > DateTime.Now)
                    {
                        TempData["SweetAlertMessage"] = $"The fixed deposit with account number {fixedDepositDTO.CustomerAccountFullAccountNumber} has not yet reached maturity and cannot be liquidated.";
                        TempData["SweetAlertType"] = "warning";
                        return RedirectToAction("Liquidation");
                    }
                    fixedDeposits.Add(fixedDepositDTO);
                }
            }

            if (!fixedDeposits.Any())
            {
                TempData["SweetAlertMessage"] = "The selected fixed deposit(s) could not be found.";
                TempData["SweetAlertType"] = "warning";
                return RedirectToAction("Liquidation");
            }

            foreach (var fixedDepositDTO in fixedDeposits)
            {
                try
                {
                    bool result = await _channelService.PayFixedDepositAsync(fixedDepositDTO, moduleNavigationItemCode, GetServiceHeader());
                    if (!result)
                    {
                        TempData["SweetAlertMessage"] = $"An error occurred while terminating the fixed deposit with ID {fixedDepositDTO.Id}.";
                        TempData["SweetAlertType"] = "error";
                        return RedirectToAction("Liquidation");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error liquidating fixed deposit {fixedDepositDTO.Id}: {ex.Message}");
                    TempData["SweetAlertMessage"] = $"An unexpected error occurred while liquidating the fixed deposit with ID {fixedDepositDTO.Id}.";
                    TempData["SweetAlertType"] = "error";
                    return RedirectToAction("Liquidation");
                }
            }

            TempData["SweetAlertMessage"] = "The selected fixed deposit(s) were successfully liquidated.";
            TempData["SweetAlertType"] = "success";
            return RedirectToAction("Index", "FixedDeposit", new { Area = "FrontOffice" });
        }




        public async Task<ActionResult> Catalogue()
        {
            await ServeNavigationMenus();
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Catalogue(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindFixedDepositsByFilterInPageAsync(
                   jQueryDataTablesModel.sSearch,
                   jQueryDataTablesModel.iDisplayStart,
                   jQueryDataTablesModel.iDisplayLength,
                   includeProductDescription: true,
                   GetServiceHeader()
               );
            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;


                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(expensePayable => expensePayable.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<FixedDepositDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }





    }
}
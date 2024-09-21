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
using System.Collections.ObjectModel;


namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class InHouseController : MasterController
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

            var pageCollectionInfo = await _channelService.FindInHouseChequesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(postingPeriod => postingPeriod.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<InHouseChequeDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }





        public async Task<ActionResult> Create(Guid? id, bool isCustomerAccount = false)
        {
            await ServeNavigationMenus();

            InHouseChequeDTO inHouseChequeDTO = new InHouseChequeDTO();

            // If an ID is provided, determine whether to fetch a customer account or G/L account based on the 'isCustomerAccount' flag
            if (id.HasValue && id != Guid.Empty)
            {
                if (isCustomerAccount)
                {
                    // Fetch Customer Account details if the flag is set to true
                    var customerAccount = await _channelService.FindCustomerAccountAsync(
                        id.Value,
                        includeBalances: true,
                        includeProductDescription: true,
                        includeInterestBalanceForLoanAccounts: true,
                        considerMaturityPeriodForInvestmentAccounts: true,
                        GetServiceHeader()
                    );

                    if (customerAccount != null)
                    {
                        // Populate the DTO with relevant customer account data
                        inHouseChequeDTO.DebitChartOfAccountAccountName = customerAccount.CustomerReference1;
                        inHouseChequeDTO.Amount = customerAccount.AvailableBalance;
                        inHouseChequeDTO.BranchDescription = customerAccount.BranchDescription;
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Customer account details could not be found.";
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    // Fetch G/L Account details if the flag is not set (default behavior)
                    var chartOfAccount = await _channelService.FindChartOfAccountAsync(id.Value, GetServiceHeader());

                    if (chartOfAccount != null)
                    {
                        inHouseChequeDTO.DebitChartOfAccountAccountName = chartOfAccount.ParentAccountName;
                        inHouseChequeDTO.CostCenter = chartOfAccount.CostCenterDescription;
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "G/L Account details could not be found.";
                        return RedirectToAction("Index");
                    }
                }
            }

            return View(inHouseChequeDTO);
        }




        public async Task<ActionResult> GetGLAccounts()
        {
            try
            {
                var glAccounts = await _channelService.FindChartOfAccountsAsync(GetServiceHeader());

                // Ensure that you are returning the data in the correct format
                return Json(new
                {
                    draw = Request["draw"],
                    recordsTotal = glAccounts.Count(),
                    recordsFiltered = glAccounts.Count(), 
                    data = glAccounts.Select(a => new
                    {
                        a.ParentAccountName,
                        a.CostCenterDescription,
                        a.Id
                    })
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Log the error (optional)
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }




        public async Task<ActionResult> GetCustomerAccounts(Guid customerId)
        {
            try
            {
                // Call the service to retrieve the customer accounts by customer ID
                var customerAccounts = await _channelService.FindCustomerAccountsByCustomerIdAsync(
                    customerId,
                    includeBalances: true,
                    includeProductDescription: true,
                    includeInterestBalanceForLoanAccounts: true,
                    considerMaturityPeriodForInvestmentAccounts: true,
                    GetServiceHeader()
                );

                if (customerAccounts == null || !customerAccounts.Any())
                {
                    return Json(new { error = "No customer accounts found for the specified customer." }, JsonRequestBehavior.AllowGet);
                }

                var customerAccountsList = customerAccounts.Select(account => new
                {
                    CustomerReference1 = account.CustomerReference1,
                    AvailableBalance = account.AvailableBalance,
                    CustomerFullName = account.CustomerFullName,
                    BranchDescription = account.BranchDescription,
                    ProductName = account.CustomerAccountTypeProductCodeDescription,
                    Membership = account.CustomerReference2,
                    CreatedDate = account.CreatedDate.ToString("dd/MM/yyyy HH:mm:ss"),
                    SelectButton = $"<a href='#' onclick='selectCustomerAccount(\"{account.CustomerReference1}\", \"{account.AvailableBalance}\")' class='btn btn-outline-info'>Select</a>"
                }).ToList();

                return Json(new { data = customerAccountsList }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = $"An error occurred while retrieving customer accounts: {ex.Message}" }, JsonRequestBehavior.AllowGet);
            }
        }






        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(InHouseChequeDTO inHouseChequeDTO, int moduleNavigationItemCode)
        {
            // 1. Validate the model
            if (inHouseChequeDTO == null)
            {
                ModelState.AddModelError("", "Invalid cheque data.");
                return await ReloadCreateView(inHouseChequeDTO); // Return the view with error message and data reload
            }

            // 2. Perform server-side validation
            if (string.IsNullOrEmpty(inHouseChequeDTO.Payee))
            {
                ModelState.AddModelError("", "Payee name is required.");
            }

            if (inHouseChequeDTO.Amount <= 0)
            {
                ModelState.AddModelError("", "Invalid cheque amount.");
            }

            if (inHouseChequeDTO.DebitCustomerAccountId == Guid.Empty && inHouseChequeDTO.DebitChartOfAccountAccountName == null)
            {
                ModelState.AddModelError("", "Either Customer Account or G/L Account must be selected.");
            }

            // If validation fails, return the view with errors
            if (!ModelState.IsValid)
            {
                return await ReloadCreateView(inHouseChequeDTO); // Reload view with validation errors
            }

            // 3. Call the service method to add the cheque
            try
            {
                ServiceHeader serviceHeader = CreateServiceHeader();
                var result = await _channelService.AddInHouseChequeAsync(inHouseChequeDTO, moduleNavigationItemCode, serviceHeader);

                if (result != null)
                {
                    TempData["SuccessMessage"] = "Cheque added successfully.";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Failed to add the cheque.");
                }
            }
            catch (Exception ex)
            {
                // Log the exception (optional)
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
            }

            return await ReloadCreateView(inHouseChequeDTO);
        }


        private async Task<ActionResult> ReloadCreateView(InHouseChequeDTO inHouseChequeDTO)
        {
            // Reload customer account data in case of failure or validation issues
            await ServeNavigationMenus();

            // Reload any dropdowns or data needed for the view
            var customerAccountsList = new List<SelectListItem>();

            // Simulate fetching account data (you might need to re-query based on your business logic)
            if (inHouseChequeDTO.DebitCustomerAccountId.HasValue && inHouseChequeDTO.DebitCustomerAccountId != Guid.Empty)
            {
                // Call FindCustomerAccountAsync with the non-nullable Guid value
                var customerAccountDTO = await _channelService.FindCustomerAccountAsync(
                    inHouseChequeDTO.DebitCustomerAccountId.Value, 
                    includeBalances: true,
                    includeProductDescription: true,
                    includeInterestBalanceForLoanAccounts: true,
                    considerMaturityPeriodForInvestmentAccounts: true,
                    GetServiceHeader()
                );

                if (customerAccountDTO != null)
                {
                    // Populate customer account dropdown
                    customerAccountsList.Add(new SelectListItem
                    {
                        Text = customerAccountDTO.CustomerFullName,
                        Value = customerAccountDTO.CustomerId.ToString()
                    });

                    // You can also update the DTO fields, e.g.:
                    inHouseChequeDTO.DebitCustomerAccountBranchCode = customerAccountDTO.BranchCode;
                    inHouseChequeDTO.DebitCustomerAccountCustomerSerialNumber = customerAccountDTO.CustomerSerialNumber;
                }
                else
                {
                    TempData["ErrorMessage"] = "Customer account details could not be found.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid Customer Account ID.";
            }

            // Reload the ViewBag data for the dropdowns
            ViewBag.CustomerAccounts = new SelectList(customerAccountsList, "Value", "Text");

            // Return the view with the DTO
            return View("Create", inHouseChequeDTO);
        }


        private ServiceHeader CreateServiceHeader()
        {
            // Implement your logic to create a service header
            return new ServiceHeader();
        }

        public async Task<ActionResult> Printing(Guid? id)
        {
            await ServeNavigationMenus();

            var inHouseChequeDTO = new InHouseChequeDTO();

            if (id.HasValue && id != Guid.Empty)
            {
                var parseId = id.Value;

                var bankLinkageDTO = await _channelService.FindBankLinkageAsync(parseId, GetServiceHeader());

                if (bankLinkageDTO != null)
                {
                    inHouseChequeDTO.BankName = bankLinkageDTO.BankName;
                    inHouseChequeDTO.BranchDescription = bankLinkageDTO.BankBranchName;
                    inHouseChequeDTO.DebitChartOfAccountAccountName = bankLinkageDTO.ChartOfAccountAccountName;
                    inHouseChequeDTO.BankAccount = bankLinkageDTO.BankAccountNumber;

                    return View(inHouseChequeDTO);
                }
                else
                {
                    TempData["ErrorMessage"] = "Bank details could not be found.";
                    return Json(new { Error = "Bank details could not be found." });
                }
            }

            return View();
        }
        //[HttpPost]
        //public async Task<ActionResult> Index(DataTableParameters parameters)
        //{
        //    // Set the page index and page size based on DataTable parameters
        //    var pageIndex = (parameters.iDisplayStart / parameters.iDisplayLength) + 1;
        //    var pageSize = parameters.iDisplayLength;

        //    // Optionally, create a service header (if needed)
        //    ServiceHeader serviceHeader = null; // Initialize if required

        //    // Fetch the paginated data using the provided method
        //    var result = await _channelService.FindInHouseChequesInPageAsync(pageIndex, pageSize, serviceHeader);

        //    // Filter the records (if search term is provided)
        //    if (!string.IsNullOrEmpty(parameters.sSearch))
        //    {
        //        result.Items = result.Items.Where(x => x.Payee.Contains(parameters.sSearch)
        //                                            || x.Reference.Contains(parameters.sSearch)
        //                                            || x.BranchDescription.Contains(parameters.sSearch)).ToList();
        //    }

        //    // Prepare the result for DataTables
        //    var jsonData = new
        //    {
        //        sEcho = parameters.sEcho, // Ensure DataTable sync
        //        iTotalRecords = result.TotalCount, // Total records in the system
        //        iTotalDisplayRecords = result.TotalCount, // Total records after filtering
        //        aaData = result.Items.Select(x => new
        //        {
        //            x.BranchDescription,
        //            x.Payee,
        //            x.Funding,
        //            x.Reference,
        //            x.DebitChartOfAccountAccountName,
        //            CreatedDate = x.CreatedDate.ToString("o"), // ISO format for client-side parsing
        //            Id = x.Id, // For the select button
        //            x.Amount,
        //            x.WordifiedAmount,
        //            x.PaddedAmount
        //        })
        //    };

        //    // Return the result as JSON
        //    return Json(jsonData);
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Printing(InHouseChequeDTO model)
        {
            if (ModelState.IsValid)
            {
                var bankLinkageDTO = new BankLinkageDTO();
                int moduleNavigationItemCode = 123; 
                ServiceHeader serviceHeader = new ServiceHeader();

                bool result = await _channelService.PrintInHouseChequeAsync(model, bankLinkageDTO, moduleNavigationItemCode, serviceHeader);

                if (result)
                {
                    TempData["SuccessMessage"] = "Cheque printed successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Failed to print the cheque. Please try again.");
                }
            }

            return View(model);
        }

        



    }
}




















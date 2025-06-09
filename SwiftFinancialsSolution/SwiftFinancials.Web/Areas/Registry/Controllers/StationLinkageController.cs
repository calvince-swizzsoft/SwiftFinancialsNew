using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.RegistryModule;
using SwiftFinancials.Web.Attributes;
using SwiftFinancials.Web.Controllers;

namespace SwiftFinancials.Web.Areas.Registry.Controllers
{
    [RoleBasedAccessControl]
    public class stationlinkageController : MasterController
    {

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);
            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> Create(Guid? stationId)
        {
            await ServeNavigationMenus();

            var stations = await _channelService.FindStationsAsync(GetServiceHeader());
            ViewBag.Stations = new SelectList(stations, "Id", "Name");
            ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);
            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);

            if (stationId.HasValue)
            {
                ViewBag.SelectedStationId = stationId.Value;

                // Get linked customers
                var customersLinkages = await _channelService.FindCustomersByStationIdAndFilterInPageAsync(
                    stationId.Value, "", 1, 0, int.MaxValue, GetServiceHeader());

                ViewBag.LinkedCustomers = customersLinkages?.PageCollection
                    .OrderByDescending(i => i.CreatedDate)
                    .ToList();

            }

            return View("Create");
        }
        public async Task<ActionResult> Search(Guid id)
        {
            //string Remarks = "";
            await ServeNavigationMenus();
            ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);
            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);
            CustomerDTO customerDTO = new CustomerDTO();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindCustomerAsync(id, GetServiceHeader());
            bool includeInterestBalanceForLoanAccounts = false;
            bool includeBalances = false;
            bool includeProductDescription = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;
            int[] Savingsproductcode = { 1 };
            int[] loansproductcode = { 2 };
            int[] investmentssproductcode = { 3 };


            var customeraccountsSavings = await _channelService.FindCustomerAccountsByCustomerIdAndProductCodesAsync(customer.Id, Savingsproductcode, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());
            var customeraccountsloans = await _channelService.FindCustomerAccountsByCustomerIdAndProductCodesAsync(customer.Id, loansproductcode, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());
            var customeraccountsInvestment = await _channelService.FindCustomerAccountsByCustomerIdAndProductCodesAsync(customer.Id, investmentssproductcode, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());
            ViewBag.SavingsAccounts = customeraccountsSavings;
            ViewBag.loansAccounts = customeraccountsloans;
            ViewBag.InvestmentAccounts = customeraccountsInvestment;


            //WithdrawalNotificationDTOs = TempData["WithdrawalNotificationDTOs"] as ObservableCollection<WithdrawalNotificationDTO>;

            if (customer != null)
            {

                customerDTO.Id = customer.Id;
                customerDTO.IndividualLastName = customer.FullName;
                customerDTO.IndividualPayrollNumbers = customer.IndividualPayrollNumbers;
                customerDTO.SerialNumber = customer.SerialNumber;
                customerDTO.IndividualIdentityCardNumber = customer.IndividualIdentityCardNumber;
                customerDTO.StationDescription = customer.StationDescription;
                customerDTO.ZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
                customerDTO.BranchDescription = customer.BranchDescription;

                //Session["Test"] =Request.Form["h"] + "";
                //string mimi = Session["Test"].ToString();
                Session["withdrawalNotificationDTO"] = customer;
                if (Session["Remarks"] != null)
                {
                    customer.Remarks = Session["Remarks"].ToString();
                }
                if (Session["BranchDescription"] != null)
                {
                    customer.BranchDescription = Session["BranchDescription"].ToString();
                }
                //
            }

            //TempData["WithdrawalNotificationDTOs"] = withdrawalNotificationDTO;
            return View("Create", customer);
        }
        [HttpPost]
        public async Task<ActionResult> LinkCustomer(Guid stationId, Guid customerId)
        {
            var customerdto = await _channelService.FindCustomerAsync(customerId, GetServiceHeader());
            customerdto.StationId = stationId;
            await _channelService.UpdateCustomerStationAsync(customerdto, GetServiceHeader());

            TempData["Success"] = "Customer linked successfully.";
            return RedirectToAction("Create", new { stationId });
        }

        [HttpPost]
        public async Task<ActionResult> UnlinkCustomer(Guid stationId, Guid customerId, String[] Customerids)
        {
            var customerdto = await _channelService.FindCustomerAsync(customerId, GetServiceHeader());

            ObservableCollection<CustomerDTO> customerDTOs = new ObservableCollection<CustomerDTO>();
            customerDTOs.Add(customerdto);
            await _channelService.ResetCustomerStationAsync(customerDTOs, GetServiceHeader());

            TempData["Success"] = "Customer removed from station.";
            return RedirectToAction("Create", new { stationId });
        }
    }
}

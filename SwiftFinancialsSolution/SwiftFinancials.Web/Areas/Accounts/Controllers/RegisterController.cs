using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Attributes;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class RegisterController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            ViewBag.AccountType = GetProductCodeSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindCustomerAccountsByFilterInPageAsync(jQueryDataTablesModel.sSearch, 1, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, false, true, true, false, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CustomerAccountDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var customerAccountDTO = await _channelService.FindCustomerAccountAsync(id, false, false, false, false, GetServiceHeader());

            return View(customerAccountDTO);
        }

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            CustomerAccountDTO customerAccountDTO = new CustomerAccountDTO();

            if (customer != null)
            {
                customerAccountDTO.CustomerId = customer.Id;
                customerAccountDTO.CustomerStationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
                customerAccountDTO.CustomerSerialNumber = customer.SerialNumber;
                customerAccountDTO.CustomerIndividualIdentityCardNumber = customer.IndividualIdentityCardNumber;
                customerAccountDTO.CustomerReference3 = customer.Reference3;
                customerAccountDTO.CustomerReference2 = customer.Reference2;
                customerAccountDTO.CustomerReference1 = customer.Reference1;
                customerAccountDTO.CustomerIndividualPayrollNumbers = customer.IndividualPayrollNumbers;            
            }

            return View(customerAccountDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(CustomerAccountDTO customerAccountDTO)
        {
            var customer = await _channelService.FindCustomerAsync(customerAccountDTO.CustomerId, GetServiceHeader());

            customerAccountDTO.CustomerId = customer.Id;
            customerAccountDTO.CustomerStationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
            customerAccountDTO.CustomerSerialNumber = customer.SerialNumber;
            customerAccountDTO.CustomerIndividualIdentityCardNumber = customer.IndividualIdentityCardNumber;
            customerAccountDTO.CustomerReference3 = customer.Reference3;
            customerAccountDTO.CustomerReference2 = customer.Reference2;
            customerAccountDTO.CustomerReference1 = customer.Reference1;
            customerAccountDTO.CustomerIndividualPayrollNumbers = customer.IndividualPayrollNumbers;

            customerAccountDTO.ValidateAll();

            if (!customerAccountDTO.HasErrors)
            {
                await _channelService.AddCustomerAccountAsync(customerAccountDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = customerAccountDTO.ErrorMessages;

                return View(customerAccountDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var customerAccountDTO = await _channelService.FindCustomerAccountAsync(id, false, false, false, false, GetServiceHeader());

            return View(customerAccountDTO.MapTo<CustomerAccountDTO>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, CustomerAccountDTO customerAccountDTO)
        {
            customerAccountDTO.ValidateAll();

            if (!customerAccountDTO.ErrorMessages.Any())
            {
                await _channelService.UpdateCustomerAccountAsync(customerAccountDTO, GetServiceHeader());
                
                return RedirectToAction("Index");
            }
            else
            {
                return View(customerAccountDTO);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetCustomersAsync()
        {
            var customersDTOs = await _channelService.FindCustomersAsync(GetServiceHeader());

            return Json(customersDTOs, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<JsonResult> GetCustomerByIdentityNumberAsync(string individualIdentityCardNumber, bool matchExtact)
        {
            var customersDTOs = await _channelService.FindCustomersByIdentityCardNumberAsync(individualIdentityCardNumber, matchExtact, GetServiceHeader());

            return Json(customersDTOs, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<JsonResult> GetCustomersByIDyNumberAsync(string individualIdentityCardNumber)
        {
            var customersDTOs = await _channelService.FindCustomersByIDNumberAsync(individualIdentityCardNumber, GetServiceHeader());

            return Json(customersDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
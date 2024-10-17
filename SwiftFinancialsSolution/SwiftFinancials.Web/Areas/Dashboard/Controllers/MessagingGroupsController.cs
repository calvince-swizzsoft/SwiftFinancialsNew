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
using Application.MainBoundedContext.DTO.MessagingModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.List;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Dashboard.Controllers
{
    public class MessagingGroupsController : MasterController
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

            var pageCollectionInfo = await _channelService.FindMessageGroupsByFilterInPageAsync(jQueryDataTablesModel.sSearch, (jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength), jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<MessageGroupDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.target = GetMessagingGroupTargetSelectList(string.Empty);

            return View();
        }



        public async Task<ActionResult> CustomerLookUp(Guid? id, MessageGroupDTO model)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("create");
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            if (customer != null)
            {

                model.customer[0].Id = customer.Id;
                model.customer[0].IndividualFirstName = customer.FullName;
                model.customer[0].AddressMobileLine = customer.AddressMobileLine;
                model.customer[0].AddressEmail = customer.AddressEmail;


                var customerType = customer.TypeDescription; var serialNumber = customer.PaddedSerialNumber; var accountNumber = customer.Reference1; var membershipNumber = customer.Reference2;
                var payrollNumber = customer.IndividualPayrollNumbers; var gender = customer.IndividualGenderDescription; var maritalStatus = customer.IndividualMaritalStatusDescription;
                var identityCardNumber = customer.IndividualIdentityCardNumber;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        CustomerID = model.customer[0].Id,
                        Customer = model.customer[0].IndividualFirstName,
                        Mobile = model.customer[0].AddressMobileLine,
                        Email = model.customer[0].AddressEmail,
                        CustomerType = customerType,
                        SerialNumber = serialNumber,
                        AccountNumber = accountNumber,
                        MembershipNumber = membershipNumber,
                        PayrollNumber = payrollNumber,
                        Gender = gender,
                        MaritalStatus = maritalStatus,
                        IdentityCardNumber = identityCardNumber
                    }
                });
            }

            return Json(new { success = false, message = "Customer not found" });
        }



        [HttpPost]
        public async Task<ActionResult> Create(MessageGroupDTO model)
        {
            return View();
        }
    }
}
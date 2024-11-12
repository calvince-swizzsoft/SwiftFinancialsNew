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
            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;
            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindMessageGroupsByFilterInPageAsync(jQueryDataTablesModel.sSearch, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;
                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? pageCollectionInfo.PageCollection.Count
                    : totalRecordCount;

                var orderedPageCollection = pageCollectionInfo.PageCollection
                    .OrderByDescending(item => item.CreatedDate)
                    .ToList();

                return this.DataTablesJson(
                    items: orderedPageCollection,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
            else
            {
                return this.DataTablesJson(
                    items: new List<MessageGroupDTO> { },
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
        }   
        
        
        
        [HttpPost]
        public async Task<JsonResult> CustomerIndex(JQueryDataTablesModel jQueryDataTablesModel)
        {

            int totalRecordCount = 0;
            int searchRecordCount = 0;
            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;
            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindCustomersByFilterInPageAsync(jQueryDataTablesModel.sSearch, (int)CustomerFilter.IdentityCardNumber, pageIndex,
                jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;
                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? pageCollectionInfo.PageCollection.Count
                    : totalRecordCount;

                var orderedPageCollection = pageCollectionInfo.PageCollection
                    .OrderByDescending(item => item.CreatedDate)
                    .ToList();

                return this.DataTablesJson(
                    items: orderedPageCollection,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
            else
            {
                return this.DataTablesJson(
                    items: new List<CustomerDTO> { },
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
        } 
        

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.target = GetMessagingGroupTargetSelectList(string.Empty);

            return View();
        }



        public async Task<ActionResult> CustomerLookUp(Guid? id, MessageGroupDTO messageGroupDTO)
        {
            if (id == Guid.Empty)
            {
                return Json(new { success = false, message = "Invalid ID" });
            }

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            ViewBag.target = GetMessagingGroupTargetSelectList(string.Empty);

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            if (customer != null)
            {
                messageGroupDTO.CustomerID = customer.Id;
                messageGroupDTO.Customer = customer.FullName;
                messageGroupDTO.CustomerEmailAddress = customer.AddressEmail;
                messageGroupDTO.CustomerMobileNumber = customer.AddressMobileLine;


                var customerType = customer.TypeDescription; var serialNumber = customer.PaddedSerialNumber; var accountNumber = customer.Reference1; var membershipNumber = customer.Reference2;
                var payrollNumber = customer.IndividualPayrollNumbers; var gender = customer.IndividualGenderDescription; var maritalStatus = customer.IndividualMaritalStatusDescription;
                var identityCardNumber = customer.IndividualIdentityCardNumber;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        ID = messageGroupDTO.CustomerID,
                        Name = messageGroupDTO.Customer,
                        Email = messageGroupDTO.CustomerEmailAddress,
                        Phone = messageGroupDTO.CustomerMobileNumber

                        //CustomerType = customerType,
                        //SerialNumber = serialNumber,
                        //AccountNumber = accountNumber,
                        //MembershipNumber = membershipNumber,
                        //PayrollNumber = payrollNumber,
                        //Gender = gender,
                        //MaritalStatus = maritalStatus,
                        //IdentityCardNumber = identityCardNumber
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
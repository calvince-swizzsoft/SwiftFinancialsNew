using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using ServiceStack;
using SwiftFinancials.Presentation.Infrastructure.Services;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Registry.Controllers
{
    public class NextOfKinController : MasterController
    {
        private readonly IChannelService _channelService;

        public NextOfKinController(IChannelService channelService)
        {
            _channelService = channelService;
        }

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);
            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, int? recordStatus, int? customerFilter, int? customerType, string filterValue)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            //var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            //var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            //int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var pageCollectionInfo = new PageCollectionInfo<CustomerDTO>();

            if (recordStatus != null && customerFilter != null)
            {
                pageCollectionInfo = await _channelService.FindCustomersByRecordStatusAndFilterInPageAsync((int)recordStatus, filterValue, (int)customerFilter, 0, int.MaxValue, GetServiceHeader());
            }
            else if (recordStatus == null && customerFilter != null)
            {
                pageCollectionInfo = await _channelService.FindCustomersByFilterInPageAsync(filterValue, (int)customerFilter, 0, int.MaxValue, GetServiceHeader());
            }
            else if (customerType != null && customerFilter == null)
            {

                if (customerType == 0)
                {
                    pageCollectionInfo = await _channelService.FindCustomersByRecordStatusAndFilterInPageAsync((int)recordStatus, filterValue, (int)CustomerFilter.FirstName, 0, int.MaxValue, GetServiceHeader());
                }

                if (customerType == 1)
                {
                    pageCollectionInfo = await _channelService.FindCustomersByFilterInPageAsync(jQueryDataTablesModel.sSearch, (int)CustomerFilter.NonIndividual_Description, 0, int.MaxValue, GetServiceHeader());
                }
                if (customerType == 2)
                {
                    pageCollectionInfo = await _channelService.FindCustomersByFilterInPageAsync(jQueryDataTablesModel.sSearch, (int)CustomerFilter.NonIndividual_Description, 0, int.MaxValue, GetServiceHeader());
                }
                if (customerType == 3)
                {
                    pageCollectionInfo = await _channelService.FindCustomersByFilterInPageAsync(jQueryDataTablesModel.sSearch, (int)CustomerFilter.NonIndividual_Description, 0, int.MaxValue, GetServiceHeader());
                }
            }
            else if (recordStatus != null && customerFilter == null)
            {
                pageCollectionInfo = await _channelService.FindCustomersByTypeAndFilterInPageAsync((int)customerType, jQueryDataTablesModel.sSearch, (int)CustomerFilter.NonIndividual_Description, 0, int.MaxValue, GetServiceHeader());
            }
            else if (recordStatus == null && customerFilter == null)
            {
                pageCollectionInfo = await _channelService.FindCustomersByRecordStatusAndFilterInPageAsync((int)RecordStatus.New, jQueryDataTablesModel.sSearch, (int)CustomerFilter.FirstName, 0, int.MaxValue, GetServiceHeader());

                if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
                {

                    var sortedData = pageCollectionInfo.PageCollection
                        .OrderByDescending(loanCase => loanCase.CreatedDate)
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
                    items: new List<CustomerDTO>(),
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
            );
            }
            if (filterValue != null)
            {
                pageCollectionInfo = await _channelService.FindCustomersByFilterInPageAsync(filterValue, (int)CustomerFilter.IdentityCardNumber, 0, int.MaxValue, GetServiceHeader());
            }
            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(loanCase => loanCase.CreatedDate)
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
                items: new List<CustomerDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho);
        }


        [HttpGet]
        public async Task<ActionResult> GetCustomerDetails(Guid customerId)
        {
            try
            {
                var customer = await _channelService.FindCustomerAsync(customerId, GetServiceHeader());
                Session["customer"] = customer;
                if (customer == null)
                {
                    return Json(new { success = false, message = "Customer not found." }, JsonRequestBehavior.AllowGet);
                }

                // Assuming you want to store the customer Id in Session
                Session["id"] = customer.Id;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        IndividualFirstName = customer.IndividualFirstName,
                        customerId = customer.Id,
                        IndividualLastName = customer.IndividualLastName,
                        FullName = customer.FullName,
                        StationZoneDivisionEmployerId = customer.StationZoneDivisionEmployerId,
                        StationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription,
                        IndividualIdentityCardNumber = customer.IndividualIdentityCardNumber,
                        IndividualPayrollNumbers = customer.IndividualPayrollNumbers,
                        Reference1 = customer.Reference1,
                        Reference2 = customer.Reference2,
                        Reference3 = customer.Reference3,
                        StationId = customer.StationId,
                        StationDescription = customer.StationDescription,
                        SerialNumber = customer.SerialNumber,
                        Remarks = customer.Remarks,
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                // Log the exception (optional)
                return Json(new { success = false, message = "An error occurred while fetching the customer details." }, JsonRequestBehavior.AllowGet);
            }
        }




        [HttpPost]
        public async Task<ActionResult> SaveNextOfKin(NextOfKinDTO nextOfKinDTO)
        {

            await ServeNavigationMenus();
            var k = Session["customer"] as CustomerDTO;
            NextOfKinDTOs = TempData["NextOfKinDTOs"] as ObservableCollection<NextOfKinDTO>;
            if (NextOfKinDTOs == null)
                NextOfKinDTOs = new ObservableCollection<NextOfKinDTO>();

            if (nextOfKinDTO.NominatedPercentage == 100.1 || nextOfKinDTO.NominatedPercentage == -0)
            {
                try
                {
                    return RedirectToAction("Create");

                }
                catch
                {

                }
            }
            NextOfKinDTOs.Add(nextOfKinDTO);
            TempData["NextOfKinDTOs"] = NextOfKinDTOs as ObservableCollection<NextOfKinDTO>;

            // For simplicity, let's return the data back as a JSON response
            return Json(new
            {
                success = true,
                data = new
                {
                    k
                }
            }, JsonRequestBehavior.AllowGet);

        }


        [HttpPost]
        public async Task<ActionResult> RemoveNextOfKin(string firstname)
        {

            await ServeNavigationMenus();
            var k = Session["customer"] as CustomerDTO;
            NextOfKinDTOs = TempData["NextOfKinDTOs"] as ObservableCollection<NextOfKinDTO>;
            if (NextOfKinDTOs == null)
                NextOfKinDTOs = new ObservableCollection<NextOfKinDTO>();
            foreach (var nextofkin in NextOfKinDTOs)
            {
                if (nextofkin.FirstName == firstname)
                    NextOfKinDTOs.Remove(nextofkin);
            }
            TempData["NextOfKinDTOs"] = NextOfKinDTOs as ObservableCollection<NextOfKinDTO>;

            // For simplicity, let's return the data back as a JSON response
            return Json(new
            {
                success = true,
                data = new
                {
                    k
                }
            }, JsonRequestBehavior.AllowGet);

        }
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.IdentityCardSelectList = GetIdentityCardTypeSelectList(string.Empty);
            ViewBag.SalutationSelectList = GetSalutationSelectList(string.Empty);
            ViewBag.GenderSelectList = GetGenderSelectList(string.Empty);
            ViewBag.GenderSelectList = GetGenderSelectList(string.Empty);
            ViewBag.RelationshipSelectList = GetRelationshipSelectList(string.Empty);
            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Create(NextOfKinDTO nextOfKinDTO, ObservableCollection<NextOfKinDTO> nextOfKinCollection)
        {

            var customerDTO = Session["customer"] as CustomerDTO;
            var customer = await _channelService.FindCustomerAsync(customerDTO.Id, GetServiceHeader());

            NextOfKinDTOs = TempData["NextOfKinDTOs"] as ObservableCollection<NextOfKinDTO>;

            if (!customer.HasErrors)
            {
                var result1 = await _channelService.UpdateNextOfKinCollectionAsync(customer, NextOfKinDTOs, GetServiceHeader());


                if (result1 == true)
                {
                    TempData["NextOfKinDTOs"] = "";
                    Session["customer"] = "";
                    TempData["SuccessMessage"] = "Next of kin created successfully!";
                    return View("Index", "Customer", new { Area = "Registry" });
                }
                else
                {
                    ModelState.AddModelError("", "An error occurred while creating the next of kin.");
                }

                return RedirectToAction("Index", "Customer", new { Area = "Registry" });
            }
            else
            {
                // Assume ErrorMessages is a string. Concatenate error messages.
                foreach (var nextOfKin in nextOfKinCollection)
                {
                    customerDTO.ErrorMessages += string.Join("; ", nextOfKin.ErrorMessages) + "; ";
                }
                TempData["ErrorMessages"] = customerDTO.ErrorMessages.TrimEnd(';', ' '); // Clean up trailing separators
            }

            return View(nextOfKinDTO);
        }

        [HttpPost]
        public JsonResult SaveEntries(List<NextOfKinBindingModel> entries)
        {
            // Store the data in TempData
            TempData["Entries"] = entries;

            // Return a success response
            return Json(new { success = true });
        }

    }

}
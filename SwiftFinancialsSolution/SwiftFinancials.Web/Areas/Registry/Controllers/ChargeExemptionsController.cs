using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Application.Seedwork;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Registry.Controllers
{
    public class ChargeExemptionsController : MasterController
    {

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, int? recordStatus, int? customerFilter, int? customerType)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var pageCollectionInfo = new PageCollectionInfo<CustomerDTO>();

            if (recordStatus != null && customerFilter != null)
            {
                pageCollectionInfo = await _channelService.FindCustomersByRecordStatusAndFilterInPageAsync((int)recordStatus, jQueryDataTablesModel.sSearch, (int)customerFilter, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());
            }
            else if (recordStatus == null && customerFilter != null)
            {
                pageCollectionInfo = await _channelService.FindCustomersByFilterInPageAsync(jQueryDataTablesModel.sSearch, (int)customerFilter, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());
            }
            else if (customerType != null && customerFilter == null)
            {
                pageCollectionInfo = await _channelService.FindCustomersByRecordStatusAndFilterInPageAsync((int)recordStatus, jQueryDataTablesModel.sSearch, (int)CustomerFilter.FirstName, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());
            }
            else if (recordStatus != null && customerFilter == null)
            {
                pageCollectionInfo = await _channelService.FindCustomersByTypeAndFilterInPageAsync((int)customerType, jQueryDataTablesModel.sSearch, (int)CustomerFilter.NonIndividual_Description, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());
            }
            else
            {
                pageCollectionInfo = await _channelService.FindCustomersByFilterInPageAsync(jQueryDataTablesModel.sSearch, (int)CustomerFilter.NonIndividual_Description, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());
            }

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;
                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(postingPeriod => postingPeriod.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CustomerDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        [HttpGet]
        public async Task<ActionResult> GetCustomerDetails(Guid customerId)
        {
            try
            {
                var customer = await _channelService.FindCustomerAsync(customerId, GetServiceHeader());
                if (customer == null)
                {
                    return Json(new { success = false, message = "Customer not found." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        Id = customer.Id,
                        IndividualFirstName = customer.FullName,
                        Reference1 = customer.Reference1,
                        StationZoneDivisionEmployerId = customer.StationZoneDivisionEmployerId,
                        StationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription,
                        Reference2 = customer.Reference2,
                        Reference3 = customer.Reference3,
                        StationId = customer.StationId,
                        StationDescription = customer.StationDescription,

                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the customer details." }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpGet]
        public async Task<ActionResult> GetChargesDetails(Guid ChargeId)
        {
            try
            {
                // Fetch the Charge details using the service
                var Charge = await _channelService.FindCommissionAsync(ChargeId, GetServiceHeader());
                if (Charge == null)
                {
                    return Json(new { success = false, message = "Charge not found." }, JsonRequestBehavior.AllowGet);
                }

                // Create a new CommissionDTO object and populate its properties
                var commissionDTO = new CommissionDTO
                {
                    Id = Charge.Id,
                    Description = Charge.Description
                    // Add other properties as needed
                };

                // Return the JSON response with the populated data
                return Json(new
                {
                    success = true,
                    data = commissionDTO
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Handle any errors that occur during the execution
                return Json(new { success = false, message = "An error occurred while fetching the Charge details.", error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }



        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            // Fetch the specific by ID using the service
            var customerDTO = await _channelService.FindCustomerAsync(id, GetServiceHeader());

            // Check if it  was found
            if (customerDTO == null)
            {
                return HttpNotFound();
            }

            // Pass the data to the Details view
            return View(customerDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(CustomerDTO customerDTO)
        {
            customerDTO.ValidateAll();

            if (!customerDTO.HasErrors)
            {
                await _channelService.AddCustomerAsync(customerDTO, GetServiceHeader());

                TempData["SuccessMessage"] = "ChargeExemption created successfully!";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = customerDTO.ErrorMessages;

                return View(customerDTO);
            }
        }


        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var customerDTO = await _channelService.FindCustomerAsync(id, GetServiceHeader());

            return View(customerDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, CustomerDTO CustomerBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateCustomerAsync(CustomerBindingModel, GetServiceHeader());

                TempData["SuccessMessage"] = "ChargeExemption updated successfully!";

                return RedirectToAction("Index");
            }
            else
            {
                return View(CustomerBindingModel);
            }
        }



        [HttpGet]
        public async Task<JsonResult> GetDepartmentsAsync(Guid id)
        {
            var cutomerDTOs = await _channelService.FindCustomerAsync(id, GetServiceHeader());

            return Json(cutomerDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
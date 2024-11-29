using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.RegistryModule;
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

        public async Task<ActionResult> Index(Guid customerId)
        {
            await ServeNavigationMenus();

            // Fetch next of kin collection by customer ID
            var nextOfKinCollection = await _channelService.FindNextOfKinCollectionByCustomerIdAsync(customerId, GetServiceHeader());

            return View(nextOfKinCollection);
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc";
            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindDelegatesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;
                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else
            {
                return this.DataTablesJson(items: new List<CustomerDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
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
                        IndividualFirstName = customer.IndividualFirstName,
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
                return Json(new { success = false, message = "An error occurred while fetching the customer details." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult AssignText(string Remarks, string ZoneId)
        {
            Session["Remarks"] = Remarks;
            Session["ZoneId"] = ZoneId;
            return null;
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();
            var delegateDTO = await _channelService.FindDelegateAsync(id, GetServiceHeader());

            if (delegateDTO == null)
            {
                return HttpNotFound();
            }

            return View(delegateDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(CustomerDTO customerDTO, ObservableCollection<NextOfKinDTO> nextOfKinCollection)
        {
            customerDTO.ValidateAll();
            foreach (var nextOfKin in nextOfKinCollection)
            {
                nextOfKin.ValidateAll();
            }

            if (!customerDTO.HasErrors && nextOfKinCollection.All(n => !n.HasErrors))
            {
                var result = await _channelService.UpdateNextOfKinCollectionAsync(customerDTO, nextOfKinCollection, GetServiceHeader());

                if (result)
                {
                    TempData["SuccessMessage"] = "Next of kin updated successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "An error occurred while updating the next of kin.");
                }
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

            return View(customerDTO);
        }



        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
            var delegateDTO = await _channelService.FindDelegateAsync(id, GetServiceHeader());
            return View(delegateDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, DelegateDTO BindingModelBase)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateDelegateAsync(BindingModelBase, GetServiceHeader());
                TempData["SuccessMessage"] = "Delegate updated successfully!";
                return RedirectToAction("Index");
            }
            else
            {
                return View(BindingModelBase);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetDepartmentsAsync(Guid id)
        {
            var delegateDTOs = await _channelService.FindDelegateAsync(id, GetServiceHeader());
            return Json(delegateDTOs, JsonRequestBehavior.AllowGet);
        }
    }

}
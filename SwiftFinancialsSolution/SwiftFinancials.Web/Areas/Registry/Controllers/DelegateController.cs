using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.RegistryModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Registry.Controllers
{
    public class DelegateController : MasterController
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

            var pageCollectionInfo = await _channelService.FindDelegatesByFilterInPageAsync(jQueryDataTablesModel.sSearch,0,int.MaxValue, GetServiceHeader());

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
                items: new List<DelegateDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
        );
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
                        CustomerId = customer.Id,
                        CustomerFullName = customer.FullName,
                        CustomerIndividualIdentityCardNumber = customer.IndividualIdentityCardNumber,
                        CustomerSerialNumber = customer.SerialNumber,
                        ZoneId = customer.StationZoneId,
                        ZoneDescription = customer.StationZoneDescription,

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

            // Fetch the specific delegate by ID using the service
            var delegateDTO = await _channelService.FindDelegateAsync(id, GetServiceHeader());

            // Check if the delegate was found
            if (delegateDTO == null)
            {
                return HttpNotFound();
            }

            // Pass the delegate data to the Details view
            return View(delegateDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Create(DelegateDTO delegateDTO)
        {
            delegateDTO.ValidateAll();

            if (!delegateDTO.HasErrors)
            {
                await _channelService.AddDelegateAsync(delegateDTO, GetServiceHeader());

                TempData["SuccessMessage"] = "Delegate created successfully!";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = delegateDTO.ErrorMessages;

                return View(delegateDTO);
            }
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
            var delegteDTOs = await _channelService.FindDelegateAsync(id, GetServiceHeader());

            return Json(delegteDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
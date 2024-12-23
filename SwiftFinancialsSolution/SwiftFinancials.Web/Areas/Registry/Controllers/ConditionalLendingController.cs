using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Domain.MainBoundedContext.RegistryModule.Aggregates.ConditionalLendingEntryAgg;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Registry.Controllers
{
    public class ConditionalLendingController : MasterController
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

            var pageCollectionInfo = await _channelService.FindConditionalLendingsByFilterInPageAsync(jQueryDataTablesModel.sSearch, 0,int.MaxValue, GetServiceHeader());

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
                items: new List<ConditionalLendingDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
        );
        }




        [HttpGet]
        public async Task<ActionResult> GetCustomerDetails(Guid customerId)
        {

            CustomerDTOs = TempData["CustomerDTOs"] as ObservableCollection<CustomerDTO>;
            if (CustomerDTOs == null)
                CustomerDTOs = new ObservableCollection<CustomerDTO>();
            CustomerDTO CustomerDTO = new CustomerDTO();
            try
            {
                var c = new CustomerDTO();

                var customer = await _channelService.FindCustomerAsync(customerId, GetServiceHeader());

                CustomerDTOs.Add(customer);
                TempData["CustomerDTOs"] = CustomerDTOs;
                ViewBag.CustomerDTOs = CustomerDTOs;
                if (customer == null)
                {
                    return Json(new { success = false, message = "Customer not found." }, JsonRequestBehavior.AllowGet);
                }

                c.IndividualFirstName = customer.IndividualFirstName;
                c.IndividualLastName = customer.IndividualLastName;
                c.StationZoneDivisionEmployerId = customer.StationZoneDivisionEmployerId;
                c.StationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
                c.IndividualIdentityCardNumber = customer.IndividualIdentityCardNumber;
                c.IndividualPayrollNumbers = customer.IndividualPayrollNumbers;
                c.Reference1 = customer.Reference1;
                c.ZoneDivisionEmployerDescription = customer.ZoneDivisionEmployerDescription;
                c.Reference2 = customer.Reference2;
                c.StationZoneId = customer.StationZoneId;
                c.StationZoneDescription = customer.StationZoneDescription;
                c.Remarks = customer.Remarks;
                ConditionalLendingDTO conditionalLendingDTO = new ConditionalLendingDTO();
                conditionalLendingDTO.CustomerDTO = customer;

                return Json(new


                {
                    success = true,
                    data = new
                    {
                        ViewBag.CustomerDTOs,
                        Id = c.Id,
                        IndividualFirstName = customer.IndividualFirstName,
                        IndividualLastName = customer.IndividualLastName,
                        FullName = customer.FullName,
                        StationZoneDivisionEmployerId = customer.StationZoneDivisionEmployerId,
                        StationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription,
                        IndividualIdentityCardNumber = customer.IndividualIdentityCardNumber,
                        IndividualPayrollNumbers = customer.IndividualPayrollNumbers,
                        Reference1 = customer.Reference1,
                        Reference2 = customer.Reference2,
                        StationZoneId = customer.StationZoneId,
                        StationZoneDescription = customer.StationZoneDescription,
                        Remarks = customer.Remarks,
                        c,
                    }

                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the customer details." }, JsonRequestBehavior.AllowGet);
            }

        }


        [HttpGet]
        public async Task<ActionResult> GetLoanProductDetails(Guid loanProductId)
        {
            try
            {
                var loanProduct = await _channelService.FindLoanProductAsync(loanProductId, GetServiceHeader());
                if (loanProduct == null)
                {
                    return Json(new { success = false, message = "Loan product not found." }, JsonRequestBehavior.AllowGet);
                }

                var loanProductDto = new LoanProductDTO
                {
                    Id = loanProduct.Id,
                    Description = loanProduct.Description,
                };

                return Json(new { success = true, data = loanProductDto }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }





        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            // Fetch the specific ConditionalLending by ID using the service
            var conditionalLendingDTO = await _channelService.FindConditionalLendingEntriesByConditionalLendingIdAsync(id, GetServiceHeader());

            // Check if the ConditionalLending was found
            if (conditionalLendingDTO == null)
            {
                return HttpNotFound();
            }

            // Pass the ConditionalLending data to the Details view
            return View(conditionalLendingDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            CustomerDTOs = TempData["CustomerDTOs"] as ObservableCollection<CustomerDTO>;
            ViewBag.CustomerDTOs = CustomerDTOs;

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(ConditionalLendingDTO conditionalLendingDTO)
        {
            CustomerDTOs = TempData["CustomerDTOs"] as ObservableCollection<CustomerDTO>;
            ObservableCollection<ConditionalLendingEntryDTO> conditionalLendingEntryCollection = new ObservableCollection<ConditionalLendingEntryDTO>();
            ConditionalLendingEntryDTO conditionalLendingEntry = new ConditionalLendingEntryDTO();

            foreach (var customer in CustomerDTOs)
            {

                conditionalLendingEntry.CustomerId = customer.Id;
                conditionalLendingEntryCollection.Add(conditionalLendingEntry);

            }
            conditionalLendingDTO.ValidateAll();

            if (!conditionalLendingDTO.HasErrors)
            {
                var result = await _channelService.AddConditionalLendingAsync(conditionalLendingDTO, GetServiceHeader());
                System.Windows.Forms.MessageBox.Show(
                         "Conditional Lending for Loan product " + conditionalLendingDTO.LoanProductDescription + "successfully.",
                         "Success",
                         System.Windows.Forms.MessageBoxButtons.OK,
                         System.Windows.Forms.MessageBoxIcon.Information,
                         System.Windows.Forms.MessageBoxDefaultButton.Button1,
                         System.Windows.Forms.MessageBoxOptions.ServiceNotification
                     );
                if (result.ErrorMessageResult != null)
                {
                    System.Windows.Forms.MessageBox.Show(
                          "FAILED" + result.ErrorMessageResult,
                          "Success",
                          System.Windows.Forms.MessageBoxButtons.OK,
                          System.Windows.Forms.MessageBoxIcon.Information,
                          System.Windows.Forms.MessageBoxDefaultButton.Button1,
                          System.Windows.Forms.MessageBoxOptions.ServiceNotification
                      );
                    ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
                    ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);
                    ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
                    await ServeNavigationMenus();
                    return RedirectToAction("cREATE", conditionalLendingDTO);

                }


                await _channelService.UpdateConditionalLendingEntryCollectionByConditionalLendingIdAsync(result.Id, conditionalLendingEntryCollection, GetServiceHeader());

                TempData["SuccessMessage"] = "conditionalLending created successfully!";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = conditionalLendingDTO.ErrorMessages;

                return View(conditionalLendingDTO);
            }
        }


        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var conditionalLendingDTO = await _channelService.FindConditionalLendingAsync(id, GetServiceHeader());

            return View(conditionalLendingDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, ConditionalLendingDTO BindingModelBase)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateConditionalLendingAsync(BindingModelBase, GetServiceHeader());

                TempData["SuccessMessage"] = "conditionalLending updated successfully!";

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
            var conditionalLendingDTOs = await _channelService.FindConditionalLendingEntriesByConditionalLendingIdAsync(id, GetServiceHeader());

            return Json(conditionalLendingDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
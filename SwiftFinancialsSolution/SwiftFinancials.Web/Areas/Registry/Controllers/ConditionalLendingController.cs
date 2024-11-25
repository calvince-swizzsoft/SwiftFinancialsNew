using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.RegistryModule;
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

            var pageCollectionInfo = await _channelService.FindConditionalLendingsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(ConditionalLending => ConditionalLending.CreatedDate).ToList();

                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<ConditionalLendingDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }




        [HttpGet]
        public async Task<ActionResult> GetCustomerDetails(Guid customerId)
        {
            try
            {
                var c = new CustomerDTO();

                var customer = await _channelService.FindCustomerAsync(customerId, GetServiceHeader());
                if (customer == null)
                {
                    return Json(new { success = false, message = "Customer not found." }, JsonRequestBehavior.AllowGet);
                }


                c.IndividualFirstName = customer.IndividualFirstName;
                c.IndividualLastName = customer.IndividualLastName;
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
                        Id = c.Id,
                        IndividualFirstName = customer.IndividualFirstName,
                        IndividualLastName = customer.IndividualLastName,
                        FullName = customer.FullName,
                        StationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription,
                        IndividualIdentityCardNumber = customer.IndividualIdentityCardNumber,
                        IndividualPayrollNumbers = customer.IndividualPayrollNumbers,
                        Reference1 = customer.Reference1,
                        ZoneDivisionEmployerDescription = customer.ZoneDivisionEmployerDescription,
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

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(ConditionalLendingDTO conditionalLendingDTO)
        {
            conditionalLendingDTO.ValidateAll();

            if (!conditionalLendingDTO.HasErrors)
            {
                await _channelService.AddConditionalLendingAsync(conditionalLendingDTO, GetServiceHeader());

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
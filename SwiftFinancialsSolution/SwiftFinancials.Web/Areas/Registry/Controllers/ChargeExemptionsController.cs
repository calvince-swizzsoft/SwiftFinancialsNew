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
    public class ChargeExemptionsController : MasterController
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

            var pageCollectionInfo = await _channelService.FindCommissionsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CommissionDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
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
                c.IndividualIdentityCardNumber = customer.IndividualIdentityCardNumber;
                c.IndividualPayrollNumbers = customer.IndividualPayrollNumbers;
                c.Reference1 = customer.Reference1;
                c.StationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
                c.Reference2 = customer.Reference2;
                c.Reference3 = customer.Reference3;
                c.StationId = customer.StationId;
                c.StationDescription = customer.StationDescription;
                c.Remarks = customer.Remarks;
                CommissionDTO commissionDTO = new CommissionDTO();
                commissionDTO.CustomerDTO = customer;               

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
                        Reference2 = customer.Reference2,
                        Reference3 = customer.Reference3,
                        StationId = customer.StationId,
                        StationDescription = customer.StationDescription,
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
        public async Task<ActionResult> GetChargeDetails(Guid chargeId)
        {
            try
            {
                var charge = await _channelService.FindCommissionAsync(chargeId, GetServiceHeader());
                if (charge == null)
                {
                    return Json(new { success = false, message = "Commission not found." }, JsonRequestBehavior.AllowGet);
                }

                var commissionDTOs = new CommissionDTO
                {
                    Id = charge.Id,
                    Description = charge.Description,
                };

                return Json(new { success = true, data = commissionDTOs }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            // Fetch by ID using the service
            var commissionDTO = await _channelService.FindCommissionAsync(id, GetServiceHeader());

            // Check if it  was found
            if (commissionDTO == null)
            {
                return HttpNotFound();
            }

            // Pass the data to the Details view
            return View(commissionDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(CommissionDTO commissionDTO)
        {
            commissionDTO.ValidateAll();

            if (!commissionDTO.HasErrors)
            {
                await _channelService.AddCommissionAsync(commissionDTO, GetServiceHeader());

                TempData["SuccessMessage"] = "ChargeExemption created successfully!";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = commissionDTO.ErrorMessages;

                return View(commissionDTO);
            }
        }


        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var commissionDTO = await _channelService.FindCommissionAsync(id, GetServiceHeader());

            return View(commissionDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, CommissionDTO BindingModelBase)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateCommissionAsync(BindingModelBase, GetServiceHeader());

                TempData["SuccessMessage"] = "ChargeExemption updated successfully!";

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
            var commissionDTOs = await _channelService.FindCommissionAsync(id, GetServiceHeader());

            return Json(commissionDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
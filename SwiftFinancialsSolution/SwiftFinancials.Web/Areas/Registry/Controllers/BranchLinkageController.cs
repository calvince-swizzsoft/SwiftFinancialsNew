using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AdministrationModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Registry.Controllers
{
    public class BranchLinkageController : MasterController
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

            var pageCollectionInfo = await _channelService.FindBranchesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<BranchDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        [HttpGet]
        public async Task<ActionResult> GetBranchDetails(Guid CompanyId)
        {
            try
            {
                var branch = await _channelService.FindBranchAsync(CompanyId, GetServiceHeader());
                if (branch == null)
                {
                    return Json(new { success = false, message = "Branch not found." }, JsonRequestBehavior.AllowGet);
                }

                var branchDTO = new BranchDTO
                {
                    Description = branch.Description,
                    CompanyId = branch.CompanyId,
                    CompanyDescription = branch.CompanyDescription,
                    CompanyAddressCity = branch.CompanyAddressCity,
                };

                return Json(new { success = true, data = branchDTO }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
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
                c.StationId = customer.StationId;
                c.StationDescription = customer.StationDescription;
                c.SerialNumber = customer.SerialNumber;
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
                        SerialNumber = customer.SerialNumber,
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

            // Fetch the specific branch by ID using the service
            var branchDTO = await _channelService.FindBranchAsync(id, GetServiceHeader());

            // Check if the branch was found
            if (branchDTO == null)
            {
                return HttpNotFound();
            }

            // Pass the branch data to the Details view
            return View(branchDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Create(BranchDTO branchDTO)
        {
            branchDTO.ValidateAll();

            if (!branchDTO.HasErrors)
            {
                await _channelService.AddBranchAsync(branchDTO, GetServiceHeader());

                TempData["SuccessMessage"] = "BranchLinkage created successfully!";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = branchDTO.ErrorMessages;

                return View(branchDTO);
            }
        }


        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var branchDTO = await _channelService.FindBranchAsync(id, GetServiceHeader());

            return View(branchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, BranchDTO BindingModelBase)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateBranchAsync(BindingModelBase, GetServiceHeader());

                TempData["SuccessMessage"] = "BranchLinkage updated successfully!";

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
            var delegteDTOs = await _channelService.FindBranchAsync(id, GetServiceHeader());

            return Json(delegteDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
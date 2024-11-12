using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.MicroCreditModule;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System.Diagnostics;
using System.Windows.Forms;



namespace SwiftFinancials.Web.Areas.MicroCredit.Controllers
{
    public class MicroCreditGroupsController : MasterController
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

            bool sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc";
            var sortedColumns = jQueryDataTablesModel.GetSortedColumns().Select(s => s.PropertyName).ToList();

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;

            var pageCollectionInfo = await _channelService.FindMicroCreditGroupsByFilterInPageAsync(
                jQueryDataTablesModel.sSearch,
                pageIndex,
                pageSize,
                GetServiceHeader()
            );

            if (pageCollectionInfo != null)
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                var sortedData = sortAscending
                    ? pageCollectionInfo.PageCollection
                        .OrderBy(item => sortedColumns.Contains("CreatedDate") ? item.CreatedDate : default(DateTime))
                        .ToList()
                    : pageCollectionInfo.PageCollection
                        .OrderByDescending(item => sortedColumns.Contains("CreatedDate") ? item.CreatedDate : default(DateTime))
                        .ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? sortedData.Count : totalRecordCount;

                return this.DataTablesJson(
                    items: sortedData,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
            else
            {
                return this.DataTablesJson(
                    items: new List<MicroCreditGroupDTO>(),
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var microCreditOfficerDTO = await _channelService.FindMicroCreditGroupAsync(id, GetServiceHeader());
            if (microCreditOfficerDTO == null)
            {
                TempData["ErrorMessage"] = "MicroCredit Officer not found.";
                return RedirectToAction("Index");
            }

            return View(microCreditOfficerDTO);
        }


        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            ViewBag.TypeDescriptionSelectList = GetMicroCreditGroupTypeSelectList(string.Empty);
            ViewBag.MeetingFrequencyDescriptionSelectList = GetMicroCreditGroupMeetingFrequencySelectList(string.Empty);
            ViewBag.MeetingDayOfWeekDescriptionSelectList = GetMicroCreditGroupMeetingDayOfWeekSelectList(string.Empty);
            ViewBag.DesignationSelectList = GetMicroCreditGroupMemberDesignationSelectList(string.Empty);
                

            return View();
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
                        CustomerNonIndividualDescription = customer.FullName,
                        CustomerId = customer.Id,
                        CustomerStationId = customer.StationId,
                        CustomerStationDescription = customer.StationDescription,
                        CustomerNonIndividualRegistrationNumber = customer.NonIndividualRegistrationNumber,
                        CustomerRegistrationDate = customer.RegistrationDate,
                        CustomerNonIndividualDateEstablished = customer.NonIndividualDateEstablished,
                        



                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the customer details." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public async Task<ActionResult> GetNameDetails(Guid customerId)
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
                        CustomerAccountCustomerReference1 = customer.Reference1,
                        CustomerAccountCustomerReference2 = customer.Reference2,
                        CustomerAccountCustomerReference3 = customer.Reference3,
                        CustomerStationId = customer.StationId,
                        CustomerStationDescription = customer.StationDescription,
                        CustomerFullName = customer.FullName,
                        CustomerId = customer.Id,
                        Employer = customer.StationZoneDivisionEmployerDescription,



                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the customer details." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public async Task<ActionResult> GetCreditOfficerDetails(Guid microCreditOfficerId)
        {
            try
            {
                var creditOficer = await _channelService.FindMicroCreditOfficerAsync(microCreditOfficerId, GetServiceHeader());

                if (creditOficer == null)
                {
                    return Json(new { success = false, message = "Credit-Officer not found." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        MicroCreditOfficerEmployeeCustomerFullName = creditOficer.EmployeeCustomerFullName,
                        MicroCreditOfficerId = creditOficer.Id,
                        MicroCreditOfficerEmployeeCustomerIndividualFirstName = creditOficer.EmployeeCustomerIndividualFirstName,
                        MicroCreditOfficerEmployeeCustomerIndividualLastName = creditOficer.EmployeeCustomerIndividualLastName,
                        MicroCreditOfficerEmployeeCustomerSalutationDescription = creditOficer.EmployeeCustomerSalutationDescription,

                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the customer details." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public async Task<ActionResult> GetParentGroupDetails(Guid microCreditGroupId)
        {
            try
            {
                var parentGroup = await _channelService.FindMicroCreditGroupAsync(microCreditGroupId, GetServiceHeader());

                if (parentGroup == null)
                {
                    return Json(new { success = false, message = "Micro-Credit-Group not found." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        MicroCreditOfficerEmployeeCustomerFullName = parentGroup.MicroCreditOfficerEmployeeCustomerFullName,


                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the micro-Credit Group details." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public async Task<ActionResult> AddGroupMember(MicroCreditGroupMemberDTO member)
        {
            try
            {
                var serviceHeader = GetServiceHeader();

                // Call the service to add the group member
                var result = await _channelService.AddMicroCreditGroupMemberAsync(member, serviceHeader);

                // If result is successful, return success response
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                // Handle exceptions and return error message
                return Json(new { success = false, message = "An error occurred while adding the member: " + ex.Message });
            }
        }


        [HttpPost]
        public async Task<ActionResult> Create(MicroCreditGroupDTO microCreditGroupDTO)
        {
            if (microCreditGroupDTO == null)
            {
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
                return View("Error"); 
            }

            microCreditGroupDTO.ValidateAll();

            if (!microCreditGroupDTO.HasErrors)
            {
                try
                {
                    var createdOfficer = await _channelService.AddMicroCreditGroupAsync(microCreditGroupDTO, GetServiceHeader());
                    TempData["Message"] = "Micro Credit Group created successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error creating Micro Credit Group: {ex.Message}");
                    TempData["ErrorMessage"] = "An error occurred while creating the Micro Credit Group. Please try again.";
                    return View(microCreditGroupDTO);
                }
            }
            else
            {
                foreach (var error in microCreditGroupDTO.ErrorMessages)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                TempData["ErrorMessage"] = "There were errors in your submission. Please review the form and try again.";
                return View(microCreditGroupDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var microCreditOfficerDTO = await _channelService.FindMicroCreditOfficerAsync(id, GetServiceHeader());

            return View(microCreditOfficerDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, MicroCreditOfficerDTO microCreditOfficerDTO)
        {
            try
            {
                bool updateSuccess = await _channelService.UpdateMicroCreditOfficerAsync(microCreditOfficerDTO, GetServiceHeader());

                if (updateSuccess)
                {
                    ViewBag.Message = "Micro-credit Officer updated successfully.";
                    ViewBag.IsSuccess = true;
                    return View("Index");
                }
                else
                {
                    ViewBag.Message = "Failed to update the Micro-credit Officer.";
                    ViewBag.IsSuccess = false;
                    return View(microCreditOfficerDTO);
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An error occurred: " + ex.Message;
                ViewBag.IsSuccess = false;
                return View(microCreditOfficerDTO);
            }


        }






    }
}
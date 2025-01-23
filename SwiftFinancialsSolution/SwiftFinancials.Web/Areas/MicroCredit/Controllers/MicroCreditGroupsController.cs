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
                0,
                int.MaxValue,
                GetServiceHeader()
            );

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(microCreditGroupDTO => microCreditGroupDTO.CreatedDate)
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
                items: new List<MicroCreditGroupDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
        );
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var microCreditGroupDTO = await _channelService.FindMicroCreditGroupAsync(id, GetServiceHeader());
            if (microCreditGroupDTO == null)
            {
                TempData["ErrorMessage"] = "MicroCredit Officer not found.";
                return RedirectToAction("Index");
            }

            return View(microCreditGroupDTO);
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
                        ParentGroup = parentGroup.MicroCreditOfficerEmployeeCustomerFullName,
                        ParentId = parentGroup.Id,


                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the micro-Credit Group details." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(MicroCreditGroupDTO microCreditGroupDTO)
        {
            if (microCreditGroupDTO == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Invalid data submitted. Please try again.";
                return View("Index");
            }

            try
            {
                microCreditGroupDTO.ValidateAll();
                if (microCreditGroupDTO.HasErrors)
                {
                    foreach (var error in microCreditGroupDTO.ErrorMessages)
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }

                    TempData["AlertType"] = "warning";
                    TempData["AlertMessage"] = "There were errors in your submission. Please review and try again.";
                    return View("Index", microCreditGroupDTO);
                }

                var createdGroup = await _channelService.AddMicroCreditGroupAsync(microCreditGroupDTO, GetServiceHeader());
                if (createdGroup == null)
                {
                    TempData["AlertType"] = "error";
                    TempData["AlertMessage"] = "An unexpected error occurred while creating the Micro Credit Group.";
                    return View("Index", microCreditGroupDTO);
                }

                TempData["AlertType"] = "success";
                TempData["AlertMessage"] = "Micro Credit Group created successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in Create action: {ex.Message}");

                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "An error occurred while processing your request. Please try again later.";
                return View("Index", microCreditGroupDTO);
            }
        }



        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.TypeDescriptionSelectList = GetMicroCreditGroupTypeSelectList(string.Empty);
            ViewBag.MeetingFrequencyDescriptionSelectList = GetMicroCreditGroupMeetingFrequencySelectList(string.Empty);
            ViewBag.MeetingDayOfWeekDescriptionSelectList = GetMicroCreditGroupMeetingDayOfWeekSelectList(string.Empty);
            ViewBag.DesignationSelectList = GetMicroCreditGroupMemberDesignationSelectList(string.Empty);

            var microCreditGroupDTO = await _channelService.FindMicroCreditGroupAsync(id, GetServiceHeader());
            if (microCreditGroupDTO == null)
            {
                TempData["ErrorMessage"] = "MicroCredit Group not found.";
                return RedirectToAction("Index");
            }

            TempData["MicroCreditGroupId"] = microCreditGroupDTO.Id;



            return View(microCreditGroupDTO);
        }


        [HttpPost]
        public async Task<ActionResult> AddMembersToGroup(List<MicroCreditGroupMemberDTO> members)
        {
            var microCreditGroupId = TempData["MicroCreditGroupId"] as Guid?;
            if (microCreditGroupId == null)
            {
                return Json(new { success = false, message = "MicroCreditGroupId not found in session." });
            }

            var groupMembers = TempData["GroupMembers"] as List<MicroCreditGroupMemberDTO> ?? new List<MicroCreditGroupMemberDTO>();
            var serviceHeader = GetServiceHeader();

            foreach (var member in members)
            {
                member.MicroCreditGroupId = microCreditGroupId.Value;

                var addedMember = await _channelService.AddMicroCreditGroupMemberAsync(member, serviceHeader);

                if (addedMember != null)
                {
                    groupMembers.Add(addedMember);
                }
                else
                {
                    return Json(new { success = false, message = "Failed to add one or more members to the group." });
                }
            }

            TempData["GroupMembers"] = groupMembers;
            TempData.Keep("GroupMembers");

            return Json(new { success = true, members = groupMembers, message = "Members added successfully!" });
        }



        [HttpPost]
        public async Task<ActionResult> RemoveMemberFromGroup(MicroCreditGroupMemberDTO member)
        {
            var groupMembers = TempData["GroupMembers"] as List<MicroCreditGroupMemberDTO> ?? new List<MicroCreditGroupMemberDTO>();
            var serviceHeader = GetServiceHeader();

            var memberToRemove = groupMembers.FirstOrDefault(m => m.CustomerId == member.CustomerId);
            if (memberToRemove == null)
            {
                return Json(new { success = false, message = "Member not found in the group." });
            }

            var membersToRemove = new ObservableCollection<MicroCreditGroupMemberDTO> { memberToRemove };

            bool removeSuccess = await _channelService.RemoveMicroCreditGroupMembersAsync(membersToRemove, serviceHeader);

            if (removeSuccess)
            {
                groupMembers.Remove(memberToRemove);

                TempData["GroupMembers"] = groupMembers;
                TempData.Keep("GroupMembers");

                return Json(new { success = true, message = "Member removed successfully!" });
            }
            else
            {
                return Json(new { success = false, message = "Failed to remove member from the group." });
            }
        }





        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, MicroCreditGroupDTO microCreditGroupDTO)
        {
            try
            {
               

                var serviceHeader = GetServiceHeader();

                bool updateSuccess = await _channelService.UpdateMicroCreditGroupAsync(microCreditGroupDTO, serviceHeader);

                if (updateSuccess)
                {
                    TempData["AlertType"] = "success";
                    TempData["AlertMessage"] = "MicroCredit Group updated successfully.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["AlertType"] = "error";
                    TempData["AlertMessage"] = "Failed to update the MicroCredit Group.";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in Create action: {ex.Message}");

                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "An error occurred while processing your request. Please try again later.";
                return View("Index", microCreditGroupDTO);
            }


            return View("Edit", microCreditGroupDTO);
        }


        public async Task<ActionResult> Apportionment(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.TypeDescriptionSelectList = GetMicroCreditGroupTypeSelectList(string.Empty);
            ViewBag.MeetingFrequencyDescriptionSelectList = GetMicroCreditGroupMeetingFrequencySelectList(string.Empty);
            ViewBag.MeetingDayOfWeekDescriptionSelectList = GetMicroCreditGroupMeetingDayOfWeekSelectList(string.Empty);
            ViewBag.DesignationSelectList = GetMicroCreditGroupMemberDesignationSelectList(string.Empty);

            var microCreditGroupDTO = await _channelService.FindMicroCreditGroupAsync(id, GetServiceHeader());
            if (microCreditGroupDTO == null)
            {
                TempData["ErrorMessage"] = "MicroCredit Group not found.";
                return RedirectToAction("Index");
            }

            TempData["MicroCreditGroupId"] = microCreditGroupDTO.Id;



            return View(microCreditGroupDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Apportionment(Guid id, MicroCreditGroupDTO microCreditGroupDTO)
        {
            try
            {


                var serviceHeader = GetServiceHeader();

                bool updateSuccess = await _channelService.UpdateMicroCreditGroupAsync(microCreditGroupDTO, serviceHeader);

                if (updateSuccess)
                {
                    TempData["AlertType"] = "success";
                    TempData["AlertMessage"] = "MicroCredit Group updated successfully.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["AlertType"] = "error";
                    TempData["AlertMessage"] = "Failed to update the MicroCredit Group.";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in Create action: {ex.Message}");

                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "An error occurred while processing your request. Please try again later.";
                return View("Index", microCreditGroupDTO);
            }


            return View("Edit", microCreditGroupDTO);
        }


    }
}
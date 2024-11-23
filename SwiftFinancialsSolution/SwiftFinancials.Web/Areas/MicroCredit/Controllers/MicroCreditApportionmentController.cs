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
    public class MicroCreditApportionmentController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, Guid microCreditGroupId)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;

            bool sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc";
            var sortedColumns = jQueryDataTablesModel.GetSortedColumns().Select(s => s.PropertyName).ToList();

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;

            // Fetch paginated data using microCreditGroupId
            var pageCollectionInfo = await _channelService.FindMicroCreditGroupMembersByMicroCreditGroupIdInPageAsync(
                microCreditGroupId,  // Ensure the group ID is passed
                jQueryDataTablesModel.sSearch,
                pageIndex,
                pageSize,
                GetServiceHeader()
            );

            if (pageCollectionInfo != null)
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                // Sort data based on columns
                var sortedData = sortAscending
                    ? pageCollectionInfo.PageCollection
                        .OrderBy(item => sortedColumns.Contains("CreatedDate") ? item.CreatedDate : default(DateTime))
                        .ToList()
                    : pageCollectionInfo.PageCollection
                        .OrderByDescending(item => sortedColumns.Contains("CreatedDate") ? item.CreatedDate : default(DateTime))
                        .ToList();

                // Adjust record count for search
                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? sortedData.Count : totalRecordCount;

                // Return DataTables-compatible JSON
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
                    items: new List<MicroCreditGroupMemberDTO>(),
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
        }


        [HttpPost]
        public async Task<JsonResult> GetGroupDetailsAndMembers(JQueryDataTablesModel jQueryDataTablesModel, Guid? microCreditGroupId)
        {
            if (!microCreditGroupId.HasValue)
            {
                return Json(new
                {
                    draw = jQueryDataTablesModel.sEcho,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<MicroCreditGroupMemberDTO>()
                });
            }

            int totalRecordCount = 0;
            int searchRecordCount = 0;

            try
            {
                var sortAscending = jQueryDataTablesModel.sSortDir_.FirstOrDefault() == "asc";
                var sortColumn = jQueryDataTablesModel.GetSortedColumns().FirstOrDefault()?.PropertyName ?? "CustomerFullName";
                var pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
                var pageSize = jQueryDataTablesModel.iDisplayLength;

                var serviceHeader = GetServiceHeader();
                var groupMembersPage = await _channelService.FindMicroCreditGroupMembersByMicroCreditGroupIdInPageAsync(
                    microCreditGroupId.Value,
                    jQueryDataTablesModel.sSearch,
                    pageIndex,
                    pageSize,
                    serviceHeader
                );

                if (groupMembersPage != null)
                {
                    totalRecordCount = groupMembersPage.ItemsCount;
                    searchRecordCount = groupMembersPage.PageCollection.Count;

                    var sortedData = sortAscending
                        ? groupMembersPage.PageCollection.OrderBy(item => item.GetType().GetProperty(sortColumn)?.GetValue(item, null)).ToList()
                        : groupMembersPage.PageCollection.OrderByDescending(item => item.GetType().GetProperty(sortColumn)?.GetValue(item, null)).ToList();

                    return Json(new
                    {
                        draw = jQueryDataTablesModel.sEcho,
                        recordsTotal = totalRecordCount,
                        recordsFiltered = totalRecordCount, // Corrected to reflect the actual total
                        data = sortedData
                    });
                }
            }
            catch (Exception ex)
            {
                // Log exception here
            }

            return Json(new
            {
                draw = jQueryDataTablesModel.sEcho,
                recordsTotal = totalRecordCount,
                recordsFiltered = searchRecordCount,
                data = new List<MicroCreditGroupMemberDTO>()
            });
        }





        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.TypeDescriptionSelectList = GetMicroCreditGroupTypeSelectList(string.Empty);
            ViewBag.MeetingFrequencyDescriptionSelectList = GetMicroCreditGroupMeetingFrequencySelectList(string.Empty);
            ViewBag.MeetingDayOfWeekDescriptionSelectList = GetMicroCreditGroupMeetingDayOfWeekSelectList(string.Empty);
            ViewBag.DesignationSelectList = GetMicroCreditGroupMemberDesignationSelectList(string.Empty);

            // Fetch the MicroCreditGroup details
            var microCreditGroupDTO = await _channelService.FindMicroCreditGroupAsync(id, GetServiceHeader());
            if (microCreditGroupDTO == null)
            {
                TempData["ErrorMessage"] = "MicroCredit Group not found.";
                return RedirectToAction("Index");
            }

            // Fetch the group members using the provided method
            var groupMembers = await _channelService.FindMicroCreditGroupMembersByMicroCreditGroupCustomerIdAsync(id, GetServiceHeader());

            // Pass the group members to the view
            ViewBag.GroupMembers = groupMembers;

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
                        MicroCreditOfficerEmployeeCustomerSalutationDescription = customer.IndividualSalutationDescription,
                        CustomerAccountCustomerReference1 = customer.Reference1,
                        CustomerAccountCustomerReference2 = customer.Reference2,
                        CustomerAccountCustomerReference3 = customer.Reference3,
                        AccountStatus = customer.StationDescription,


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

                Session["MicroCreditGroupId"] = customer.Id;
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
                        ParentId = parentGroup.Id,
                        ParentMicroCreditDescription = parentGroup.MicroCreditOfficerEmployeeCustomerIndividualFirstName,
                        MicroCreditGroupCustomerId = parentGroup.CustomerId,
                        MinimumMembers = parentGroup.MinimumMembers,
                        MaximumMembers = parentGroup.MaximumMembers,
                        MicroCreditOfficerEmployeeCustomerSalutationDescription = parentGroup.MicroCreditOfficerEmployeeCustomerSalutationDescription,
                        CustomerStationDescription = parentGroup.CustomerStationDescription,
                        CustomerNonIndividualRegistrationNumber = parentGroup.CustomerNonIndividualRegistrationNumber,
                        CustomerNonIndividualDescription = parentGroup.CustomerNonIndividualDescription,
                        CustomerAccountFullAccountNumber = parentGroup.CustomerAccountFullAccountNumber,
                        CustomerId = parentGroup.CustomerId,
                        CustomerStationId = parentGroup.CustomerStationId,
                        CustomerRegistrationDate = parentGroup.CustomerRegistrationDate,
                        CustomerNonIndividualDateEstablished = parentGroup.CustomerNonIndividualDateEstablished,
                        CustomerAccountCustomerReference1 = parentGroup.CustomerAccountCustomerReference1,
                        CustomerAccountCustomerReference2 = parentGroup.CustomerAccountCustomerReference2,
                        CustomerAccountCustomerReference3 = parentGroup.CustomerAccountCustomerReference3,
                        AccountStatus = parentGroup.AccountStatus,
                        MicroCreditOfficerEmployeeCustomerFullName = parentGroup.MicroCreditOfficerEmployeeCustomerFullName,
                        Activities = parentGroup.Activities,
                        Purpose = parentGroup.Purpose,
                        TypeDescription = parentGroup.TypeDescription,



                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the micro-Credit Group details." }, JsonRequestBehavior.AllowGet);
            }
        }





        [HttpGet]
        public async Task<ActionResult> GetParentGroupmemberDetails(Guid customerId)
        {
            try
            {
                var parentGroup = await _channelService.FindMicroCreditGroupMemberByCustomerIdAsync(customerId, GetServiceHeader());

                if (parentGroup == null)
                {
                    return Json(new { success = false, message = "Micro-Credit-Group not found." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        CustomerAccountFullAccountNumber = parentGroup.CustomerAccountFullAccountNumber,
                        ParentId = parentGroup.Id,
                        MicroCreditGroupId = parentGroup.MicroCreditGroupId,
                        MicroCreditGroupCustomerId = parentGroup.MicroCreditGroupCustomerId,
                        CustomerId = parentGroup.CustomerId,
                        CustomerTypeDescription = parentGroup.CustomerTypeDescription,
                        CustomerIndividualSalutationDescription = parentGroup.CustomerIndividualSalutationDescription,
                        CustomerIndividualIdentityCardTypeDescription = parentGroup.CustomerIndividualIdentityCardTypeDescription,
                        CustomerIndividualIdentityCardNumber = parentGroup.CustomerIndividualIdentityCardNumber,
                        CustomerIndividualIdentityCardSerialNumber = parentGroup.CustomerIndividualIdentityCardSerialNumber,
                        CustomerIndividualNationalityDescription = parentGroup.CustomerIndividualNationalityDescription,
                        CustomerSerialNumber = parentGroup.CustomerSerialNumber,
                        CustomerIndividualPayrollNumbers = parentGroup.CustomerIndividualPayrollNumbers,
                        CustomerFullName = parentGroup.CustomerFullName,
                        CustomerIndividualGenderDescription = parentGroup.CustomerIndividualGenderDescription,
                        CustomerStationDescription = parentGroup.CustomerStationDescription,
                        CustomerReference1 = parentGroup.CustomerReference1,
                        CustomerReference2 = parentGroup.CustomerReference2,
                        CustomerReference3 = parentGroup.CustomerReference3,


                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the micro-Credit Group details." }, JsonRequestBehavior.AllowGet);
            }
        }



        [HttpPost]
        public async Task<ActionResult> Create(MicroCreditGroupDTO microCreditGroupDTO)
        {
            if (microCreditGroupDTO == null)
            {
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
                return View("Index");
            }

            microCreditGroupDTO.ValidateAll();

            if (!microCreditGroupDTO.HasErrors)
            {
                try
                {
                    var createdOfficer = await _channelService.AddMicroCreditGroupAsync(microCreditGroupDTO, GetServiceHeader());
                    MessageBox.Show(
                                                              "Operation Success",
                                                              "Customer Receipts",
                                                              MessageBoxButtons.OK,
                                                              MessageBoxIcon.Information,
                                                              MessageBoxDefaultButton.Button1,
                                                              MessageBoxOptions.ServiceNotification
                                                          );
                    TempData["SuccessMessage"] = "Micro Credit Group created successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error creating Micro Credit Group: {ex.Message}");
                    TempData["ErrorMessage"] = "An error occurred while creating the Micro Credit Group. Please try again.";
                    return View("Index");
                }
            }
            else
            {
                foreach (var error in microCreditGroupDTO.ErrorMessages)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                TempData["ErrorMessage"] = "There were errors in your submission. Please review the form and try again.";
                return View("Index");
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
                return new JsonResult { Data = new { success = false, message = "MicroCreditGroupId not found in session." } };
            }

            var groupMembers = TempData["GroupMembers"] as List<MicroCreditGroupMemberDTO> ?? new List<MicroCreditGroupMemberDTO>();
            var serviceHeader = GetServiceHeader();

            foreach (var member in members)
            {
                member.MicroCreditGroupId = microCreditGroupId.Value;

                var addedMember = await _channelService.AddMicroCreditGroupMemberAsync(member, serviceHeader);
                MessageBox.Show(
                                                              "Operation Success",
                                                              "Customer Receipts",
                                                              MessageBoxButtons.OK,
                                                              MessageBoxIcon.Information,
                                                              MessageBoxDefaultButton.Button1,
                                                              MessageBoxOptions.ServiceNotification
                                                          );

                if (addedMember != null)
                {
                    groupMembers.Add(addedMember);
                }
                else
                {
                    return new JsonResult { Data = new { success = false, message = "Failed to add one or more members to the group." } };
                }
            }

            TempData["GroupMembers"] = groupMembers;
            TempData.Keep("GroupMembers");

            return new JsonResult { Data = new { success = true, members = groupMembers } };
        }


        [HttpPost]
        public async Task<ActionResult> RemoveMemberFromGroup(MicroCreditGroupMemberDTO member)
        {
            var groupMembers = TempData["GroupMembers"] as List<MicroCreditGroupMemberDTO> ?? new List<MicroCreditGroupMemberDTO>();
            var serviceHeader = GetServiceHeader();

            var memberToRemove = groupMembers.FirstOrDefault(m => m.CustomerId == member.CustomerId);
            if (memberToRemove == null)
            {
                return new JsonResult { Data = new { success = false, message = "Member not found in the group." } };
            }

            var membersToRemove = new ObservableCollection<MicroCreditGroupMemberDTO> { memberToRemove };

            bool removeSuccess = await _channelService.RemoveMicroCreditGroupMembersAsync(membersToRemove, serviceHeader);
            MessageBox.Show(
                                                              "Operation Success",
                                                              "Customer Receipts",
                                                              MessageBoxButtons.OK,
                                                              MessageBoxIcon.Information,
                                                              MessageBoxDefaultButton.Button1,
                                                              MessageBoxOptions.ServiceNotification
                                                          );

            if (removeSuccess)
            {
                groupMembers.Remove(memberToRemove);

                TempData["GroupMembers"] = groupMembers;
                TempData.Keep("GroupMembers");

                return new JsonResult { Data = new { success = true } };
            }
            else
            {
                return new JsonResult { Data = new { success = false, message = "Failed to remove member from the group." } };
            }
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Update(Guid id, MicroCreditGroupDTO microCreditGroupDTO)
        {
            try
            {
                var storedGroupId = TempData["MicroCreditGroupId"] as Guid?;
                if (!storedGroupId.HasValue || storedGroupId.Value != id)
                {
                    TempData["ErrorMessage"] = "The MicroCredit Group ID is invalid.";
                    return RedirectToAction("Index");
                }

                var serviceHeader = GetServiceHeader();

                bool updateSuccess = await _channelService.UpdateMicroCreditGroupAsync(microCreditGroupDTO, serviceHeader);
                MessageBox.Show(
                                                              "Operation Success",
                                                              "Customer Receipts",
                                                              MessageBoxButtons.OK,
                                                              MessageBoxIcon.Information,
                                                              MessageBoxDefaultButton.Button1,
                                                              MessageBoxOptions.ServiceNotification
                                                          );

                if (updateSuccess)
                {
                    TempData["SuccessMessage"] = "MicroCredit Group updated successfully.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update the MicroCredit Group.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
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













    }
}
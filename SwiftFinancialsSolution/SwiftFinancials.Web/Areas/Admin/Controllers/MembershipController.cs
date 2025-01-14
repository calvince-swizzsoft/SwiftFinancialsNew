
using Application.MainBoundedContext.DTO.AdministrationModule;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using SwiftFinancials.Web.Attributes;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using Application.MainBoundedContext.DTO;
using SwiftFinancials.Presentation.Infrastructure.Util;
using System.Collections.ObjectModel;
using DistributedServices.Seedwork.EndpointBehaviors;
using System.ServiceModel;

namespace SwiftFinancials.Web.Areas.Admin.Controllers
{
    [RoleBasedAccessControl]
    public class MembershipController : MasterController
    {
        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

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

            var search = jQueryDataTablesModel.sSearch.ToLower();

            var pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var pageCollectionInfo = await _channelService.FindMembershipByFilterInPageAsync(jQueryDataTablesModel.sSearch, 0, int.MaxValue, sortedColumns, sortAscending, GetServiceHeader());

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
                items: new List<UserDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var applicationUser = await _applicationUserManager.FindByIdAsync(id.ToString());

            var CompanyDTO = new BranchDTO();

            string CompanyDescription = string.Empty;

            string roleName = string.Empty;

            if (applicationUser.BranchId != Guid.Empty && applicationUser.BranchId != null)
            {
                CompanyDTO = await _channelService.FindBranchAsync((Guid)applicationUser.BranchId, GetServiceHeader());

                if (CompanyDTO != null)
                    CompanyDescription = CompanyDTO.Description;
            }

            var userRoles = await _applicationUserManager.GetRolesAsync(applicationUser.Id);
            ViewBag.UserRoles = userRoles;

            var branches = await _channelService.FindBranchesAsync(GetServiceHeader());
            var userBranches = branches.Where(x => x.Id == applicationUser.BranchId);
            ViewBag.UserBranches = userBranches;

            if (userRoles != null)
                roleName = userRoles.FirstOrDefault();
            var userDTO = new UserDTO
            {
                Id = applicationUser.Id,
                FirstName = applicationUser.FirstName + "  " +
              applicationUser.OtherNames,
                Email = applicationUser.Email,
                PhoneNumber = applicationUser.PhoneNumber,
                BranchId = applicationUser.BranchId,
                BranchDescription = CompanyDTO.Description,
                UserName = applicationUser.UserName,
                RoleName = roleName,
                TwoFactorEnabled = applicationUser.TwoFactorEnabled,
                LockoutEnabled = applicationUser.LockoutEnabled,
                CreatedDate = applicationUser.CreatedDate
            };

            return View("Details", userDTO.MapTo<UserBindingModel>());
        }

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();
            // var roles = await _channelService.GetAllRolesAsync(GetServiceHeader());
            var roles = _applicationRoleManager.Roles.ToList();

            //
            ViewBag.Commisions = roles.Select(role => new { role.Name }).ToList();

            ObservableCollection<string> rol = new ObservableCollection<string>();
            foreach (var ro in roles)
            {
                string role = ro.Name;

                rol.Add(role);

            }
            ViewBag.roles = rol;
            Guid parseId;
            var branch = await _channelService.FindBranchesAsync(GetServiceHeader());
            ViewBag.branches = branch;
            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            UserBindingModel userBindingModel = new UserBindingModel();

            //if (branch != null)
            //{
            //    userBindingModel.BranchId = branch.Id;
            //    userBindingModel.BranchDescription = branch.Description;
            //}


            return View(userBindingModel);
        }

     
        public async Task<ActionResult> GetEmployeeDetails(Guid? id)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var employee = await _channelService.FindEmployeeAsync(parseId, GetServiceHeader());
            var dep = await _channelService.FindEmployeeTypeAsync(employee.EmployeeTypeId, GetServiceHeader());
            var Department = employee.DepartmentDescription;

            if (employee != null)
            {
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        EmployeeCustomerFullName = employee.Customer.FullName,
                        EmployeeCustomerId = employee.CustomerId,
                        EmployeeId = employee.Id,
                        EmployeeBloodGroupDescription = employee.BloodGroupDescription,
                        EmployeeNationalHospitalInsuranceFundNumber = employee.NationalHospitalInsuranceFundNumber,
                        EmployeeNationalSocialSecurityFundNumber = employee.NationalSocialSecurityFundNumber,
                        EmployeeCustomerPersonalIdentificationNumber = employee.CustomerPersonalIdentificationNumber,
                        EmployeeEmployeeTypeCategoryDescription = employee.EmployeeTypeCategoryDescription,
                        EmployeeEmployeeTypeDescription = employee.EmployeeTypeDescription,
                        EmployeeDepartmentDescription = employee.DepartmentDescription,
                        EmployeeDepartmentId = employee.DepartmentId,
                        EmployeeDesignationDescription = employee.DesignationDescription,
                        EmployeeDesignationId = employee.DesignationId,
                        EmployeeBranchDescription = employee.BranchDescription,
                        EmployeeBranchId = employee.BranchId,
                        EmployeeCustomerIndividualGenderDescription = employee.CustomerIndividualGenderDescription,
                        EmployeeCustomerIndividualPayrollNumbers = employee.CustomerIndividualPayrollNumbers,
                        EmployeeEmployeeTypeId = employee.EmployeeTypeId,
                        EmployeeEmployeeTypeChartOfAccountId = employee.EmployeeTypeChartOfAccountId,
                        PhoneNumber = employee.CustomerAddressMobileLine,
                        BranchId = employee.BranchId,
                        Branch = employee.BranchDescription,
                        department = Department,
                    }
                });
            }
            return Json(new { success = false, message = "Employee Not Found!" });
        }


        [HttpPost]
        public async Task<ActionResult> Create(UserBindingModel userBindingModel, string[] Branchids, string[] roles)
        {

            var employee = await _channelService.FindEmployeeAsync(userBindingModel.EmployeeId, GetServiceHeader());
            userBindingModel.FirstName = employee.CustomerIndividualFirstName;
            userBindingModel.OtherNames = employee.CustomerIndividualLastName;
            userBindingModel.PhoneNumber = employee.CustomerAddressMobileLine;
            //userBindingModel.EmployeeId = employee.Id;

            userBindingModel.ValidateAll();

            if (userBindingModel.HasErrors)
            {
                await ServeNavigationMenus();

                TempData["Error"] = userBindingModel.ErrorMessages;
                // var roles = await _channelService.GetAllRolesAsync(GetServiceHeader());           
                return RedirectToAction("Create");
            }
            List<AuditTrailDTO> auditTrailDTOs = new List<AuditTrailDTO>();
            ObservableCollection<string> role = new ObservableCollection<string>();
            foreach (var ro in roles)
            {
                //string processedString = ro.Replace("{ Name =", "").Trim().TrimEnd('}').Trim();

                role.Add(ro);

            }


            var userDTO = await _channelService.AddNewMembershipAsync(userBindingModel.MapTo<UserDTO>(), GetServiceHeader());

            //          var result = await _channelService.AddUserToRolesAsync(userDTO.UserName, role, GetServiceHeader());

            AuditTrailDTO auditLogDTO = new AuditTrailDTO();
            var serviceHeader = CustomHeaderUtility.ReadHeader(OperationContext.Current);
            auditLogDTO.EnvironmentIPAddress = serviceHeader.EnvironmentIPAddress;
            auditLogDTO.EnvironmentMACAddress = serviceHeader.EnvironmentMACAddress;
            auditLogDTO.EnvironmentMachineName = serviceHeader.EnvironmentMachineName;
            auditLogDTO.ApplicationUserName = serviceHeader.ApplicationUserName;
            auditLogDTO.EnvironmentMotherboardSerialNumber = serviceHeader.EnvironmentMotherboardSerialNumber;
            auditLogDTO.EnvironmentProcessorId = serviceHeader.EnvironmentProcessorId;
            auditLogDTO.EnvironmentDomainName = serviceHeader.EnvironmentDomainName;
            auditLogDTO.EnvironmentOSVersion = serviceHeader.EnvironmentOSVersion;
            auditTrailDTOs.Add(auditLogDTO);

            var k = await _channelService.AddAuditTrailsAsync(auditTrailDTOs, GetServiceHeader());


            if (userDTO != null)
            {
                TempData["SuccessMessage"] = "User Registered Successfully";

                return RedirectToAction("Index", "Membership", new { Area = "Admin" });
            }

            TempData["Error"] = "Create Membership Failed!";

            return View();
        }

        // GET: SystemUser/Edit/5
        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var applicationUser = await _applicationUserManager.FindByIdAsync(id.ToString());

            var employeeDetails = await _channelService.FindEmployeeAsync((Guid)applicationUser.EmployeeId, GetServiceHeader());

            var CompanyDTO = new BranchDTO();

            string CompanyDescription = string.Empty;

            string roleName = string.Empty;

            if (applicationUser.BranchId != Guid.Empty && applicationUser.BranchId != null)
            {
                CompanyDTO = await _channelService.FindBranchAsync((Guid)applicationUser.BranchId, GetServiceHeader());

                if (CompanyDTO != null)
                    CompanyDescription = CompanyDTO.Description;
            }

            var userRoles = await _applicationUserManager.GetRolesAsync(applicationUser.Id);
            ViewBag.UserRoles = userRoles;

            var branches = await _channelService.FindBranchesAsync(GetServiceHeader());
            var userBranches = branches.Where(x => x.Id == applicationUser.BranchId);
            ViewBag.UserBranches = userBranches;

            if (userRoles != null)
                roleName = userRoles.FirstOrDefault();
            var userDTO = new UserDTO
            {
                Id = applicationUser.Id,
                FirstName = applicationUser.FirstName + "  " + applicationUser.OtherNames,
                Email = applicationUser.Email,
                PhoneNumber = applicationUser.PhoneNumber,
                BranchId = applicationUser.BranchId,
                BranchDescription = CompanyDTO.Description,
                UserName = applicationUser.UserName,
                RoleName = roleName,
                TwoFactorEnabled = applicationUser.TwoFactorEnabled,
                LockoutEnabled = applicationUser.LockoutEnabled,
                CreatedDate = applicationUser.CreatedDate,
                EmployeeId = (Guid)applicationUser.EmployeeId,
            };

            return View("Edit", userDTO.MapTo<UserBindingModel>());
        }




        // POST: SystemUser/Edit/5
        [HttpPost]
        public async Task<ActionResult> Edit(UserBindingModel userBindingModel)
        {
            userBindingModel.ValidateAll();

            if (userBindingModel.HasErrors)
            {
                await ServeNavigationMenus();

                TempData["Error"] = userBindingModel.ErrorMessages;

                return View();
            }

            var result = await _channelService.UpdateMembershipAsync(userBindingModel.MapTo<UserDTO>(), GetServiceHeader());

            if (result)
            {
                TempData["Success"] = "User Updated Successfully";

                return RedirectToAction("Index", "Membership", new { Area = "Admin" });
            }

            TempData["Error"] = "Update Membership Failed!";

            return View();
        }
    }
}
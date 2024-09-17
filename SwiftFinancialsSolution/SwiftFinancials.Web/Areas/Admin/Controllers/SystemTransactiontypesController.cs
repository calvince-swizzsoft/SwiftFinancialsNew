using Application.MainBoundedContext.DTO.AdministrationModule;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using Microsoft.AspNet.Identity.EntityFramework;
using DistributedServices.MainBoundedContext.Identity;
using SwiftFinancials.Presentation.Infrastructure.Models;
using SwiftFinancials.Web.Areas.Admin.Models;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Attributes;

namespace SwiftFinancials.Web.Areas.Admin.Controllers
{
    [RoleBasedAccessControl]
    public class SystemTransactiontypesController : MasterController
    {
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            ViewBag.SystemPermissionTypeSelectList = GetsystemPermissionTypeList(string.Empty);
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetBranchesAsync()
        {

            await ServeNavigationMenus();
            var branchesDTOs = await _channelService.FindBranchesAsync(GetServiceHeader());
            return Json(branchesDTOs, JsonRequestBehavior.AllowGet);
        }

       
        [HttpGet]
        public async Task<JsonResult> GetAction(int systemPermissionTypeId)
        {
            await ServeNavigationMenus();
            var allRoles = _applicationRoleManager.Roles.ToList();
            var allBranches = await _channelService.FindBranchesAsync(GetServiceHeader());

            var linkedRoles = await _channelService.GetRolesForSystemPermissionTypeAsync(systemPermissionTypeId, GetServiceHeader());
            var linkedBranches = await _channelService.GetBranchesForSystemPermissionTypeAsync(systemPermissionTypeId, GetServiceHeader());

            return Json(new
            {
                allRoles = allRoles.Select(role => new { role.Id, role.Name }).ToList(),
                allBranches = allBranches.Select(branch => new { branch.Id, branch.Description }).ToList(),
                linkedRoles = linkedRoles,
                linkedBranches = linkedBranches.Select(branch => new { branch.Id }).ToList()
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetRoles()
        {
            var roles = _applicationRoleManager.Roles.ToList();
            return Json(roles, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> Create(SystemPermissionTypeInRoleDTO roleBindingModel, string selectedRoles, string selectedBranches, int systemPermissionType)
        {
            await ServeNavigationMenus();
            ViewBag.SystemPermissionTypeSelectList = GetsystemPermissionTypeList(string.Empty);

            roleBindingModel.systemPermissionTypeInRoles = ParseSelectedRoles(selectedRoles);
            roleBindingModel.systemPermissionTypeInBranchDTOs = ParseSelectedBranches(selectedBranches);

            if (!roleBindingModel.HasErrors)
            {
                await _channelService.AddSystemPermissionTypeToRolesAsync(systemPermissionType, roleBindingModel.systemPermissionTypeInRoles, GetServiceHeader());
                await _channelService.AddSystemPermissionTypeToBranchesAsync(systemPermissionType, roleBindingModel.systemPermissionTypeInBranchDTOs, GetServiceHeader());
                await _channelService.MapSystemPermissionTypeToRolesAsync(systemPermissionType, roleBindingModel.systemPermissionTypeInRoles, GetServiceHeader());
                await _channelService.MapSystemPermissionTypeToBranchesAsync(systemPermissionType, roleBindingModel.systemPermissionTypeInBranchDTOs, GetServiceHeader());

                //    // Success notification
                //    var successMessage = new
                //    {
                //        success = true,
                //        message = "System Permissions Linked Successfully!",
                //        icon = "✔️" // Checkmark icon for success
                //    };

                //    return Json(successMessage, JsonRequestBehavior.AllowGet);
                //}

                //// Error notification
                //var errorMessage = new
                //{
                //    success = false,
                //    message = "There was an error linking system permissions.",
                //    icon = "❌" // X icon for failure
                //};

                return View("Create");
            }

            await ServeNavigationMenus();
            return View("Create");
        }


        public ActionResult SendNotification()
        {
            var notificationMessage = new
            {
                success = true,
                message = "Notification sent successfully!"
            };

            return Json(notificationMessage, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public async Task<JsonResult> Remove(SystemPermissionTypeInRoleDTO roleBindingModel, string selectedRoles, string selectedBranches, int systemPermissionType)
        {
            await ServeNavigationMenus();
            ViewBag.SystemPermissionTypeSelectList = GetsystemPermissionTypeList(string.Empty);

            roleBindingModel.systemPermissionTypeInRoles = ParseSelectedRoles(selectedRoles);
            roleBindingModel.systemPermissionTypeInBranchDTOs = ParseSelectedBranches(selectedBranches);

            var linkedRoles = await _channelService.GetRolesForSystemPermissionTypeAsync(systemPermissionType, GetServiceHeader());
            var linkedBranches = await _channelService.GetBranchesForSystemPermissionTypeAsync(systemPermissionType, GetServiceHeader());
            ObservableCollection<string> k = new ObservableCollection<string>();
            foreach (var n in roleBindingModel.systemPermissionTypeInRoles)
            {
                string p = n.RoleName;
                k.Add(p);
            }
            if (!roleBindingModel.HasErrors)
            {
                await _channelService.RemoveSystemPermissionTypeFromRolesAsync(roleBindingModel.SystemPermissionType, linkedRoles, GetServiceHeader());
                await _channelService.RemoveSystemPermissionTypeFromBranchesAsync(roleBindingModel.SystemPermissionType, linkedBranches, GetServiceHeader());

                return Json(new { success = true, message = "System Permissions Updated Successfully" });
            }

            await ServeNavigationMenus();
            return Json(new { success = false, message = "There was an error updating system permissions." });
        }

        private ObservableCollection<SystemPermissionTypeInRoleDTO> ParseSelectedRoles(string selectedRoles)
        {

            if (string.IsNullOrEmpty(selectedRoles))
                return new ObservableCollection<SystemPermissionTypeInRoleDTO>();

            try
            {
                var roles = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SystemPermissionTypeInRoleDTO>>(selectedRoles);
                return new ObservableCollection<SystemPermissionTypeInRoleDTO>(roles.Select(role => new SystemPermissionTypeInRoleDTO
                {
                    RoleId = Guid.Parse(role.RoleId.ToString()),
                    RoleName = role.RoleName
                }).ToList());
            }
            catch (Exception ex)
            {
                // Handle parsing errors
                // Log exception if necessary
                return new ObservableCollection<SystemPermissionTypeInRoleDTO>();
            }
        }

        private ObservableCollection<BranchDTO> ParseSelectedBranches(string selectedBranches)
        {
            if (string.IsNullOrEmpty(selectedBranches))
                return new ObservableCollection<BranchDTO>();

            try
            {
                var branches = Newtonsoft.Json.JsonConvert.DeserializeObject<List<BranchDTO>>(selectedBranches);
                return new ObservableCollection<BranchDTO>(branches.Select(branch => new BranchDTO
                {
                    Id = Guid.Parse(branch.Id.ToString()),
                    Description = branch.Description
                }).ToList());
            }
            catch (Exception ex)
            {
                // Handle parsing errors
                // Log exception if necessary
                return new ObservableCollection<BranchDTO>();
            }
        }
    }
}

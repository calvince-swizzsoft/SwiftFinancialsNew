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
            return View("Create");
        }

        [HttpGet]
        public async Task<JsonResult> GetBranchesAsync()
        {

            await ServeNavigationMenus();
            var branchesDTOs = await _channelService.FindBranchesAsync(GetServiceHeader());
            return Json(branchesDTOs, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public async Task<JsonResult> MarkRoleCheckbox(int navigationItemCode, string roleName)
        {
            await _applicationRoleManager.FindByNameAsync(roleName);
            //bool result =aw
            //if (result == true)
            //{
            //    return Json(1, JsonRequestBehavior.AllowGet);
            //}
            //else
            //{
            //    return Json(2, JsonRequestBehavior.AllowGet);
            //}
            return Json(2, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<JsonResult> GetAction(int systemPermissionTypeId)
        {
            await ServeNavigationMenus();
            var allRoles = _applicationRoleManager.Roles.ToList();
            var allBranches = await _channelService.FindBranchesAsync(GetServiceHeader());

            var linkedRoles = await _channelService.GetRolesForSystemPermissionTypeAsync(systemPermissionTypeId, GetServiceHeader());
            var linkedBranches = await _channelService.GetBranchesForSystemPermissionTypeAsync(systemPermissionTypeId, GetServiceHeader());

            var foundRoles = new ObservableCollection<RoleDTO>();
            foreach (var roleName in linkedRoles)
            {
                var role = await _applicationRoleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    foundRoles.Add(new RoleDTO { Id = role.Id, Name = role.Name });
                }
            }

            return Json(new
            {
                allRoles = allRoles.Select(role => new { role.Id, role.Name }).ToList(),
                allBranches = allBranches.Select(branch => new { branch.Id, branch.Description }).ToList(),
                linkedRoles = foundRoles.Select(role => new { role.Id, role.Name }).ToList(),
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
            // Parse selectedRoles (roleId|roleName format)

            roleBindingModel.systemPermissionTypeInRoles = ParseSelectedRoles(selectedRoles);
            roleBindingModel.systemPermissionTypeInBranchDTOs = ParseSelectedBranches(selectedBranches);

            if (!roleBindingModel.HasErrors)
            {
                await _channelService.AddSystemPermissionTypeToRolesAsync(systemPermissionType, roleBindingModel.systemPermissionTypeInRoles, GetServiceHeader());
                await _channelService.AddSystemPermissionTypeToBranchesAsync(systemPermissionType, roleBindingModel.systemPermissionTypeInBranchDTOs, GetServiceHeader());
                //await _channelService.MapSystemPermissionTypeToRolesAsync(systemPermissionType, roleBindingModel.systemPermissionTypeInRoles, GetServiceHeader());
                //await _channelService.MapSystemPermissionTypeToBranchesAsync(systemPermissionType, roleBindingModel.systemPermissionTypeInBranchDTOs, GetServiceHeader());

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = true, message = "System Permissions Updated Successfully" });
                }

                TempData["SuccessMessage"] = "System Permissions Updated Successfully";
                return RedirectToAction("Create"); // Redirect back to the Create view
            }

            await ServeNavigationMenus();
            return View("Create", roleBindingModel); // Return the same view with model data
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
        public async Task<ActionResult> Remove(SystemPermissionTypeInRoleDTO roleBindingModel, string selectedRoles, string selectedBranches, int systemPermissionType)
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
                await _channelService.RemoveSystemPermissionTypeFromRolesAsync(roleBindingModel.SystemPermissionType, k, GetServiceHeader());
                await _channelService.RemoveSystemPermissionTypeFromBranchesAsync(roleBindingModel.SystemPermissionType, roleBindingModel.systemPermissionTypeInBranchDTOs, GetServiceHeader());

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
                // Split the input string into individual role items
                var rolesArray = selectedRoles.Split(',');
                var rolesList = new List<SystemPermissionTypeInRoleDTO>();

                foreach (var roleItem in rolesArray)
                {
                    // Split each role item into roleId and roleName
                    var roleData = roleItem.Split('|');
                    if (roleData.Length == 2) // Ensure we have both roleId and roleName
                    {
                        var roleId = roleData[0];  // Get the roleId
                        var roleName = roleData[1]; // Get the roleName

                        // Create a new SystemPermissionTypeInRoleDTO object and add it to the list
                        rolesList.Add(new SystemPermissionTypeInRoleDTO
                        {
                            RoleId = Guid.Parse(roleId), // Parse roleId as Guid
                            RoleName = roleName
                        });
                    }
                }

                // Return the list as an ObservableCollection
                return new ObservableCollection<SystemPermissionTypeInRoleDTO>(rolesList);
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
                // Split the string by comma to get individual GUIDs
                var branchGuids = selectedBranches.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                // Convert each GUID string into a BranchDTO object
                var branches = branchGuids.Select(guidStr => new BranchDTO
                {
                    Id = Guid.TryParse(guidStr, out Guid parsedId) ? parsedId : Guid.Empty,
                    //Description = "Branch for " + guidStr  // Set any description you want
                }).ToList();

                // Return as an ObservableCollection
                return new ObservableCollection<BranchDTO>(branches);
            }
            catch (Exception ex)
            {
                // Log or handle the parsing errors
                return new ObservableCollection<BranchDTO>();
            }
        }


    }
}

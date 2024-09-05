using Application.MainBoundedContext.DTO.AdministrationModule;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using SwiftFinancials.Web.Attributes;
using SwiftFinancials.Web.Controllers;
using System.Collections.ObjectModel;
using System;
using Microsoft.AspNet.Identity.EntityFramework;
using DistributedServices.MainBoundedContext.Identity;
using System.Collections.Generic;
using System.Data.Entity;

namespace SwiftFinancials.Web.Areas.Admin.Controllers
{
    [RoleBasedAccessControl]
    public class SystemTransactiontypesController : MasterController
    {
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            ViewBag.systemPermissionTypeTypeSelectList = GetsystemPermissionTypeList(string.Empty);
            return View();
        }
        [HttpGet]
        public async Task<JsonResult> GetBranchesAsync()
        {
            var branchesDTOs = await _channelService.FindBranchesAsync(GetServiceHeader());
            return Json(branchesDTOs, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public JsonResult GetRoles()
        {
            var result = _applicationRoleManager.Roles.ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [RoleBasedAccessControl]
        [HttpPost]
        public async Task<ActionResult> Create(SystemPermissionTypeInRoleDTO roleBindingModel, string SelectedRoles, string SelectedBranches)
        {
            await ServeNavigationMenus();
            ViewBag.systemPermissionTypeTypeSelectList = GetsystemPermissionTypeList(string.Empty);

            // Process selected roles
            if (!string.IsNullOrEmpty(SelectedRoles))
            {
                var selectedRoles = Newtonsoft.Json.JsonConvert.DeserializeObject<List<RoleDTO>>(SelectedRoles);
                roleBindingModel.systemPermissionTypeInRoles = selectedRoles
                    .Select(role => new SystemPermissionTypeInRoleDTO
                    {
                        RoleId = Guid.Parse(role.Id),
                        RoleName = role.Name
                    })
                    .ToList();
            }
            else
            {
                roleBindingModel.systemPermissionTypeInRoles = new List<SystemPermissionTypeInRoleDTO>();
            }

            // Retrieve roles and prepare the collection
            ObservableCollection<SystemPermissionTypeInRoleDTO> systemPermissionTypeInRole = new ObservableCollection<SystemPermissionTypeInRoleDTO>();
            foreach (var roleDTO in roleBindingModel.systemPermissionTypeInRoles)
            {
                var result = await _applicationRoleManager.FindByIdAsync(roleDTO.RoleId.ToString());
                if (result != null)
                {
                    systemPermissionTypeInRole.Add(roleDTO);
                }
            }

            // Process selected branches
            if (!string.IsNullOrEmpty(SelectedBranches))
            {
                // Define a DTO to match the JSON structure
                var branchDTOs = Newtonsoft.Json.JsonConvert.DeserializeObject<List<BranchDTO>>(SelectedBranches);
                roleBindingModel.systemPermissionTypeInBranchDTOs = branchDTOs;
            }
            else
            {
                roleBindingModel.systemPermissionTypeInBranchDTOs = new List<BranchDTO>();
            }

            // Retrieve branches and prepare the collection
            ObservableCollection<BranchDTO> branchDTOsCollection = new ObservableCollection<BranchDTO>();
            foreach (var branchDTO in roleBindingModel.systemPermissionTypeInBranchDTOs)
            {
                var branch = await _channelService.FindBranchAsync(branchDTO.Id, GetServiceHeader());
                if (branch != null)
                {
                    branchDTOsCollection.Add(branch);
                }
            }

            // Check for errors and perform the operations
            if (!roleBindingModel.HasErrors)
            {
                await _channelService.AddSystemPermissionTypeToRolesAsync(roleBindingModel.SystemPermissionType, systemPermissionTypeInRole, GetServiceHeader());
                await _channelService.AddSystemPermissionTypeToBranchesAsync(roleBindingModel.SystemPermissionType, branchDTOsCollection, GetServiceHeader());

                TempData["Success"] = "System Permissions Linked Successfully";
                return View("Create");
            }

            return View("Create");
        }





    }
}

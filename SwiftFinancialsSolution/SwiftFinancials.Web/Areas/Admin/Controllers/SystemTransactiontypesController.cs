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
        public JsonResult GetRoles()
        {
            //fetch all roles
            var result = _applicationRoleManager.Roles.ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public async Task<ActionResult> Create(SystemPermissionTypeInRoleDTO roleBindingModel, string SelectedRoles, string SelectedBranches, ObservableCollection<BranchDTO> branchDTO, ObservableCollection<RoleDTO> roleDTOs, RoleDTO roleDTO)
        {
            await ServeNavigationMenus();

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

            ObservableCollection<SystemPermissionTypeInRoleDTO> systemPermissionTypeInRole = new ObservableCollection<SystemPermissionTypeInRoleDTO>();


            foreach (var roleDTO1 in roleBindingModel.systemPermissionTypeInRoles)
            {
                var result = await _applicationRoleManager.FindByIdAsync(roleDTO1.RoleId.ToString());
                roleBindingModel.RoleId = roleDTO1.RoleId;
                roleBindingModel.RoleName = roleDTO1.RoleName;
                systemPermissionTypeInRole.Add(roleDTO1);
            }


            if (!string.IsNullOrEmpty(SelectedBranches))
            {
                var selectedBranchIds = SelectedBranches.Split(',').Select(Guid.Parse).ToList();
                roleBindingModel.systemPermissionTypeInBranchDTOs = selectedBranchIds
                    .Select(id => new BranchDTO { Id = id })
                    .ToList();
            }

            ObservableCollection<BranchDTO> branchDTOs = new ObservableCollection<BranchDTO>();


            foreach (var levySplitDTO in roleBindingModel.systemPermissionTypeInBranchDTOs)
            {
                var k = await _channelService.FindBranchAsync(levySplitDTO.Id, GetServiceHeader());
                branchDTOs.Add(k);
            }

            if (!roleBindingModel.HasErrors)

            {
                await _channelService.AddSystemPermissionTypeToRolesAsync(roleBindingModel.SystemPermissionType, systemPermissionTypeInRole, GetServiceHeader());
                await _channelService.AddSystemPermissionTypeToBranchesAsync(roleBindingModel.SystemPermissionType, branchDTOs, GetServiceHeader());

                TempData["Success"] = "System Permissions Linked Successfully";

                return RedirectToAction("Create", "SystemTransactiontypes", new { Area = "Admin" });
            }

            return View();
        }

        [HttpGet]
        public async Task<JsonResult> MarkRoleCheckbox(int navigationItemCode, string roleName)
        {
            bool result = await _channelService.IsNavigationItemInRoleAsync(navigationItemCode, roleName, GetServiceHeader());

            if (result == true)
            {
                return Json(1, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(2, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetBranchesAsync()
        {
            var branchesDTOs = await _channelService.FindBranchesAsync(GetServiceHeader());

            return Json(branchesDTOs, JsonRequestBehavior.AllowGet);
        }
        public async Task<ActionResult> Edit(string id)
        {
            await ServeNavigationMenus();
            ViewBag.systemPermissionTypeTypeSelectList = GetsystemPermissionTypeList(string.Empty);
            var k = await _channelService.GetRolesListForSystemPermissionTypeAsync(1, GetServiceHeader());
            return View();

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(SystemPermissionTypeInRoleDTO roleBindingModel, string SelectedRoles, string SelectedBranches, ObservableCollection<BranchDTO> branchDTO, ObservableCollection<RoleDTO> roleDTOs, RoleDTO roleDTO)
        {
            await ServeNavigationMenus();

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

            ObservableCollection<SystemPermissionTypeInRoleDTO> systemPermissionTypeInRole = new ObservableCollection<SystemPermissionTypeInRoleDTO>();
            if (roleBindingModel.systemPermissionTypeInBranchDTOs != null)
            {
                foreach (var roleDTO1 in roleBindingModel.systemPermissionTypeInRoles)
                {
                    var result = await _applicationRoleManager.FindByIdAsync(roleDTO1.RoleId.ToString());
                    roleBindingModel.RoleId = roleDTO1.RoleId;
                    roleBindingModel.RoleName = roleDTO1.RoleName;
                    systemPermissionTypeInRole.Add(roleDTO1);
                }
            }

            if (!string.IsNullOrEmpty(SelectedBranches))
            {
                var selectedBranchIds = SelectedBranches.Split(',').Select(Guid.Parse).ToList();
                roleBindingModel.systemPermissionTypeInBranchDTOs = selectedBranchIds
                    .Select(id => new BranchDTO { Id = id })
                    .ToList();
            }

            ObservableCollection<BranchDTO> branchDTOs = new ObservableCollection<BranchDTO>();
            if (roleBindingModel.systemPermissionTypeInBranchDTOs != null)
            {
                foreach (var levySplitDTO in roleBindingModel.systemPermissionTypeInBranchDTOs)
                {
                    var k = await _channelService.FindBranchAsync(levySplitDTO.Id, GetServiceHeader());
                    branchDTOs.Add(k);
                }
            }
            ObservableCollection<string> n = new ObservableCollection<string>();
            foreach (var roleDTO8 in roleBindingModel.systemPermissionTypeInRoles)
            {
                roleBindingModel.RoleName = roleDTO8.RoleName;
                string k = roleBindingModel.RoleName;
                n.Add(k);
            }
            if (!roleBindingModel.HasErrors)

            {

                await _channelService.RemoveSystemPermissionTypeFromBranchesAsync(roleBindingModel.SystemPermissionType, branchDTOs, GetServiceHeader());
                await _channelService.RemoveSystemPermissionTypeFromRolesAsync(roleBindingModel.SystemPermissionType, n, GetServiceHeader());

                TempData["Success"] = "System Permissions Removed Successfully";

                return RedirectToAction("Create", "SystemTransactiontypes", new { Area = "Admin" });
            }

            return View();
        }


    }
}
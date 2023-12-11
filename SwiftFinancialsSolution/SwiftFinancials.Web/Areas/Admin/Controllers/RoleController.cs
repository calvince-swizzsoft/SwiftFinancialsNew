using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AdministrationModule;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using SwiftFinancials.Web.Attributes;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using SwiftFinancials.Presentation.Infrastructure.Util;

namespace SwiftFinancials.Web.Areas.Admin.Controllers
{
    [RoleBasedAccessControl]
    public class RoleController : MasterController
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

            var search = jQueryDataTablesModel.sSearch.ToLower();

            var pageCollectionInfo = await _channelService.FindMembershipRolesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, sortedColumns, sortAscending, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }

            else return this.DataTablesJson(items: new List<UserDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(string id)
        {
            await ServeNavigationMenus();

            if (id != null)
            {
                var identityRole = await _applicationRoleManager.FindByIdAsync(id);

                return View(identityRole.MapTo<RoleDTO>());
            }
            else
                return View();
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(RoleBindingModel roleBindingModel)
        {
            if (ModelState.IsValid)
            {
                var newIdentityRole = new IdentityRole(roleBindingModel.Name);

                var roleresult = await _applicationRoleManager.CreateAsync(newIdentityRole);

                if (!roleresult.Succeeded)
                {
                    TempData["Error"] = string.Join(",", roleresult.Errors);

                    return View();
                }

                TempData["Success"] = "Role Created Successfully";

                return RedirectToAction("Index", "Role", new { Area = "Admin" });
            }
            return View();
        }

        public async Task<ActionResult> Edit(string id)
        {
            await ServeNavigationMenus();

            if (id != null)
            {
                var identityRole = await _applicationRoleManager.FindByIdAsync(id);

                return View(identityRole.MapTo<RoleBindingModel>());
            }
            else
                return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, RoleBindingModel roleBindingModel)
        {
            if (ModelState.IsValid)
            {
                var current = await _applicationRoleManager.FindByIdAsync(roleBindingModel.Id);
                current.Name = roleBindingModel.Name;

                var result = await _applicationRoleManager.UpdateAsync(current);

                if (result.Succeeded)
                {
                    TempData["Success"] = "Role Updated Successfully";

                    return RedirectToAction("Index", "Role", new { Area = "Admin" });
                }
                else
                {
                    TempData["Error"] = string.Join(",", result.Errors);

                    return View();
                }
            }
            return View();
        }

        [HttpGet]
        public JsonResult GetRoles()
        {
            var roles = _applicationRoleManager.Roles.ToList();

            return Json(roles, JsonRequestBehavior.AllowGet);
        }
    }
}
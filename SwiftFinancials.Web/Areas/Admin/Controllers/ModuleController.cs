using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO.AdministrationModule;
using SwiftFinancials.Presentation.Infrastructure.Models;
using SwiftFinancials.Web.Areas.Admin.Models;
using SwiftFinancials.Web.Controllers;

namespace SwiftFinancials.Web.Areas.Admin.Controllers
{
    public class ModuleController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpGet]
        public JsonResult GetRoles()
        {
            //fetch all roles
            var result = _applicationRoleManager.Roles.ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<JsonResult> GetModuleNavigationNodes()
        {
            var nodes = new List<JsTreeModel>();

            var navigationItems = await _channelService.FindNavigationItemsAsync(GetServiceHeader());

            var NavigationItemAreaCode = "#";

            string NavigationItemId = null;

            foreach (var NavigationItem in navigationItems)
            {
                //If parent code is null(0) then replace it with the '#' - jsTree standard.
                if (NavigationItem.AreaCode > 0)
                    NavigationItemAreaCode = NavigationItem.AreaCode.ToString();
                else
                    NavigationItemAreaCode = "#";

                //when seeding module navigation items we usually ignore the 'controller_name' and 'action_name' for parents.
                //Therefore, do not pass 'NavigationItemId' so that in tree view we do not accidentally link a parent to a role.
                if (NavigationItem.ControllerName == null || NavigationItem.ActionName == null || NavigationItem.IsArea)
                    NavigationItemId = null;
                else
                    NavigationItemId = NavigationItem.Id.ToString("D");

                //we are using code as is since that is the only way we can know who is child and parent
                //parents 'code' is child's 'areacode'.
                nodes.Add(new JsTreeModel() { id = NavigationItem.Code.ToString(), parent = NavigationItemAreaCode, text = NavigationItem.Description, icon = NavigationItemId });
            }
            return Json(nodes, JsonRequestBehavior.AllowGet);
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

        [HttpPost]
        public async Task<ActionResult> AddNavigationItemToRole(NavigationItemToRoleViewModel NavigationItemToRoleViewModel)
        {
            var navigationItemInRoleDTO = new NavigationItemInRoleDTO
            {
                NavigationItemId = NavigationItemToRoleViewModel.NavigationItemId,

                RoleName = NavigationItemToRoleViewModel.RoleName
            };

            navigationItemInRoleDTO.ValidateAll();

            if (navigationItemInRoleDTO.HasErrors)
            {
                TempData["Error"] = string.Join(Environment.NewLine, navigationItemInRoleDTO.ErrorMessages);

                return View("ModuleAccessControl");
            }

            var result = await _channelService.AddNavigationItemToRoleAsync(navigationItemInRoleDTO, GetServiceHeader());

            if (result)
                await LoadModuleAccessRights(HttpContext.User.Identity.Name);

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<JsonResult> RemoveNavigationItemToRole(NavigationItemToRoleViewModel navigationItemToRoleViewModel)
        {
            var result = await _channelService.RemoveNavigationItemRoleAsync(navigationItemToRoleViewModel.NavigationItemId, navigationItemToRoleViewModel.RoleName, GetServiceHeader());

            if (result)
                await LoadModuleAccessRights(HttpContext.User.Identity.Name);

            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
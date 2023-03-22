
using Application.MainBoundedContext.DTO.AdministrationModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Fedhaplus.DashboardApplication.Attributes;
using Fedhaplus.DashboardApplication.Controllers;
using Fedhaplus.DashboardApplication.Helpers;
using Fedhaplus.Presentation.Infrastructure.Util;
using Application.MainBoundedContext.DTO;

namespace Fedhaplus.DashboardApplication.Areas.Admin.Controllers
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

        // GET: SystemUsers
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }

        // POST: SystemUsers
        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var search = jQueryDataTablesModel.sSearch.ToLower();

            var pageCollectionInfo = await _channelService.FindMembershipByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, sortedColumns, sortAscending, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }

            else return this.DataTablesJson(items: new List<UserDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        // GET: SystemUser/Details/5
        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var applicationUser = await _applicationUserManager.FindByIdAsync(id.ToString());

            var CompanyDTO = new CompanyDTO();

            string CompanyDescription = string.Empty;

            string roleName = string.Empty;

            if (applicationUser.CompanyId != Guid.Empty && applicationUser.CompanyId != null)
            {
                CompanyDTO = await _channelService.FindCompanyAsync((Guid)applicationUser.CompanyId, GetServiceHeader());

                if (CompanyDTO != null)
                    CompanyDescription = CompanyDTO.Description;
            }

            var roles = await _applicationUserManager.GetRolesAsync(applicationUser.Id);

            if (roles != null)
                roleName = roles.FirstOrDefault();
            var userDTO = new UserDTO
            {
                Id = applicationUser.Id,
                FirstName = applicationUser.FirstName,
                OtherNames = applicationUser.OtherNames,
                Email = applicationUser.Email,
                PhoneNumber = applicationUser.PhoneNumber,
                CompanyId = applicationUser.CompanyId,
                CompanyDescription = CompanyDescription,
                RoleName = roleName,
                TwoFactorEnabled = applicationUser.TwoFactorEnabled,
                LockoutEnabled = applicationUser.LockoutEnabled,
                CreatedDate = applicationUser.CreatedDate
            };

            return View(userDTO);
        }

        // GET: SystemUser/Create
        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            return View();
        }

        // POST: SystemUser/Create
        [HttpPost]
        public async Task<ActionResult> Create(UserBindingModel userBindingModel)
        {
            userBindingModel.ValidateAll();

            if (userBindingModel.HasErrors)
            {
                await ServeNavigationMenus();

                TempData["Error"] = userBindingModel.ErrorMessages;

                return View();
            }

            var userDTO = await _channelService.AddNewMembershipAsync(userBindingModel.MapTo<UserDTO>(), GetServiceHeader());

            if (userDTO != null)
            {
                TempData["Success"] = "User Registered Successfully";

                return RedirectToAction("Index", "Membership", new { Area = "Admin" });
            }

            TempData["Error"] = "Create Membership Failed!";

            return View();
        }

        // GET: SystemUser/Edit/5
        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            string CompanyDescription = string.Empty;

            string roleName = string.Empty;

            var user = await _applicationUserManager.FindByIdAsync(id.ToString());

            if (user != null)
            {
                if (user.CompanyId != Guid.Empty && user.CompanyId != null)
                {
                    var companyDTO = await _channelService.FindCompanyAsync((Guid)user.CompanyId, GetServiceHeader());

                    if (companyDTO != null)
                        CompanyDescription = companyDTO.Description;
                }

                var roles = await _applicationUserManager.GetRolesAsync(user.Id);

                if (roles != null)
                    roleName = roles.FirstOrDefault();

                var userBindingModel = user.MapTo<UserBindingModel>();

                userBindingModel.CompanyDescription = CompanyDescription;

                userBindingModel.RoleName = roleName;

                return View(userBindingModel);
            }

            return View();
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

        [HttpGet]
        public ActionResult LoadCompanies()
        {
            return PartialView("_Companies");
        }
    }
}
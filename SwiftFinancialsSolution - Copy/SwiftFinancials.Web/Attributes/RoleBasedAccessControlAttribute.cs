using Application.MainBoundedContext.DTO.AdministrationModule;
using DistributedServices.MainBoundedContext.Identity;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
using SwiftFinancials.Presentation.Infrastructure.Services;
using SwiftFinancials.Web.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Attributes
{
    public class RoleBasedAccessControlAttribute : AuthorizeAttribute
    {
        private IChannelService channelService;
        public IChannelService _channelService
        {
            get
            {
                if (channelService == null)
                {
                    channelService = DependencyResolver.Current.GetService<IChannelService>();
                }
                return channelService;
            }
            set { channelService = value; }
        }


        private ApplicationRoleManager applicationRoleManager;
        public ApplicationRoleManager _applicationRoleManager
        {
            get
            {
                if (applicationRoleManager == null)
                {
                    applicationRoleManager = DependencyResolver.Current.GetService<ApplicationRoleManager>();
                }
                return applicationRoleManager;
            }
            set { applicationRoleManager = value; }
        }

        private ApplicationUserManager applicationUserManager;
        public ApplicationUserManager _applicationUserManager
        {
            get
            {
                if (applicationUserManager == null)
                {
                    applicationUserManager = DependencyResolver.Current.GetService<ApplicationUserManager>();
                }
                return applicationUserManager;
            }
            set { applicationUserManager = value; }
        }

        private IWebConfigurationService webConfigurationService;
        public IWebConfigurationService _webConfigurationService
        {
            get
            {
                if (webConfigurationService == null)
                {
                    webConfigurationService = DependencyResolver.Current.GetService<IWebConfigurationService>();
                }
                return webConfigurationService;
            }
            set { webConfigurationService = value; }
        }


        public ServiceHeader GetServiceHeader()
        {
            return _webConfigurationService.GetServiceHeader();
        }
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            var controllerName = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;

            var actionName = filterContext.ActionDescriptor.ActionName;

            bool hasPermission = default(bool);

            var user = filterContext.HttpContext.User;

            var permissiontype = filterContext.HttpContext.User.GetType();

            if (user != null && user.Identity.IsAuthenticated)
            {
                if (user.IsInRole(WellKnownUserRoles.SuperAdministrator))
                {
                    return;
                }

                var moduleAccessCachedList = HttpRuntime.Cache[user.Identity.GetUserId()] as ICollection<NavigationItemInRoleDTO>;

                var allRoles = _applicationRoleManager.Roles.ToList();
                var allBranches = _channelService.FindBranchesAsync(GetServiceHeader());
                var items = Enum.GetValues(typeof(SystemPermissionType));
                foreach (int k in items)
                {
                    var linkedRoles = _channelService.GetRolesForSystemPermissionTypeAsync(k, GetServiceHeader());
                    var linkedBranches = _channelService.GetBranchesForSystemPermissionTypeAsync(1, GetServiceHeader());

                }

                var foundRoles = new ObservableCollection<RoleDTO>();
                if (moduleAccessCachedList != null)
                {
                    foreach (var accessRight in moduleAccessCachedList)
                    {
                        var role = _applicationRoleManager.FindByNameAsync(accessRight.RoleName);
                        if (role != null)
                        {
                            foundRoles.Add(new RoleDTO { Id = role.Result.Id.ToString(), Name = role.Result.Name.ToString() });
                            var test = role.Result.Name.ToString();

                        }
                        if (accessRight.NavigationItemControllerName.ToLower() == controllerName.ToLower())
                        {
                            hasPermission = true;

                        }

                    }

                    if (hasPermission) return;

                    throw new System.InvalidOperationException($"Sorry, you are not authorized to access {controllerName.ToUpper()} module.");
                }
                else
                {
                    var urlHelper = new UrlHelper(filterContext.RequestContext);

                    filterContext.Result = new RedirectResult(urlHelper.Action("Login", "Account", new { Area = "" }));
                }
            }
            else
            {
                var urlHelper = new UrlHelper(filterContext.RequestContext);

                filterContext.Result = new RedirectResult(urlHelper.Action("Login", "Account", new { Area = "" }));
            }
        }
    }

}
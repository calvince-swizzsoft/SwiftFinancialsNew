using Application.MainBoundedContext.DTO.AdministrationModule;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

namespace Fedhaplus.DashboardApplication.Attributes
{
    public class RoleBasedAccessControlAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            var controllerName = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;

            var actionName = filterContext.ActionDescriptor.ActionName;

            bool hasPermission = default(bool);

            var user = filterContext.HttpContext.User;

            if (user != null && user.Identity.IsAuthenticated)
            {
                if (user.IsInRole(WellKnownUserRoles.SuperAdministrator))
                {
                    return;
                }

                var moduleAccessCachedList = HttpRuntime.Cache[user.Identity.GetUserId()] as ICollection<NavigationItemInRoleDTO>;

                if (moduleAccessCachedList != null)
                {
                    foreach (var accessRight in moduleAccessCachedList)
                    {
                        if (accessRight.NavigationItemControllerName.ToLower() == controllerName.ToLower())
                        {
                            hasPermission = true;

                            break;
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
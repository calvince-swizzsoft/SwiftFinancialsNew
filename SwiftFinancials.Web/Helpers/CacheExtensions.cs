using Application.MainBoundedContext.DTO.AdministrationModule;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Web;
using System.Web.Caching;

namespace SwiftFinancials.Web.Helpers
{
    [DataObject]
    public class CacheExtensions
    {
        public void CacheModuleAccessRightsInRole(List<ModuleNavigationItemInRoleDTO> NavigationItemInRoles, string sessionKey, ServiceHeader serviceHeader)
        {
            var currentCachedRoles = HttpRuntime.Cache[sessionKey];

            if (currentCachedRoles != null)
            {
                HttpRuntime.Cache.Remove(sessionKey);
            }

            var currentCachedRoles2 = HttpRuntime.Cache[sessionKey];

            if (currentCachedRoles2 == null)
            {
                HttpRuntime.Cache.Insert(
                    /* key */                sessionKey,
                    /* value */              NavigationItemInRoles,
                    /* dependencies */       null,
                    /* absoluteExpiration */ Cache.NoAbsoluteExpiration,
                    /* slidingExpiration */  TimeSpan.FromMinutes(double.Parse(ConfigurationManager.AppSettings["SessionTimeout"])),
                    /* priority */           CacheItemPriority.NotRemovable,
                    /* onRemoveCallback */   null);
            }
        }
    }
}
using DistributedServices.MainBoundedContext.Identity;
using Infrastructure.Crosscutting.Framework.Adapter;
using Infrastructure.Crosscutting.Framework.Logging;
using LazyCache;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using System;
using System.Data.Entity;
using System.Runtime.Caching;
using System.Web;
using Unity;
using Unity.Injection;
using Unity.Lifetime;
using SwiftFinancials.Web.Identity;
using SwiftFinancials.Web.Services;
using SwiftFinancials.Web.TypeAdapterFactory;
using SwiftFinancials.Presentation.Infrastructure.Services;

namespace SwiftFinancials.Web
{
    /// <summary>
    /// Specifies the Unity configuration for the main container.
    /// </summary>
    public static class UnityConfig
    {
        #region Unity Container
        private static Lazy<IUnityContainer> container =
          new Lazy<IUnityContainer>(() =>
          {
              var container = new UnityContainer();
              RegisterTypes(container);
              return container;
          });

        /// <summary>
        /// Configured Unity Container.
        /// </summary>
        public static IUnityContainer Container => container.Value;
        #endregion

        /// <summary>
        /// Registers the type mappings with the Unity container.
        /// </summary>
        /// <param name="container">The unity container to configure.</param>
        /// <remarks>
        /// There is no need to register concrete types such as controllers or
        /// API controllers (unless you want to change the defaults), as Unity
        /// allows resolving a concrete type even if it was not previously
        /// registered.
        /// </remarks>
        public static void RegisterTypes(IUnityContainer container)
        {
            // NOTE: To load from web.config uncomment the line below.
            // Make sure to add a Unity.Configuration to the using statements.
            // container.LoadConfiguration();

            // TODO: Register your type's mappings here.
            // container.RegisterType<IProductRepository, ProductRepository>();

            container.RegisterType<IAppCache, CachingService>(new ContainerControlledLifetimeManager(), new InjectionConstructor(MemoryCache.Default));

            container.RegisterType<DbContext, ApplicationDbContext>(new InjectionConstructor("AuthStore"));

            container.RegisterType<UserManager<ApplicationUser>>();

            container.RegisterType<IUserStore<ApplicationUser>, UserStore<ApplicationUser>>();

            container.RegisterType<IRoleStore<IdentityRole, string>, RoleStore<IdentityRole, string, IdentityUserRole>>();

            container.RegisterType<ApplicationUserManager>();

            container.RegisterType<ApplicationSignInManager>();

            container.RegisterType<ApplicationRoleManager>();

            container.RegisterType<ILogger, SerilogLogger>(new ContainerControlledLifetimeManager());

            container.RegisterType<IChannelService, ChannelService>(new ContainerControlledLifetimeManager());

            container.RegisterType<EmailService>();

            container.RegisterType<SmsService>();

            container.RegisterType<IAuthenticationManager>(new InjectionFactory(c => HttpContext.Current.GetOwinContext().Authentication));

            container.RegisterType<IWebConfigurationService, WebConfigurationService>();

            container.RegisterType<ITypeAdapterFactory, AutomapperTypeAdapterFactory>(new ContainerControlledLifetimeManager());
        }
    }
}
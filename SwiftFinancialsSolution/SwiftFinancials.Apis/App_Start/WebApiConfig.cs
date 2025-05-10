using Application.MainBoundedContext.Services;
using System.Web.Http;
using Unity;
using Unity.Lifetime;
using SwiftFinancials.Apis.Filters;
using VanguardFinancials.Presentation.Infrastructure.Services;
using WebApiThrottle;

namespace SwiftFinancials.Apis
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // Uncomment the following line of code to enable query support for actions with an IQueryable or IQueryable<T> return type.
            // To avoid processing unexpected or malicious queries, use the validation settings on QueryableAttribute to validate incoming queries.
            // For more information, visit http://go.microsoft.com/fwlink/?LinkId=279712.
            //config.EnableQuerySupport();

            config.MessageHandlers.Add(new CustomThrottlingHandler()
            {
                Policy = ThrottlePolicy.FromStore(new PolicyConfigurationProvider()),
                Repository = new CacheRepository(),
                Logger = new EntLibThrottleLogger(),
            });

            config.MessageHandlers.Add(new SuppressRedirectHandler());

            config.MessageHandlers.Add(new MessageLoggingHandler());

            config.Filters.Add(new UnhandledExceptionFilter());

            var container = new UnityContainer();

            container.RegisterType<IChannelService, ChannelService>(new ContainerControlledLifetimeManager());

            container.RegisterType<IMessageQueueService, MessageQueueService>();

            config.DependencyResolver = new UnityWebApiResolver(container);
        }
    }
}
using Infrastructure.Crosscutting.Framework.Utils;
using LazyCache;
using Numero3.EntityFramework.Interfaces;
using System;
using System.Configuration;
using System.Data.Entity;

namespace Infrastructure.Data.MainBoundedContext.UnitOfWork
{
    public class RuntimeContextFactory : IDbContextFactory
    {
        private readonly IAppCache _appCache;

        public RuntimeContextFactory(
            IAppCache appCache)
        {
            if (appCache == null)
                throw new ArgumentNullException("appCache");

            _appCache = appCache;
        }

        public TDbContext CreateDbContext<TDbContext>(ServiceHeader serviceHeader) where TDbContext : DbContext
        {
            var nameOrConnectionString = GetCachedConnectionString(serviceHeader);

            var dbContext = new BoundedContextUnitOfWork(nameOrConnectionString);

            return dbContext as TDbContext;
        }

        string GetCachedConnectionString(ServiceHeader serviceHeader)
        {
            if (serviceHeader == null)
                throw new ArgumentNullException("serviceHeader");

            if (string.IsNullOrWhiteSpace(serviceHeader.ApplicationDomainName))
                throw new ArgumentNullException("serviceHeader.ApplicationDomainName");

            return _appCache.GetOrAdd<string>(serviceHeader.ApplicationDomainName, () =>
            {
                var connectionStringSettings = ConfigurationManager.ConnectionStrings[serviceHeader.ApplicationDomainName];

                if (connectionStringSettings == null)
                    throw new InvalidOperationException(string.Format("App Domain Name '{0}' Invalid!", serviceHeader.ApplicationDomainName));

                return connectionStringSettings.ConnectionString;
            });
        }
    }
}

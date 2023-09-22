using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.Seedwork;
using Infrastructure.Crosscutting.Framework.Utils;
using Domain.MainBoundedContext.AccountsModule.Aggregates.SystemGeneralLedgerAccountMappingAgg;
using Domain.Seedwork;
using Domain.Seedwork.Specification;
using LazyCache;
using Numero3.EntityFramework.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.MainBoundedContext.AccountsModule.Services
{
    public class SystemGeneralLedgerAccountMappingAppService : ISystemGeneralLedgerAccountMappingAppService
    {
        private readonly IDbContextScopeFactory _dbContextScopeFactory;
        private readonly IRepository<SystemGeneralLedgerAccountMapping> _systemGeneralLedgerAccountMappingRepository;
        private readonly IAppCache _appCache;

        public SystemGeneralLedgerAccountMappingAppService(
           IDbContextScopeFactory dbContextScopeFactory,
           IRepository<SystemGeneralLedgerAccountMapping> systemGeneralLedgerAccountMappingRepository,
           IAppCache appCache)
        {
            if (dbContextScopeFactory == null)
                throw new ArgumentNullException(nameof(dbContextScopeFactory));

            if (systemGeneralLedgerAccountMappingRepository == null)
                throw new ArgumentNullException(nameof(systemGeneralLedgerAccountMappingRepository));

            if (appCache == null)
                throw new ArgumentNullException(nameof(appCache));

            _dbContextScopeFactory = dbContextScopeFactory;
            _systemGeneralLedgerAccountMappingRepository = systemGeneralLedgerAccountMappingRepository;
            _appCache = appCache;
        }

        public SystemGeneralLedgerAccountMappingDTO AddNewSystemGeneralLedgerAccountMapping(SystemGeneralLedgerAccountMappingDTO systemGeneralLedgerAccountMappingDTO, ServiceHeader serviceHeader)
        {
            if (systemGeneralLedgerAccountMappingDTO != null && systemGeneralLedgerAccountMappingDTO.ChartOfAccountId != Guid.Empty)
            {
                using (var dbContextScope = _dbContextScopeFactory.Create())
                {
                    var systemGeneralLedgerAccountMapping = SystemGeneralLedgerAccountMappingFactory.CreateSystemGeneralLedgerAccountMapping(systemGeneralLedgerAccountMappingDTO.SystemGeneralLedgerAccountCode, systemGeneralLedgerAccountMappingDTO.ChartOfAccountId);

                    systemGeneralLedgerAccountMapping.SystemGeneralLedgerAccountCode = (short)_systemGeneralLedgerAccountMappingRepository.DatabaseSqlQuery<int>(string.Format("SELECT ISNULL(MAX(SystemGeneralLedgerAccountCode),0) + 1 AS Expr1 FROM {0}SystemGeneralLedgerAccountMappings", DefaultSettings.Instance.TablePrefix), serviceHeader).FirstOrDefault();


                    _systemGeneralLedgerAccountMappingRepository.Add(systemGeneralLedgerAccountMapping, serviceHeader);

                    dbContextScope.SaveChanges(serviceHeader);

                    return systemGeneralLedgerAccountMapping.ProjectedAs<SystemGeneralLedgerAccountMappingDTO>();
                }
            }
            else return null;
        }

        public bool UpdateSystemGeneralLedgerAccountMapping(SystemGeneralLedgerAccountMappingDTO systemGeneralLedgerAccountMappingDTO, ServiceHeader serviceHeader)
        {
            if (systemGeneralLedgerAccountMappingDTO == null || systemGeneralLedgerAccountMappingDTO.Id == Guid.Empty || systemGeneralLedgerAccountMappingDTO.ChartOfAccountId == Guid.Empty)
                return false;

            using (var dbContextScope = _dbContextScopeFactory.Create())
            {
                var persisted = _systemGeneralLedgerAccountMappingRepository.Get(systemGeneralLedgerAccountMappingDTO.Id, serviceHeader);

                if (persisted != null)
                {
                    var current = SystemGeneralLedgerAccountMappingFactory.CreateSystemGeneralLedgerAccountMapping(systemGeneralLedgerAccountMappingDTO.SystemGeneralLedgerAccountCode, systemGeneralLedgerAccountMappingDTO.ChartOfAccountId);

                    current.ChangeCurrentIdentity(persisted.Id, persisted.SequentialId, persisted.CreatedBy, persisted.CreatedDate);
                    current.SystemGeneralLedgerAccountCode = persisted.SystemGeneralLedgerAccountCode;

                    _systemGeneralLedgerAccountMappingRepository.Merge(persisted, current, serviceHeader);


                    return dbContextScope.SaveChanges(serviceHeader) >= 0;
                }
                else return false;
            }
        }

        public List<SystemGeneralLedgerAccountMappingDTO> FindSystemGeneralLedgerAccountMappings(ServiceHeader serviceHeader)
        {
            using (_dbContextScopeFactory.CreateReadOnly())
            {
                ISpecification<SystemGeneralLedgerAccountMapping> spec = SystemGeneralLedgerAccountMappingSpecifications.DefaultSpec();

                var systemGeneralLedgerAccountMappings = _systemGeneralLedgerAccountMappingRepository.AllMatching(spec, serviceHeader);

                if (systemGeneralLedgerAccountMappings != null && systemGeneralLedgerAccountMappings.Any())
                {
                    return systemGeneralLedgerAccountMappings.ProjectedAsCollection<SystemGeneralLedgerAccountMappingDTO>();
                }
                else return null;
            }
        }



        public PageCollectionInfo<SystemGeneralLedgerAccountMappingDTO> FindSystemGeneralLedgerAccountMappings(int pageIndex, int pageSize, ServiceHeader serviceHeader)
        {
            using (_dbContextScopeFactory.CreateReadOnly())
            {
                var filter = SystemGeneralLedgerAccountMappingSpecifications.DefaultSpec();

                ISpecification<SystemGeneralLedgerAccountMapping> spec = filter;

                var sortFields = new List<string> { "SequentialId" };

                var systemGeneralLedgerAccountMappingCollection = _systemGeneralLedgerAccountMappingRepository.AllMatchingPaged(spec, pageIndex, pageSize, sortFields, true, serviceHeader);

                if (systemGeneralLedgerAccountMappingCollection != null)
                {
                    var pageCollection = systemGeneralLedgerAccountMappingCollection.PageCollection.ProjectedAsCollection<SystemGeneralLedgerAccountMappingDTO>();

                    var itemsCount = systemGeneralLedgerAccountMappingCollection.ItemsCount;

                    return new PageCollectionInfo<SystemGeneralLedgerAccountMappingDTO> { PageCollection = pageCollection, ItemsCount = itemsCount };
                }
                else return null;
            }
        }

        public PageCollectionInfo<SystemGeneralLedgerAccountMappingDTO> FindSystemGeneralLedgerAccountMappings(string text, int pageIndex, int pageSize, ServiceHeader serviceHeader)
        {
            using (_dbContextScopeFactory.CreateReadOnly())
            {
                var filter = SystemGeneralLedgerAccountMappingSpecifications.SystemGeneralLedgerAccountMappingFullText(text);

                ISpecification<SystemGeneralLedgerAccountMapping> spec = filter;

                var sortFields = new List<string> { "SequentialId" };

                var systemGeneralLedgerAccountMappingCollection = _systemGeneralLedgerAccountMappingRepository.AllMatchingPaged(spec, pageIndex, pageSize, sortFields, true, serviceHeader);

                if (systemGeneralLedgerAccountMappingCollection != null)
                {
                    var pageCollection = systemGeneralLedgerAccountMappingCollection.PageCollection.ProjectedAsCollection<SystemGeneralLedgerAccountMappingDTO>();

                    var itemsCount = systemGeneralLedgerAccountMappingCollection.ItemsCount;

                    return new PageCollectionInfo<SystemGeneralLedgerAccountMappingDTO> { PageCollection = pageCollection, ItemsCount = itemsCount };
                }
                else return null;
            }
        }

        public SystemGeneralLedgerAccountMappingDTO FindSystemGeneralLedgerAccountMapping(Guid systemGeneralLedgerAccountMappingId, ServiceHeader serviceHeader)
        {
            if (systemGeneralLedgerAccountMappingId != Guid.Empty)
            {
                using (_dbContextScopeFactory.CreateReadOnly())
                {
                    var systemGeneralLedgerAccountMapping = _systemGeneralLedgerAccountMappingRepository.Get(systemGeneralLedgerAccountMappingId, serviceHeader);

                    if (systemGeneralLedgerAccountMapping != null)
                    {
                        return systemGeneralLedgerAccountMapping.ProjectedAs<SystemGeneralLedgerAccountMappingDTO>();
                    }
                    else return null;
                }
            }
            else return null;
        }

    }

}

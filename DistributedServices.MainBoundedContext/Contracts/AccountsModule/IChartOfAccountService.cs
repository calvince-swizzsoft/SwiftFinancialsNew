using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using DistributedServices.Seedwork.ErrorHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace DistributedServices.MainBoundedContext
{
    [ServiceContract]
    public interface IChartOfAccountService
    {
        #region Chart Of Account

        [OperationContract()]
        [FaultContract(typeof(ApplicationServiceError))]
        Task<PageCollectionInfo<ChartOfAccountDTO>> FindChartOfAccountsByFilterInPageAsync(string text, int pageIndex, int pageSize);

        [OperationContract(AsyncPattern = true)]
        [FaultContract(typeof(ApplicationServiceError))]
        Task<List<ChartOfAccountDTO>> FindChartOfAccountsAsync();
        
        [OperationContract()]
        [FaultContract(typeof(ApplicationServiceError))]
        ChartOfAccountDTO AddChartOfAccount(ChartOfAccountDTO chartOfAccountDTO);

        [OperationContract()]
        [FaultContract(typeof(ApplicationServiceError))]
        bool UpdateChartOfAccount(ChartOfAccountDTO chartOfAccountDTO);

        [OperationContract()]
        [FaultContract(typeof(ApplicationServiceError))]
        ChartOfAccountDTO FindChartOfAccount(Guid chartOfAccountId);

        [OperationContract()]
        [FaultContract(typeof(ApplicationServiceError))]
        List<GeneralLedgerAccount> FindGeneralLedgerAccounts(bool includeBalances, bool updateDepth);

        [OperationContract()]
        [FaultContract(typeof(ApplicationServiceError))]
        GeneralLedgerAccount FindGeneralLedgerAccount(Guid chartOfAccountId, bool includeBalances);

        [OperationContract()]
        [FaultContract(typeof(ApplicationServiceError))]
        Guid GetChartOfAccountMappingForSystemGeneralLedgerAccountCode(int systemGeneralLedgerAccountCode);

        [OperationContract()]
        [FaultContract(typeof(ApplicationServiceError))]
        bool MapSystemGeneralLedgerAccountCodeToChartOfAccount(int systemGeneralLedgerAccountCode, Guid chartOfAccountId);

        [OperationContract()]
        [FaultContract(typeof(ApplicationServiceError))]
        PageCollectionInfo<SystemGeneralLedgerAccountMappingDTO> FindSystemGeneralLedgerAccountMappingsInPage(int pageIndex, int pageSize);

        #endregion
    }
}

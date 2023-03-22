using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using DistributedServices.Seedwork.ErrorHandlers;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace SwiftFinancials.Presentation.Contracts.HumanResourcesModule
{
    [ServiceContract(Name = "ISalaryPeriodService")]
    public interface ISalaryPeriodService
    {
        [OperationContract(AsyncPattern = true)]
        [FaultContract(typeof(ApplicationServiceError))]
        IAsyncResult BeginAddSalaryPeriod(SalaryPeriodDTO salaryPeriodDTO, AsyncCallback callback, Object state);
        SalaryPeriodDTO EndAddSalaryPeriod(IAsyncResult result);

        [OperationContract(AsyncPattern = true)]
        [FaultContract(typeof(ApplicationServiceError))]
        IAsyncResult BeginUpdateSalaryPeriod(SalaryPeriodDTO salaryPeriodDTO, AsyncCallback callback, Object state);
        bool EndUpdateSalaryPeriod(IAsyncResult result);

        [OperationContract(AsyncPattern = true)]
        [FaultContract(typeof(ApplicationServiceError))]
        IAsyncResult BeginProcessSalaryPeriod(SalaryPeriodDTO salaryPeriodDTO, List<EmployeeDTO> employees, AsyncCallback callback, Object state);
        bool EndProcessSalaryPeriod(IAsyncResult result);

        [OperationContract(AsyncPattern = true)]
        [FaultContract(typeof(ApplicationServiceError))]
        IAsyncResult BeginCloseSalaryPeriod(SalaryPeriodDTO salaryPeriodDTO, AsyncCallback callback, Object state);
        bool EndCloseSalaryPeriod(IAsyncResult result);

        [OperationContract(AsyncPattern = true)]
        [FaultContract(typeof(ApplicationServiceError))]
        IAsyncResult BeginPostPaySlip(Guid paySlipId, int moduleNavigationItemCode, AsyncCallback callback, Object state);
        bool EndPostPaySlip(IAsyncResult result);

        [OperationContract(AsyncPattern = true)]
        [FaultContract(typeof(ApplicationServiceError))]
        IAsyncResult BeginFindSalaryPeriods(AsyncCallback callback, Object state);
        List<SalaryPeriodDTO> EndFindSalaryPeriods(IAsyncResult result);

        [OperationContract(AsyncPattern = true)]
        [FaultContract(typeof(ApplicationServiceError))]
        IAsyncResult BeginFindSalaryPeriodsInPage(int pageIndex, int pageSize, AsyncCallback callback, Object state);
        PageCollectionInfo<SalaryPeriodDTO> EndFindSalaryPeriodsInPage(IAsyncResult result);

        [OperationContract(AsyncPattern = true)]
        [FaultContract(typeof(ApplicationServiceError))]
        IAsyncResult BeginFindSalaryPeriod(Guid salaryPeriodId, AsyncCallback callback, Object state);
        SalaryPeriodDTO EndFindSalaryPeriod(IAsyncResult result);

        [OperationContract(AsyncPattern = true)]
        [FaultContract(typeof(ApplicationServiceError))]
        IAsyncResult BeginFindSalaryPeriodsByFilterInPage(int status, DateTime startDate, DateTime endDate, string text, int pageIndex, int pageSize, AsyncCallback callback, Object state);
        PageCollectionInfo<SalaryPeriodDTO> EndFindSalaryPeriodsByFilterInPage(IAsyncResult result);
    }
}

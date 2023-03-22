using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using DistributedServices.Seedwork.ErrorHandlers;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace DistributedServices.MainBoundedContext
{
    [ServiceContract]
    public interface ISalaryPeriodService
    {
        #region Salary Period

        [OperationContract()]
        [FaultContract(typeof(ApplicationServiceError))]
        SalaryPeriodDTO AddSalaryPeriod(SalaryPeriodDTO salaryPeriodDTO);

        [OperationContract()]
        [FaultContract(typeof(ApplicationServiceError))]
        bool UpdateSalaryPeriod(SalaryPeriodDTO salaryPeriodDTO);

        [OperationContract()]
        [FaultContract(typeof(ApplicationServiceError))]
        bool ProcessSalaryPeriod(SalaryPeriodDTO salaryPeriodDTO, List<EmployeeDTO> employees);

        [OperationContract()]
        [FaultContract(typeof(ApplicationServiceError))]
        bool CloseSalaryPeriod(SalaryPeriodDTO salaryPeriodDTO);

        [OperationContract()]
        [FaultContract(typeof(ApplicationServiceError))]
        bool PostPaySlip(Guid paySlipId, int moduleNavigationItemCode);

        [OperationContract()]
        [FaultContract(typeof(ApplicationServiceError))]
        List<SalaryPeriodDTO> FindSalaryPeriods();

        [OperationContract()]
        [FaultContract(typeof(ApplicationServiceError))]
        PageCollectionInfo<SalaryPeriodDTO> FindSalaryPeriodsInPage(int pageIndex, int pageSize);

        [OperationContract()]
        [FaultContract(typeof(ApplicationServiceError))]
        SalaryPeriodDTO FindSalaryPeriod(Guid salaryPeriodId);

        [OperationContract()]
        [FaultContract(typeof(ApplicationServiceError))]
        PageCollectionInfo<SalaryPeriodDTO> FindSalaryPeriodsByFilterInPage(int status, DateTime startDate, DateTime endDate, string text, int pageIndex, int pageSize);

        #endregion
    }
}

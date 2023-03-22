using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Collections.Generic;

namespace Application.MainBoundedContext.HumanResourcesModule.Services
{
    public interface ISalaryPeriodAppService
    {
        SalaryPeriodDTO AddNewSalaryPeriod(SalaryPeriodDTO salaryPeriodDTO, ServiceHeader serviceHeader);

        bool UpdateSalaryPeriod(SalaryPeriodDTO salaryPeriodDTO, ServiceHeader serviceHeader);

        bool ProcessSalaryPeriod(SalaryPeriodDTO salaryPeriodDTO, List<EmployeeDTO> employees, ServiceHeader serviceHeader);

        bool CloseSalaryPeriod(SalaryPeriodDTO salaryPeriodDTO, ServiceHeader serviceHeader);

        bool PostPaySlip(Guid paySlipId, int moduleNavigationItemCode, ServiceHeader serviceHeader);

        List<SalaryPeriodDTO> FindSalaryPeriods(ServiceHeader serviceHeader);

        PageCollectionInfo<SalaryPeriodDTO> FindSalaryPeriods(int pageIndex, int pageSize, ServiceHeader serviceHeader);

        PageCollectionInfo<SalaryPeriodDTO> FindSalaryPeriods(string text, int pageIndex, int pageSize, ServiceHeader serviceHeader);

        PageCollectionInfo<SalaryPeriodDTO> FindSalaryPeriods(int status, DateTime startDate, DateTime endDate, string text, int pageIndex, int pageSize, ServiceHeader serviceHeader);

        SalaryPeriodDTO FindSalaryPeriod(Guid salaryPeriodId, ServiceHeader serviceHeader);

        SalaryPeriodDTO FindCachedSalaryPeriod(Guid salaryPeriodId, ServiceHeader serviceHeader);
    }
}

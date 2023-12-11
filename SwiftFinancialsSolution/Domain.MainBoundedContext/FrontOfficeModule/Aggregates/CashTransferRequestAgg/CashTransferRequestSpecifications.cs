using Domain.Seedwork.Specification;
using Infrastructure.Crosscutting.Framework.Utils;
using System;

namespace Domain.MainBoundedContext.FrontOfficeModule.Aggregates.CashTransferRequestAgg
{
    public static class CashTransferRequestSpecifications
    {
        public static Specification<CashTransferRequest> DefaultSpec()
        {
            Specification<CashTransferRequest> specification = new TrueSpecification<CashTransferRequest>();

            return specification;
        }

        public static Specification<CashTransferRequest> CashTransferRequestWithDateRange(DateTime startDate, DateTime endDate, int status)
        {
            endDate = UberUtil.AdjustTimeSpan(endDate);

            Specification<CashTransferRequest> specification = DefaultSpec();

            specification &= new DirectSpecification<CashTransferRequest>(x => x.CreatedDate >= startDate && x.CreatedDate <= endDate && x.Status == status);

            return specification;
        }

        public static Specification<CashTransferRequest> CashTransferRequestWithEmployeeId(Guid employeeId, DateTime startDate, DateTime endDate, int status)
        {
            endDate = UberUtil.AdjustTimeSpan(endDate);

            Specification<CashTransferRequest> specification = DefaultSpec();

            specification &= new DirectSpecification<CashTransferRequest>(x => x.EmployeeId == employeeId && x.CreatedDate >= startDate && x.CreatedDate <= endDate && x.Status == status);

            return specification;
        }

        public static Specification<CashTransferRequest> ActionableCashTransferRequestWithEmployeeId(Guid employeeId)
        {
            Specification<CashTransferRequest> specification = DefaultSpec();

            specification &= new DirectSpecification<CashTransferRequest>(x => x.EmployeeId == employeeId && (x.Status == (int)CashTransferRequestStatus.Acknowledged) && !x.Utilized);

            return specification;
        }
    }
}

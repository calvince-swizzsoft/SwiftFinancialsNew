using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using System;
using System.Collections.Generic;
using TestApis.Services;

namespace TestApis.Models
{
    public class CustomerFullDetailsResponse
    {
        public CustomerDTO Customer { get; set; }
        public List<NextOfKinDTO> NextOfKins { get; set; }
        public List<CustomerAccountDTO> Accounts { get; set; }
        public PercentageSummaryDTO PercentageSummary { get; set; }
    }

    public class PaginatedResponse<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public List<T> Items { get; set; }
    }

    public class DuplicateCheckResponse
    {
        public bool Exists { get; set; }
        public string IdentityCardNumber { get; set; }
        public CustomerSummaryDTO Customer { get; set; }
    }

    public class CustomerSummaryDTO
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string IdentityCardNumber { get; set; }
        public string MemberNumber { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
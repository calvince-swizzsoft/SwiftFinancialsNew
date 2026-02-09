using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using System;
using System.Collections.Generic;
using TestApis.Services;

namespace Application.MainBoundedContext.DTO.RegistryModule
{
    public class CustomerWithNextOfKinsDTO : CustomerDTO
    {
        public int NextOfKinCount { get; set; }
        public IEnumerable<NextOfKinDTO> NextOfKins { get; set; }
        public PercentageSummaryDTO PercentageSummary { get; set; }
    }

    public class CustomerWithEverythingDTO : CustomerDTO
    {
        public int AccountCount { get; set; }
        public int NextOfKinCount { get; set; }
        public IEnumerable<CustomerAccountDTO> Accounts { get; set; }
        public IEnumerable<NextOfKinDTO> NextOfKins { get; set; }
        public PercentageSummaryDTO PercentageSummary { get; set; }
    }
}
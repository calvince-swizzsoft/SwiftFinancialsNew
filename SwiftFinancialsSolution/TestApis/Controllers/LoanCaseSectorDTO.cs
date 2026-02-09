using System;

namespace TestApis.Controllers
{
    public class LoanCaseSectorDTO
    {
        public Guid LoanCaseId { get; set; }
        public string SectorCode { get; set; }
        public string SubSectorCode { get; set; }
    }
}
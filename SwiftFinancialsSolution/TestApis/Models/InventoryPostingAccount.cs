using System;

namespace TestApis.Models
{
    public class InventoryPostingAccount
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public Guid ChartOfAccountId { get; set; }
        public Guid SequentialId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}

using System;

namespace TestApis.Models
{
    public class PAYETaxBand
    {
        public int Id { get; set; }
        public int TaxYear { get; set; }
        public decimal LowerLimit { get; set; }
        public decimal? UpperLimit { get; set; }   // NULL = open-ended
        public decimal Rate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TestApis.Models
{
	public class PAYESetup
	{
        public int Id { get; set; }
        public string Type { get; set; }
        public decimal LowerLimit { get; set; }
        public decimal UpperLimit { get; set; }
        public decimal BandAmount { get; set; }
        public decimal Rate { get; set; }
        public decimal ReliefAmount { get; set; }
    }
}
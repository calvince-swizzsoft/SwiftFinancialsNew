using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TestApis.Models
{
    public class MemberFullDetailsDTO
    {
        [Newtonsoft.Json.JsonIgnore]
        public CustomerDTO2 Member { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public List<MemberBankDetailDTO> BankDetails { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public List<NextOfKinDTO2> NextOfKin { get; set; }
    }
}
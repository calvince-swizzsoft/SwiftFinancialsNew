using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SwiftFinancials.Web.Models
{
    public class OpenAIResponse
    {
        public List<Choice> Choices { get; set; }
    }

    public class Choice
    {
        public Message Message { get; set; }
    }
}

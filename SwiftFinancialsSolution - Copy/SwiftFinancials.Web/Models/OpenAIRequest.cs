using System.Collections.Generic;

namespace SwiftFinancials.Web.Models
{
    public class OpenAIRequest
    {
        public string Model { get; set; } = "gpt-5"; // <-- updated
        public List<Message> Messages { get; set; }
        public double Temperature { get; set; } = 0.7;
    }

    public class Message
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }
}

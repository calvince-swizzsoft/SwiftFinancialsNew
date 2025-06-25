using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using SwiftFinancials.Web.Models;
using Newtonsoft.Json;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Attributes;
using SwiftFinancials.Web.Identity;

namespace SwiftFinancials.Web.Controllers
{
    public class ChatController : MasterController
    {
        private readonly string _apiKey = "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";
        private readonly string _apiUrl = "https://api.openai.com/v1/chat/completions";

        // GET: Chat
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();
            ViewBag.UserName = User.Identity.Name ?? "You";
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Ask(string prompt)
        {
            var request = new OpenAIRequest
            {
                Model = "gpt-4o",
                Temperature = 0.7,
                Messages = new List<Message>
        {
            new Message { Role = "user", Content = prompt }
        }
            };

            var json = JsonConvert.SerializeObject(request);
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _apiKey);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(_apiUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    try
                    {
                        var errorObj = JsonConvert.DeserializeObject<dynamic>(responseBody);
                        string errorMsg = errorObj?.error?.message ?? "Something went wrong.";
                        return Json(new { reply = $"⚠️ {errorMsg}", isError = true });
                    }
                    catch
                    {
                        return Json(new { reply = "⚠️ Unexpected error occurred.", isError = true });
                    }
                }

                var result = JsonConvert.DeserializeObject<OpenAIResponse>(responseBody);
                var reply = result?.Choices?[0]?.Message?.Content?.Trim();

                return Json(new { reply, isError = false });
            }
        }
    }
}

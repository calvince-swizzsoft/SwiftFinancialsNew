using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class MpesaHelper
    {
        private readonly HttpClient _client;
        private readonly string _consumerKey = "6cFZG0GjxmELiDILxC8Gn6tftwJDzh7sQdGQNoXDfQFq0Jtz"; // Replace with your actual Sandbox Consumer Key
        private readonly string _consumerSecret = "CMBGs7f4aYBUzloDuXGiGqdjtTUe58iidGAr6yravTxzgxcNwquNwGYn7X0JLfXt"; // Replace with your actual Sandbox Consumer Secret
        private readonly string _shortCode = "600000"; // Test Paybill ShortCode
        private readonly string _initiatorName = "testapi"; // For Sandbox
        private readonly string _securityCredential = "Safaricom123!"; // Use encrypted string for production
        private readonly string _resultUrl = "https://webhook.site/d359b7ee-a56a-4067-ac0b-979898128121";
        private readonly string _timeoutUrl = "https://webhook.site/d359b7ee-a56a-4067-ac0b-979898128121";


        public MpesaHelper()
        {
            _client = new HttpClient();
        }

        public async Task<string> GetAccessTokenAsync()
        {
            try
            {
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_consumerKey}:{_consumerSecret}"));
                _client.DefaultRequestHeaders.Clear();
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                var response = await _client.GetAsync("https://sandbox.safaricom.co.ke/oauth/v1/generate?grant_type=client_credentials");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var tokenObj = JsonDocument.Parse(json);
                return tokenObj.RootElement.GetProperty("access_token").GetString();
            }
            catch (Exception ex)
            {
                return $"AccessToken Error: {ex.Message}";
            }
        }

        public async Task<string> SendB2CAsync(string phoneNumber, decimal amount)
        {
            try
            {
                phoneNumber = "254700000001";
                var accessToken = await GetAccessTokenAsync();
                if (accessToken.StartsWith("AccessToken Error"))
                    return accessToken;

                _client.DefaultRequestHeaders.Clear();
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var payload = new
                {
                    InitiatorName = _initiatorName,
                    SecurityCredential = _securityCredential,
                    CommandID = "BusinessPayment",
                    Amount = amount,
                    PartyA = _shortCode,
                    PartyB = phoneNumber,
                    Remarks = "Loan Disbursement",
                    QueueTimeOutURL = _timeoutUrl,
                    ResultURL = _resultUrl,
                    Occasion = "LoanDisbursement"
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _client.PostAsync("https://sandbox.safaricom.co.ke/mpesa/b2c/v1/paymentrequest", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                return responseContent;
            }
            catch (Exception ex)
            {
                return $"B2C Error: {ex.Message}";
            }
        }
    }
}

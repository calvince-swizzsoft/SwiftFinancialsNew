using Application.MainBoundedContext.DTO.MessagingModule;
using Infrastructure.Crosscutting.Framework.Logging;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.Practices.EnterpriseLibrary.Common.Utility;
using MiscUtil;
using System;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Script.Serialization;
using SwiftFinancials.Apis.Configuration;
using SwiftFinancials.Apis.Filters;
using VanguardFinancials.Presentation.Infrastructure.Models;
using VanguardFinancials.Presentation.Infrastructure.Services;

namespace SwiftFinancials.Apis.Controllers
{
    [InternetBankingAuthenticationFilter(true)]
    public class InternetBankingController : ApiController
    {
        private readonly IChannelService _channelService;
        private int _pageIndex;
        private int _pageSize;

        public InternetBankingController(
            IChannelService channelService)
        {
            Guard.ArgumentNotNull(channelService, "channelService");

            _channelService = channelService;
            _pageIndex = 0;
            _pageSize = 100;
        }

        // GET: api/internetbanking
        public HttpResponseMessage Get()
        {
            var assemblyAttributes = new AssemblyAttributes();

            var httpResponseMessage = Request.CreateResponse(HttpStatusCode.OK);

            httpResponseMessage.Content = new StringContent(string.Format("Company: {0}\nProduct: {1}\nCopyright: {2}\nTrademark: {3}\nVersion: {4}\nDescription: {5}\nConfiguration: {6}", assemblyAttributes.Company, assemblyAttributes.Product, assemblyAttributes.Copyright, assemblyAttributes.Trademark, assemblyAttributes.Version, assemblyAttributes.Description, assemblyAttributes.Configuration));

            return httpResponseMessage;
        }

        // POST api/internetbanking
        public async Task<HttpResponseMessage> Post(InternetBankingViewModel formData)
        {
            var httpResponseMessage = Request.CreateResponse(HttpStatusCode.OK);

            if (formData == null)
                httpResponseMessage = Request.CreateErrorResponse(HttpStatusCode.Forbidden, "null payload");
            else
            {
                try
                {
                    var webApiConfigSection = (WebApiConfigSection)ConfigurationManager.GetSection("webApiConfiguration");

                    if (webApiConfigSection == null)
                        httpResponseMessage = Request.CreateErrorResponse(HttpStatusCode.Forbidden, "invalid configuration");
                    else
                    {
                        foreach (var settingsItem in webApiConfigSection.WebApiSettingsItems)
                        {
                            var webApiSettingsElement = (WebApiSettingsElement)settingsItem;

                            if (webApiSettingsElement != null && webApiSettingsElement.Enabled == 1)
                            {
                                var serviceHeader = new ServiceHeader { ApplicationDomainName = webApiSettingsElement.UniqueId };

                                var javaScriptSerializer = new JavaScriptSerializer();

                                var dynamic = javaScriptSerializer.Deserialize<dynamic>(string.Format("{0}", formData));

                                var requestType = default(int);

                                if (!int.TryParse(string.Format("{0}", dynamic["request_type"]), out requestType))
                                    throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden, Content = new StringContent("could not parse request_type") });

                                if (!Enum.IsDefined(typeof(RequestType), requestType))
                                    throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden, Content = new StringContent("invalid request_type") });

                                switch ((RequestType)requestType)
                                {
                                    case RequestType.Authentication:

                                        #region Authentication

                                        string Authentication_Text = string.Format("{0}", dynamic["member_no"]);

                                        var Authentication_CustomerDTOs = await _channelService.FindCustomersByFilterInPageAsync(Authentication_Text, (int)CustomerFilter.Reference1, _pageIndex, _pageSize, serviceHeader);

                                        if (Authentication_CustomerDTOs != null && Authentication_CustomerDTOs.PageCollection != null && Authentication_CustomerDTOs.PageCollection.Count == 1)
                                        {
                                            var Authentication_TargetCustomerDTO = Authentication_CustomerDTOs.PageCollection[0];

                                            if (string.IsNullOrWhiteSpace(Authentication_TargetCustomerDTO.AddressMobileLine) || !Regex.IsMatch(Authentication_TargetCustomerDTO.AddressMobileLine, @"^\+(?:[0-9]??){6,14}[0-9]$"))
                                                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden, Content = new StringContent("invalid mobile number") });

                                            var Authentication_CustomerAccountDTOs = await _channelService.FindCustomerAccountsByCustomerIdAsync(Authentication_TargetCustomerDTO.Id, true, true, true, false, serviceHeader);

                                            var dynamicPayload = new
                                            {
                                                Id = Authentication_TargetCustomerDTO.Id,
                                                PaddedSerialNumber = Authentication_TargetCustomerDTO.PaddedSerialNumber,
                                                FullName = Authentication_TargetCustomerDTO.FullName,
                                                IdentityCardType = Authentication_TargetCustomerDTO.IndividualIdentityCardTypeDescription,
                                                IdentificationNumber = Authentication_TargetCustomerDTO.IdentificationNumber,
                                                Nationality = Authentication_TargetCustomerDTO.IndividualNationalityDescription,
                                                Gender = Authentication_TargetCustomerDTO.IndividualGenderDescription,
                                                BirthDate = Authentication_TargetCustomerDTO.IndividualBirthDate.HasValue ? Authentication_TargetCustomerDTO.IndividualBirthDate.Value.ToString("dd/MM/yyyy") : string.Empty,
                                                AccountNumber = Authentication_TargetCustomerDTO.Reference1,
                                                Employer = Authentication_TargetCustomerDTO.StationZoneDivisionEmployerDescription,
                                                City = Authentication_TargetCustomerDTO.AddressCity,
                                                MobileLine = Authentication_TargetCustomerDTO.AddressMobileLine,
                                                Email = Authentication_TargetCustomerDTO.AddressEmail,
                                                OneTimePassword = string.Format("{0}", StaticRandom.Next(1000, 9999)),
                                                Accounts = Authentication_CustomerAccountDTOs != null ? from c in Authentication_CustomerAccountDTOs
                                                                                                        select new
                                                                                                        {
                                                                                                            Id = c.Id,
                                                                                                            FullAccountNumber = c.FullAccountNumber,
                                                                                                            TargetProductDescription = c.CustomerAccountTypeTargetProductDescription,
                                                                                                            ProductCode = c.CustomerAccountTypeProductCode,
                                                                                                            ProductCodeDescription = c.CustomerAccountTypeProductCodeDescription,
                                                                                                            BookBalance = c.BookBalance,
                                                                                                            AvailableBalance = c.AvailableBalance,
                                                                                                            Status = c.StatusDescription,
                                                                                                        } : null
                                            };

                                            var bulkMessageDTO = new BulkMessageDTO
                                            {
                                                Recipients = Authentication_TargetCustomerDTO.AddressMobileLine,
                                                TextMessage = string.Format("Your transaction authorization token is {0}.", dynamicPayload.OneTimePassword),
                                                Priority = (int)QueuePriority.Highest,
                                                SecurityCritical = true,
                                            };

                                            if (await _channelService.AddBulkMessageAsync(bulkMessageDTO, serviceHeader))
                                                httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(dynamicPayload)));
                                            else throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden, Content = new StringContent("token serialization failed") });
                                        }
                                        else throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden, Content = new StringContent("no match") });

                                        #endregion

                                        break;
                                    case RequestType.AccountStatement:

                                        #region AccountStatement

                                        string AccountStatement_ACCOUNTNO = string.Format("{0}", dynamic["account_no"]);

                                        var startDate = DateTime.Today;

                                        var endDate = DateTime.Now;

                                        string[] formats = { "dd/MM/yyyy" };

                                        if (!int.TryParse(string.Format("{0}", dynamic["page_index"]), out _pageIndex))
                                            throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden, Content = new StringContent("invalid page index") });

                                        if (!int.TryParse(string.Format("{0}", dynamic["page_size"]), out _pageSize))
                                            throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden, Content = new StringContent("invalid page size") });

                                        if (!DateTime.TryParseExact(string.Format("{0}", dynamic["start_date"]), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate))
                                            throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden, Content = new StringContent("invalid start date") });

                                        if (!DateTime.TryParseExact(string.Format("{0}", dynamic["end_date"]), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate))
                                            throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden, Content = new StringContent("invalid end date") });

                                        var AccountStatement_CustomerAccountDTO = await _channelService.FindCustomerAccountByFullAccountNumberAsync(AccountStatement_ACCOUNTNO, false, true, false, false, serviceHeader);
                                        if (AccountStatement_CustomerAccountDTO == null)
                                            throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden, Content = new StringContent("no match by account no") });

                                        var AccountStatement_Transactions = await _channelService.FindGeneralLedgerTransactionsByCustomerAccountIdAndDateRangeInPageAsync(_pageIndex, _pageSize, AccountStatement_CustomerAccountDTO, startDate, endDate, string.Empty, (int)JournalEntryFilter.JournalPrimaryDescription, true, serviceHeader);

                                        if (AccountStatement_Transactions != null && AccountStatement_Transactions.PageCollection != null)
                                        {
                                            var dynamicPayload = new
                                            {
                                                ItemsCount = AccountStatement_Transactions.ItemsCount,
                                                AvailableBalanceBroughtFoward = AccountStatement_Transactions.AvailableBalanceBroughtFoward,
                                                BookBalanceBroughtFoward = AccountStatement_Transactions.BookBalanceBroughtFoward,
                                                TotalDebits = AccountStatement_Transactions.TotalDebits,
                                                TotalCredits = AccountStatement_Transactions.TotalCredits,
                                                AvailableBalanceCarriedForward = AccountStatement_Transactions.AvailableBalanceCarriedForward,
                                                BookBalanceCarriedForward = AccountStatement_Transactions.BookBalanceCarriedForward,
                                                Transactions = from t in AccountStatement_Transactions.PageCollection
                                                               select new
                                                               {
                                                                   CreatedDate = t.JournalCreatedDate.ToString("dd/MM/yyyy HH:mm:ss tt"),
                                                                   ValueDate = t.JournalValueDate.HasValue ? t.JournalValueDate.Value.ToString("dd/MM/yyyy") : string.Empty,
                                                                   Branch = t.BranchDescription,
                                                                   PrimaryDescription = t.JournalPrimaryDescription,
                                                                   SecondaryDescription = t.JournalSecondaryDescription,
                                                                   Reference = t.JournalReference,
                                                                   Debit = t.Debit,
                                                                   Credit = t.Credit,
                                                                   RunningBalance = t.RunningBalance,
                                                                   TransactionCode = t.JournalTransactionCodeDescription,
                                                               }
                                            };

                                            httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(dynamicPayload)));
                                        }
                                        else throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden, Content = new StringContent("no match") });

                                        #endregion

                                        break;
                                    default:
                                        httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new { RESPCODE = 1, RESPDESC = "request_type not implemented" })));
                                        break;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex is HttpResponseException)
                    {
                        var responseException = ex as HttpResponseException;

                        var responseContent = await responseException.Response.Content.ReadAsStringAsync();

                        LoggerFactory.CreateLog().LogError("InternetBanking-HttpResponseException-{0}-{1}->", ex, responseException.Response.StatusCode, responseContent);

                        httpResponseMessage = responseException.Response;
                    }
                    else
                    {
                        LoggerFactory.CreateLog().LogError("InternetBanking-Exception->", ex);

                        httpResponseMessage = Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message);
                    }
                }
            }

            return httpResponseMessage;
        }
    }
}
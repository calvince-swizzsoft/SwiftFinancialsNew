using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.MessagingModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Application.MainBoundedContext.Services;
using Infrastructure.Crosscutting.Framework.Extensions;
using Infrastructure.Crosscutting.Framework.Logging;
using Infrastructure.Crosscutting.Framework.Models;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.Practices.EnterpriseLibrary.Common.Utility;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Messaging;
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
    [CitiusAuthenticationFilter(true)]
    public class CitiusController : ApiController
    {
        private readonly IChannelService _channelService;
        private readonly IMessageQueueService _messageQueueService;

        private readonly int _pageIndex;
        private readonly int _pageSize;

        public CitiusController(
            IChannelService channelService,
            IMessageQueueService messageQueueService)
        {
            Guard.ArgumentNotNull(channelService, "channelService");
            Guard.ArgumentNotNull(messageQueueService, "messageQueueService");

            _channelService = channelService;
            _messageQueueService = messageQueueService;

            _pageIndex = 0;
            _pageSize = 100;
        }

        // GET: api/citius
        public HttpResponseMessage Get()
        {
            var assemblyAttributes = new AssemblyAttributes();

            var httpResponseMessage = Request.CreateResponse(HttpStatusCode.OK);

            httpResponseMessage.Content = new StringContent(string.Format("Company: {0}\nProduct: {1}\nCopyright: {2}\nTrademark: {3}\nVersion: {4}\nDescription: {5}\nConfiguration: {6}", assemblyAttributes.Company, assemblyAttributes.Product, assemblyAttributes.Copyright, assemblyAttributes.Trademark, assemblyAttributes.Version, assemblyAttributes.Description, assemblyAttributes.Configuration));

            return httpResponseMessage;
        }

        // POST api/citius
        public async Task<HttpResponseMessage> Post(CitiusViewModel formData)
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

                                var functionCode = default(int);

                                #region House Keeping

                                if (!int.TryParse(string.Format("{0}", dynamic["FUNCTIONCD"]), out functionCode))
                                {
                                    throw new HttpResponseException(new HttpResponseMessage
                                    {
                                        StatusCode = HttpStatusCode.Forbidden,
                                        Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new
                                        {
                                            RESPCODE = 1,
                                            RESPDESC = "Could Not Parse FUNCTIONCD"
                                        })))
                                    });
                                }

                                if (!Enum.IsDefined(typeof(FUNCTIONCODE), functionCode))
                                {
                                    throw new HttpResponseException(new HttpResponseMessage
                                    {
                                        StatusCode = HttpStatusCode.Forbidden,
                                        Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new
                                        {
                                            RESPCODE = 1,
                                            RESPDESC = "Invalid FUNCTIONCD"
                                        })))
                                    });
                                }

                                var alternateChannelLogDTO = new AlternateChannelLogDTO
                                {
                                    AlternateChannelType = (int)AlternateChannelType.Citius,
                                    WALLETMessage = string.Format("{0}", formData),
                                };

                                AlternateChannelDTO targetAlternateChannelDTO = null;

                                CustomerAccountDTO targetCustomerAccountDTO = null;

                                var targetTellerType = TellerType.AgentPointOfSale;

                                var settlementGLAccountId = Guid.Empty;

                                var settlementGLAccountBalanceCarriedForward = 0m;

                                var settlementName = string.Empty;

                                var commissionSettlementGLAccountId = Guid.Empty;

                                CustomerAccountDTO floatCustomerAccountDTO = null;

                                CustomerAccountDTO commissionCustomerAccountDTO = null;

                                if (functionCode.In((int)FUNCTIONCODE.AcceptSavingDeposit, (int)FUNCTIONCODE.AcceptSavingWithdraw, (int)FUNCTIONCODE.GetMiniStatement))
                                {
                                    alternateChannelLogDTO.WALLETMessageTypeIdentification = string.Format("{0}", functionCode);
                                    alternateChannelLogDTO.WALLETPrimaryAccountNumber = string.Format("{0}", dynamic["MEMBERID"]);
                                    alternateChannelLogDTO.WALLETSystemTraceAuditNumber = string.Format("{0}", dynamic["TRANID"]);
                                    alternateChannelLogDTO.WALLETRetrievalReferenceNumber = string.Format("{0}", dynamic["DEVICEID"]);

                                    var AcceptSavingDeposit_ExistingLogs = await _channelService.MatchWALLETAlternateChannelLogsAsync(alternateChannelLogDTO, true, 1, serviceHeader);

                                    if (AcceptSavingDeposit_ExistingLogs != null && AcceptSavingDeposit_ExistingLogs.Any())
                                    {
                                        throw new HttpResponseException(new HttpResponseMessage
                                        {
                                            StatusCode = HttpStatusCode.Forbidden,
                                            Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new
                                            {
                                                RESPCODE = 1,
                                                RESPDESC = "Duplicate Transmission"
                                            })))
                                        });
                                    }

                                    alternateChannelLogDTO = await _channelService.AddAlternateChannelLogAsync(alternateChannelLogDTO, serviceHeader);

                                    var alternateChannelDTOs = await _channelService.FindAlternateChannelsByCardNumberAndCardTypeAsync(alternateChannelLogDTO.WALLETPrimaryAccountNumber, (int)AlternateChannelType.Citius, false, serviceHeader);

                                    if (alternateChannelDTOs == null || !alternateChannelDTOs.Any() || alternateChannelDTOs.Count != 1)
                                    {
                                        throw new HttpResponseException(new HttpResponseMessage
                                        {
                                            StatusCode = HttpStatusCode.Forbidden,
                                            Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new
                                            {
                                                RESPCODE = 1,
                                                RESPDESC = "Alternate Channel Does Not Exist"
                                            })))
                                        });
                                    }

                                    targetAlternateChannelDTO = alternateChannelDTOs.First();

                                    if (targetAlternateChannelDTO.IsLocked || targetAlternateChannelDTO.RecordStatus != (int)RecordStatus.Approved || targetAlternateChannelDTO.CustomerAccountStatus != (int)CustomerAccountStatus.Normal || targetAlternateChannelDTO.CustomerAccountRecordStatus != (int)RecordStatus.Approved)
                                    {
                                        throw new HttpResponseException(new HttpResponseMessage
                                        {
                                            StatusCode = HttpStatusCode.Forbidden,
                                            Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new
                                            {
                                                RESPCODE = 1,
                                                RESPDESC = "Closed Account"
                                            })))
                                        });
                                    }

                                    targetCustomerAccountDTO = await _channelService.FindCustomerAccountAsync(targetAlternateChannelDTO.CustomerAccountId, true, true, false, false, serviceHeader);

                                    var tellerDTOs = await _channelService.FindTellersByReferenceAsync(alternateChannelLogDTO.WALLETRetrievalReferenceNumber, true, serviceHeader);

                                    if (tellerDTOs != null && tellerDTOs.Any() && tellerDTOs.Count == 1)
                                    {
                                        var targetTeller = tellerDTOs[0];

                                        settlementName = targetTeller.Description;

                                        targetTellerType = (TellerType)targetTeller.Type;

                                        switch (targetTellerType)
                                        {
                                            case TellerType.InhousePointOfSale:

                                                settlementGLAccountId = targetTeller.ChartOfAccountId ?? Guid.Empty;

                                                settlementGLAccountBalanceCarriedForward = targetTeller.BookBalance;

                                                break;
                                            case TellerType.AgentPointOfSale:

                                                if (targetTeller.FloatCustomerAccountId.HasValue && targetTeller.FloatCustomerAccountId != Guid.Empty)
                                                    floatCustomerAccountDTO = await _channelService.FindCustomerAccountAsync(targetTeller.FloatCustomerAccountId.Value, true, true, false, false, serviceHeader);

                                                if (targetTeller.CommissionCustomerAccountId.HasValue && targetTeller.CommissionCustomerAccountId != Guid.Empty)
                                                    commissionCustomerAccountDTO = await _channelService.FindCustomerAccountAsync(targetTeller.CommissionCustomerAccountId.Value, true, true, false, false, serviceHeader);

                                                commissionSettlementGLAccountId = await _channelService.GetChartOfAccountMappingForSystemGeneralLedgerAccountCodeAsync((int)SystemGeneralLedgerAccountCode.AgentCommissionSettlement, serviceHeader);

                                                break;
                                            default:
                                                break;
                                        }
                                    }

                                    switch (targetTellerType)
                                    {
                                        case TellerType.InhousePointOfSale:

                                            if (settlementGLAccountId == Guid.Empty)
                                            {
                                                throw new HttpResponseException(new HttpResponseMessage
                                                {
                                                    StatusCode = HttpStatusCode.Forbidden,
                                                    Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new
                                                    {
                                                        RESPCODE = 1,
                                                        RESPDESC = "No Match For Agent Account"
                                                    })))
                                                });
                                            }

                                            break;
                                        case TellerType.AgentPointOfSale:

                                            if (floatCustomerAccountDTO == null || commissionCustomerAccountDTO == null)
                                            {
                                                throw new HttpResponseException(new HttpResponseMessage
                                                {
                                                    StatusCode = HttpStatusCode.Forbidden,
                                                    Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new
                                                    {
                                                        RESPCODE = 1,
                                                        RESPDESC = "No Match For Agent Account"
                                                    })))
                                                });
                                            }

                                            break;
                                        default:
                                            break;
                                    }
                                }

                                #endregion

                                switch ((FUNCTIONCODE)functionCode)
                                {
                                    case FUNCTIONCODE.SearchMember:

                                        #region SearchMember

                                        string SearchMember_Text = string.Format("{0}", dynamic["SEARCHSTRING"]);

                                        string SearchMember_Filter = string.Format("{0}", dynamic["SEARCHBY"]);

                                        PageCollectionInfo<CustomerDTO> SearchMember_CustomerDTOs = null;

                                        switch (SearchMember_Filter)
                                        {
                                            case "0":// MemberId
                                                SearchMember_CustomerDTOs = await _channelService.FindCustomersByFilterInPageAsync(SearchMember_Text, (int)CustomerFilter.Reference1, _pageIndex, _pageSize, serviceHeader);
                                                break;
                                            case "1":// National 
                                                SearchMember_CustomerDTOs = await _channelService.FindCustomersByFilterInPageAsync(SearchMember_Text, (int)CustomerFilter.IdentityCardNumber, _pageIndex, _pageSize, serviceHeader);
                                                break;
                                            case "2":// NAME
                                                SearchMember_CustomerDTOs = await _channelService.FindCustomersByFilterInPageAsync(SearchMember_Text, (int)CustomerFilter.FirstName, _pageIndex, _pageSize, serviceHeader);
                                                break;
                                            default:
                                                break;
                                        }

                                        if (SearchMember_CustomerDTOs != null && SearchMember_CustomerDTOs.PageCollection != null)
                                        {
                                            var SearchMember_Payload = new
                                            {
                                                RESPCODE = 0,
                                                ITEMSCOUNT = SearchMember_CustomerDTOs.ItemsCount,
                                                MEMBERS = SearchMember_CustomerDTOs != null ? from c in SearchMember_CustomerDTOs.PageCollection
                                                                                              select new
                                                                                              {
                                                                                                  ID = c.Id,
                                                                                                  NAME = c.FullName,
                                                                                                  MEMBERID = c.Reference1,
                                                                                                  NATIONALID = c.IdentificationNumber,
                                                                                                  SERIALNUMBER = c.PaddedSerialNumber,
                                                                                                  DOB = c.IndividualBirthDate.HasValue ? c.IndividualBirthDate.Value.ToString("dd/MM/yyyy") : string.Empty,
                                                                                                  CITY = c.AddressCity,
                                                                                                  REGMOBNO = c.AddressMobileLine
                                                                                              } : null
                                            };

                                            httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(SearchMember_Payload)));
                                        }
                                        else
                                        {
                                            httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new { RESPCODE = 1, RESPDESC = "No Match" })));
                                        };

                                        #endregion

                                        break;
                                    case FUNCTIONCODE.GetAccounts:

                                        #region GetAccounts

                                        string GetAccounts_MEMBERID = string.Format("{0}", dynamic["MEMBERID"]);

                                        int GetAccounts_ProductCode = default(int);

                                        if (int.TryParse(string.Format("{0}", dynamic["ACCTYPE"]), out GetAccounts_ProductCode))
                                        {
                                            if (GetAccounts_ProductCode.In(0, 1, 2, 3))
                                            {
                                                PageCollectionInfo<CustomerAccountDTO> GetAccounts_CustomerAccountDTOs = null;

                                                if (GetAccounts_ProductCode.In(0))
                                                    GetAccounts_CustomerAccountDTOs = await _channelService.FindCustomerAccountsByFilterInPageAsync(GetAccounts_MEMBERID, (int)CustomerFilter.Reference1, _pageIndex, _pageSize, true, true, true, false, serviceHeader);
                                                else if (GetAccounts_ProductCode.In(1, 2, 3))
                                                    GetAccounts_CustomerAccountDTOs = await _channelService.FindCustomerAccountsByProductCodeAndFilterInPageAsync(GetAccounts_ProductCode, GetAccounts_MEMBERID, (int)CustomerFilter.Reference1, _pageIndex, _pageSize, true, true, true, false, serviceHeader);

                                                if (GetAccounts_CustomerAccountDTOs != null && GetAccounts_CustomerAccountDTOs.PageCollection != null)
                                                {
                                                    var GetAccounts_Payload = new
                                                    {
                                                        RESPCODE = 0,
                                                        ITEMSCOUNT = GetAccounts_CustomerAccountDTOs.ItemsCount,
                                                        ACCOUNTS = from c in GetAccounts_CustomerAccountDTOs.PageCollection
                                                                   select new
                                                                   {
                                                                       ID = c.Id,
                                                                       ACCNO = c.FullAccountNumber,
                                                                       ACCDESC = c.CustomerAccountTypeTargetProductDescription,
                                                                       ACCTYPE = c.CustomerAccountTypeProductCode,
                                                                       ACCTYPEDESC = c.CustomerAccountTypeProductCodeDescription,
                                                                       BOOKBAL = c.BookBalance,
                                                                       CURBAL = c.AvailableBalance,
                                                                       REGMOBNO = c.CustomerAddressMobileLine
                                                                   }
                                                    };

                                                    httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(GetAccounts_Payload)));
                                                }
                                                else
                                                {
                                                    httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new { RESPCODE = 1, RESPDESC = "No Match" })));
                                                };
                                            }
                                            else
                                            {
                                                httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new { RESPCODE = 3, RESPDESC = "Invalid ACCTYPE" })));
                                            };
                                        }
                                        else
                                        {
                                            httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new { RESPCODE = 1, RESPDESC = "Could Not Parse ACCTYPE" })));
                                        };

                                        #endregion

                                        break;
                                    case FUNCTIONCODE.AcceptSavingDeposit:

                                        #region AcceptSavingDeposit

                                        decimal AcceptSavingDeposit_AMOUNT = 0m;

                                        if (!decimal.TryParse(string.Format("{0}", dynamic["AMOUNT"]), out AcceptSavingDeposit_AMOUNT))
                                        {
                                            throw new HttpResponseException(new HttpResponseMessage
                                            {
                                                StatusCode = HttpStatusCode.Forbidden,
                                                Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new
                                                {
                                                    RESPCODE = 1,
                                                    RESPDESC = "Invalid Amount"
                                                })))
                                            });
                                        }

                                        if (targetTellerType.In(TellerType.AgentPointOfSale) && ((floatCustomerAccountDTO.AvailableBalance - AcceptSavingDeposit_AMOUNT) < 0m))
                                        {
                                            throw new HttpResponseException(new HttpResponseMessage
                                            {
                                                StatusCode = HttpStatusCode.Forbidden,
                                                Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new
                                                {
                                                    RESPCODE = 1,
                                                    RESPDESC = "Agent Has Insufficient Funds"
                                                })))
                                            });
                                        }

                                        var AcceptSavingDeposit_Tariffs = await _channelService.ComputeTariffsByAlternateChannelTypeAsync(targetAlternateChannelDTO.Type, (int)AlternateChannelKnownChargeType.DepositCharges, AcceptSavingDeposit_AMOUNT, targetCustomerAccountDTO, serviceHeader);

                                        JournalDTO AcceptSavingDeposit_JournalDTO = null;

                                        switch (targetTellerType)
                                        {
                                            case TellerType.InhousePointOfSale:

                                                AcceptSavingDeposit_JournalDTO = await _channelService.AddJournalWithCustomerAccountAndAlternateChannelLogAndTariffsAsync(targetCustomerAccountDTO.BranchId, alternateChannelLogDTO.Id, AcceptSavingDeposit_AMOUNT, string.Format("cash deposit {0}", targetAlternateChannelDTO.TypeDescription), targetAlternateChannelDTO.MaskedCardNumber, string.Format("{0} > {1}", dynamic["ACCOUNTNO"], alternateChannelLogDTO.WALLETRetrievalReferenceNumber), 0x9999, (int)SystemTransactionCode.Citius, targetCustomerAccountDTO.CustomerAccountTypeTargetProductChartOfAccountId, settlementGLAccountId, targetCustomerAccountDTO, targetCustomerAccountDTO, AcceptSavingDeposit_Tariffs, serviceHeader);

                                                break;
                                            case TellerType.AgentPointOfSale:

                                                AcceptSavingDeposit_JournalDTO = await _channelService.AddJournalWithCustomerAccountAndAlternateChannelLogAndTariffsAsync(targetCustomerAccountDTO.BranchId, alternateChannelLogDTO.Id, AcceptSavingDeposit_AMOUNT, string.Format("cash deposit {0}", targetAlternateChannelDTO.TypeDescription), targetAlternateChannelDTO.MaskedCardNumber, string.Format("{0} > {1}", dynamic["ACCOUNTNO"], alternateChannelLogDTO.WALLETRetrievalReferenceNumber), 0x9999, (int)SystemTransactionCode.Citius, targetCustomerAccountDTO.CustomerAccountTypeTargetProductChartOfAccountId, floatCustomerAccountDTO.CustomerAccountTypeTargetProductChartOfAccountId, targetCustomerAccountDTO, floatCustomerAccountDTO, AcceptSavingDeposit_Tariffs, serviceHeader);

                                                break;
                                            default:
                                                break;
                                        }

                                        if (AcceptSavingDeposit_JournalDTO == null)
                                        {
                                            throw new HttpResponseException(new HttpResponseMessage
                                            {
                                                StatusCode = HttpStatusCode.Forbidden,
                                                Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new
                                                {
                                                    RESPCODE = 1,
                                                    RESPDESC = "Deposit Transaction Failed"
                                                })))
                                            });
                                        }
                                        else if (targetTellerType.In(TellerType.AgentPointOfSale) && commissionSettlementGLAccountId != Guid.Empty && AcceptSavingDeposit_Tariffs != null && AcceptSavingDeposit_Tariffs.Any())
                                        {
                                            var depositSettlements = from t in AcceptSavingDeposit_Tariffs
                                                                     where t.CreditGLAccountId == commissionSettlementGLAccountId
                                                                     select new QueueDTO
                                                                     {
                                                                         AppDomainName = serviceHeader.ApplicationDomainName,
                                                                         TarrifDeviceId = alternateChannelLogDTO.ISO8583PrimaryAccountNumber,
                                                                         TarrifMaskedCardNumber = targetAlternateChannelDTO.MaskedCardNumber,
                                                                         TarrifCreditGLAccountId = t.CreditGLAccountId,
                                                                         TarrifAmount = t.Amount,
                                                                         TarrifDescription = t.Description,
                                                                         TarrifCommissionCustomerAccountId = commissionCustomerAccountDTO.Id,
                                                                     };

                                            _messageQueueService.Send(webApiConfigSection.WebApiSettingsItems.CitiusQueuePath, depositSettlements.ToList(), MessageCategory.AgencyBankingSettlement, MessagePriority.Normal, 120);
                                        }

                                        alternateChannelLogDTO.WALLETAmount = AcceptSavingDeposit_AMOUNT;
                                        alternateChannelLogDTO.Response = string.Format("{0}", javaScriptSerializer.Serialize(new { RESPCODE = 0 }));

                                        await _channelService.UpdateAlternateChannelLogAsync(alternateChannelLogDTO, serviceHeader);

                                        httpResponseMessage.Content = new StringContent(alternateChannelLogDTO.Response);

                                        #endregion

                                        break;
                                    case FUNCTIONCODE.AcceptSavingWithdraw:

                                        #region AcceptSavingWithdraw

                                        decimal AcceptSavingWithdraw_AMOUNT = 0m;

                                        if (!decimal.TryParse(string.Format("{0}", dynamic["AMOUNT"]), out AcceptSavingWithdraw_AMOUNT))
                                        {
                                            throw new HttpResponseException(new HttpResponseMessage
                                            {
                                                StatusCode = HttpStatusCode.Forbidden,
                                                Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new
                                                {
                                                    RESPCODE = 1,
                                                    RESPDESC = "Invalid Amount"
                                                })))
                                            });
                                        }

                                        if (targetTellerType.In(TellerType.InhousePointOfSale) && ((settlementGLAccountBalanceCarriedForward + AcceptSavingWithdraw_AMOUNT) > 0m))
                                        {
                                            throw new HttpResponseException(new HttpResponseMessage
                                            {
                                                StatusCode = HttpStatusCode.Forbidden,
                                                Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new
                                                {
                                                    RESPCODE = 1,
                                                    RESPDESC = "Agent Has Insufficient Funds"
                                                })))
                                            });
                                        }

                                        var AcceptSavingWithdraw_ValueOfTransactionsDoneToday = 0m;

                                        var AcceptSavingWithdraw_TransactionsDoneToday = await _channelService.FindGeneralLedgerTransactionsByCustomerAccountIdAndDateRangeAsync(targetCustomerAccountDTO, DateTime.Today, DateTime.Today, false, serviceHeader);

                                        if (AcceptSavingWithdraw_TransactionsDoneToday != null && AcceptSavingWithdraw_TransactionsDoneToday.PageCollection != null && AcceptSavingWithdraw_TransactionsDoneToday.PageCollection.Any())
                                        {
                                            foreach (var item in AcceptSavingWithdraw_TransactionsDoneToday.PageCollection)
                                            {
                                                if (!item.JournalIsLocked && item.JournalTransactionCode == (int)SystemTransactionCode.Citius && (item.JournalParentId == null || item.JournalParentId == Guid.Empty))
                                                {
                                                    AcceptSavingWithdraw_ValueOfTransactionsDoneToday += item.Debit;
                                                }
                                            }
                                        }

                                        if ((AcceptSavingWithdraw_ValueOfTransactionsDoneToday + AcceptSavingWithdraw_AMOUNT) > targetAlternateChannelDTO.DailyLimit)
                                        {
                                            throw new HttpResponseException(new HttpResponseMessage
                                            {
                                                StatusCode = HttpStatusCode.Forbidden,
                                                Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new
                                                {
                                                    RESPCODE = 1,
                                                    RESPDESC = "Would Exceed Daily Limit"
                                                })))
                                            });
                                        }

                                        var AcceptSavingWithdraw_Tariffs = await _channelService.ComputeTariffsByAlternateChannelTypeAsync(targetAlternateChannelDTO.Type, (int)AlternateChannelKnownChargeType.WithdrawalCharges, AcceptSavingWithdraw_AMOUNT, targetCustomerAccountDTO, serviceHeader);

                                        if ((AcceptSavingWithdraw_AMOUNT + AcceptSavingWithdraw_Tariffs.Where(x => x.ChargeBenefactor == (int)ChargeBenefactor.Customer).Sum(x => x.Amount)) > targetCustomerAccountDTO.AvailableBalance)
                                        {
                                            throw new HttpResponseException(new HttpResponseMessage
                                            {
                                                StatusCode = HttpStatusCode.Forbidden,
                                                Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new
                                                {
                                                    RESPCODE = 1,
                                                    RESPDESC = "Insufficient Funds"
                                                })))
                                            });
                                        }

                                        JournalDTO AcceptSavingWithdraw_JournalDTO = null;

                                        switch (targetTellerType)
                                        {
                                            case TellerType.InhousePointOfSale:

                                                AcceptSavingWithdraw_JournalDTO = await _channelService.AddJournalWithCustomerAccountAndAlternateChannelLogAndTariffsAsync(targetAlternateChannelDTO.CustomerAccountBranchId, alternateChannelLogDTO.Id, AcceptSavingWithdraw_AMOUNT, string.Format("cash withdrawal {0}", targetAlternateChannelDTO.TypeDescription), targetAlternateChannelDTO.MaskedCardNumber, string.Format("{0} > {1}", dynamic["ACCOUNTNO"], alternateChannelLogDTO.WALLETRetrievalReferenceNumber), 0x9999, (int)SystemTransactionCode.Citius, settlementGLAccountId, targetCustomerAccountDTO.CustomerAccountTypeTargetProductChartOfAccountId, targetCustomerAccountDTO, targetCustomerAccountDTO, AcceptSavingWithdraw_Tariffs, serviceHeader);

                                                break;
                                            case TellerType.AgentPointOfSale:

                                                AcceptSavingWithdraw_JournalDTO = await _channelService.AddJournalWithCustomerAccountAndAlternateChannelLogAndTariffsAsync(targetAlternateChannelDTO.CustomerAccountBranchId, alternateChannelLogDTO.Id, AcceptSavingWithdraw_AMOUNT, string.Format("cash withdrawal {0}", targetAlternateChannelDTO.TypeDescription), targetAlternateChannelDTO.MaskedCardNumber, string.Format("{0} > {1}", dynamic["ACCOUNTNO"], alternateChannelLogDTO.WALLETRetrievalReferenceNumber), 0x9999, (int)SystemTransactionCode.Citius, floatCustomerAccountDTO.CustomerAccountTypeTargetProductChartOfAccountId, targetCustomerAccountDTO.CustomerAccountTypeTargetProductChartOfAccountId, floatCustomerAccountDTO, targetCustomerAccountDTO, AcceptSavingWithdraw_Tariffs, serviceHeader);

                                                break;
                                            default:
                                                break;
                                        }

                                        if (AcceptSavingWithdraw_JournalDTO == null)
                                        {
                                            throw new HttpResponseException(new HttpResponseMessage
                                            {
                                                StatusCode = HttpStatusCode.Forbidden,
                                                Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new
                                                {
                                                    RESPCODE = 1,
                                                    RESPDESC = "Withdraw Transaction Failed"
                                                })))
                                            });
                                        }
                                        else if (targetTellerType.In(TellerType.AgentPointOfSale) && commissionSettlementGLAccountId != Guid.Empty && AcceptSavingWithdraw_Tariffs != null && AcceptSavingWithdraw_Tariffs.Any())
                                        {
                                            var withdrawalSettlements = from t in AcceptSavingWithdraw_Tariffs
                                                                        where t.CreditGLAccountId == commissionSettlementGLAccountId
                                                                        select new QueueDTO
                                                                        {
                                                                            AppDomainName = serviceHeader.ApplicationDomainName,
                                                                            TarrifDeviceId = alternateChannelLogDTO.ISO8583PrimaryAccountNumber,
                                                                            TarrifMaskedCardNumber = targetAlternateChannelDTO.MaskedCardNumber,
                                                                            TarrifCreditGLAccountId = t.CreditGLAccountId,
                                                                            TarrifAmount = t.Amount,
                                                                            TarrifDescription = t.Description,
                                                                            TarrifCommissionCustomerAccountId = commissionCustomerAccountDTO.Id,
                                                                        };

                                            _messageQueueService.Send(webApiConfigSection.WebApiSettingsItems.CitiusQueuePath, withdrawalSettlements.ToList(), MessageCategory.AgencyBankingSettlement, MessagePriority.Normal, 120);
                                        }

                                        alternateChannelLogDTO.WALLETAmount = AcceptSavingWithdraw_AMOUNT;
                                        alternateChannelLogDTO.Response = string.Format("{0}", javaScriptSerializer.Serialize(new { RESPCODE = 0 }));

                                        await _channelService.UpdateAlternateChannelLogAsync(alternateChannelLogDTO, serviceHeader);

                                        httpResponseMessage.Content = new StringContent(alternateChannelLogDTO.Response);

                                        #endregion

                                        break;
                                    case FUNCTIONCODE.GetAllBranches:

                                        #region GetAllBranches

                                        var GetAllBranches_BranchDTOS = await _channelService.FindBranchesByFilterInPageAsync(string.Empty, _pageIndex, _pageSize, serviceHeader);

                                        if (GetAllBranches_BranchDTOS != null && GetAllBranches_BranchDTOS.PageCollection != null)
                                        {
                                            var GetAllBranches_Payload = new
                                            {
                                                RESPCODE = 0,
                                                ITEMSCOUNT = GetAllBranches_BranchDTOS.ItemsCount,
                                                BRANCHES = from c in GetAllBranches_BranchDTOS.PageCollection
                                                           select new
                                                           {
                                                               ID = c.Id,
                                                               CODE = c.PaddedCode,
                                                               NAME = c.Description,
                                                           }
                                            };

                                            httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(GetAllBranches_Payload)));
                                        }
                                        else
                                        {
                                            httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new { RESPCODE = 1, RESPDESC = "No Match" })));
                                        };

                                        #endregion

                                        break;
                                    case FUNCTIONCODE.GetAccountInfo:

                                        #region GetAccountInfo

                                        string GetAccountInfo_ACCOUNTNO = string.Format("{0}", dynamic["ACCOUNTNO"]);

                                        var GetAccountInfo_CustomerAccountDTO = await _channelService.FindCustomerAccountByFullAccountNumberAsync(GetAccountInfo_ACCOUNTNO, true, true, true, false, serviceHeader);

                                        if (GetAccountInfo_CustomerAccountDTO != null)
                                        {
                                            var GetAccountInfo_Payload = new
                                            {
                                                RESPCODE = 0,
                                                INFO = new
                                                {
                                                    ID = GetAccountInfo_CustomerAccountDTO.Id,
                                                    ACCNO = GetAccountInfo_CustomerAccountDTO.FullAccountNumber,
                                                    ACCDESC = GetAccountInfo_CustomerAccountDTO.CustomerAccountTypeTargetProductDescription,
                                                    ACCTYPE = GetAccountInfo_CustomerAccountDTO.CustomerAccountTypeProductCode,
                                                    ACCTYPEDESC = GetAccountInfo_CustomerAccountDTO.CustomerAccountTypeProductCodeDescription,
                                                    BOOKBAL = GetAccountInfo_CustomerAccountDTO.BookBalance,
                                                    CURBAL = GetAccountInfo_CustomerAccountDTO.AvailableBalance,
                                                    REGMOBNO = GetAccountInfo_CustomerAccountDTO.CustomerAddressMobileLine
                                                }
                                            };

                                            httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(GetAccountInfo_Payload)));
                                        }
                                        else
                                        {
                                            httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new { RESPCODE = 1, RESPDESC = "No Match" })));
                                        };

                                        #endregion

                                        break;
                                    case FUNCTIONCODE.GetMiniStatement:

                                        #region GetMiniStatement

                                        string GetMiniStatement_ACCOUNTNO = string.Format("{0}", dynamic["ACCOUNTNO"]);

                                        int GetMiniStatement_TRANCNT = default(int);

                                        if (!int.TryParse(string.Format("{0}", dynamic["TRANCNT"]), out GetMiniStatement_TRANCNT))
                                        {
                                            throw new HttpResponseException(new HttpResponseMessage
                                            {
                                                StatusCode = HttpStatusCode.Forbidden,
                                                Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new
                                                {
                                                    RESPCODE = 1,
                                                    RESPDESC = "Could Not Parse TRANCNT"
                                                })))
                                            });
                                        }

                                        var GetMiniStatement_CustomerAccountDTO = await _channelService.FindCustomerAccountByFullAccountNumberAsync(GetMiniStatement_ACCOUNTNO, true, true, true, false, serviceHeader);

                                        if (GetMiniStatement_CustomerAccountDTO == null)
                                        {
                                            throw new HttpResponseException(new HttpResponseMessage
                                            {
                                                StatusCode = HttpStatusCode.Forbidden,
                                                Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new
                                                {
                                                    RESPCODE = 1,
                                                    RESPDESC = "No Match For ACCOUNTNO"
                                                })))
                                            });
                                        }

                                        var GetMiniStatement_LastXTransactions = await _channelService.FindLastXGeneralLedgerTransactionsByCustomerAccountIdAsync(GetMiniStatement_CustomerAccountDTO, GetMiniStatement_TRANCNT, false, serviceHeader);

                                        if (GetMiniStatement_LastXTransactions == null || GetMiniStatement_LastXTransactions.PageCollection == null)
                                        {
                                            throw new HttpResponseException(new HttpResponseMessage
                                            {
                                                StatusCode = HttpStatusCode.Forbidden,
                                                Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new
                                                {
                                                    RESPCODE = 1,
                                                    RESPDESC = "No Match For TRANCNT"
                                                })))
                                            });
                                        }

                                        var GetMiniStatement_Sequence = default(int);

                                        var GetMiniStatement_Payload = new
                                        {
                                            RESPCODE = 0,
                                            TRANSACTIONS = from c in GetMiniStatement_LastXTransactions.PageCollection
                                                           select new
                                                           {
                                                               TRANDATE = c.JournalCreatedDate.ToString("dd/MM/yyyy HH:mm:ss tt"),
                                                               VALUEDATE = c.JournalValueDate.HasValue ? c.JournalValueDate.Value.ToString("dd/MM/yyyy") : string.Empty,
                                                               NARRATION = c.JournalPrimaryDescription,
                                                               AMOUNT = c.Debit + c.Credit,
                                                               DRORCR = (c.Credit > 0m) ? "CR" : "DR",
                                                               BALANCE = c.RunningBalance,
                                                               SEQNO = ++GetMiniStatement_Sequence
                                                           }
                                        };

                                        var GetMiniStatement_MiniStatementTariffs = await _channelService.ComputeTariffsByAlternateChannelTypeAsync(targetAlternateChannelDTO.Type, (int)AlternateChannelKnownChargeType.MiniStatementCharges, 0m, targetCustomerAccountDTO, serviceHeader);

                                        if (GetMiniStatement_MiniStatementTariffs.Any())
                                        {
                                            await _channelService.AddTariffJournalsWithCustomerAccountAsync(null, targetAlternateChannelDTO.CustomerAccountBranchId, alternateChannelLogDTO.Id, targetAlternateChannelDTO.MaskedCardNumber, string.Format("{0} > {1}", GetMiniStatement_ACCOUNTNO, alternateChannelLogDTO.WALLETRetrievalReferenceNumber), 0x9999, (int)SystemTransactionCode.Citius, targetCustomerAccountDTO, targetCustomerAccountDTO, GetMiniStatement_MiniStatementTariffs, serviceHeader);
                                        }

                                        alternateChannelLogDTO.Response = string.Format("{0}", javaScriptSerializer.Serialize(GetMiniStatement_Payload));

                                        await _channelService.UpdateAlternateChannelLogAsync(alternateChannelLogDTO, serviceHeader);

                                        httpResponseMessage.Content = new StringContent(alternateChannelLogDTO.Response);

                                        #endregion

                                        break;
                                    case FUNCTIONCODE.CreateNewCustomer:

                                        #region CreateNewCustomer

                                        var CreateNewCustomer_CustomerDTO = new CustomerDTO
                                        {
                                            Type = (int)CustomerType.Individual,
                                            IndividualType = (int)IndividualType.Adult,
                                            IndividualFirstName = string.Format("{0}", dynamic["FIRSTNAME"]),
                                            IndividualLastName = string.Format("{0} {1}", dynamic["MIDDLENAME"], dynamic["LASTNAME"]),
                                            IndividualIdentityCardType = (int)IdentityCardType.NationalID,
                                            IndividualIdentityCardNumber = string.Format("{0}", dynamic["NATIONALID"]),
                                            AddressAddressLine1 = string.Format("{0}", dynamic["ADDRESS"]),
                                            AddressCity = string.Format("{0}", dynamic["CITY"]),
                                            AddressMobileLine = string.Format("{0}", dynamic["MOBNO"]),
                                            Remarks = string.Format("{0}", dynamic["TEMPCUSTID"]),
                                            StationId = Guid.NewGuid()/*to pass validation*/,
                                        };

                                        string CreateNewCustomer_TITLE = string.Format("{0}", dynamic["TITLE"]);

                                        switch (CreateNewCustomer_TITLE)
                                        {
                                            case "MR":
                                                CreateNewCustomer_CustomerDTO.IndividualSalutation = (int)Salutation.Mr;
                                                break;
                                            case "MRS":
                                                CreateNewCustomer_CustomerDTO.IndividualSalutation = (int)Salutation.Mrs;
                                                break;
                                            case "MISS":
                                                CreateNewCustomer_CustomerDTO.IndividualSalutation = (int)Salutation.Ms;
                                                break;
                                            default:
                                                break;
                                        }

                                        string[] formats = { "dd/MM/yyyy" };

                                        var birthDate = DateTime.Today;

                                        if (DateTime.TryParseExact(string.Format("{0}", dynamic["DOB"]), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out birthDate))
                                            CreateNewCustomer_CustomerDTO.IndividualBirthDate = birthDate;

                                        CreateNewCustomer_CustomerDTO.ValidateAll();

                                        if (CreateNewCustomer_CustomerDTO.HasErrors)
                                        {
                                            throw new HttpResponseException(new HttpResponseMessage
                                            {
                                                StatusCode = HttpStatusCode.Forbidden,
                                                Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new
                                                {
                                                    RESPCODE = 1,
                                                    RESPDESC = string.Format("{0}", string.Join(Environment.NewLine, CreateNewCustomer_CustomerDTO.ErrorMessages))
                                                })))
                                            });
                                        }

                                        CreateNewCustomer_CustomerDTO.StationId = null/*reset after validation*/;

                                        CreateNewCustomer_CustomerDTO = await _channelService.AddCustomerAsync(CreateNewCustomer_CustomerDTO, null, null, 0x9999, serviceHeader);

                                        if (CreateNewCustomer_CustomerDTO == null)
                                        {
                                            throw new HttpResponseException(new HttpResponseMessage
                                            {
                                                StatusCode = HttpStatusCode.Forbidden,
                                                Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new
                                                {
                                                    RESPCODE = 1,
                                                    RESPDESC = "Create New Customer Failed"
                                                })))
                                            });
                                        }

                                        httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new { RESPCODE = 0 })));

                                        #endregion

                                        break;
                                    case FUNCTIONCODE.ValidateCBSUser:

                                        #region ValidateCBSUser

                                        string ValidateCBSUser_Text = string.Format("{0}", dynamic["USERID"]);

                                        var ValidateCBSUser_EmployeeDTO = await _channelService.GetUserInfoAsync(ValidateCBSUser_Text, serviceHeader);

                                        if (ValidateCBSUser_EmployeeDTO != null)
                                        {
                                            var ValidateCBSUser_Payload = new
                                            {
                                                RESPCODE = 0,
                                                USERNAME = ValidateCBSUser_EmployeeDTO.ApplicationUserName,
                                                NAME = ValidateCBSUser_EmployeeDTO.CustomerFullName,
                                                BRANCH = ValidateCBSUser_EmployeeDTO.BranchDescription,
                                            };

                                            httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(ValidateCBSUser_Payload)));
                                        }
                                        else
                                        {
                                            httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new { RESPCODE = 1, RESPDESC = "No Match" })));
                                        };

                                        #endregion

                                        break;
                                    case FUNCTIONCODE.SendOTP:

                                        #region SendOTP

                                        string SendOTP_MEMBERID = string.Format("{0}", dynamic["MEMBERID"]);

                                        string SendOTP_MESSAGE = string.Format("{0}", dynamic["MESSAGE"]);

                                        var SendOTP_CustomerDTOs = await _channelService.FindCustomersByFilterInPageAsync(SendOTP_MEMBERID, (int)CustomerFilter.Reference1, _pageIndex, _pageSize, serviceHeader);

                                        if (SendOTP_CustomerDTOs != null && SendOTP_CustomerDTOs.PageCollection != null && SendOTP_CustomerDTOs.PageCollection.Count == 1)
                                        {
                                            var SendOTP_TextAlertDTOs = from c in SendOTP_CustomerDTOs.PageCollection
                                                                        where !string.IsNullOrWhiteSpace(c.AddressMobileLine) && Regex.IsMatch(c.AddressMobileLine.Trim(), @"^\+(?:[0-9]??){6,14}[0-9]$") && c.AddressMobileLine.Trim().Length >= 13
                                                                        select new TextAlertDTO
                                                                        {
                                                                            TextMessageOrigin = (int)MessageOrigin.Within,
                                                                            TextMessageRecipient = c.AddressMobileLine,
                                                                            TextMessageBody = SendOTP_MESSAGE,
                                                                            MessageCategory = (int)MessageCategory.SMSAlert,
                                                                            AppendSignature = false,
                                                                            TextMessagePriority = (int)QueuePriority.Highest,
                                                                        };

                                            if (SendOTP_TextAlertDTOs != null && await _channelService.AddTextAlertsAsync(new ObservableCollection<TextAlertDTO>(SendOTP_TextAlertDTOs), serviceHeader))
                                            {
                                                httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new { RESPCODE = 0 })));
                                            }
                                            else
                                            {
                                                httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new { RESPCODE = 1, RESPDESC = "Failed" })));
                                            };
                                        }
                                        else
                                        {
                                            httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new { RESPCODE = 1, RESPDESC = "No Match" })));
                                        };

                                        #endregion

                                        break;
                                    case FUNCTIONCODE.GetSchemes:

                                        #region GetSchemes

                                        int GetSchemes_ProductCode = default(int);

                                        if (!int.TryParse(string.Format("{0}", dynamic["ACCTYPE"]), out GetSchemes_ProductCode))
                                        {
                                            throw new HttpResponseException(new HttpResponseMessage
                                            {
                                                StatusCode = HttpStatusCode.Forbidden,
                                                Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new
                                                {
                                                    RESPCODE = 1,
                                                    RESPDESC = "Could Not Parse ACCTYPE"
                                                })))
                                            });
                                        }

                                        if (!GetSchemes_ProductCode.In(1, 2, 3))
                                        {
                                            throw new HttpResponseException(new HttpResponseMessage
                                            {
                                                StatusCode = HttpStatusCode.Forbidden,
                                                Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new
                                                {
                                                    RESPCODE = 1,
                                                    RESPDESC = "Invalid ACCTYPE"
                                                })))
                                            });
                                        }

                                        switch (GetSchemes_ProductCode)
                                        {
                                            case 1:

                                                var GetSchemes_SavingsProductDTOs = await _channelService.FindSavingsProductsAsync(serviceHeader);

                                                if (GetSchemes_SavingsProductDTOs != null && GetSchemes_SavingsProductDTOs.Any())
                                                {
                                                    var dynamicPayload = new
                                                    {
                                                        RESPCODE = 0,
                                                        SCHEMES = from c in GetSchemes_SavingsProductDTOs
                                                                  select new
                                                                  {
                                                                      ID = c.Id,
                                                                      SCHEMECD = c.PaddedCode,
                                                                      SCHEMEDESC = c.Description,
                                                                  }
                                                    };

                                                    httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(dynamicPayload)));
                                                }
                                                else
                                                {
                                                    httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new { RESPCODE = 1, RESPDESC = "No Match" })));
                                                };

                                                break;
                                            case 2:

                                                var GetSchemes_LoanProductDTOs = await _channelService.FindLoanProductsAsync(serviceHeader);

                                                if (GetSchemes_LoanProductDTOs != null && GetSchemes_LoanProductDTOs.Any())
                                                {
                                                    var dynamicPayload = new
                                                    {
                                                        RESPCODE = 0,
                                                        SCHEMES = from c in GetSchemes_LoanProductDTOs
                                                                  select new
                                                                  {
                                                                      ID = c.Id,
                                                                      SCHEMECD = c.PaddedCode,
                                                                      SCHEMEDESC = c.Description,
                                                                  }
                                                    };

                                                    httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(dynamicPayload)));
                                                }
                                                else
                                                {
                                                    httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new { RESPCODE = 1, RESPDESC = "No Match" })));
                                                };

                                                break;
                                            case 3:

                                                var GetSchemes_InvestmentProductDTOs = await _channelService.FindInvestmentProductsAsync(serviceHeader);

                                                if (GetSchemes_InvestmentProductDTOs != null && GetSchemes_InvestmentProductDTOs.Any())
                                                {
                                                    var dynamicPayload = new
                                                    {
                                                        RESPCODE = 0,
                                                        SCHEMES = from c in GetSchemes_InvestmentProductDTOs
                                                                  select new
                                                                  {
                                                                      ID = c.Id,
                                                                      SCHEMECD = c.PaddedCode,
                                                                      SCHEMEDESC = c.Description,
                                                                  }
                                                    };

                                                    httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(dynamicPayload)));
                                                }
                                                else
                                                {
                                                    httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new { RESPCODE = 1, RESPDESC = "No Match" })));
                                                };

                                                break;
                                            default:
                                                break;
                                        }

                                        #endregion

                                        break;
                                    default:
                                        httpResponseMessage.Content = new StringContent(string.Format("{0}", javaScriptSerializer.Serialize(new { RESPCODE = 1, RESPDESC = "FUNCTIONCD Not Implemented" })));
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

                        LoggerFactory.CreateLog().LogError("Citius-HttpResponseException-{0}-{1}->", ex, responseException.Response.StatusCode, responseContent);

                        httpResponseMessage = responseException.Response;
                    }
                    else
                    {
                        LoggerFactory.CreateLog().LogError("citius-Exception->", ex);

                        httpResponseMessage = Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message);
                    }
                }
            }

            return httpResponseMessage;
        }
    }
}

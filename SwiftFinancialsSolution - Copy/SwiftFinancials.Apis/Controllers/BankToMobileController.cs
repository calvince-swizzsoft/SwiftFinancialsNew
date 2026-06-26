using SwiftFinancials.Apis.Configuration;

namespace SwiftFinancials.Apis.Controllers
{
    public class BankToMobileController : ApiController
    {
        private readonly IMessageQueueService _messageQueueService;
        private readonly IChannelService _channelService;

        public BankToMobileController(
            IMessageQueueService messageQueueService,
            IChannelService channelService)
        {
            Guard.ArgumentNotNull(messageQueueService, "messageQueueService");
            Guard.ArgumentNotNull(channelService, "channelService");

            _messageQueueService = messageQueueService;
            _channelService = channelService;
        }

        // GET: api/banktomobile
        public HttpResponseMessage Get()
        {
            var assemblyAttributes = new AssemblyAttributes();

            var httpResponseMessage = Request.CreateResponse(HttpStatusCode.OK);

            httpResponseMessage.Content = new StringContent(string.Format("Company: {0}\nProduct: {1}\nCopyright: {2}\nTrademark: {3}\nVersion: {4}\nDescription: {5}\nConfiguration: {6}", assemblyAttributes.Company, assemblyAttributes.Product, assemblyAttributes.Copyright, assemblyAttributes.Trademark, assemblyAttributes.Version, assemblyAttributes.Description, assemblyAttributes.Configuration));

            return httpResponseMessage;
        }

        // POST api/banktomobile
        public async Task<HttpResponseMessage> Post(BankToMobileViewModel data)
        {
            var httpResponseMessage = Request.CreateResponse(HttpStatusCode.OK);
            httpResponseMessage.Content = new StringContent("REQ_RECV");

            if (data == null)
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
                                var request = data.FromEncryptedBankToMobileRequest(webApiSettingsElement.BankToMobileApiUsername, webApiSettingsElement.BankToMobileApiPassword, webApiSettingsElement.BankToMobileBankId);

                                request.AppDomainName = webApiSettingsElement.UniqueId;

                                var serviceHeader = new ServiceHeader { ApplicationDomainName = request.AppDomainName };

                                if (request.TransactionType.In((int)BankToMobileTransactionRequest.LoanLimitRequest, (int)BankToMobileTransactionRequest.AlternateChannelsRequest, (int)BankToMobileTransactionRequest.BalanceEnquiry, (int)BankToMobileTransactionRequest.SavingsAccountsRequest, (int)BankToMobileTransactionRequest.LoanAccountsRequest, (int)BankToMobileTransactionRequest.InvestmentAccountsRequest))
                                {
                                    var alternateChannelLogDTO = new AlternateChannelLogDTO
                                    {
                                        AlternateChannelType = (int)AlternateChannelType.PesaPepe,
                                        WALLETMessage = string.Format("{0}", request),
                                    };

                                    alternateChannelLogDTO.WALLETMessageTypeIdentification = string.Format("{0}", request.TransactionType);
                                    alternateChannelLogDTO.WALLETPrimaryAccountNumber = request.AccountNumber;
                                    alternateChannelLogDTO.WALLETSystemTraceAuditNumber = request.TransactionCode;
                                    alternateChannelLogDTO.WALLETRetrievalReferenceNumber = request.UniqueTransactionID;
                                    alternateChannelLogDTO.WALLETAmount = request.TransactionAmount;

                                    var existingLogs = await _channelService.MatchWALLETAlternateChannelLogsAsync(alternateChannelLogDTO, true, 1, serviceHeader);

                                    if (existingLogs != null && existingLogs.Any())
                                    {
                                        request.StatusCode = string.Format("{0}", (int)BankToMobileTransactionResponse.DuplicateTransmissionDetected);
                                        request.StatusDescription = string.Format("{0}", EnumHelper.GetDescription(BankToMobileTransactionResponse.DuplicateTransmissionDetected));
                                        request.Narration = "duplicate transmission";
                                    }
                                    else
                                    {
                                        alternateChannelLogDTO = await _channelService.AddAlternateChannelLogAsync(alternateChannelLogDTO, serviceHeader);

                                        var alternateChannels = await _channelService.FindAlternateChannelsByCardNumberAndCardTypeAsync(alternateChannelLogDTO.WALLETPrimaryAccountNumber, (int)AlternateChannelType.PesaPepe, false, serviceHeader);

                                        if (alternateChannels == null || !alternateChannels.Any() || alternateChannels.Count != 1)
                                        {
                                            request.StatusCode = string.Format("{0}", (int)BankToMobileTransactionResponse.Failed);
                                            request.StatusDescription = string.Format("{0}", EnumHelper.GetDescription(BankToMobileTransactionResponse.Failed));
                                            request.Narration = "alternate channel does not exist, kindly contact your SACCO";
                                        }
                                        else
                                        {
                                            var targetAlternateChannel = alternateChannels.First();

                                            if (targetAlternateChannel.IsLocked || targetAlternateChannel.RecordStatus != (int)RecordStatus.Approved || targetAlternateChannel.CustomerAccountStatus != (int)CustomerAccountStatus.Normal || targetAlternateChannel.CustomerAccountRecordStatus != (int)RecordStatus.Approved)
                                            {
                                                request.StatusCode = string.Format("{0}", (int)BankToMobileTransactionResponse.Declined);
                                                request.StatusDescription = string.Format("{0}", EnumHelper.GetDescription(BankToMobileTransactionResponse.Declined));
                                                request.Narration = "alternate channel and/or account status invalid, kindly contact your SACCO";
                                            }
                                            else
                                            {
                                                switch ((BankToMobileTransactionRequest)request.TransactionType)
                                                {
                                                    case BankToMobileTransactionRequest.LoanLimitRequest:

                                                        #region LoanLimitRequest

                                                        LoanProductDTO targetLimitLoanProduct = null;

                                                        var limitLoanProducts = await _channelService.FindLoanProductsByCodeAsync(request.LoanProductCode, serviceHeader);
                                                        if (limitLoanProducts != null && limitLoanProducts.Count == 1)
                                                            targetLimitLoanProduct = limitLoanProducts[0];

                                                        if (targetLimitLoanProduct == null)
                                                        {
                                                            request.StatusCode = string.Format("{0}", (int)BankToMobileTransactionResponse.Failed);
                                                            request.StatusDescription = string.Format("{0}", EnumHelper.GetDescription(BankToMobileTransactionResponse.Failed));
                                                            request.Narration = "invalid loan product, kindly contact your SACCO";
                                                        }
                                                        else
                                                        {
                                                            CustomerAccountDTO targetLimitLoanCustomerAccount = null;

                                                            var existingLimitLoanAccounts = await _channelService.FindCustomerAccountsByCustomerIdAndCustomerAccountTypeTargetProductIdAsync(targetAlternateChannel.CustomerAccountCustomerId, targetLimitLoanProduct.Id, true, true, true, false, serviceHeader);

                                                            if (existingLimitLoanAccounts != null && existingLimitLoanAccounts.Count == 1)
                                                                targetLimitLoanCustomerAccount = existingLimitLoanAccounts[0];
                                                            else
                                                            {
                                                                var customerAccountDTO = new CustomerAccountDTO
                                                                {
                                                                    BranchId = targetAlternateChannel.CustomerAccountBranchId,
                                                                    CustomerId = targetAlternateChannel.CustomerAccountCustomerId,
                                                                    CustomerAccountTypeProductCode = (int)ProductCode.Loan,
                                                                    CustomerAccountTypeTargetProductId = targetLimitLoanProduct.Id,
                                                                    CustomerAccountTypeTargetProductCode = targetLimitLoanProduct.Code,
                                                                    Status = (int)CustomerAccountStatus.Normal,
                                                                    RecordStatus = (int)RecordStatus.Approved,
                                                                };

                                                                targetLimitLoanCustomerAccount = await _channelService.AddCustomerAccountAsync(customerAccountDTO, serviceHeader);
                                                            }

                                                            if (targetLimitLoanCustomerAccount == null)
                                                            {
                                                                request.StatusCode = string.Format("{0}", (int)BankToMobileTransactionResponse.Declined);
                                                                request.StatusDescription = string.Format("{0}", EnumHelper.GetDescription(BankToMobileTransactionResponse.Declined));
                                                                request.Narration = "loan account determination failed, kindly contact your SACCO";
                                                            }
                                                            else
                                                            {
                                                                request.StatusCode = string.Format("{0}", (int)BankToMobileTransactionResponse.Success);
                                                                request.StatusDescription = string.Format("{0}", EnumHelper.GetDescription(BankToMobileTransactionResponse.Success));
                                                                request.LoanLimit = targetLimitLoanCustomerAccount.ScoredLoanLimit;
                                                                request.Narration = targetLimitLoanCustomerAccount.ScoredLoanLimitRemarks;
                                                                request.BookBalance = targetLimitLoanCustomerAccount.BookBalance;
                                                                request.PrincipalBalance = targetLimitLoanCustomerAccount.PrincipalBalance;
                                                                request.InterestBalance = targetLimitLoanCustomerAccount.InterestBalance;
                                                            }
                                                        }

                                                        #endregion

                                                        break;
                                                    case BankToMobileTransactionRequest.AlternateChannelsRequest:

                                                        #region AlternateChannelsRequest

                                                        var alternateChannelsRequestCustomerId = Guid.Empty;

                                                        if (Guid.TryParse(request.UniqueCustomerID, out alternateChannelsRequestCustomerId))
                                                        {
                                                            var customerAlternateChannels = await _channelService.FindAlternateChannelsByCustomerIdAsync(alternateChannelsRequestCustomerId, false, serviceHeader) ?? new ObservableCollection<AlternateChannelDTO>(new List<AlternateChannelDTO> { });

                                                            var result = from c in customerAlternateChannels
                                                                         where !c.IsLocked
                                                                         select new
                                                                         {
                                                                             Id = c.Id,
                                                                             Type = c.Type,
                                                                             TypeDescription = c.TypeDescription,
                                                                             MaskedCardNumber = c.MaskedCardNumber,
                                                                         };

                                                            var javaScriptSerializer = new JavaScriptSerializer();

                                                            request.StatusCode = string.Format("{0}", (int)BankToMobileTransactionResponse.Success);
                                                            request.StatusDescription = string.Format("{0}", EnumHelper.GetDescription(BankToMobileTransactionResponse.Success));
                                                            request.Narration = javaScriptSerializer.Serialize(result);
                                                        }
                                                        else
                                                        {
                                                            request.StatusCode = string.Format("{0}", (int)BankToMobileTransactionResponse.Declined);
                                                            request.StatusDescription = string.Format("{0}", EnumHelper.GetDescription(BankToMobileTransactionResponse.Declined));
                                                            request.Narration = "invalid customer identifier";
                                                        }

                                                        #endregion

                                                        break;
                                                    case BankToMobileTransactionRequest.BalanceEnquiry:

                                                        #region BalanceEnquiry

                                                        var balanceEnquiryCustomerAccountId = Guid.Empty;

                                                        if (Guid.TryParse(request.UniqueCustomerAccountId, out balanceEnquiryCustomerAccountId))
                                                        {
                                                            var balanceEnquiryCustomerAccount = await _channelService.FindCustomerAccountAsync(balanceEnquiryCustomerAccountId, true, true, true, true, serviceHeader);

                                                            request.StatusCode = string.Format("{0}", (int)BankToMobileTransactionResponse.Success);
                                                            request.StatusDescription = string.Format("{0}", EnumHelper.GetDescription(BankToMobileTransactionResponse.Success));
                                                            request.AvailableBalance = balanceEnquiryCustomerAccount.AvailableBalance;
                                                            request.BookBalance = balanceEnquiryCustomerAccount.BookBalance;

                                                            if (balanceEnquiryCustomerAccount.CustomerAccountTypeProductCode == (int)ProductCode.Loan && balanceEnquiryCustomerAccount.CarryForwardsBalance * -1 > 0m)
                                                                request.BookBalance += balanceEnquiryCustomerAccount.CarryForwardsBalance;

                                                            var targetCustomerAccount = (balanceEnquiryCustomerAccountId == targetAlternateChannel.CustomerAccountId)
                                                                ? balanceEnquiryCustomerAccount
                                                                : await _channelService.FindCustomerAccountAsync(targetAlternateChannel.CustomerAccountId, false, false, false, false, serviceHeader);

                                                            var balanceEnquiryTariffs = await _channelService.ComputeTariffsByAlternateChannelTypeAsync(targetAlternateChannel.Type, (int)AlternateChannelKnownChargeType.BalanceInquiryCharges, 0m, targetCustomerAccount, serviceHeader);

                                                            if (balanceEnquiryTariffs.Any())
                                                            {
                                                                await _channelService.AddTariffJournalsWithCustomerAccountAsync(null, targetAlternateChannel.CustomerAccountBranchId, alternateChannelLogDTO.Id, targetAlternateChannel.MaskedCardNumber, request.TransactionCode, 0x9999, (int)SystemTransactionCode.PesaPepe, targetCustomerAccount, targetCustomerAccount, balanceEnquiryTariffs, serviceHeader);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            request.StatusCode = string.Format("{0}", (int)BankToMobileTransactionResponse.Declined);
                                                            request.StatusDescription = string.Format("{0}", EnumHelper.GetDescription(BankToMobileTransactionResponse.Declined));
                                                            request.Narration = "invalid customer account identifier";
                                                        }

                                                        #endregion

                                                        break;
                                                    case BankToMobileTransactionRequest.SavingsAccountsRequest:

                                                        #region SavingsAccountsRequest

                                                        var savingsAccountsRequestCustomerId = Guid.Empty;

                                                        if (Guid.TryParse(request.UniqueCustomerID, out savingsAccountsRequestCustomerId))
                                                        {
                                                            var customerSavingsAccounts = await _channelService.FindCustomerAccountsByCustomerIdAndProductCodesAsync(savingsAccountsRequestCustomerId, new int[] { (int)ProductCode.Savings }, false, true, false, false, serviceHeader) ?? new ObservableCollection<CustomerAccountDTO>(new List<CustomerAccountDTO> { });

                                                            var result = from c in customerSavingsAccounts
                                                                         select new
                                                                         {
                                                                             Id = c.Id,
                                                                             TargetProductCode = c.CustomerAccountTypeTargetProductCode,
                                                                             TargetProductDescription = string.Format("{0}", c.CustomerAccountTypeTargetProductDescription).Trim(),
                                                                             FullAccountNumber = c.FullAccountNumber,
                                                                         };

                                                            var javaScriptSerializer = new JavaScriptSerializer();

                                                            request.StatusCode = string.Format("{0}", (int)BankToMobileTransactionResponse.Success);
                                                            request.StatusDescription = string.Format("{0}", EnumHelper.GetDescription(BankToMobileTransactionResponse.Success));
                                                            request.Narration = javaScriptSerializer.Serialize(result);
                                                        }
                                                        else
                                                        {
                                                            request.StatusCode = string.Format("{0}", (int)BankToMobileTransactionResponse.Declined);
                                                            request.StatusDescription = string.Format("{0}", EnumHelper.GetDescription(BankToMobileTransactionResponse.Declined));
                                                            request.Narration = "invalid customer identifier";
                                                        }

                                                        #endregion

                                                        break;
                                                    case BankToMobileTransactionRequest.LoanAccountsRequest:

                                                        #region LoanAccountsRequest

                                                        var loanAccountsRequestCustomerId = Guid.Empty;

                                                        if (Guid.TryParse(request.UniqueCustomerID, out loanAccountsRequestCustomerId))
                                                        {
                                                            var customerLoanAccounts = await _channelService.FindCustomerAccountsByCustomerIdAndProductCodesAsync(loanAccountsRequestCustomerId, new int[] { (int)ProductCode.Loan }, true, true, true, false, serviceHeader) ?? new ObservableCollection<CustomerAccountDTO>(new List<CustomerAccountDTO> { });

                                                            var result = from c in customerLoanAccounts
                                                                         where (c.BookBalance * -1 > 0m)
                                                                         select new
                                                                         {
                                                                             Id = c.Id,
                                                                             TargetProductCode = c.CustomerAccountTypeTargetProductCode,
                                                                             TargetProductDescription = string.Format("{0}", c.CustomerAccountTypeTargetProductDescription).Trim(),
                                                                             FullAccountNumber = c.FullAccountNumber,
                                                                         };

                                                            var javaScriptSerializer = new JavaScriptSerializer();

                                                            request.StatusCode = string.Format("{0}", (int)BankToMobileTransactionResponse.Success);
                                                            request.StatusDescription = string.Format("{0}", EnumHelper.GetDescription(BankToMobileTransactionResponse.Success));
                                                            request.Narration = javaScriptSerializer.Serialize(result);
                                                        }
                                                        else
                                                        {
                                                            request.StatusCode = string.Format("{0}", (int)BankToMobileTransactionResponse.Declined);
                                                            request.StatusDescription = string.Format("{0}", EnumHelper.GetDescription(BankToMobileTransactionResponse.Declined));
                                                            request.Narration = "invalid customer identifier";
                                                        }

                                                        #endregion

                                                        break;
                                                    case BankToMobileTransactionRequest.InvestmentAccountsRequest:

                                                        #region InvestmentAccountsRequest

                                                        var investmentAccountsRequestCustomerId = Guid.Empty;

                                                        if (Guid.TryParse(request.UniqueCustomerID, out investmentAccountsRequestCustomerId))
                                                        {
                                                            var customerInvestmentAccounts = await _channelService.FindCustomerAccountsByCustomerIdAndProductCodesAsync(investmentAccountsRequestCustomerId, new int[] { (int)ProductCode.Investment }, false, true, false, false, serviceHeader) ?? new ObservableCollection<CustomerAccountDTO>(new List<CustomerAccountDTO> { });

                                                            var result = from c in customerInvestmentAccounts
                                                                         select new
                                                                         {
                                                                             Id = c.Id,
                                                                             TargetProductCode = c.CustomerAccountTypeTargetProductCode,
                                                                             TargetProductDescription = string.Format("{0}", c.CustomerAccountTypeTargetProductDescription).Trim(),
                                                                             FullAccountNumber = c.FullAccountNumber,
                                                                         };

                                                            var javaScriptSerializer = new JavaScriptSerializer();

                                                            request.StatusCode = string.Format("{0}", (int)BankToMobileTransactionResponse.Success);
                                                            request.StatusDescription = string.Format("{0}", EnumHelper.GetDescription(BankToMobileTransactionResponse.Success));
                                                            request.Narration = javaScriptSerializer.Serialize(result);
                                                        }
                                                        else
                                                        {
                                                            request.StatusCode = string.Format("{0}", (int)BankToMobileTransactionResponse.Declined);
                                                            request.StatusDescription = string.Format("{0}", EnumHelper.GetDescription(BankToMobileTransactionResponse.Declined));
                                                            request.Narration = "invalid customer identifier";
                                                        }

                                                        #endregion

                                                        break;
                                                    default:
                                                        break;
                                                }
                                            }
                                        }
                                    }

                                    var response = request.ToEncryptedBankToMobileRequest(webApiSettingsElement.BankToMobileApiUsername, webApiSettingsElement.BankToMobileApiPassword, webApiSettingsElement.BankToMobileBankId);

                                    if (alternateChannelLogDTO != null)
                                    {
                                        alternateChannelLogDTO.Response = response;

                                        await _channelService.UpdateAlternateChannelLogAsync(alternateChannelLogDTO, serviceHeader);
                                    }

                                    httpResponseMessage.Content = new StringContent(response);
                                }
                                else
                                {
                                    _messageQueueService.Send(webApiConfigSection.WebApiSettingsItems.BankToMobileQueuePath, string.Format("{0}", request), MessageCategory.BankToMobile, MessagePriority.VeryHigh, 1440);

                                    httpResponseMessage.Content = new StringContent("REQ_ACK");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLog().LogError("banktomobile-in->", ex);

                    httpResponseMessage = Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message);
                }
            }

            return httpResponseMessage;
        }
    }
}

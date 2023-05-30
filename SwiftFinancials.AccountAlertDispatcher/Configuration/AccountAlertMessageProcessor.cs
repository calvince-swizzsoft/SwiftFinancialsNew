using Application.MainBoundedContext.DTO.AdministrationModule;
using Application.MainBoundedContext.DTO.MessagingModule;
using Infrastructure.Crosscutting.Framework.Extensions;
using Infrastructure.Crosscutting.Framework.Logging;
using Infrastructure.Crosscutting.Framework.Models;
using Infrastructure.Crosscutting.Framework.Utils;
using RazorEngine;
using RazorEngine.Templating;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;
using SwiftFinancials.Presentation.Infrastructure.Services;
using Application.MainBoundedContext.DTO.RegistryModule;

namespace SwiftFinancials.AccountAlertDispatcher.Configuration
{
    public class AccountAlertMessageProcessor : MessageProcessor<List<QueueDTO>>
    {
        private readonly IChannelService _channelService;
        private readonly ILogger _logger;
        private readonly AccountAlertDispatcherConfigSection _accountAlertDispatcherConfigSection;

        public AccountAlertMessageProcessor(IChannelService channelService, ILogger logger, AccountAlertDispatcherConfigSection accountAlertDispatcherConfigSection)
            : base(accountAlertDispatcherConfigSection.AccountAlertDispatcherSettingsItems.QueuePath, accountAlertDispatcherConfigSection.AccountAlertDispatcherSettingsItems.QueueReceivers)
        {
            _channelService = channelService;
            _logger = logger;
            _accountAlertDispatcherConfigSection = accountAlertDispatcherConfigSection;
        }

        protected override void LogError(Exception exception)
        {
            _logger.LogError("{0}->AccountAlertMessageProcessor...", exception, _accountAlertDispatcherConfigSection.AccountAlertDispatcherSettingsItems.QueuePath);
        }

        protected override async Task Process(List<QueueDTO> @object, int appSpecific)
        {
            foreach (var settingsItem in _accountAlertDispatcherConfigSection.AccountAlertDispatcherSettingsItems)
            {
                var accountAlertDispatcherSettingsElement = (AccountAlertDispatcherSettingsElement)settingsItem;

                if (accountAlertDispatcherSettingsElement != null && accountAlertDispatcherSettingsElement.Enabled == 1)
                {
                    var messageCategory = (MessageCategory)appSpecific;

                    if (messageCategory.In(MessageCategory.AccountAlert))
                    {
                        foreach (var queueDTO in @object)
                        {
                            if (!accountAlertDispatcherSettingsElement.UniqueId.Equals(queueDTO.AppDomainName, StringComparison.OrdinalIgnoreCase))
                                continue;

                            var serviceHeader = new ServiceHeader { ApplicationDomainName = queueDTO.AppDomainName };

                            var userDTO = new UserDTO();

                            var customer = new CustomerDTO();

                            switch ((AccountAlertTrigger)queueDTO.AccountAlertTrigger)
                            {
                                case AccountAlertTrigger.MembershipAccountRegistration:

                                    #region Do we need to send alerts?

                                    userDTO = await _channelService.FindMembershipAsync(queueDTO.RecordId.ToString(), serviceHeader);

                                    if (userDTO != null)
                                    {
                                        var companyDTO = await _channelService.FindCompanyAsync((Guid)userDTO.CompanyId, serviceHeader);

                                        #region Email Alert

                                        var emailAlertTemplatePath = Path.Combine(accountAlertDispatcherSettingsElement.TemplatesPath, string.Format("{0}_EmailTemplate.cshtml", SystemTransactionCode.MembershipAccountRegistration));

                                        if (File.Exists(emailAlertTemplatePath))
                                        {
                                            var template = File.ReadAllText(emailAlertTemplatePath);

                                            dynamic expando = new ExpandoObject();

                                            var model = expando as IDictionary<string, object>;

                                            model.Add("FirstName", userDTO.FirstName);
                                            model.Add("Username", userDTO.Email);
                                            model.Add("UserPassword", queueDTO.UserPassword);
                                            model.Add("CallbackUrl", queueDTO.CallbackUrl);
                                            model.Add("CompanyDescription", companyDTO.Description);

                                            var emailResult = Engine.Razor.RunCompile(template, string.Format("{0}_EmailTemplate", "AccountRegistration"), null, model);

                                            EmailAlertDTO emailAlertDTO = new EmailAlertDTO
                                            {
                                                CompanyId = userDTO.CompanyId.Value,
                                                MailMessageFrom = companyDTO.AddressEmail,
                                                MailMessageTo = userDTO.Email,
                                                MailMessageSubject = "Account Registration",
                                                MailMessageBody = emailResult,
                                                MailMessageIsBodyHtml = true,
                                                MailMessageDLRStatus = (int)DLRStatus.Pending,
                                                MailMessageOrigin = (int)MessageOrigin.Within,
                                                MailMessageSecurityCritical = true
                                            };

                                            await _channelService.AddEmailAlertAsync(emailAlertDTO, serviceHeader);
                                        }

                                        #endregion
                                    }

                                    #endregion

                                    break;

                                case AccountAlertTrigger.MembershipAccountVerification:

                                    #region Do we need to send alerts?

                                    userDTO = await _channelService.FindMembershipAsync(queueDTO.RecordId.ToString(), serviceHeader);

                                    if (userDTO != null)
                                    {
                                        var companyDTO = await _channelService.FindCompanyAsync((Guid)userDTO.CompanyId, serviceHeader);

                                        switch ((TwoFactorProviders)queueDTO.Provider)
                                        {
                                            case TwoFactorProviders.PhoneCode:

                                                #region Text Alert

                                                var textAlertTemplatePath = Path.Combine(accountAlertDispatcherSettingsElement.TemplatesPath, string.Format("{0}_TextTemplate.cshtml", SystemTransactionCode.MembershipAccountVerification));

                                                if (File.Exists(textAlertTemplatePath))
                                                {
                                                    var template = File.ReadAllText(textAlertTemplatePath);

                                                    dynamic expando = new ExpandoObject();

                                                    var model = expando as IDictionary<string, object>;

                                                    model.Add("FirstName", userDTO.FirstName);
                                                    model.Add("Token", queueDTO.Token);
                                                    model.Add("CompanyDescription", companyDTO.Description);

                                                    var textResult = Engine.Razor.RunCompile(template, string.Format("{0}_TextTemplate", SystemTransactionCode.MembershipAccountVerification), null, model);

                                                    TextAlertDTO textAlertDTO = new TextAlertDTO
                                                    {
                                                        CompanyId = userDTO.CompanyId.Value,
                                                        TextMessageBody = textResult,
                                                        TextMessageRecipient = userDTO.PhoneNumber,
                                                        TextMessageDLRStatus = (int)DLRStatus.Pending,
                                                        TextMessageOrigin = (int)MessageOrigin.Within,
                                                        TextMessagePriority = (int)QueuePriority.High,
                                                        TextMessageSecurityCritical = true
                                                    };

                                                //    await _channelService.AddTextAlertAsync(textAlertDTO, serviceHeader);
                                                }

                                                #endregion

                                                break;

                                            case TwoFactorProviders.EmailCode:

                                                #region Email Alert

                                                var emailAlertTemplatePath = Path.Combine(accountAlertDispatcherSettingsElement.TemplatesPath, string.Format("{0}_EmailTemplate.cshtml", SystemTransactionCode.MembershipAccountVerification));

                                                if (File.Exists(emailAlertTemplatePath))
                                                {
                                                    var template = File.ReadAllText(emailAlertTemplatePath);

                                                    dynamic expando = new ExpandoObject();

                                                    var model = expando as IDictionary<string, object>;

                                                    model.Add("FirstName", userDTO.FirstName);
                                                    model.Add("Token", queueDTO.Token);
                                                    model.Add("CompanyDescription", companyDTO.Description);

                                                    var emailResult = Engine.Razor.RunCompile(template, string.Format("{0}_EmailTemplate", SystemTransactionCode.MembershipAccountVerification), null, model);

                                                    EmailAlertDTO emailAlertDTO = new EmailAlertDTO
                                                    {
                                                        CompanyId = userDTO.CompanyId.Value,
                                                        MailMessageFrom = companyDTO.AddressEmail,
                                                        MailMessageTo = userDTO.Email,
                                                        MailMessageSubject = "Account Verification",
                                                        MailMessageBody = emailResult,
                                                        MailMessageIsBodyHtml = true,
                                                        MailMessageDLRStatus = (int)DLRStatus.Pending,
                                                        MailMessageOrigin = (int)MessageOrigin.Within,
                                                        MailMessageSecurityCritical = true
                                                    };

                                                    await _channelService.AddEmailAlertAsync(emailAlertDTO, serviceHeader);
                                                }

                                                #endregion

                                                break;
                                        }
                                    }

                                    #endregion

                                    break;

                                case AccountAlertTrigger.MembershipResetPassword:

                                    #region Do we need to send alerts?

                                    userDTO = await _channelService.FindMembershipAsync(queueDTO.RecordId.ToString(), serviceHeader);

                                    if (userDTO != null)
                                    {
                                        var companyDTO = await _channelService.FindCompanyAsync((Guid)userDTO.CompanyId, serviceHeader);

                                        #region Email Alert

                                        var emailAlertTemplatePath = Path.Combine(accountAlertDispatcherSettingsElement.TemplatesPath, string.Format("{0}_EmailTemplate.cshtml", SystemTransactionCode.MembershipResetPassword));

                                        if (File.Exists(emailAlertTemplatePath))
                                        {
                                            var template = File.ReadAllText(emailAlertTemplatePath);

                                            dynamic expando = new ExpandoObject();

                                            var model = expando as IDictionary<string, object>;

                                            model.Add("FirstName", userDTO.FirstName);
                                            model.Add("CallbackUrl", queueDTO.CallbackUrl);
                                            model.Add("CompanyDescription", companyDTO.Description);

                                            var emailResult = Engine.Razor.RunCompile(template, string.Format("{0}_EmailTemplate", SystemTransactionCode.MembershipResetPassword), null, model);

                                            EmailAlertDTO emailAlertDTO = new EmailAlertDTO
                                            {
                                                CompanyId = userDTO.CompanyId.Value,
                                                MailMessageFrom = companyDTO.AddressEmail,
                                                MailMessageTo = userDTO.Email,
                                                MailMessageSubject = "Reset Password",
                                                MailMessageBody = emailResult,
                                                MailMessageIsBodyHtml = true,
                                                MailMessageDLRStatus = (int)DLRStatus.Pending,
                                                MailMessageOrigin = (int)MessageOrigin.Within,
                                                MailMessageSecurityCritical = true
                                            };

                                            await _channelService.AddEmailAlertAsync(emailAlertDTO, serviceHeader);
                                        }

                                        #endregion
                                    }

                                    #endregion

                                    break;;

                                default:
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }
}
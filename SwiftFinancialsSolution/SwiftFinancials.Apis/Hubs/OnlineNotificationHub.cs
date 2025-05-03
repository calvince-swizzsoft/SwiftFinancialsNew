using Application.MainBoundedContext.DTO.HumanResourcesModule;
using Application.MainBoundedContext.DTO.MessagingModule;
using Infrastructure.Crosscutting.Framework.Logging;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using VanguardFinancials.Presentation.Infrastructure.Services;

namespace SwiftFinancials.Apis.Hubs
{
    [HubName("vfinONHub")]
    public class OnlineNotificationHub : Hub
    {
        private static readonly List<EmployeeDTO> _usersList = new List<EmployeeDTO>();
        private static readonly List<InstantMessageDTO> _messagesList = new List<InstantMessageDTO>();

        private readonly IChannelService _channelService;

        public OnlineNotificationHub()
            : base()
        {
            _channelService = DependencyResolver.Current.GetService<IChannelService>();
        }

        public async Task Connect(string currentAppDomain, string applicationUserName, string environmentMACAddress, string environmentMotherboardSerialNumber, string environmentProcessorId, string environmentUserName, string environmentMachineName, string environmentDomainName, string environmentOSVersion)
        {
            try
            {
                var serviceHeader = new ServiceHeader { ApplicationDomainName = currentAppDomain };

                var userInfo = await _channelService.GetUserInfoAsync(applicationUserName, serviceHeader);

                if (userInfo != null)
                {
                    userInfo.SignalRConnectionId = Context.ConnectionId;
                    userInfo.EnvironmentMACAddress = environmentMACAddress;
                    userInfo.EnvironmentMotherboardSerialNumber = environmentMotherboardSerialNumber;
                    userInfo.EnvironmentProcessorId = environmentProcessorId;
                    userInfo.EnvironmentUserName = environmentUserName;
                    userInfo.EnvironmentMachineName = environmentMachineName;
                    userInfo.EnvironmentDomainName = environmentDomainName;
                    userInfo.EnvironmentOSVersion = environmentOSVersion;

                    _usersList.Add(userInfo);

                    var totalSessions = _usersList.Count(x => x.Id == userInfo.Id);

                    if (totalSessions > 1)
                    {
                        var sb = new StringBuilder();

                        foreach (var item in _usersList)
                        {
                            if (item.Id == userInfo.Id)
                            {
                                sb.AppendLine(string.Format("{0}<-->{1}/{2}/{3}", applicationUserName, item.EnvironmentUserName, item.EnvironmentMachineName, item.EnvironmentDomainName));
                            }
                        }

                        Clients.Client(userInfo.SignalRConnectionId).onNotifyMessage(Context.ConnectionId, userInfo.ApplicationUserName, string.Format("WARNING: You have {0} active sessions!{1}{2}", totalSessions, Environment.NewLine, sb));
                    }

                    LoggerFactory.CreateLog().LogInfo("OnlineNotificationHub.Connect...{0}->{1}", userInfo.ApplicationUserName, userInfo.CustomerFullName);
                }
            }
            catch (Exception ex)
            {
                LoggerFactory.CreateLog().LogError("OnlineNotificationHub.Connect...", ex);
            }
        }

        public async Task NotifyUsersInPermissionType(string currentAppDomain, string fromUserName, string systemPermissionType, string message)
        {
            try
            {
                var serviceHeader = new ServiceHeader { ApplicationDomainName = currentAppDomain };

                if (_usersList.Count != 0)
                {
                    var userInfo = await _channelService.GetUserInfoAsync(fromUserName, serviceHeader);

                    if (userInfo != null)
                    {
                        _messagesList.Add(new InstantMessageDTO
                        {
                            ConnectionId = Context.ConnectionId,
                            Sender = userInfo.ApplicationUserName,
                            FullName = userInfo.CustomerFullName,
                            Message = message,
                            CreatedDate = DateTime.Now
                        });

                        var targetRoles = await _channelService.GetRolesForSystemPermissionTypeAsync(int.Parse(systemPermissionType), serviceHeader);

                        if (targetRoles != null && targetRoles.Any())
                        {
                            var targetUsers = new List<string>();

                            foreach (var role in targetRoles)
                            {
                                var usersInRole = await _channelService.GetUsersInRoleAsync(role, serviceHeader);

                                if (usersInRole != null && usersInRole.Any())
                                {
                                    targetUsers.AddRange(usersInRole);
                                }
                            }

                            if (targetUsers.Any())
                            {
                                var distinctTargets = targetUsers.Distinct();

                                if (distinctTargets != null && distinctTargets.Any())
                                {
                                    foreach (var userName in distinctTargets)
                                    {
                                        if (!userName.Equals(fromUserName, StringComparison.OrdinalIgnoreCase))
                                        {
                                            var employee = _usersList.FirstOrDefault(x => x.ApplicationUserName.Equals(userName, StringComparison.OrdinalIgnoreCase));

                                            if (employee != null && employee.OnlineNotificationsEnabled)
                                            {
                                                Clients.Client(employee.SignalRConnectionId).onNotifyMessage(Context.ConnectionId, userInfo.ApplicationUserName, message);

                                                LoggerFactory.CreateLog().LogInfo("OnlineNotificationHub.NotifyUsersInPermissionType...{0}From:{1}->{2}{0}To:{3}->{4}{0}Message:{5}", Environment.NewLine, userInfo.ApplicationUserName, userInfo.CustomerFullName, employee.ApplicationUserName, employee.CustomerFullName, message);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerFactory.CreateLog().LogError("OnlineNotificationHub.Notify...", ex);
            }
        }

        public async Task NotifyUser(string currentAppDomain, string fromUserName, string targetUserName, string message)
        {
            try
            {
                var serviceHeader = new ServiceHeader { ApplicationDomainName = currentAppDomain };

                if (_usersList.Count != 0)
                {
                    var userInfo = await _channelService.GetUserInfoAsync(fromUserName, serviceHeader);

                    if (userInfo != null)
                    {
                        _messagesList.Add(new InstantMessageDTO
                        {
                            ConnectionId = Context.ConnectionId,
                            Sender = userInfo.ApplicationUserName,
                            FullName = userInfo.CustomerFullName,
                            Message = message,
                            CreatedDate = DateTime.Now
                        });

                        var employee = _usersList.FirstOrDefault(x => x.ApplicationUserName.Equals(targetUserName, StringComparison.OrdinalIgnoreCase));

                        if (employee != null && employee.OnlineNotificationsEnabled)
                        {
                            Clients.Client(employee.SignalRConnectionId).onNotifyMessage(Context.ConnectionId, userInfo.ApplicationUserName, message);

                            LoggerFactory.CreateLog().LogInfo("OnlineNotificationHub.NotifyUser...{0}->From:{1}->{2}{0}To:{1}->{2}{0}Message:{1}", Environment.NewLine, userInfo.ApplicationUserName, userInfo.CustomerFullName, employee.ApplicationUserName, employee.CustomerFullName, message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerFactory.CreateLog().LogError("OnlineNotificationHub.NotifyUser...", ex);
            }
        }

        public async Task Broadcast(string currentAppDomain, string userName, string message)
        {
            try
            {
                var serviceHeader = new ServiceHeader { ApplicationDomainName = currentAppDomain };

                if (_usersList.Count != 0)
                {
                    var userInfo = await _channelService.GetUserInfoAsync(userName, serviceHeader);

                    if (userInfo != null)
                    {
                        _messagesList.Add(new InstantMessageDTO
                        {
                            ConnectionId = Context.ConnectionId,
                            Sender = userInfo.ApplicationUserName,
                            FullName = userInfo.CustomerFullName,
                            Message = message,
                            CreatedDate = DateTime.Now
                        });

                        Clients.Others.onBroadcastMessage(Context.ConnectionId, userInfo.ApplicationUserName, message);

                        LoggerFactory.CreateLog().LogInfo("OnlineNotificationHub.Broadcast...{0}->{1}->{2}", userInfo.ApplicationUserName, userInfo.CustomerFullName, message);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerFactory.CreateLog().LogError("OnlineNotificationHub.Send...", ex);
            }
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            try
            {
                var item = _usersList.FirstOrDefault(x => x.SignalRConnectionId == Context.ConnectionId);

                if (item != null)
                {
                    _usersList.Remove(item);

                    LoggerFactory.CreateLog().LogInfo("OnlineNotificationHub.OnDisconnected...{0}->{1}", item.ApplicationUserName, item.CustomerFullName);
                }
            }
            catch (Exception ex)
            {
                LoggerFactory.CreateLog().LogError("OnlineNotificationHub.OnDisconnected...", ex);
            }

            return base.OnDisconnected(stopCalled);
        }
    }
}
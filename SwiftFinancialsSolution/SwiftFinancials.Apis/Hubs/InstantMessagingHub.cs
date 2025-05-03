using Application.MainBoundedContext.DTO.HumanResourcesModule;
using Application.MainBoundedContext.DTO.MessagingModule;
using Infrastructure.Crosscutting.Framework.Logging;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using VanguardFinancials.Presentation.Infrastructure.Services;

namespace SwiftFinancials.Apis.Hubs
{
    [HubName("vfinIMHub")]
    public class InstantMessagingHub : Hub
    {
        private static readonly List<EmployeeDTO> _usersList = new List<EmployeeDTO>();
        private static readonly List<InstantMessageDTO> _messagesList = new List<InstantMessageDTO>();

        private readonly IChannelService _channelService;

        public InstantMessagingHub()
            : base()
        {
            _channelService = DependencyResolver.Current.GetService<IChannelService>();
        }

        public async Task Connect(string currentAppDomain, string userName)
        {
            try
            {
                var serviceHeader = new ServiceHeader { ApplicationDomainName = currentAppDomain };

                var userInfo = await _channelService.GetUserInfoAsync(userName, serviceHeader);

                if (userInfo != null)
                {
                    userInfo.SignalRConnectionId = Context.ConnectionId;

                    _usersList.Add(userInfo);

                    var usersJsonString = JsonConvert.SerializeObject(_usersList);

                    Clients.All.onConnected(usersJsonString);

                    var messagesJsonString = JsonConvert.SerializeObject(_messagesList);

                    Clients.All.onGetMessages(messagesJsonString);

                    LoggerFactory.CreateLog().LogInfo("InstantMessagingHub.Connect...{0}->{1}", userInfo.ApplicationUserName, userInfo.CustomerFullName);
                }
            }
            catch (Exception ex)
            {
                LoggerFactory.CreateLog().LogError("InstantMessagingHub.Connect...", ex);
            }
        }

        public async Task Send(string currentAppDomain, string userName, string message)
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

                        Clients.All.onBroadcastMessage(Context.ConnectionId, userInfo.ApplicationUserName, message);

                        LoggerFactory.CreateLog().LogInfo("InstantMessagingHub.Send...{0}->{1}->{2}", userInfo.ApplicationUserName, userInfo.CustomerFullName, message);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerFactory.CreateLog().LogError("InstantMessagingHub.Send...", ex);
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

                    var jsonString = JsonConvert.SerializeObject(_usersList);

                    Clients.All.onConnected(jsonString);

                    Clients.All.onBroadcastMessage(Context.ConnectionId, item.ApplicationUserName, "I have stepped out");

                    LoggerFactory.CreateLog().LogInfo("InstantMessagingHub.OnDisconnected...{0}->{1}", item.ApplicationUserName, item.CustomerFullName);
                }
            }
            catch (Exception ex)
            {
                LoggerFactory.CreateLog().LogError("InstantMessagingHub.OnDisconnected...", ex);
            }

            return base.OnDisconnected(stopCalled);
        }
    }
}
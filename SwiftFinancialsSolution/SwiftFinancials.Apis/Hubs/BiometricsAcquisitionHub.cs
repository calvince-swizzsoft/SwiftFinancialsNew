using Application.MainBoundedContext.DTO;
using Infrastructure.Crosscutting.Framework.Logging;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SwiftFinancials.Apis.Hubs
{
    [HubName("vfinBiometricsAcquisitionHub")]
    public class BiometricsAcquisitionHub : Hub
    {
        private static readonly List<SignalRClientProfile> _clientProfileList = new List<SignalRClientProfile>();

        public Task Connect(string payload)
        {
            return Task.Run(() =>
            {
                try
                {
                    var clientProfile = JsonConvert.DeserializeObject<SignalRClientProfile>(payload);

                    if (clientProfile != null && !_clientProfileList.Any(x => x.ClientType == clientProfile.ClientType && x.EnvironmentMACAddress == clientProfile.EnvironmentMACAddress))
                    {
                        clientProfile.ConnectionId = Context.ConnectionId;

                        _clientProfileList.Add(clientProfile);

                        var clientProfileJsonString = JsonConvert.SerializeObject(clientProfile);

                        Clients.Client(clientProfile.ConnectionId).onConnected(clientProfileJsonString);

                        LoggerFactory.CreateLog().LogInfo("BiometricsAcquisitionHub.Connect...{0}->{1}", clientProfile.ClientType, clientProfile.EnvironmentMACAddress);
                    }
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLog().LogError("BiometricsAcquisitionHub.Connect...", ex);
                }
            });
        }

        public Task BeginEnrollFingerprint(string payload)
        {
            return Task.Run(() =>
            {
                try
                {
                    var applicationUserClientProfile = _clientProfileList.SingleOrDefault(x => x.ConnectionId == Context.ConnectionId);

                    if (applicationUserClientProfile != null)
                    {
                        var applicationDeviceClientProfile = _clientProfileList.SingleOrDefault(x => x.ClientType == string.Format("{0}", SignalRClientType.ApplicationDevice) && x.EnvironmentMACAddress == applicationUserClientProfile.EnvironmentMACAddress);

                        if (applicationDeviceClientProfile != null)
                        {
                            Clients.Client(applicationDeviceClientProfile.ConnectionId).onBeginEnrollFingerprint(Context.ConnectionId, payload);

                            LoggerFactory.CreateLog().LogInfo("BiometricsAcquisitionHub.BeginEnrollFingerprint...{0}->{1}", applicationUserClientProfile.ClientType, applicationUserClientProfile.EnvironmentMACAddress);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLog().LogError("BiometricsAcquisitionHub.BeginEnrollFingerprint...", ex);
                }
            });
        }

        public Task EndEnrollFingerprint(string connectionId, string payload)
        {
            return Task.Run(() =>
            {
                try
                {
                    var applicationDeviceClientProfile = _clientProfileList.SingleOrDefault(x => x.ConnectionId == Context.ConnectionId);

                    if (applicationDeviceClientProfile != null)
                    {
                        var applicationUserClientProfile = _clientProfileList.SingleOrDefault(x => x.ConnectionId == connectionId);

                        if (applicationUserClientProfile != null)
                        {
                            Clients.Client(applicationUserClientProfile.ConnectionId).onEndEnrollFingerprint(payload);

                            LoggerFactory.CreateLog().LogInfo("BiometricsAcquisitionHub.EndEnrollFingerprint...{0}->{1}", applicationDeviceClientProfile.ClientType, applicationDeviceClientProfile.EnvironmentMACAddress);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLog().LogError("BiometricsAcquisitionHub.EndEnrollFingerprint...", ex);
                }
            });
        }

        public Task BeginVerifyFingerprint(string payload)
        {
            return Task.Run(() =>
            {
                try
                {
                    var applicationUserClientProfile = _clientProfileList.SingleOrDefault(x => x.ConnectionId == Context.ConnectionId);

                    if (applicationUserClientProfile != null)
                    {
                        var applicationDeviceClientProfile = _clientProfileList.SingleOrDefault(x => x.ClientType == string.Format("{0}", SignalRClientType.ApplicationDevice) && x.EnvironmentMACAddress == applicationUserClientProfile.EnvironmentMACAddress);

                        if (applicationDeviceClientProfile != null)
                        {
                            Clients.Client(applicationDeviceClientProfile.ConnectionId).onBeginVerifyFingerprint(Context.ConnectionId, payload);

                            LoggerFactory.CreateLog().LogInfo("BiometricsAcquisitionHub.BeginVerifyFingerprint...{0}->{1}", applicationUserClientProfile.ClientType, applicationUserClientProfile.EnvironmentMACAddress);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLog().LogError("BiometricsAcquisitionHub.BeginVerifyFingerprint...", ex);
                }
            });
        }

        public Task EndVerifyFingerprint(string connectionId)
        {
            return Task.Run(() =>
            {
                try
                {
                    var applicationDeviceClientProfile = _clientProfileList.SingleOrDefault(x => x.ConnectionId == Context.ConnectionId);

                    if (applicationDeviceClientProfile != null)
                    {
                        var applicationUserClientProfile = _clientProfileList.SingleOrDefault(x => x.ConnectionId == connectionId);

                        if (applicationUserClientProfile != null)
                        {
                            var clientProfileJsonString = JsonConvert.SerializeObject(applicationDeviceClientProfile);

                            Clients.Client(applicationUserClientProfile.ConnectionId).onEndVerifyFingerprint(clientProfileJsonString);

                            LoggerFactory.CreateLog().LogInfo("BiometricsAcquisitionHub.EndVerifyFingerprint...{0}->{1}", applicationDeviceClientProfile.ClientType, applicationDeviceClientProfile.EnvironmentMACAddress);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLog().LogError("BiometricsAcquisitionHub.EndVerifyFingerprint...", ex);
                }
            });
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            return Task.Run(() =>
            {
                try
                {
                    var clientProfile = _clientProfileList.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);

                    if (clientProfile != null)
                    {
                        _clientProfileList.Remove(clientProfile);

                        LoggerFactory.CreateLog().LogInfo("BiometricsAcquisitionHub.OnDisconnected...{0}->{1}", clientProfile.ClientType, clientProfile.EnvironmentMACAddress);
                    }
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLog().LogError("BiometricsAcquisitionHub.OnDisconnected...", ex);
                }

                return base.OnDisconnected(stopCalled);
            });
        }
    }
}
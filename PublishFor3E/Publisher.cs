﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Xml;

namespace PublishFor3E
    {
    internal class Publisher
        {
        private readonly PublishParameters _publishParameters;
        private readonly HttpClient _httpClient;

        public Publisher(PublishParameters publishParameters)
            {
            this._publishParameters = publishParameters ?? throw new ArgumentNullException(nameof(publishParameters));
            this._httpClient = BuildHttpClient();
            }

        public bool TryPublish()
            {
            Console.WriteLine($"Checking WAPI servers on {this._publishParameters.Target.Environment}");
            var wapisAndPools = GetRunningAppPools().ToList();
            SwitchOffRunningWapis(wapisAndPools);
            StartPublish();
            Console.WriteLine("Publishing started");
            var publishResult = MonitorPublishingState();
            TurnAppPoolsBackOn(wapisAndPools);
            return publishResult == State.Succeeded;
            }

        private IEnumerable<WapiPool> GetRunningAppPools()
            {
            foreach (var wapi in this._publishParameters.Wapis)
                {
                string? appPool = GetPoolForWapi(wapi);
                if (appPool != null)
                    {
                    var result = new WapiPool { WapiServer = wapi, AppPool = appPool };
                    if (GetAppPoolState(result) == AppPoolState.Started)
                        {
                        yield return result;
                        }
                    }
                }
            }

        private string? GetPoolForWapi(string wapi)
            {
            ManagementScope scope = GetManagementScope(wapi);

            // Query IIS WMI property 
            string applicationName = $"W3SVC/1/ROOT/{this._publishParameters.Target.Environment}/Web";
            ObjectQuery objectQuery = new ObjectQuery($"SELECT * FROM IIsWebVirtualDirSetting WHERE Name = \"{applicationName}\"");

            // Search and collect details thru WMI methods
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, objectQuery);
            ManagementObjectCollection virtualDirectorySettings = searcher.Get();

            // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
            foreach (ManagementObject virtualDirectorySetting in virtualDirectorySettings)
                {
                string? virtualDirName = virtualDirectorySetting["Name"] as string;
                //string path = virtualDirectorySetting["Path"] as string;
                string? appPool = virtualDirectorySetting["AppPoolId"] as string;

                if (string.Equals(virtualDirName, applicationName, StringComparison.OrdinalIgnoreCase)) 
                    {
                    return appPool;
                    }
                }
            return null;
            }

        private AppPoolState GetAppPoolState(WapiPool wapiPool)
            {
            ManagementScope scope = GetManagementScope(wapiPool.WapiServer);

            // Query IIS WMI property
            ObjectQuery objectQuery = new ObjectQuery("SELECT * FROM IISApplicationPoolSetting");

            // Search and collect details thru WMI methods
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, objectQuery);
            ManagementObjectCollection applicationPoolSettings = searcher.Get();

            // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
            foreach (ManagementObject applicationPoolSetting in applicationPoolSettings)
                {
                // full name is of form W3SVC/AppPools/DefaultAppPool
                string? fullAppName = applicationPoolSetting["Name"] as string;
                string? partialAppName = fullAppName?.Split('/')[2];
                AppPoolState appPoolState = (AppPoolState) applicationPoolSetting["AppPoolState"];

                if (string.Equals(partialAppName, wapiPool.AppPool, StringComparison.OrdinalIgnoreCase)) 
                    {
                    return appPoolState;
                    }
                }
            return AppPoolState.Unknown;
            }

        private void SwitchOffRunningWapis(IEnumerable<WapiPool> wapisAndPools)
            {
            foreach (var item in wapisAndPools)
                {
                Console.Write($"Stopping app pool {item.AppPool} on {item.WapiServer}...");
                try
                    {
                    SendControlRequestToAppPool(item, "Stop");
                    Console.WriteLine("done");
                    }
                catch (Exception ex)
                    {
                    Console.WriteLine($"failed {ex.Message}");
                    }
                }
            }

        private void TurnAppPoolsBackOn(IEnumerable<WapiPool> wapisAndPools)
            {
            foreach (var item in wapisAndPools)
                {
                Console.Write($"Starting app pool {item.AppPool} on {item.WapiServer}...");
                try
                    {
                    SendControlRequestToAppPool(item, "Start");
                    Console.WriteLine("done");
                    }
                catch (Exception ex)
                    {
                    Console.WriteLine($"failed {ex.Message}");
                    }
                }
            }

        private void SendControlRequestToAppPool(WapiPool wapiPool, string action)
            {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (!action.Equals("Start", StringComparison.OrdinalIgnoreCase) 
                && !action.Equals("Stop", StringComparison.OrdinalIgnoreCase)
                && !action.Equals("Recycle", StringComparison.OrdinalIgnoreCase))
                {
                throw new ArgumentOutOfRangeException(nameof(action), "Specify Start, Stop or Recycle");
                }

            ManagementScope scope = GetManagementScope(wapiPool.WapiServer);

            // IIS WMI object IISApplicationPool to perform actions on IIS Application Pool
            ObjectQuery objectQuery = new ObjectQuery("SELECT * FROM IISApplicationPool");

            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, objectQuery);
            ManagementObjectCollection applicationPools = searcher.Get();

            // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
            foreach (ManagementObject applicationPool in applicationPools)
                {
                string? fullAppName = applicationPool["Name"] as string;
                string? partialAppName = fullAppName?.Split('/')[2];
                if (string.Equals(partialAppName, wapiPool.AppPool, StringComparison.OrdinalIgnoreCase))
                    {
                    applicationPool.InvokeMethod(action, null!);
                    }
                }
            }

        private ManagementScope GetManagementScope(string wapiServer)
            {
            ConnectionOptions options = new ConnectionOptions
                {
                // Packet Privacy means authentication with encrypted connection.
                Authentication = AuthenticationLevel.PacketPrivacy,

                // EnablePrivileges : Value indicating whether user privileges 
                // need to be enabled for the connection operation. 
                // This property should only be used when the operation performed 
                // requires a certain user privilege to be enabled.
                EnablePrivileges = true
                };
            if (this._publishParameters.WmiCredentials != null)
                {
                // https://web.archive.org/web/20150213044821/http://www.manageengine.com/network-monitoring/help/troubleshoot_opmanager/troubleshoot_wmi.html
                options.Username = $"{this._publishParameters.WmiCredentials.Domain}\\{this._publishParameters.WmiCredentials.UserName}";
                options.Password = this._publishParameters.WmiCredentials.Password;
                }

            ManagementScope scope = new ManagementScope($@"\\{wapiServer}\root\MicrosoftIISv2", options);
            return scope;
            }

        private HttpClient BuildHttpClient()
            {
            var credentialCache = new CredentialCache
                {
                    { this._publishParameters.Target.BaseUri, "Negotiate", CredentialCache.DefaultNetworkCredentials }
                };

            var handler = new HttpClientHandler { Credentials = credentialCache, PreAuthenticate = true };
            var result = new HttpClient(handler);
            result.BaseAddress = new Uri(this._publishParameters.Target.BaseUri, "services/DesignerService.asmx/");
            return result;
            }

        private void StartPublish()
            {
            var response = CallDesignerService("PublishAll");
            if (response != "started")
                {
                if (string.IsNullOrWhiteSpace(response))
                    {
                    response = "(no response)";
                    }
                throw new InvalidOperationException($"Could not start publishing: {response}");
                }
            }

        private State MonitorPublishingState()
            {
            string? lastResponse = null;
            State state;
            do
                {
                Thread.Sleep(250);
                string status = CallDesignerService("CheckPublishingState");
                string newInfo = ExtractNewInfo(status, out state);
                if (newInfo != lastResponse)
                    {
                    Console.WriteLine(newInfo);
                    }
                lastResponse = newInfo;
                } while (state == State.Ongoing);

            return state;
            }

        private string CallDesignerService(string serviceName)
            {
            var response = this._httpClient.GetAsync(serviceName).Result;
            response.EnsureSuccessStatusCode();

            var xml = new XmlDocument();
            xml.LoadXml(response.Content.ReadAsStringAsync().Result);
            var result = xml.DocumentElement!.InnerText;
            return result;
            }

        private static string ExtractNewInfo(string status, out State state)
            {
            if (status.StartsWith("."))
                {
                state = State.Ongoing;
                return status.Substring(1);
                }

            if (status.StartsWith("**"))
                {
                state = State.Failed;
                return status.Substring(2);
                }

            if (status.StartsWith("*!"))
                {
                state = State.Succeeded;
                return status.Substring(2);
                }

            throw new InvalidOperationException($"Invalid response: {status}");
            }

        private struct WapiPool
            {
            public string WapiServer;
            public string AppPool;
            }

        private enum AppPoolState
            {
            Unknown = 0,
            // ReSharper disable UnusedMember.Local
            Starting = 1,
            Started = 2,
            Stopping = 3,
            Stopped = 4
            // ReSharper restore UnusedMember.Local
            }

        private enum State
            {
            Ongoing,
            Succeeded,
            Failed
            }
        }
    }

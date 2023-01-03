using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Xml;

namespace PublishFor3E
    {
    internal class WapiDiscovery
        {
        private readonly Target _target;

        public WapiDiscovery(Target target)
            {
            this._target = target ?? throw new ArgumentNullException(nameof(target));
            }

        public IEnumerable<string> GetPossibleWapis()
            {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var serviceInfo = GetServiceInfo();
            result.Add(serviceInfo.Server);

            result.UnionWith(GetWapis(serviceInfo.Version));
            return result;
            }

        private ServiceInfo GetServiceInfo()
            {
            Console.Write("Getting ServiceInfo...");
            try
                {
                string response = CallDesignerService("ServiceInfo");
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(response);
                Debug.Assert(xmlDoc.DocumentElement != null);
                var server = xmlDoc.DocumentElement!.GetAttribute("Server");
                var version = Version.Parse(xmlDoc.DocumentElement.GetAttribute("Version"));
                Console.WriteLine($" Server: {server}, Version: {version}");
                return new ServiceInfo { Server = server, Version = version };
                }
            catch (Exception ex)
                {
                Console.WriteLine(" Failed: " + ex.Message);
                throw new InvalidOperationException("Cannot continue - WAPI servers cannot be determined.");
                }
            }

        private string CallDesignerService(string serviceName)
            {
            var credentialCache = new CredentialCache
                {
                    { this._target.BaseUri, "Negotiate", CredentialCache.DefaultNetworkCredentials }
                };

            var handler = new HttpClientHandler { Credentials = credentialCache, PreAuthenticate = true };
            var httpClient = new HttpClient(handler);
            httpClient.BaseAddress = new Uri(this._target.BaseUri, "services/DesignerService.asmx/");

            var response = httpClient.GetAsync(serviceName).Result;
            response.EnsureSuccessStatusCode();

            var xml = new XmlDocument();
            xml.LoadXml(response.Content.ReadAsStringAsync().Result);
            var result = xml.DocumentElement!.InnerText;
            return result;
            }

        private IEnumerable<string> GetWapis(Version version)
            {
            Console.Write("Getting notification server list...");
            // ReSharper disable once StringLiteralTypo
            string uri = $"web{(version.Major < 3 ? "/ui" : string.Empty)}/TransactionService.asmx";
            var credentialCache = new CredentialCache
                {
                    { this._target.BaseUri, "Negotiate", CredentialCache.DefaultNetworkCredentials }
                };

            var handler = new HttpClientHandler { Credentials = credentialCache, PreAuthenticate = true };
            using var request = new HttpClient(handler);
            request.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
            request.DefaultRequestHeaders.Add("SOAPAction", "\"http://tempuri.org//ServiceExecuteProcess/GetArchetypeData\"");
            request.BaseAddress = this._target.BaseUri;
            var content = new StringContent(GetServersXml(this._target.Environment), Encoding.UTF8, "text/xml");
            try
                {
                var response = request.PostAsync(uri, content).Result;
                response.EnsureSuccessStatusCode();

                var result = ExtractServerNames(response.Content.ReadAsStringAsync().Result);
                Console.WriteLine(result.Any() ? " " + string.Join(",", result) : " no likely servers found - the 3E scheduler may not be running");
                return result;
                }
            catch (Exception ex)
                {
                Console.WriteLine(" Failed: " + ex.Message);
                return Array.Empty<string>();
                }
            }

        private static string[] ExtractServerNames(string soapResponse)
            {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(soapResponse);
            var xnm = new XmlNamespaceManager(xmlDoc.NameTable);
            xnm.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            xnm.AddNamespace("r", "http://tempuri.org//ServiceExecuteProcess");

            var dataElement = (XmlElement?) xmlDoc.SelectSingleNode("soap:Envelope/soap:Body/r:GetArchetypeDataResponse/r:GetArchetypeDataResult", xnm);
            if (dataElement == null)
                {
                return Array.Empty<string>();
                }
            xmlDoc.LoadXml(dataElement.InnerText);
            var serverElements = xmlDoc.SelectNodes("Data/NxNtfServer/ServerName")!;
            return serverElements.Cast<XmlElement>().Select(element => element.InnerText).ToArray();
            }

        private static string GetServersXml(string environment)
            {
            var xoql = Resources.GetServers.Replace("%%Environment%%", $"/{environment}/%");
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xoql);
            var result = xmlDoc.DocumentElement!.OuterXml;
            return result;
            }

        private struct ServiceInfo
            {
            public string Server;
            public Version Version;
            }
        }
    }

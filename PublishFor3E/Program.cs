using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Xml;

// https://www.codeproject.com/Tips/1063552/IIS-Application-Pool-Operations-via-Csharp

namespace PublishFor3E
    {
    internal static class Program
        {
        public static int Main(string[] args)
            {
            try
                {
                Console.WriteLine("Publish (and be damned) on a 3E environment");
                Console.WriteLine();
                if (args.Length == 0)
                    {
                    Console.WriteLine($"Usage: {Assembly.GetExecutingAssembly().GetName().Name} <environment> [wapi,wapi...]");
                    Console.WriteLine($"Where <environment> is a url like http://my3eserver/TE_3E_{DateTime.Today.DayOfWeek}/");
                    Console.WriteLine("and [wapi,wapi...] is an optional list of wapi servers to recycle.");
                    Console.WriteLine("If no list of wapis is provided, an attempt will be made to automatically determine which they are.");
                    return 0;
                    }

                var argsQueue = new Queue<string>(args);
                Uri enteredUrl;
                string environment;
                try
                    {
                    enteredUrl = new Uri(argsQueue.Dequeue(), UriKind.Absolute);
                    if (enteredUrl.Segments.Length < 2)
                        throw new InvalidOperationException("URL is too short");
                    environment = enteredUrl.AbsolutePath.Split('/')[1];
                    if (!environment.StartsWith("TE_3E_", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("URL doesn't look like a 3e environment as the path doesn't start TE_3E_");
                    }
                catch (Exception ex)
                    {
                    Console.WriteLine("Invalid URL to an environment specified: " + ex.Message);
                    return 1;
                    }
                
                Console.WriteLine($"Publishing on environment {environment}");
                var baseUrl = new Uri(enteredUrl, $"/{environment}/");

                List<string> wapis;
                switch (argsQueue.Count)
                    {
                    case 0:
                        Console.WriteLine($"Discovering WAPI servers for {environment}:");
                        wapis = GetPossibleWapis(baseUrl).ToList();
                        break;
                    case 1: 
                        wapis = ExtractWapiList(argsQueue.Dequeue()).ToList();
                        Console.WriteLine($"WAPI servers specified: {string.Join(", ", wapis)}");
                        break;
                    default:
                        wapis = argsQueue.ToList();
                        Console.WriteLine($"WAPI servers specified: {string.Join(", ", wapis)}");
                        break;
                    }

                Console.WriteLine();
                var publisher = new Publisher(baseUrl, wapis);
                bool publishSucceeded = publisher.TryPublish();
                Console.WriteLine();
                Console.WriteLine(publishSucceeded ? "Publish succeeded" : "Publish failed");

                return publishSucceeded ? 0 : 1;
                }
            catch (Exception ex)
                {
                Console.WriteLine($"Publish failed: {ex.Message}");
                return 1;
                }
            }

        private static IEnumerable<string> GetPossibleWapis(Uri baseUri)
            {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var serviceInfo = GetServiceInfo(baseUri);
            result.Add(serviceInfo.Server);

            var environment = baseUri.AbsolutePath.Split('/')[1];

            result.UnionWith(GetWapis(baseUri, serviceInfo.Version, environment));
            return result;
            }

        private static ServiceInfo GetServiceInfo(Uri baseUri)
            {
            Console.Write("Getting ServiceInfo...");
            try
                {
                string response = CallDesignerService(baseUri, "ServiceInfo");
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(response);
                Debug.Assert(xmlDoc.DocumentElement != null);
                var server = xmlDoc.DocumentElement!.GetAttribute("Server");
                var version = Version.Parse(xmlDoc.DocumentElement.GetAttribute("Version"));
                Console.WriteLine($"Server: {server}, Version: {version}");
                return new ServiceInfo { Server = server, Version = version };
                }
            catch (Exception ex)
                {
                Console.WriteLine("Failed: " + ex.Message);
                throw new InvalidOperationException("Cannot continue - WAPI servers cannot be determined.");
                }
            }

        private static string CallDesignerService(Uri baseUri, string serviceName)
            {
            var credentialCache = new CredentialCache
                {
                    { baseUri, "Negotiate", CredentialCache.DefaultNetworkCredentials }
                };

            var handler = new HttpClientHandler { Credentials = credentialCache, PreAuthenticate = true };
            var httpClient = new HttpClient(handler);
            httpClient.BaseAddress = new Uri(baseUri, "services/DesignerService.asmx/");

            var response = httpClient.GetAsync(serviceName).Result;
            response.EnsureSuccessStatusCode();

            var xml = new XmlDocument();
            xml.LoadXml(response.Content.ReadAsStringAsync().Result);
            var result = xml.DocumentElement!.InnerText;
            return result;
            }

        private static IEnumerable<string> GetWapis(Uri baseUri, Version version, string environment)
            {
            Console.Write("Getting notification server list...");
            // ReSharper disable once StringLiteralTypo
            string uri = (version.Major == 2 ? "web/ui" :  "web") + "/TransactionService.asmx";
            var credentialCache = new CredentialCache
                {
                    { baseUri, "Negotiate", CredentialCache.DefaultNetworkCredentials }
                };

            var handler = new HttpClientHandler { Credentials = credentialCache, PreAuthenticate = true };
            using (var request = new HttpClient(handler))
                {
                request.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
                request.DefaultRequestHeaders.Add("SOAPAction", "\"http://tempuri.org//ServiceExecuteProcess/GetArchetypeData\"");
                request.BaseAddress = baseUri;
                var content = new StringContent(GetServersXml(environment), Encoding.UTF8, "text/xml");
                try
                    {
                    var response = request.PostAsync(uri, content).Result;
                    response.EnsureSuccessStatusCode();

                    var result = ExtractServerNames(response.Content.ReadAsStringAsync().Result);
                    Console.WriteLine(result.Any() ? string.Join(",", result) : "no likely servers found");
                    return result;
                    }
                catch (Exception ex)
                    {
                    Console.WriteLine("Failed: " + ex.Message);
                    return new string[] { };
                    }
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
                return new string[] { };
            xmlDoc.LoadXml(dataElement.InnerText);
            var serverElements = xmlDoc.SelectNodes("Data/NxNtfServer/ServerName");
            Debug.Assert(serverElements != null);
            return serverElements.Cast<XmlElement>().Select(element => element.InnerText).ToArray();
            }

        private static string GetServersXml(string environment)
            {
            var xoql = Resources.GetServers.Replace("%%Environment%%", environment);
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xoql);
            Debug.Assert(xmlDoc.DocumentElement != null, "xmlDoc.DocumentElement != null");
            var result = xmlDoc.DocumentElement!.OuterXml;
            return result;
            }

        private static IEnumerable<string> ExtractWapiList(string wapiList)
            {
            var separatorPosition = wapiList.IndexOfAny("|,;".ToCharArray());
            if (separatorPosition == -1)
                {
                // assume only one wapi
                yield return wapiList;
                yield break;
                }

            var separator = wapiList[separatorPosition];
            var items = wapiList.Split(new [] {separator}, StringSplitOptions.RemoveEmptyEntries);
            foreach (string item in items)
                {
                yield return item;
                }
            }

        private struct ServiceInfo
            {
            public string Server;
            public Version Version;
            }
        }
    }

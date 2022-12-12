using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
                    string exampleEnvName = DateTime.Today.DayOfWeek.ToString().ToUpperInvariant();
                    Console.WriteLine($"Usage: {Assembly.GetExecutingAssembly().GetName().Name} <environment-url> [wapi,wapi...]");
                    Console.WriteLine($"Where <environment-url> is a url like http://my3eserver/TE_3E_{exampleEnvName}/");
                    Console.WriteLine("and [wapi,wapi...] is an optional list of wapi servers to recycle.");
                    Console.WriteLine("If no list of wapis is provided, an attempt will be made to automatically determine which they are.");
                    Console.WriteLine("The usage details will be saved to an xml file to allow a shortcut form of the command to be used:");
                    Console.WriteLine($"Shortcut: {Assembly.GetExecutingAssembly().GetName().Name} <environment>");
                    Console.WriteLine($"Where <environment> is TE_3E_{exampleEnvName}");
                    return 0;
                    }

                PublishParameters publishParameters;
                var argsQueue = new Queue<string>(args);
                var firstArgument = argsQueue.Peek();
                if (!firstArgument.Contains("/") && argsQueue.Count == 1)
                    {
                    if (!TryParseForShortCutForm(firstArgument, out publishParameters!))
                        return 1;
                    }
                else
                    {
                    if (!TryParseForFullForm(argsQueue, out publishParameters!))
                        return 1;
                    SavePublishParameters(publishParameters);
                    }

                Console.WriteLine();
                var publisher = new Publisher(publishParameters);
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

        internal static bool TryParseForFullForm(Queue<string> argsQueue, out PublishParameters? publishParameters)
            {
            if (!Target.TryParse(argsQueue.Dequeue(), out Target? target, out string? reason))
                {
                Console.WriteLine("Invalid URL to an environment specified: " + reason);
                publishParameters = null;
                return false;
                }

            Console.WriteLine($"Publishing on environment {target!.Environment}");

            publishParameters = new PublishParameters(target);
                
            switch (argsQueue.Count)
                {
                case 0:
                    Console.WriteLine($"Discovering WAPI servers for {target.Environment}:");
                    var wapiDiscovery = new WapiDiscovery(target);
                    publishParameters.AddWapis(wapiDiscovery.GetPossibleWapis());
                    break;
                case 1: 
                    publishParameters.AddWapis(ExtractWapiList(argsQueue.Dequeue()));
                    Console.WriteLine($"WAPI servers specified: {string.Join(", ", publishParameters.Wapis)}");
                    break;
                default:
                    publishParameters.AddWapis(argsQueue);
                    Console.WriteLine($"WAPI servers specified: {string.Join(", ", publishParameters.Wapis)}");
                    break;
                }

            return true;
            }

        internal static bool TryParseForShortCutForm(string criteria, out PublishParameters? publishParameters)
            {
            publishParameters = LoadPublishParameters(criteria);
            return publishParameters != null;
            }

        internal static IEnumerable<string> ExtractWapiList(string wapiList)
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

        internal static void SavePublishParameters(PublishParameters publishParameters)
            {
            string path = SettingsFile();
            XmlDocument xmlDoc;
            XmlElement? environments;
            if (File.Exists(path))
                {
                xmlDoc = new XmlDocument();
                try
                    {
                    xmlDoc.Load(path);
                    }
                catch (Exception ex)
                    {
                    Console.WriteLine($"Could not load settings file {path}: {ex.Message}");
                    return;
                    }

                environments = xmlDoc.SelectSingleNode("Environments") as XmlElement;
                if (environments == null)
                    {
                    Console.WriteLine("XML settings file contains invalid content and cannot be read or updated.");
                    return;
                    }
                }
            else
                {
                xmlDoc = new XmlDocument();
                environments = xmlDoc.CreateElement("Environments");
                xmlDoc.AppendChild(environments);
                }

            var environment = xmlDoc.SelectSingleNode($"Environments/{publishParameters.Target.Environment}")
                             ?? environments.AppendChild(xmlDoc.CreateElement(publishParameters.Target.Environment));

            XmlElement baseUri = (XmlElement) (environment.SelectSingleNode("BaseUri") ?? environment.AppendChild(xmlDoc.CreateElement("BaseUri")));
            baseUri.InnerText = publishParameters.Target.BaseUri.ToString();

            XmlElement wapis = (XmlElement)(environment.SelectSingleNode("Wapis") ?? environment.AppendChild(xmlDoc.CreateElement("Wapis")));
            wapis.InnerText = string.Join(" ", publishParameters.Wapis);

            xmlDoc.Save(path);
            }

        internal static PublishParameters? LoadPublishParameters(string criteria)
            {
            string path = SettingsFile();
            if (!File.Exists(path))
                {
                Console.WriteLine($"Could not load settings file {path}: File does not exist");
                return null;
                }

            XmlDocument xmlDoc = new XmlDocument();
            try
                {
                xmlDoc.Load(path);
                }
            catch (Exception ex)
                {
                Console.WriteLine($"Could not load settings file {path}: {ex.Message}");
                return null;
                }

            XmlElement? environments = xmlDoc.SelectSingleNode("Environments") as XmlElement;
            if (environments == null)
                {
                Console.WriteLine("XML settings file contains invalid content and cannot be read or updated.");
                return null;
                }

            var environmentList = new List<string>();
            foreach (XmlElement element in environments.ChildNodes)
                {
                environmentList.Add(element.Name);
                }

            var matches = criteria.StartsWith("TE_3E_", StringComparison.OrdinalIgnoreCase) 
                ? environmentList.Where(item => item.StartsWith(criteria, StringComparison.OrdinalIgnoreCase)).ToList()
                : environmentList.Where(item => item.IndexOf(criteria, StringComparison.OrdinalIgnoreCase) != -1).ToList();

            if (matches.Count == 0)
                {
                Console.WriteLine("Could not find any matches.");
                return null;
                }

            if (matches.Count > 1)
                {
                Console.WriteLine($"Match found to multiple environments: {string.Join(", ", matches)}");
                return null;
                }

            XmlElement? environment = environments.SelectSingleNode(matches[0]) as XmlElement;
            Debug.Assert(environment != null);

            var baseUri = environment!.SelectSingleNode("BaseUri")?.InnerText;
            var wapis = environment!.SelectSingleNode("Wapis")?.InnerText;
            if (baseUri == null || wapis == null)
                {
                Console.WriteLine($"Saved settings for environment {matches[0]} are incorrect.");
                return null;
                }

            if (!Target.TryParse(baseUri, out Target? target, out string? reason))
                {
                Console.WriteLine($"Saved URL for environment {matches[0]} is invalid: {reason}");
                }

            var result = new PublishParameters(target!);
            result.AddWapis(wapis.Split(' '));
            return result;
            }

        internal static string SettingsFile()
            {
            string fullPath = Assembly.GetExecutingAssembly().Location;
            string directory = Path.GetDirectoryName(fullPath)!;
            var fileWithoutExtension = Path.GetFileNameWithoutExtension(fullPath);
            var newFileName = Path.ChangeExtension(fileWithoutExtension + "Settings", "xml");
            var result = Path.Combine(directory, newFileName);
            return result;
            }
        }
    }

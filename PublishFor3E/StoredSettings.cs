using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace PublishFor3E
    {
    internal static class StoredSettings
        {
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
                xmlDoc.AppendChild(xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null));
                xmlDoc.AppendChild(xmlDoc.CreateComment($"This file contains the saved settings used previously to publish to 3E environments by {Assembly.GetExecutingAssembly().GetName().Name}.exe"));
                environments = xmlDoc.CreateElement("Environments");
                xmlDoc.AppendChild(environments);
                }

            var environment = publishParameters.Target.Environment;
            XmlElement environmentElement = (XmlElement) (environments.SelectSingleNode($"{environment}") ?? environments.AppendChild(xmlDoc.CreateElement(environment))!);

            XmlElement baseUri = (XmlElement) (environmentElement.SelectSingleNode("BaseUri") ?? environmentElement.AppendChild(xmlDoc.CreateElement("BaseUri"))!);
            baseUri.InnerText = publishParameters.Target.BaseUri.ToString();

            XmlElement wapis = (XmlElement) (environmentElement.SelectSingleNode("Wapis") ?? environmentElement.AppendChild(xmlDoc.CreateElement("Wapis"))!);
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

            List<string> matches;
            if (criteria.StartsWith("TE_3E_", StringComparison.OrdinalIgnoreCase))
                {
                matches = environmentList.Where(item => item.Equals(criteria, StringComparison.OrdinalIgnoreCase)).ToList();
                if (!matches.Any())
                    {
                    matches = environmentList.Where(item => item.StartsWith(criteria, StringComparison.OrdinalIgnoreCase)).ToList();
                    }
                }
            else
                {
                matches = environmentList.Where(item => item.IndexOf(criteria, StringComparison.OrdinalIgnoreCase) != -1).ToList();
                }

            if (matches.Count == 0)
                {
                Console.WriteLine("Could not find any matches.");
                return null;
                }

            if (matches.Count > 1)
                {
                Console.WriteLine($"Matches found to multiple environments: {string.Join(", ", matches)}");
                return null;
                }

            XmlElement environment = (environments.SelectSingleNode(matches[0]) as XmlElement)!;

            var baseUri = environment.SelectSingleNode("BaseUri")?.InnerText;
            var wapis = environment.SelectSingleNode("Wapis")?.InnerText;
            if (string.IsNullOrWhiteSpace(baseUri) || string.IsNullOrWhiteSpace(wapis))
                {
                Console.WriteLine($"Saved settings for environment {matches[0]} are incorrect.");
                return null;
                }

            // ReSharper disable once RedundantSuppressNullableWarningExpression - baseUri is not null thanks to using IsNullOrWhiteSpace but compiler flags emits a warning without explicit suppression
            if (!Target.TryParse(baseUri!, out Target? target, out string? reason))
                {
                Console.WriteLine($"Saved URL for environment {matches[0]} is invalid: {reason}");
                }

            var result = new PublishParameters(target!);
            // ReSharper disable once RedundantSuppressNullableWarningExpression - wapis is not null thanks to using IsNullOrWhiteSpace but compiler flags emits a warning without explicit suppression
            result.AddWapis(wapis!.Split(' '));
            return result;
            }

        internal static string SettingsFile()
            {
            string directory = AppContext.BaseDirectory;
            var fileWithoutExtension = Assembly.GetExecutingAssembly().GetName().Name;
            var newFileName = Path.ChangeExtension(fileWithoutExtension + "Settings", "xml");
            var result = Path.Combine(directory, newFileName);
            return result;
            }
        }
    }

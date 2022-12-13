using System;
using System.Collections.Generic;
using System.Reflection;

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
                    StoredSettings.SavePublishParameters(publishParameters);
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
            publishParameters = StoredSettings.LoadPublishParameters(criteria);
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
        }
    }

using Infrastructure.Crosscutting.Framework.Logging;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Presentation.Infrastructure.Services;
using System;
using System.Threading;

namespace SwiftFinancials.Utility
{
    class Program
    {
        static void Main(string[] args)
        {
            ConfigureFactories();

            ILogger logger = new SerilogLogger();

            IChannelService channelService = new ChannelService(logger);

            try
            {
                if (args.Length != 1)
                {
                    Console.WriteLine("Usage: SwiftFinancials.Utility.exe [CurrentAppDomainName]");

                    Console.WriteLine("Press any key to continue...");

                    Console.ReadKey();
                }
                else
                {
                    var serviceHeader = new ServiceHeader { ApplicationDomainName = args[0] };

                    Action<string[]> worker;

                    worker = async (input) =>
                    {
                        var result = default(bool);

                        Console.WriteLine("CurrentAppDomainName>{0}", serviceHeader.ApplicationDomainName);

                        result = await channelService.ConfigureAspNetIdentityDatabaseAsync(serviceHeader, 180d);
                        Console.WriteLine("ConfigureAspNetIdentityDatabaseAsync>{0}", result);

                        result = await channelService.ConfigureAspNetMembershipDatabaseAsync(serviceHeader, 180d);
                        Console.WriteLine("ConfigureAspNetMembershipDatabaseAsync>{0}", result);

                        result = await channelService.ConfigureApplicationDatabaseAsync(serviceHeader, 180d);
                        Console.WriteLine("ConfigureApplicationDatabaseAsync>{0}", result);

                        if (result)
                        {
                            result = await channelService.SeedEnumerationsAsync(serviceHeader, 180d);
                            Console.WriteLine("ApplicationDatabase>SeedEnumerationsAsync>{0}", result);
                        }

                        Console.WriteLine("DONE!");

                        Console.WriteLine("Press any key to continue...");

                        Console.ReadKey();

                        Environment.Exit(0);
                    };

                    worker(args);

                    Thread.Sleep(Timeout.Infinite);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception!");

                logger.LogError("SwiftFinancials.Utility...", ex);
            }
        }

        private static void ConfigureFactories()
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (se, cert, chain, sslerror) =>
            {
                return true;
            };
        }
    }
}

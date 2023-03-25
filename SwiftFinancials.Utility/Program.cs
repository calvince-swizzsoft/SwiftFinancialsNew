using Application.MainBoundedContext.DTO.AdministrationModule;
using Infrastructure.Crosscutting.Framework.Logging;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Presentation.Infrastructure.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading;

namespace SwiftFinancials.Utility
{
    class Program
    {
        private static void Main(string[] args)
        {
            ConfigureFactories();

            ILogger logger = new SerilogLogger();

            IChannelService channelService = new ChannelService(logger);

            var navigationItems = GetAvailableNavigationMenus();

            try
            {
                if (args.Length != 1)
                {
                    Console.WriteLine("Usage: EasyBim.Utility.exe [CurrentAppDomainName]");

                    Console.WriteLine("Press any key to continue...");

                    Console.ReadKey();
                }
                else
                {
                    ServiceHeader serviceHeader = new ServiceHeader { ApplicationDomainName = args[0] };

                    async void worker(string[] input)
                    {
                        bool result = default(bool);

                        Console.WriteLine("CurrentAppDomainName>{0}", serviceHeader.ApplicationDomainName);

                        result = await channelService.ConfigureAspNetIdentityDatabaseAsync(serviceHeader, 180d);
                        Console.WriteLine("ConfigureAspNetIdentityDatabaseAsync>{0}", result);

                        result = await channelService.ConfigureApplicationDatabaseAsync(serviceHeader, 180d);
                        Console.WriteLine("ConfigureApplicationDatabaseAsync>{0}", result);

                        result = await channelService.AddModuleNavigationItemsAsync(navigationItems, serviceHeader);
                        Console.WriteLine("AddNavigationItemsAsync>{0}", result);

                        if (result)
                        {
                            result = await channelService.SeedEnumerationsAsync(serviceHeader, 180d);
                            Console.WriteLine("ApplicationDatabase>SeedEnumerationsAsync>{0}", result);
                        }

                        Console.WriteLine("DONE!");

                        Console.WriteLine("Press any key to continue...");

                        Console.ReadKey();

                        Environment.Exit(0);
                    }

                    worker(args);

                    Thread.Sleep(Timeout.Infinite);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception!");

                logger.LogError("EasyBim.Utility...", ex);
            }
        }

        private static ObservableCollection<ModuleNavigationItemDTO> GetAvailableNavigationMenus()
        {
            NavigationMenu navigationMenu = new NavigationMenu();

            var result = new ObservableCollection<ModuleNavigationItemDTO>();

            foreach (var menu in navigationMenu.GetMenus())
            {
                result.Add(new ModuleNavigationItemDTO
                {
                    Description = menu.Description,
                    Icon = menu.Icon,
                    Code = menu.Code,
                    ControllerName = menu.ControllerName,
                    ActionName = menu.ActionName,
                    AreaName = menu.AreaName,
                    AreaCode = menu.AreaCode,
                    IsArea = menu.IsArea
                });
            }

            return result;
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

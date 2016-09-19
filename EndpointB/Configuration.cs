using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Contracts.Commands;
using FileBasedRouting;
using NServiceBus;
using NServiceBus.Persistence;

namespace EndpointB
{
    static class Configuration
    {
        public static async Task Start(string discriminator)
        {
            var endpointConfiguration = new EndpointConfiguration("endpointB");
            endpointConfiguration.MakeInstanceUniquelyAddressable(discriminator);

            endpointConfiguration.UsePersistence<InMemoryPersistence, StorageType.Timeouts>();
            endpointConfiguration.SendFailedMessagesTo("error");

            endpointConfiguration.EnableFeature<FileBasedRoutingFeature>();
            endpointConfiguration.UsePersistence<StaticRoutingPersistence, StorageType.Subscriptions>();

            var endpoint = await Endpoint.Start(endpointConfiguration);

            Console.WriteLine("Press [Esc] to quit.");

            while (true)
            {
                var key = Console.ReadKey();
                if (key.Key == ConsoleKey.Escape)
                {
                    break;
                }
            }

            await endpoint.Stop();
        }
    }
}

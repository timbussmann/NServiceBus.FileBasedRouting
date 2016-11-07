﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts.Commands;
using Contracts.Events;
using FileBasedRouting;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;

namespace EndpointA
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncMain().GetAwaiter().GetResult();
        }

        static async Task AsyncMain()
        {
            var endpointConfiguration = new EndpointConfiguration("endpointA");

            endpointConfiguration.SendFailedMessagesTo("error");
            endpointConfiguration.DisableFeature<AutoSubscribe>();
            endpointConfiguration.UsePersistence<InMemoryPersistence>();


            endpointConfiguration.EnableFeature<FileBasedRoutingFeature>();

            var endpoint = await Endpoint.Start(endpointConfiguration);

            Console.WriteLine("Press [c] to send a command. Press [e] to publish an event. Press [Esc] to quit.");

            while (true)
            {
                var key = Console.ReadKey();
                if (key.Key == ConsoleKey.Escape)
                {
                    break;
                }

                if (key.Key == ConsoleKey.C)
                {
                    var commandId = Guid.NewGuid();
                    await endpoint.Send(new DemoCommand {CommandId = commandId});
                    Console.WriteLine();
                    Console.WriteLine("Sent command with id: " + commandId);
                }

                if (key.Key == ConsoleKey.E)
                {
                    var eventId = Guid.NewGuid();
                    await endpoint.Publish(new DemoEvent() {EventId = eventId});
                    Console.WriteLine();
                    Console.WriteLine("Sent event with id: " + eventId);
                }
            }

            await endpoint.Stop();
        }
    }
}

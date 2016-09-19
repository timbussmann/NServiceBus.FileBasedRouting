using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Routing;
using NServiceBus.Routing.MessageDrivenSubscriptions;
using NServiceBus.Transport;
using Timer = System.Threading.Timer;

namespace FileBasedRouting
{
    public class FileBasedRoutingFeature : Feature
    {
        private UnicastRoutingTable unicastRoutingTable;
        private RoutingTable routingTable = new RoutingTable();
        private Timer timer;

        protected override void Setup(FeatureConfigurationContext context)
        {
            unicastRoutingTable = context.Settings.Get<UnicastRoutingTable>();

            routingTable.routingDataUpdated += RoutingTableOnRoutingDataUpdated;
            routingTable.Reload();

            context.Container.ConfigureComponent(() => new StaticRoutingSubscriptionStorage(routingTable, context.Settings.Get<TransportInfrastructure>()), DependencyLifecycle.SingleInstance);

            timer = new Timer(state => routingTable.Reload(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        private void RoutingTableOnRoutingDataUpdated(object sender, EventArgs eventArgs)
        {
            UpdateRoutingTable(routingTable.Endpoints);
        }

        private void UpdateRoutingTable(EndpointRoutingConfiguration[] endpoints)
        {
            var commandRoutes = new List<RouteTableEntry>();
            foreach (var endpoint in endpoints)
            {
                foreach (var command in endpoint.Commands)
                {
                    commandRoutes.Add(new RouteTableEntry(command,
                        UnicastRoute.CreateFromEndpointName(endpoint.LogicalEndpointName)));
                }
            }

            unicastRoutingTable.AddOrReplaceRoutes("FileBasedRouting", commandRoutes);
        }
    }
}
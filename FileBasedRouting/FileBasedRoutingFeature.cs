using System.Collections.Generic;
using System.Linq;
using System.Xml;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Routing;
using NServiceBus.Routing.MessageDrivenSubscriptions;
using NServiceBus.Transport;

namespace FileBasedRouting
{
    public class FileBasedRoutingFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            var routingFile = new RoutingFile();
            var endpoints = routingFile.Read().ToArray();

            var routingTable = context.Settings.Get<UnicastRoutingTable>();
            var commandRoutes = new List<RouteTableEntry>();
            var publishers = context.Settings.Get<Publishers>();
            var subscriptionRoutes = new List<PublisherTableEntry>();
            var endpointInstances = context.Settings.Get<EndpointInstances>();
            var instances = new List<EndpointInstance>();

            foreach (var endpoint in endpoints)
            {
                foreach (var command in endpoint.Commands)
                {
                    commandRoutes.Add(new RouteTableEntry(command, UnicastRoute.CreateFromEndpointName(endpoint.LogicalEndpointName)));
                }

                instances.AddRange(endpoint.Instances);
            }

            context.Container.ConfigureComponent(b => new StaticRoutingSubscriptionStorage(endpoints, context.Settings.Get<TransportInfrastructure>()), DependencyLifecycle.SingleInstance);

            routingTable.AddOrReplaceRoutes("FileBasedRouting", commandRoutes);
            publishers.AddOrReplacePublishers("FileBasedRouting", subscriptionRoutes);
            endpointInstances.AddOrReplaceInstances("FileBasedRouting", instances);
        }
    }
}
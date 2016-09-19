using System.Collections.Generic;
using System.Xml;
using NServiceBus.Features;
using NServiceBus.Routing;
using NServiceBus.Routing.MessageDrivenSubscriptions;

namespace FileBasedRouting
{
    public class FileBasedRoutingFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            var routingFile = new RoutingFile();
            var endpoints = routingFile.Read();

            var routingTable = context.Settings.Get<UnicastRoutingTable>();
            var commandRoutes = new List<RouteTableEntry>();
            var publishers = context.Settings.Get<Publishers>();
            var subscriptionRoutes = new List<PublisherTableEntry>();
            var endpointInstances = context.Settings.Get<EndpointInstances>();
            var instances = new List<EndpointInstance>();

            foreach (var endpoint in endpoints)
            {
                foreach (var command in endpoint.Handles)
                {
                    commandRoutes.Add(new RouteTableEntry(command, UnicastRoute.CreateFromEndpointName(endpoint.LogicalEndpointName)));
                }

                foreach (var @event in endpoint.Publishes)
                {
                    subscriptionRoutes.Add(new PublisherTableEntry(@event, PublisherAddress.CreateFromEndpointName(endpoint.LogicalEndpointName)));
                }

                instances.AddRange(endpoint.Instances);
            }

            routingTable.AddOrReplaceRoutes("FileBasedRouting", commandRoutes);
            publishers.AddOrReplacePublishers("FileBasedRouting", subscriptionRoutes);
            endpointInstances.AddOrReplaceInstances("FileBasedRouting", instances);
        }
    }
}
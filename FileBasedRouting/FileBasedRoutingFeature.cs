using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
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

            foreach (var endpoint in endpoints)
            {
                foreach (var command in endpoint.Commands)
                {
                    commandRoutes.Add(new RouteTableEntry(command, UnicastRoute.CreateFromEndpointName(endpoint.LogicalEndpointName)));
                }
            }

            routingTable.AddOrReplaceRoutes("FileBasedRouting", commandRoutes);
            publishers.AddOrReplacePublishers("FileBasedRouting", subscriptionRoutes);

            List<EndpointInstance> instances = null;
            using (var instanceMappingFile = File.OpenRead("instance-mapping.xml"))
            {
                var parser = new InstanceMappingFileParser();
                instances = parser.Parse(XDocument.Load(instanceMappingFile));
            }

            context.Container.ConfigureComponent(b => new StaticRoutingSubscriptionStorage(endpoints, instances, context.Settings.Get<TransportInfrastructure>()), DependencyLifecycle.SingleInstance);
        }
    }
}
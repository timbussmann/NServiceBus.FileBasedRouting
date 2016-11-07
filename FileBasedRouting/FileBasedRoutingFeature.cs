using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Routing;
using NServiceBus.Routing.MessageDrivenSubscriptions;
using NServiceBus.Transport;
using Timer = System.Threading.Timer;

namespace FileBasedRouting
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class FileBasedRoutingFeature : Feature
    {
        private readonly RoutingTable routingTable = new RoutingTable();
        private UnicastRoutingTable unicastRoutingTable;
        private Publishers publishers;
        private Timer timer;

        protected override void Setup(FeatureConfigurationContext context)
        {
            unicastRoutingTable = context.Settings.Get<UnicastRoutingTable>();
            publishers = context.Settings.Get<Publishers>();

            routingTable.RoutingDataUpdated += RoutingTableOnRoutingDataUpdated;
            context.RegisterStartupTask(new UpdateSubscriptionsTask(routingTable, context.Settings.EndpointName()));
            routingTable.Reload();

            timer = new Timer(state => routingTable.Reload(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        private void RoutingTableOnRoutingDataUpdated(object sender, EventArgs eventArgs)
        {
            UpdateRoutingTable(routingTable.Endpoints);
            UpdatePublishers(routingTable.Endpoints);
        }

        private void UpdatePublishers(EndpointRoutingConfiguration[] endpoints)
        {
            IList<PublisherTableEntry> publisherRoutes = new List<PublisherTableEntry>();
            foreach (var endpoint in endpoints)
            {
                foreach (var publishedEvent in endpoint.PublishedEvents)
                {
                    publisherRoutes.Add(new PublisherTableEntry(publishedEvent,
                        PublisherAddress.CreateFromEndpointName(endpoint.LogicalEndpointName)));
                }
            }

            publishers.AddOrReplacePublishers("FileBasedRouting", publisherRoutes);
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

    class UpdateSubscriptionsTask : FeatureStartupTask
    {
        private readonly RoutingTable routingTable;
        private readonly string endpointName;
        private readonly object startStopLock = new object();
        private readonly object updateLock = new object();
        private Type[] previous = new Type[0];
        private IMessageSession session;

        public UpdateSubscriptionsTask(RoutingTable routingTable, string endpointName)
        {
            this.routingTable = routingTable;
            this.endpointName = endpointName;
        }

        private void RoutingTableOnRoutingDataUpdated(object sender, EventArgs eventArgs)
        {
            UpdateSubscriptions();
        }

        private void UpdateSubscriptions()
        {
            lock (updateLock)
            {
                var current = routingTable.Endpoints
                    .Single(e => e.LogicalEndpointName == endpointName)
                    .SubscribedEvents;

                lock (startStopLock)
                {
                    var added = current.Except(previous);
                    foreach (var t in added)
                    {
                        this.session?.Subscribe(t);
                    }

                    var removed = previous.Except(current);
                    foreach (var type in removed)
                    {
                        this.session?.Unsubscribe(type);
                    }
                }

                previous = current;
            }
        }

        protected override Task OnStart(IMessageSession session)
        {
            lock (this.startStopLock)
            {
                this.session = session;
            }

            // Initial subscriptions:
            UpdateSubscriptions();

            routingTable.RoutingDataUpdated += RoutingTableOnRoutingDataUpdated;

            return Task.CompletedTask;
        }

        protected override Task OnStop(IMessageSession session)
        {
            routingTable.RoutingDataUpdated -= RoutingTableOnRoutingDataUpdated;

            lock (this.startStopLock)
            {
                this.session = null;
            }

            return Task.CompletedTask;
        }
    }
}
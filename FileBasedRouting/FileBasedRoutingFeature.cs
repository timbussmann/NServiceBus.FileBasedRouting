using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Pipeline;
using NServiceBus.Routing;
using NServiceBus.Routing.MessageDrivenSubscriptions;
using NServiceBus.Transport;
using Timer = System.Threading.Timer;

namespace FileBasedRouting
{
    class FileBasedPublishSubscribe : IUnicastPublishSubscribe
    {
        private readonly EndpointInstances endpointInstances;
        private readonly RoutingTable routingTable;
        private readonly IDistributionPolicy distributionPolicy;
        private readonly TransportInfrastructure transportInfrastructure;
        private readonly ReaderWriterLockSlim readerWriterLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private Dictionary<Type, HashSet<string>> subscribedEndpoints;
        private static readonly string[] EmptyResult = new string[0];

        public FileBasedPublishSubscribe(EndpointInstances endpointInstances, RoutingTable routingTable, IDistributionPolicy distributionPolicy, TransportInfrastructure transportInfrastructure)
        {
            this.endpointInstances = endpointInstances;
            this.routingTable = routingTable;
            this.distributionPolicy = distributionPolicy;
            this.transportInfrastructure = transportInfrastructure;

            routingTable.routingDataUpdated += UpdateSubscribers;
        }

        private void UpdateSubscribers(object sender, EventArgs eventArgs)
        {
            try
            {
                readerWriterLock.EnterWriteLock();

                subscribedEndpoints = new Dictionary<Type, HashSet<string>>();
                foreach (var endpoint in routingTable.Endpoints)
                {
                    foreach (var @event in endpoint.Events)
                    {
                        HashSet<string> endpoints;
                        if (subscribedEndpoints.TryGetValue(@event, out endpoints))
                        {
                            endpoints.Add(endpoint.LogicalEndpointName);
                        }
                        else
                        {
                            subscribedEndpoints.Add(@event, new HashSet<string> { endpoint.LogicalEndpointName });
                        }
                    }
                }
            }
            finally
            {
                readerWriterLock.ExitWriteLock();
            }
        }

        public Task Subscribe(ISubscribeContext context)
        {
            // ignore
            return Task.CompletedTask;
        }

        public Task Unsubscribe(IUnsubscribeContext context)
        {
            // ignore
            return Task.CompletedTask;
        }

        public Task<List<UnicastRoutingStrategy>> GetRoutingStrategies(IOutgoingPublishContext context, Type eventType)
        {
            var endpoints = EmptyResult;
            try
            {
                readerWriterLock.EnterReadLock();

                endpoints = subscribedEndpoints
                    .Where(s => s.Key.IsAssignableFrom(eventType))
                    .SelectMany(s => s.Value)
                    .Distinct()
                    .ToArray();
            }
            finally
            {
                readerWriterLock.ExitReadLock();
            }


            var instanceAddressesPerEndpoint = endpoints
                .ToDictionary(e => e, e => endpointInstances.FindInstances(e)
                    .Select(i => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)))
                    .ToArray());

            var result = instanceAddressesPerEndpoint
                .Select(e => distributionPolicy
                    .GetDistributionStrategy(e.Key, DistributionStrategyScope.Publish)
                    .SelectReceiver(e.Value))
                .Select(r => new UnicastRoutingStrategy(r))
                .ToList();

            return Task.FromResult(result);
        }
    }

    public class FileBasedRoutingFeature : Feature
    {
        private UnicastRoutingTable unicastRoutingTable;
        private RoutingTable routingTable = new RoutingTable();
        private Timer timer;

        protected override void Setup(FeatureConfigurationContext context)
        {
            unicastRoutingTable = context.Settings.Get<UnicastRoutingTable>();

            FileBasedPublishSubscribe filebasedPubSub = new FileBasedPublishSubscribe(
                context.Settings.Get<EndpointInstances>(),
                routingTable,
                context.Settings.Get<DistributionPolicy>(),
                context.Settings.Get<TransportInfrastructure>());
            context.Container.ConfigureComponent<IUnicastPublishSubscribe>(() => filebasedPubSub,
                DependencyLifecycle.SingleInstance);

            routingTable.routingDataUpdated += RoutingTableOnRoutingDataUpdated;
            routingTable.Reload();

            //context.Container.ConfigureComponent(() => new StaticRoutingSubscriptionStorage(routingTable, context.Settings.Get<TransportInfrastructure>()), DependencyLifecycle.SingleInstance);

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
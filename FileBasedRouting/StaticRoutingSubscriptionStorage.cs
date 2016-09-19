using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Routing;
using NServiceBus.Transport;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

namespace FileBasedRouting
{
    class StaticRoutingSubscriptionStorage : ISubscriptionStorage
    {
        private readonly RoutingTable routingTable;
        private readonly TransportInfrastructure ti;
        private Dictionary<MessageType, List<Subscriber>> subscribers;

        public StaticRoutingSubscriptionStorage(RoutingTable routingTable, TransportInfrastructure ti)
        {
            this.routingTable = routingTable;
            this.ti = ti;
            Rebuild(this, EventArgs.Empty);

            this.routingTable.routingDataUpdated += Rebuild;
        }

        private void Rebuild(object sender, EventArgs eventArgs)
        {
            Dictionary<MessageType, List<EndpointRoutingConfiguration>> subscriberEndpoints =
                routingTable.Endpoints
                    .SelectMany(x => x.Events, (configuration, type) => new {type, configuration})
                    .GroupBy(x => x.type)
                    .ToDictionary(x => new MessageType(x.Key), x => x.Select(v => v.configuration).ToList());

            Dictionary<string, List<Subscriber>> instances =
                routingTable.EndpointInstances
                    .GroupBy(x => x.Endpoint)
                    .ToDictionary(x => x.Key,
                        x =>
                            x.Select(
                                e => new Subscriber(ti.ToTransportAddress(LogicalAddress.CreateRemoteAddress(e)), e.Endpoint))
                                .ToList());

            subscribers = subscriberEndpoints.ToDictionary(
                x => x.Key,
                x =>
                    x.Value.SelectMany(
                        e =>
                        {
                            List<Subscriber> subscribedInstances;
                            if (instances.TryGetValue(e.LogicalEndpointName, out subscribedInstances))
                            {
                                return subscribedInstances;
                            }

                            return new List<Subscriber>(1)
                            {
                                new Subscriber(e.LogicalEndpointName, e.LogicalEndpointName)
                            };
                        }).ToList());
        }

        public Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            //ignore
            return Task.CompletedTask;
        }

        public Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            //ignore
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            var result = new List<Subscriber>();
            foreach (var messageType in messageTypes)
            {
                List<Subscriber> typeSubscribers;
                if (subscribers.TryGetValue(messageType, out typeSubscribers))
                {
                    result.AddRange(typeSubscribers);
                }
            }

            //TODO deduplicate subscribers
            return Task.FromResult<IEnumerable<Subscriber>>(result);
        }
    }
}
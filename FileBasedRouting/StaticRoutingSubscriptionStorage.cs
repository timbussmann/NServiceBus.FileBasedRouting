using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Transport;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

namespace FileBasedRouting
{
    class StaticRoutingSubscriptionStorage : ISubscriptionStorage
    {
        private readonly Dictionary<MessageType, List<Subscriber>> subscribers;

        public StaticRoutingSubscriptionStorage(IEnumerable<EndpointRoutingConfiguration> endpoints, TransportInfrastructure ti)
        {
            Dictionary<MessageType, List<EndpointRoutingConfiguration>> subscriberEndpoints =
                endpoints
                    .SelectMany(x => x.Events, (configuration, type) => new {type, configuration})
                    .GroupBy(x => x.type)
                    .ToDictionary(x => new MessageType(x.Key), x => x.Select(v => v.configuration).ToList());

            subscribers = subscriberEndpoints.ToDictionary(
                x => x.Key,
                x =>
                    x.Value.SelectMany(
                        e =>
                        {
                            var instances = e.Instances.Select(
                                i =>
                                    new Subscriber(ti.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)),
                                        e.LogicalEndpointName));

                            if (instances.Any())
                            {
                                return instances;
                            }

                            return new[] {new Subscriber(e.LogicalEndpointName, e.LogicalEndpointName)};

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
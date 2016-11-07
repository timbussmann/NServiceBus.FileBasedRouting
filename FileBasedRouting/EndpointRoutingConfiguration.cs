using System;
using NServiceBus.Routing;

namespace FileBasedRouting
{
    class EndpointRoutingConfiguration
    {
        public string LogicalEndpointName { get; set; }

        public Type[] Commands { get; set; }

        public Type[] SubscribedEvents { get; set; }

        public Type[] PublishedEvents { get; set; }
    }
}
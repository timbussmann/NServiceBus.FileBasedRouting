using System;
using NServiceBus.Routing;

namespace FileBasedRouting
{
    class EndpointRoutingConfiguration
    {
        public string LogicalEndpointName { get; set; }

        public Type[] Handles { get; set; }

        public Type[] Publishes { get; set; }

        public EndpointInstance[] Instances { get; set; }
    }
}
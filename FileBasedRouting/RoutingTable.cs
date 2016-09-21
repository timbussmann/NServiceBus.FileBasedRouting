using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NServiceBus.Routing;

namespace FileBasedRouting
{
    class RoutingTable
    {
        private readonly RoutingFile routingFileAccess = new RoutingFile();

        public event EventHandler routingDataUpdated;

        public void Reload()
        {
            Endpoints = routingFileAccess.Read().ToArray();

            using (var instanceMappingFile = File.OpenRead("instance-mapping.xml"))
            {
                var parser = new InstanceMappingFileParser();
                EndpointInstances = parser.Parse(XDocument.Load(instanceMappingFile));
            }

            routingDataUpdated?.Invoke(this, EventArgs.Empty);
        }

        public EndpointRoutingConfiguration[] Endpoints { get; private set; }

        public List<EndpointInstance> EndpointInstances { get; private set; }
    }
}
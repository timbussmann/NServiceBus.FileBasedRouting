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

        public event EventHandler RoutingDataUpdated;

        public void Reload()
        {
            Endpoints = routingFileAccess.Read().ToArray();

            RoutingDataUpdated?.Invoke(this, EventArgs.Empty);
        }

        public EndpointRoutingConfiguration[] Endpoints { get; private set; }
    }
}
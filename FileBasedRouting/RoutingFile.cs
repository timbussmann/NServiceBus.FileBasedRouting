﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NServiceBus.Routing;

namespace FileBasedRouting
{
    class RoutingFile
    {
        public IEnumerable<EndpointRoutingConfiguration> Read()
        {
            using (var fileStream = File.OpenRead("endpoints.xml"))
            {
                XDocument document = XDocument.Load(fileStream);
                var endpointElements = document.Root.Descendants("endpoint");

                var configs = new List<EndpointRoutingConfiguration>();
                foreach (var endpointElement in endpointElements)
                {
                    var config = new EndpointRoutingConfiguration();
                    config.LogicalEndpointName = endpointElement.Attribute("name").Value;

                    config.Commands = endpointElement.Element("handles")
                        ?.Elements("command")
                        .Select(e => Type.GetType(e.Attribute("type").Value, true))
                        .ToArray() ?? new Type[0];

                    config.Events = endpointElement.Element("handles")
                        ?.Elements("event")
                        .Select(e => Type.GetType(e.Attribute("type").Value, true))
                        .ToArray() ?? new Type[0];

                    configs.Add(config);
                }

                return configs;
            }
        }
    }
}
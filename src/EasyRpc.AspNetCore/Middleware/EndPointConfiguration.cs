﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace EasyRpc.AspNetCore.Middleware
{
    public class EndPointConfiguration
    {
        public EndPointConfiguration(string route, 
            ConcurrentDictionary<string, IExposedMethodInformation> methods,
            bool enableDocumentation, 
            DocumentationConfiguration documentationConfiguration)
        {
            Route = route;
            Methods = methods;
            EnableDocumentation = enableDocumentation;
            DocumentationConfiguration = documentationConfiguration;
        }

        public string Route { get; }

        public ConcurrentDictionary<string, IExposedMethodInformation> Methods { get; }

        public bool EnableDocumentation { get; }

        public DocumentationConfiguration DocumentationConfiguration { get; }
    }
}

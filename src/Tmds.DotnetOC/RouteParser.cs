using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    class RouteParser
    {
        public static Route ParseRoute(JObject route)
        {
            string name = (string)route["metadata"]["name"];
            JToken spec = route["spec"];
            string host = (string)spec["host"];

            var backendList = new List<ServiceBackend>();
            JToken to = spec["to"];
            backendList.Add(new ServiceBackend
            {
                Weight = (int)to["weight"],
                Name = (string)to["name"]
            });

            return new Route
            {
                Name = name,
                Host = host,
                Backends = backendList.ToArray()
            };
        }
    }
}
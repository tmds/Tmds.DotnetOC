using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    class ServiceParser
    {
        public static Service ParseService(JObject service)
        {
            string name = (string)service["metadata"]["name"];
            JToken spec = service["spec"];

            var selectorList = new List<string>();
            foreach ((string selectorKey, JToken selectorValue) in (JObject)spec["selector"])
            {
                selectorList.Add($"{selectorKey}={selectorValue}");
            }

            return new Service
            {
                Name = name,
                Selectors = selectorList.ToArray()
            };
        }
    }
}
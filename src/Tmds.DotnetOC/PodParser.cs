using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    static class PodParser
    {
        public static Pod Parse(JObject pod)
        {
            JObject jobject = pod;
            string phase = (string)jobject["status"]["phase"];
            return new Pod
            {
                Phase = phase
            };
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    static class ReplicationControllerParser
    {
        public static ReplicationController Parse(JObject replicationController)
        {
            JObject jobject = replicationController;
            string phase = (string)jobject["metadata"]["annotations"]["openshift.io/deployment.phase"];
            string version = (string)jobject["metadata"]["annotations"]["openshift.io/deployment-config.latest-version"];
            return new ReplicationController
            {
                Phase = phase,
                Version = version
            };
        }
    }
}
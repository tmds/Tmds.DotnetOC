using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    static class BuildParser
    {
        public static Build GetLatestBuild(JObject builds, string buildConfigName)
        {
            JObject jobject = builds;
            Build build = null;
            int highestBuildNumber = -1;
            foreach (var item in jobject["items"])
            {
                int buildNumber = int.Parse((string)item["metadata"]["annotations"]["openshift.io/build.number"]);
                if (buildNumber <= highestBuildNumber)
                {
                    continue;
                }
                highestBuildNumber = buildNumber;
                string podName = (string)item["metadata"]["annotations"]["openshift.io/build.pod-name"];
                JToken status = item["status"];
                string statusMessage = (string)status["message"];
                string phase = (string)status["phase"];
                string reason = (string)status["reason"];
                build = new Build
                {
                    PodName = podName,
                    StatusMessage = statusMessage,
                    Phase = phase,
                    Reason = reason
                };
            }
            return build;
        }
    }
}
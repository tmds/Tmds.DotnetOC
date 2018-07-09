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

        public static DeploymentPod ParseDeploymentPod(JObject pod)
        {
            JObject jobject = pod;
            string phase = (string)jobject["status"]["phase"];
            string name = (string)jobject["metadata"]["name"];
            string version = (string)jobject["metadata"]["annotations"]["openshift.io/deployment-config.latest-version"];
            JToken containerStatus = (jobject["status"]["containerStatuses"] as JArray).First;
            JToken containerState = containerStatus["state"];
            JToken childState = containerState["running"] ?? containerState["waiting"] ?? containerState["terminated"];
            string reason = (string)childState["reason"];
            string message = (string)childState["message"];
            int restartCount = (int)containerStatus["restartCount"];

            // When Failed/PodInitializing, try to find more info in initContainerStatuses
            if (phase == "Failed" && reason == "PodInitializing")
            {
                containerStatus = (jobject["status"]["initContainerStatuses"] as JArray).First;
                containerState = containerStatus["state"];
                childState = containerState["running"] ?? containerState["waiting"] ?? containerState["terminated"];
                reason = (string)childState["reason"];
                message = (string)childState["message"];
                restartCount = (int)containerStatus["restartCount"];
            }

            return new DeploymentPod
            {
                Phase = phase,
                Name = name,
                DeploymentConfigLatestVersion = version,
                Reason = reason,
                Message = message,
                RestartCount = restartCount
            };
        }
    }
}
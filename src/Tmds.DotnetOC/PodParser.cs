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
            JToken containerStatus = jobject["status"]["containerStatuses"].First(); // TODO: handle more than 1 container
            JToken containerState = containerStatus["state"];
            JToken childState = containerState["running"] ?? containerState["waiting"] ?? containerState["terminated"];
            string reason = (string)childState["reason"];
            string message = (string)childState["message"];
            int restartCount = (int)containerStatus["restartCount"];
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
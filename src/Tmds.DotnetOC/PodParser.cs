using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    static class PodParser
    {
        public static Pod ParsePod(JObject pod)
        {
            if (pod == null)
            {
                throw new ArgumentNullException(nameof(pod));
            }
            JObject jobject = pod;
            string phase = (string)jobject["status"]["phase"];
            string name = (string)jobject["metadata"]["name"];
            string version = (string)jobject["metadata"]["annotations"]["openshift.io/deployment-config.latest-version"];
            ContainerStatus[] containerStatuses = ParseContainerStatuses(jobject["status"]["containerStatuses"] as JArray);
            ContainerStatus[] initContainerStatuses = ParseContainerStatuses(jobject["status"]["initContainerStatuses"] as JArray);
            return new Pod
            {
                Phase = phase,
                Name = name,
                DeploymentConfigLatestVersion = version,
                Containers = containerStatuses,
                InitContainers = initContainerStatuses
            };
        }

        private static ContainerStatus[] ParseContainerStatuses(JArray statuses)
        {
            if (statuses == null)
            {
                return Array.Empty<ContainerStatus>();
            }
            var containerStatuses = new List<ContainerStatus>();
            foreach (JToken containerStatus in statuses)
            {
                string name = (string)containerStatus["name"];
                JObject containerState = containerStatus["state"] as JObject;
                JProperty prop = containerState.Properties().First();
                string state = prop.Name;
                JToken childState = prop.Value;
                string reason = (string)childState["reason"];
                string message = (string)childState["message"];
                int restartCount = (int)containerStatus["restartCount"];
                bool ready = (bool)containerStatus["ready"];
                containerStatuses.Add(
                    new ContainerStatus
                    {
                        Reason = reason,
                        Message = message,
                        RestartCount = restartCount,
                        State = state,
                        Name = name,
                        Ready = ready
                    }
                );
            }
            return containerStatuses.ToArray();
        }
    }
}
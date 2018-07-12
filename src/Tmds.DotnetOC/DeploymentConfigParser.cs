using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    class DeploymentConfigParser
    {
        public static DeploymentConfig ParseDeploymentConfig(JObject deploymentConfig)
        {
            string name = (string)deploymentConfig["metadata"]["name"];
            JToken spec = deploymentConfig["spec"];
            int specReplicas = (int)spec["replicas"];
            int updatedReplicas = (int)deploymentConfig["status"]["updatedReplicas"];

            var triggerList = new List<ImageChangeTrigger>();
            foreach (var trigger in spec["triggers"])
            {
                string type = (string)trigger["type"];
                if (type != "ImageChange")
                {
                    continue;
                }
                triggerList.Add(new ImageChangeTrigger
                {
                    FromName = (string)trigger["imageChangeParams"]["from"]["name"]
                });
            }

            var labelsList = new List<string>();
            foreach ((string labelName, JToken labelValue) in (JObject)spec["template"]["metadata"]["labels"])
            {
                labelsList.Add($"{labelName}={labelValue}");
            }

            return new DeploymentConfig
            {
                Name = name,
                SpecReplicas = specReplicas,
                Triggers = triggerList.ToArray(),
                Labels = labelsList.ToArray(),
                UpdatedReplicas = updatedReplicas
            };
        }
    }
}
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    static class BuildConfigParser
    {
        public static S2iBuildConfig ParseS2iBuildConfig(JObject buildConfig)
        {
            JToken spec = buildConfig["spec"];
            if ((string)spec["source"]?["type"] != "Git")
            {
                return null;
            }
            JToken sourceStrategy = spec["strategy"]["sourceStrategy"];
            string fromName = (string)sourceStrategy["from"]["name"];
            int separator = fromName.IndexOf(':', StringComparison.InvariantCulture);
            string imageName = fromName.Substring(0, separator);
            string imageVersion = fromName.Substring(separator + 1);
            string outputName = (string)spec["output"]["to"]["name"];
            string name = (string)buildConfig["metadata"]["name"];
            JToken gitSource = spec["source"]["git"];
            string gitRef = (string)gitSource["ref"];
            string gitUri = (string)gitSource["uri"];
            var environment = new Dictionary<string, string>();
            foreach (var envVar in sourceStrategy["env"])
            {
                environment.Add((string)envVar["name"], (string)envVar["value"]);
            }
            return new S2iBuildConfig
            {
                Name = name,
                GitRef = gitRef,
                GitUri = gitUri,
                ImageVersion = imageVersion,
                ImageName = imageName,
                Environment = environment,
                OutputName = outputName
            };
        }
    }
}
using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    class ImageStreamTag
    {
        public string Version { get; set; }
        public string Image { get; set ;}
    }

    class Build
    {
        public string PodName { get; set; }
        public string StatusMessage { get; set; }
        public string Phase { get; set; }
        public string Reason { get; set; }
        public int BuildNumber { get; set; }
    }

    class Pod
    {
        public string Phase { get; set; }
    }

    class DeploymentPod
    {
        public string Phase { get; set; }
        public string Name { get; set; }
        public string DeploymentConfigLatestVersion { get; set; }
        public string Reason { get; set; }
        public string Message { get; set; }
        public int RestartCount { get; set; }
    }

    class ReplicationController
    {
        public string Phase { get; set; }
        public string Version { get; set; }
    }

    interface IOpenShift
    {
        bool IsCommunity();

        void EnsureConnection();

        ImageStreamTag[] GetImageTagVersions(string name, string ocNamespace);

        void Create(JObject content, string ocNamespace = null);

        void Replace(JObject value, string ocNamespace = null);

        void CreateImageStream(string name);

        string GetCurrentNamespace();

        Build GetBuild(string buildConfigName, int? buildNumber = null, bool mustExist = true);

        void GetLog(string podName, Action<StreamReader> reader, bool follow = false);

        Pod GetPod(string podName, bool mustExist = true);

        ReplicationController GetReplicationController(string deploymentConfigName, string version, bool mustExist = true);

        DeploymentPod[] GetDeploymentPods(string deploymentConfigName, string version);
    }
}

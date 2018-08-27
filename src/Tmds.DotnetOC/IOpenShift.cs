using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public string Commit { get; set; }
        public string CommitMessage { get; set; }
        public string ImageDigest { get; set; }
    }

    class S2iBuildConfig
    {
        public string Name { get; set; }
        public string GitRef { get; set; }
        public string GitUri { get; set; }
        public string ImageName { get; set; }
        public string ImageVersion { get; set; }
        public Dictionary<string, string> Environment { get; set; }
        public string OutputName { get; set; }
    }

    class ContainerStatus
    {
        public string Name { get; set; }
        public string State { get; set; }
        public string Reason { get; set; }
        public string Message { get; set; }
        public int RestartCount { get; set; }
        public bool Ready { get; set; }
        public DateTime? StartedAt { get; set; }
        public string ImageDigest { get; set; }
    }

    class Pod
    {
        public string Phase { get; set; }
        public string Name { get; set; }
        public string DeploymentConfigLatestVersion { get; set; }

        public ContainerStatus[] Containers { get; set; }

        public ContainerStatus[] InitContainers { get; set; }
    }

    class ReplicationController
    {
        public string Phase { get; set; }
        public string Version { get; set; }
    }

   class DeploymentConfig
    {
        public string Name { get; set; }
        public ImageChangeTrigger[] Triggers { get; set; }
        public string[] Labels { get; set; }
        public int UpdatedReplicas { get; set; }
        public int SpecReplicas { get; set; }
    }

    class ImageChangeTrigger
    {
        public string FromName { get; set; }
    }

    class Service
    {
        public string Name { get; set; }
        public string[] Selectors { get; set; }
    }

    class Route
    {
        public string Name { get; set; }
        public string Host { get; set; }
        public ServiceBackend[] Backends { get; set; }
        public bool IsTls { get; set; }
    }

    class ServiceBackend
    {
        public int Weight { get; set; }
        public string Name { get; set; }
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

        Build GetBuild(string buildConfigName, int buildNumber, bool mustExist = true);

        Build[] GetBuilds(string buildConfigName);

        Result GetLog(string podName, string container, Action<StreamReader> reader, bool follow = false, bool ignoreError = false);

        Pod GetPod(string podName, bool mustExist = true);
        ReplicationController GetReplicationController(string deploymentConfigName, string version, bool mustExist = true);

        Pod[] GetPods(string deploymentConfigName, string version);

        S2iBuildConfig[] GetS2iBuildConfigs(string imageName);

        DeploymentConfig[] GetDeploymentConfigs();

        Service[] GetServices();

        Route[] GetRoutes();

        void CreateBuilderPullSecret(string ocNamespace, string name, string server, string username, string password);

        bool HasBuilderPullSecret(string ocNamespace, string server);
    }

    class S2IDeployment
    {
        public DeploymentConfig DeploymentConfig { get; set; }
        public S2iBuildConfig BuildConfig { get; set; }
        public Service[] Services { get; set; }
        public Route[] Routes { get; set; }
    }

    static class OpenShiftExtensions
    {
        public static S2IDeployment[] GetDotnetDeployments(this IOpenShift openshift)
            => GetDotnetDeploymentsCore(openshift);

        public static S2IDeployment GetDotnetDeployment(this IOpenShift openshift, string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            S2IDeployment[] deployments = GetDotnetDeploymentsCore(openshift, name);
            if (deployments.Length == 0)
            {
                throw new FailedException("Deployment not found.");
            }
            return deployments[0];
        }

        public static S2IDeployment[] GetDotnetDeploymentsCore(this IOpenShift openshift, string deploymentName = null)
        {
            var buildConfigs = openshift.GetS2iBuildConfigs("dotnet");
            var deploymentConfigs = openshift.GetDeploymentConfigs();
            var services = openshift.GetServices();
            var routes = openshift.GetRoutes();

            var deployments = new List<S2IDeployment>();
            foreach (var deploymentConfig in deploymentConfigs)
            {
                if (deploymentName != null && deploymentConfig.Name != deploymentName)
                {
                    continue;
                }
                foreach (var trigger in deploymentConfig.Triggers)
                {
                    S2iBuildConfig buildConfig = buildConfigs.FirstOrDefault(
                        bc => bc.OutputName == trigger.FromName
                    );
                    if (buildConfig == null)
                    {
                        continue;
                    }
                    Service[] deploymentServices = services.Where(
                        svc => svc.Selectors.IsSubsetOf(deploymentConfig.Labels)
                    ).ToArray();
                    Route[] deploymentRoutes = routes.Where(
                        rt => rt.Backends.Any(be => deploymentServices.Any(ds => ds.Name == be.Name))
                    ).ToArray();
                    deployments.Add(new S2IDeployment
                    {
                        DeploymentConfig = deploymentConfig,
                        BuildConfig = buildConfig,
                        Services = deploymentServices,
                        Routes = deploymentRoutes
                    });
                }
                if (deploymentName != null && deploymentConfig.Name == deploymentName)
                {
                    break;
                }
            }

            return deployments.ToArray();
        }
    }
}

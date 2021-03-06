using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    class OCCli : IOpenShift
    {
        public void EnsureConnection()
        {
            Result result = ProcessUtils.Run("oc", "whoami");
            if (!result.IsSuccess)
            {
                throw new FailedException($"Cannot connect to OpenShift cluster: {result.ErrorMessage}");
            }
        }

        public bool IsCommunity()
        {
            ImageStreamTag[] nodejsStreamTags = GetImageTagVersions("nodejs", ocNamespace: "openshift");
            foreach (var tag in nodejsStreamTags)
            {
                if (tag.Image.Contains("registry.access.redhat.com"))
                {
                    return false;
                }
            }
            return true;
        }

        public ImageStreamTag[] GetImageTagVersions(string name, string ocNamespace)
        {
            string arguments = $"get is -o json {NamespaceArg(ocNamespace)} {name}";
            Result<JObject> result = ProcessUtils.Run<JObject>("oc", arguments);
            if (result.IsSuccess)
            {
                return ImageStreamParser.GetTags(result.Value);
            }
            else
            {
                if (result.ErrorMessage.Contains("NotFound"))
                {
                    return Array.Empty<ImageStreamTag>();
                }
                else
                {
                    throw new FailedException($"Unable to retrieve image stream tags: {result.ErrorMessage}");
                }
            }
        }

        private string NamespaceArg(string ocNamespace)
        {
            if (ocNamespace == null)
            {
                return string.Empty;
            }
            return $"--namespace {ocNamespace}";
        }

        public void Create(JObject value, string ocNamespace = null)
            => RunCommand("create", ocNamespace, value);

        public void Replace(JObject value, string ocNamespace = null)
            => RunCommand("replace", ocNamespace, value);

        private void RunCommand(string command, string ocNamespace, JObject value)
        {
            Result result = ProcessUtils.Run("oc", command + " " + NamespaceArg(ocNamespace) + " -f -", value);
            if (!result.IsSuccess)
            {
                throw new FailedException($"Unable to '{command}': {result.ErrorMessage}");
            }
        }

        public void CreateImageStream(string name)
        {
            Result result = ProcessUtils.Run("oc", $"create is {name}");
            if (!result.IsSuccess)
            {
                throw new FailedException($"Error creating image stream: {result.ErrorMessage}");
            }
        }

        public string GetCurrentNamespace()
        {
            // TODO: cache this
            Result<string> result = ProcessUtils.Run<string>("oc", "project -q");
            if (result.IsSuccess)
            {
                return result.Value.Trim();
            }
            else
            {
                throw new FailedException($"Cannot determine current project: {result.ErrorMessage}");
            }
        }

        public Build GetBuild(string buildConfigName, int buildNumber, bool mustExist)
        {
            Build[] builds = GetBuilds(buildConfigName);
            foreach (var build in builds)
            {
                if (build.BuildNumber == buildNumber)
                {
                    return build;
                }
            }
            if (mustExist)
            {
                throw new FailedException($"Cannot find build");
            }
            return null;
        }

        public Build[] GetBuilds(string buildConfigName)
        {
            Result<JObject> result = ProcessUtils.Run<JObject>("oc", $"get builds -l openshift.io/build-config.name={buildConfigName} -o json");
            if (result.IsSuccess)
            {
                var builds = new List<Build>();
                foreach (var item in result.Value["items"])
                {
                    builds.Add(BuildParser.ParseBuild(item as JObject));
                }
                return builds.ToArray();
            }
            else
            {
                throw new FailedException($"Cannot get build information: {result.ErrorMessage}");
            }
        }

        public Result GetLog(string podName, string container, Action<StreamReader> reader, bool follow, bool ignoreError)
        {
            Result result = ProcessUtils.Run("oc", $"logs {(follow ? "-f" : string.Empty)} {podName} {(string.IsNullOrEmpty(container) ? "" : "-c " + container)}", reader);
            if (!ignoreError && !result.IsSuccess)
            {
                throw new FailedException($"Cannot get pod log: {result.ErrorMessage}");
            }
            return result;
        }

        public Pod GetPod(string podName, bool mustExist)
        {
            if (string.IsNullOrEmpty(podName))
            {
                throw new ArgumentException(nameof(podName));
            }
            Result<JObject> result = ProcessUtils.Run<JObject>("oc", $"get pod {podName} -o json");
            if (result.IsSuccess)
            {
                return PodParser.ParsePod(result.Value);
            }
            else
            {
                if (!mustExist && result.ErrorMessage.Contains("NotFound"))
                {
                    return null;
                }
                throw new FailedException($"Cannot get pod information: {result.ErrorMessage}");
            }
        }

        public Pod[] GetPods(string deploymentConfigName, string version)
        {
            Result<JObject> result = ProcessUtils.Run<JObject>("oc", $"get pods -l deploymentconfig={deploymentConfigName} -o json");
            if (result.IsSuccess)
            {
                var pods = new List<Pod>();
                foreach (var item in result.Value["items"])
                {
                    var pod = PodParser.ParsePod(item as JObject);
                    if (version == null || pod.DeploymentConfigLatestVersion == version) // TODO: version == null?
                    {
                        pods.Add(pod);
                    }
                }
                return pods.ToArray();
            }
            else
            {
                throw new FailedException($"Cannot get pod information: {result.ErrorMessage}");
            }
        }

        public ReplicationController GetReplicationController(string deploymentConfigName, string version, bool mustExist)
        {
            Result<JObject> result = ProcessUtils.Run<JObject>("oc", $"get rc -l openshift.io/deployment-config.name={deploymentConfigName} -o json");
            if (result.IsSuccess)
            {
                foreach (var item in result.Value["items"])
                {
                    ReplicationController rc =  ReplicationControllerParser.Parse(item as JObject);
                    if (rc.Version == version)
                    {
                        return rc;
                    }
                }
                if (!mustExist)
                {
                    return null;
                }
                throw new FailedException("Replication controller not found");
            }
            else
            {
                if (!mustExist && result.ErrorMessage.Contains("NotFound"))
                {
                    return null;
                }
                throw new FailedException($"Cannot get replication controller: {result.ErrorMessage}");
            }
        }

        public S2iBuildConfig[] GetS2iBuildConfigs(string imageName)
        {
            Result<JObject> result = ProcessUtils.Run<JObject>("oc", $"get bc -o json");
            if (result.IsSuccess)
            {
                var buildConfigs = new List<S2iBuildConfig>();
                foreach (var item in result.Value["items"])
                {
                    var buildConfig = BuildConfigParser.ParseS2iBuildConfig(item as JObject);
                    if (buildConfig != null && buildConfig.ImageName == imageName)
                    {
                        buildConfigs.Add(buildConfig);
                    }
                }
                return buildConfigs.ToArray();
            }
            else
            {
                throw new FailedException($"Cannot get build configurations: {result.ErrorMessage}");
            }
        }

        public DeploymentConfig[] GetDeploymentConfigs()
        {
            Result<JObject> result = ProcessUtils.Run<JObject>("oc", $"get dc -o json");
            if (result.IsSuccess)
            {
                var deployConfigs = new List<DeploymentConfig>();
                foreach (var item in result.Value["items"])
                {
                    deployConfigs.Add(DeploymentConfigParser.ParseDeploymentConfig(item as JObject));
                }
                return deployConfigs.ToArray();
            }
            else
            {
                throw new FailedException($"Cannot get deployment configurations: {result.ErrorMessage}");
            }
        }

        public Service[] GetServices()
        {
            Result<JObject> result = ProcessUtils.Run<JObject>("oc", $"get svc -o json");
            if (result.IsSuccess)
            {
                var services = new List<Service>();
                foreach (var item in result.Value["items"])
                {
                    services.Add(ServiceParser.ParseService(item as JObject));
                }
                return services.ToArray();
            }
            else
            {
                throw new FailedException($"Cannot get services: {result.ErrorMessage}");
            }
        }

        public Route[] GetRoutes()
        {
            Result<JObject> result = ProcessUtils.Run<JObject>("oc", $"get route -o json");
            if (result.IsSuccess)
            {
                var routes = new List<Route>();
                foreach (var item in result.Value["items"])
                {
                    routes.Add(RouteParser.ParseRoute(item as JObject));
                }
                return routes.ToArray();
            }
            else
            {
                throw new FailedException($"Cannot get routes: {result.ErrorMessage}");
            }
        }

        public void CreateBuilderPullSecret(string ocNamespace, string name, string server, string username, string password)
        {
            Result result = ProcessUtils.Run("oc", $"create secret docker-registry {name} --namespace {ocNamespace} --docker-server {server} --docker-username={username} --docker-password {password} --docker-email=unused");
            if (!result.IsSuccess)
            {
                throw new FailedException($"Cannot create secret: {result.ErrorMessage}");
            }
            result = ProcessUtils.Run("oc", $"secret link default {name} --namespace {ocNamespace} --for=pull");
            if (!result.IsSuccess)
            {
                throw new FailedException($"Cannot link secret for pull: {result.ErrorMessage}");
            }
            result = ProcessUtils.Run("oc", $"secret link builder {name} --namespace {ocNamespace} ");
            if (!result.IsSuccess)
            {
                throw new FailedException($"Cannot link secret for builder: {result.ErrorMessage}");
            }
        }

        public bool HasBuilderPullSecret(string ocNamespace, string server)
        {
            Result<JObject> result = ProcessUtils.Run<JObject>("oc", $"get secret -o json --namespace {ocNamespace}");
            if (!result.IsSuccess)
            {
                throw new FailedException($"Cannot get secrets: {result.ErrorMessage}");
            }
            foreach (var item in result.Value["items"])
            {
                string secretType = (string)item["type"];
                string dockerCfgEncoded = null;
                if (secretType == "kubernetes.io/dockercfg")
                {
                    dockerCfgEncoded = (string)item?["data"]?[".dockercfg"];
                }
                else if (secretType == "kubernetes.io/dockerconfigjson")
                {
                    dockerCfgEncoded = (string)item?["data"]?[".dockerconfigjson"];
                }
                if (dockerCfgEncoded != null)
                {
                    string dockerCfg = Encoding.UTF8.GetString(Convert.FromBase64String(dockerCfgEncoded));
                    if (dockerCfg.Contains($"\"{server}\""))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
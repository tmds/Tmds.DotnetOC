using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    [Command]
    class AppNewCommand : CommandBase
    {
        private readonly IOpenShift _openshift;
        public AppNewCommand(IConsole console, IOpenShift openshift)
            : base(console)
        {
            _openshift = openshift;
        }

        [Option("-n|--name", CommandOptionType.SingleValue)]
        public string Name { get; }

        [Option("--git-url", CommandOptionType.SingleValue)]
        public string GitUrl { get; }

        [Option("--git-ref", CommandOptionType.SingleValue)]
        public string GitRef { get; }

        [Option("--sdk-version", CommandOptionType.SingleValue)]
        public string SdkVerison { get; }

        [Option("--memory", CommandOptionType.SingleValue)]
        public int Memory { get; } = 100;

        [Option("--startup-project", CommandOptionType.SingleValue)]
        public string StartupProject { get; }

        [Option("--runtime-version", CommandOptionType.SingleValue)]
        public string RuntimeVersion { get; }

        protected override async Task ExecuteAsync(CommandLineApplication app)
        {
            // Find the startup project file
            bool multipleProjectFiles = false;
            string startupProjectFullName = StartupProject;
            if (startupProjectFullName == null)
            {
                startupProjectFullName = ".";
            }
            if (File.Exists(startupProjectFullName))
            {
                startupProjectFullName = Path.GetFullPath(startupProjectFullName);
                multipleProjectFiles = Directory.GetFiles(Path.GetDirectoryName(startupProjectFullName), "*.??proj", SearchOption.TopDirectoryOnly).Length > 1;
            }
            else if (Directory.Exists(startupProjectFullName))
            {
                var projFiles = Directory.GetFiles(startupProjectFullName, "*.??proj", SearchOption.TopDirectoryOnly);
                if (projFiles.Length == 0)
                {
                    Fail("No projects found. Specify a '--startup-project'.");
                }
                else if (projFiles.Length > 1)
                {
                    Fail("Multiple projects found. Specify a '--startup-project'.");
                }
                else
                {
                    startupProjectFullName = Path.GetFullPath(projFiles[0]);
                    multipleProjectFiles = false;
                }
            }
            else
            {
                Fail("Startup project does not exist.");
            }

            // Find git root
            string gitRoot = GitUtils.FindRepoRoot(Path.GetDirectoryName(startupProjectFullName));
            if (gitRoot == null)
            {
                Fail("The current directory is not a git repository.");
            }

            // Determine name
            string name = Name;
            if (name == null)
            {
                name = Path.GetFileNameWithoutExtension(startupProjectFullName);
                string[] splitName = name.Split('.');
                name = splitName[splitName.Length - 1].ToLowerInvariant();
            }

            // Determine startup project
            string startupProject = startupProjectFullName;
            if (!multipleProjectFiles)
            {
                startupProject = Path.GetDirectoryName(startupProject);
            }
            startupProject = startupProject.Substring(gitRoot.Length + 1);

            // Determine git url
            string gitUrl = GitUrl;
            if (gitUrl == null)
            {
                gitUrl = GitUtils.GetRemoteUrl(gitRoot, "openshift-origin");
                if (gitUrl == null)
                {
                    gitUrl = GitUtils.GetRemoteUrl(gitRoot, "origin");
                }
            }
            if (gitUrl == null)
            {
                Fail("Cannot determine git remote url. Specify a '--git-url'.");
            }

            // Determine git ref
            string gitRef = GitRef;
            if (gitRef == null)
            {
                gitRef = GitUtils.GetCurrentBranch(gitRoot);
            }
            if (gitRef == null)
            {
                Fail("Cannot determine the current git branch. Specify '--git-ref'.");
            }

            // Determine runtime version            
            string runtimeVersion = RuntimeVersion;
            if (runtimeVersion == null)
            {
                string targetFramework = DotnetUtils.GetTargetFramework(startupProjectFullName);
                if (targetFramework != null)
                {
                    if (targetFramework.StartsWith("netcoreapp"))
                    {
                        runtimeVersion = targetFramework.Substring(10);
                    }
                }
            }
            if (runtimeVersion == null)
            {
                Fail("Cannot determine the runtime version. Specify '--runtime-version'.");
            }

            // Determine sdk version
            string sdkVersion = SdkVerison;
            if (sdkVersion == null)
            {
                sdkVersion = DotnetUtils.GetSdkVersion();
                if (sdkVersion == null)
                {
                    sdkVersion = runtimeVersion + ".0";
                }
            }

            // Determine memory
            int memory = Memory;

            PrintLine($" - name            : {name}");
            PrintLine($" - git-url         : {gitUrl}");
            PrintLine($" - git-ref         : {gitRef}");
            PrintLine($" - startup-project : {startupProject}");
            PrintLine($" - runtime-version : {runtimeVersion}");
            PrintLine($" - sdk-version     : {sdkVersion}");
            PrintLine($" - memory (MB)     : {memory}");

            _openshift.EnsureConnection();

            // Check if runtime version is installed
            Func<ImageStreamTag, bool> FindRuntimeVersion = (ImageStreamTag tag) => tag.Version == runtimeVersion;
            string imageNamespace = _openshift.GetCurrentNamespace();
            ImageStreamTag[] streamTags = _openshift.GetImageTagVersions("dotnet", imageNamespace);
            if (!streamTags.Any(FindRuntimeVersion))
            {
                imageNamespace = "openshift";
                streamTags = _openshift.GetImageTagVersions("dotnet", imageNamespace);
                if (!streamTags.Any(FindRuntimeVersion))
                {
                    Fail($"Runtime version {runtimeVersion} is not installed. You can run the 'install' command to install to the latest versions.");
                }
            }

            string imageStreamName = name;
            // Deployment config
            JObject deploymentConfig = CreateDeploymentConfig(name, imageStreamName, memory);
            System.Console.WriteLine(deploymentConfig.ToString());
            _openshift.Create(deploymentConfig);

            // Build config
            string buildConfigName = name;
            JObject buildConfig = CreateBuildConfig(buildConfigName, imageStreamName, imageNamespace, $"dotnet:{runtimeVersion}", gitUrl, gitRef, startupProject, sdkVersion);

            _openshift.CreateImageStream(imageStreamName);

            _openshift.Create(buildConfig);

            PrintLine($"Creating build pod.");
            Build build = null;
            while (true)
            {
                build = _openshift.GetLatestBuild(buildConfigName);
                if (build.Phase != "Pending")
                {
                    break;
                }
                System.Threading.Thread.Sleep(1000);
            }
            PrintLine($"Starting build on {build.PodName}.");
            bool podFound = false;
            while (true)
            {
                Pod pod = _openshift.GetPod(build.PodName, mustExist: false);
                if (pod == null || pod.Phase != "Pending")
                {
                    podFound = pod != null;
                    break;
                }
                System.Threading.Thread.Sleep(1000);
            }
            if (podFound)
            {
                PrintLine("Build log:");
                _openshift.GetLog(build.PodName, ReadToConsole, follow: true);
            }
            build = _openshift.GetLatestBuild(buildConfigName); // TODO: build number!!
            if (build.Phase != "Complete")
            {
                Fail($"The build failed: {build.Phase}({build.Reason}): {build.StatusMessage}");
            }
            PrintLine("Build finished succesfully.");

            string controllerPhase = null;
            var podStates = new Dictionary<string, string>();
            while (true)
            {
                // TODO: rc may not exist yet?
                ReplicationController controller = _openshift.GetReplicationController(name, version: "1");
                bool isDone = controller.Phase == "Complete" || controller.Phase == "Failed";
                if (controller.Phase != controllerPhase && !isDone)
                {
                    PrintLine($"Deployment is {controller.Phase}");
                }
                controllerPhase = controller.Phase;
                DeploymentPod[] pods = _openshift.GetDeploymentPods(name, version: "1");
                foreach (var pod in pods)
                {
                    string podState = pod.Phase;
                    if (!string.IsNullOrEmpty(pod.Reason) || pod.RestartCount > 0)
                    {
                        podState += "(";
                        podState += $"{pod.Reason}";
                        if (pod.RestartCount > 0)
                        {
                            if (!string.IsNullOrEmpty(pod.Reason)){
                                podState += ", ";
                            }
                            podState += $"{pod.RestartCount} restarts";
                        }
                        podState += ")";
                    }
                    if (!string.IsNullOrEmpty(pod.Message))
                    {
                        podState += $": {pod.Message}";
                    }
                    if (!podStates.TryGetValue(pod.Name, out string previousState) || previousState != podState)
                    {
                        PrintLine($"Pod {pod.Name} is {podState}");
                        podStates[pod.Name] = podState;
                        if (pod.Reason == "CrashLoopBackOff")
                        {
                            PrintLine($"{pod.Name} log:");
                            _openshift.GetLog(pod.Name, ReadToConsole);
                        }
                    }
                }

                if (isDone)
                {
                    break;
                }
            }
            if (controllerPhase == "Failed")
            {
                Fail("Deployment failed.");
            }
            else
            {
                PrintLine("Deployment finished succesfull.");
            }
        }

        private JObject CreateBuildConfig(string name, string imageStreamName, string imageNamespace, string imageTag, string gitUrl, string gitRef, string startupProject, string sdkVersion)
        {
            string content = File.ReadAllText("buildconfig.json");
            content = content.Replace("${NAME}", name);
            content = content.Replace("${IMAGE_STREAM_NAME}", imageStreamName);
            content = content.Replace("${SOURCE_REPOSITORY_URL}", gitUrl);
            content = content.Replace("${SOURCE_REPOSITORY_REF}", gitRef);
            content = content.Replace("${DOTNET_IMAGE_NAMESPACE}", imageNamespace);
            content = content.Replace("${DOTNET_IMAGE_STREAM_TAG}", imageTag);
            content = content.Replace("${DOTNET_STARTUP_PROJECT}", startupProject);
            content = content.Replace("${DOTNET_SDK_VERSION}", sdkVersion);
            return JObject.Parse(content);
        }

        private JObject CreateDeploymentConfig(string name, string imageStreamName, int memoryLimit)
        {
            string content = File.ReadAllText("deploymentconfig.json");
            content = content.Replace("${NAME}", name);
            content = content.Replace("${IMAGE_STREAM_NAME}", imageStreamName);
            content = content.Replace("${MEMORY_LIMIT}", memoryLimit.ToString());
            return JObject.Parse(content);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    [Command(Description = "Deploy an application.")]
    class AppNewCommand : CommandBase
    {
        private readonly IOpenShift _openshift;
        private readonly IS2iRepo _s2iRepo;
        public AppNewCommand(IConsole console, IOpenShift openshift, IS2iRepo s2iRepo)
            : base(console)
        {
            _openshift = openshift;
            _s2iRepo = s2iRepo;
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

        [Option("-y", "Assume yes", CommandOptionType.NoValue)]
        public bool AssumeYes { get; }

        [Argument(0, "project")]
        private string Project { get; } // TODO: try to guess this

        protected override async Task ExecuteAsync(CommandLineApplication app)
        {
            if (Project == null)
            {
                Fail("project is a required argument");
            }

            // Find the startup project file
            bool multipleProjectFiles = false;
            string startupProjectFullName = Project;
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
                Fail("The project is not in a git repository.");
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
            string startupProject;
            if (StartupProject != null)
            {
                startupProject = StartupProject;
            }
            else
            {
                startupProject =  startupProjectFullName;
                if (!multipleProjectFiles)
                {
                    startupProject = Path.GetDirectoryName(startupProject);
                }
                startupProject = startupProject.Substring(gitRoot.Length + 1);
            }

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
            if (gitRef == "HEAD")
            {
                gitRef = GitUtils.GetHeadCommitId(gitRoot);
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
                sdkVersion = string.Empty;
            }

            // Determine memory
            int memory = Memory;

            PrintLine("Creating application:");
            PrintLine($" - name            : {name}");
            PrintLine($" - git-url         : {gitUrl}");
            PrintLine($" - git-ref         : {gitRef}");
            PrintLine($" - startup-project : {startupProject}");
            PrintLine($" - runtime-version : {runtimeVersion}");
            PrintLine($" - sdk-version     : {(string.IsNullOrEmpty(sdkVersion) ? "(image default)" : sdkVersion)}");
            PrintLine($" - memory (MB)     : {memory}");

            PrintEmptyLine();

            // Prompt
            if (!AssumeYes)
            {
                PrintLine("These parameters can be overwritten by passing arguments on the command-line.");
                if (!PromptYesNo("Is this ok", defaultAnswer: false))
                {
                    return;
                }

                PrintEmptyLine();
            }

            PrintLine($"Checking .NET Core {runtimeVersion} is installed on OpenShift...");
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
            else
            {
                PrintLine($".NET Core {runtimeVersion} is installed.");
            }
            PrintEmptyLine();

            Print("Creating deployment config...");
            string imageStreamName = name;
            // Deployment config
            JObject deploymentConfig = CreateDeploymentConfig(name, imageStreamName, memory);
            _openshift.Create(deploymentConfig);
            PrintLine("done");

            // Build config
            Print("Creating build config...");
            string buildConfigName = name;
            JObject buildConfig = CreateBuildConfig(buildConfigName, imageStreamName, imageNamespace, $"dotnet:{runtimeVersion}", gitUrl, gitRef, startupProject, sdkVersion);
            _openshift.CreateImageStream(imageStreamName);
            _openshift.Create(buildConfig);
            PrintLine("done");

            PrintEmptyLine();

            // Wait for build
            PrintLine($"Waiting for build to start...");
            Build build = null;
            while (true)
            {
                build = _openshift.GetBuild(buildConfigName, buildNumber: 1, mustExist: false);
                if (build != null && build.Phase == "New" && string.IsNullOrEmpty(build.Reason))
                {
                    build = null;
                }
                if (build != null)
                {
                    break;
                }
                System.Threading.Thread.Sleep(1000);
            }

            if (build.Phase == "New")
            {
                Fail($"Cannot create build: {build.Reason}: {build.StatusMessage}");
            }

            // Wait for build
            PrintLine($"Waiting for build pod {build.PodName} to run...");
            while (true)
            {
                Pod pod = _openshift.GetPod(build.PodName, mustExist: false);
                if (pod != null)
                {
                    break;
                }
                System.Threading.Thread.Sleep(1000);
            }

            // Print build log
            PrintPodBuildLog(build.PodName);

            // Check build status
            build = _openshift.GetBuild(buildConfigName, buildNumber: build.BuildNumber);
            if (build.Phase != "Complete")
            {
                Fail($"The build failed: {build.Phase}({build.Reason}): {build.StatusMessage}");
            }
            PrintLine("Build finished succesfully.");
            PrintEmptyLine();

            // Follow up on deployment
            PrintLine("Deployment status:");
            string controllerPhase = null;
            var podStates = new Dictionary<string, string>();
            while (true)
            {
                System.Threading.Thread.Sleep(1000); // TODO: this next call sometimes blocks ?!? :/
                ReplicationController controller = _openshift.GetReplicationController(name, version: "1", mustExist: false);
                if (controller != null)
                {
                    controllerPhase = controller.Phase;
                    Pod[] pods = _openshift.GetPods(name, version: "1");
                    foreach (var pod in pods)
                    {
                        if (pod.Containers.Length == 0)
                        {
                            continue;
                        }
                        ContainerStatus container = pod.Containers[0]; // pods have 1 container for the dotnet application
                        string containerState = null;
                        // user-friendly states:
                        if (container.State == "running")
                        {
                            if (container.Ready)
                            {
                                containerState = "is ready.";
                            }
                            else
                            {
                                containerState = "has started, not ready.";
                            }
                        }
                        else if (container.State == "waiting")
                        {
                            if (container.Reason == "ContainerCreating")
                            {
                                containerState = "is being created.";
                            }
                            else if (container.Reason == "CrashLoopBackOff")
                            {
                                containerState = $"is crashing: ${container.Message}";
                            }
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(container.Message))
                            {
                                if (container.Reason == "Error" ||
                                    container.Reason == "Completed" ||
                                    string.IsNullOrEmpty(container.Reason))
                                {
                                    containerState = "has terminated.";
                                }
                            }
                        }
                        // fallback:
                        if (containerState == null)
                        {
                            containerState = $"is {container.State}(reason={container.Reason}): {container.Message}";
                        }

                        // Check if podState changed
                        if (!podStates.TryGetValue(pod.Name, out string previousState) || previousState != containerState)
                        {
                            PrintLine($"Pod {pod.Name} container {containerState}");
                            podStates[pod.Name] = containerState;

                            // Print pod log when it terminated
                            // or when we see CrashLoopBackOff and missed the terminated state.
                            if (container.State == "terminated" ||
                                (container.Reason == "CrashLoopBackOff" && (previousState == null || !previousState.Contains("terminated")))) // TODO: improve terminated check
                            {
                                PrintLine($"{pod.Name} log:");
                                _openshift.GetLog(pod.Name, container.Name, ReadToConsole);
                            }
                        }
                    }

                    if (controllerPhase == "Complete" || controllerPhase == "Failed")
                    {
                        break;
                    }
                }
                System.Threading.Thread.Sleep(1000);
            }
            if (controllerPhase == "Failed")
            {
                Fail("Deployment failed.");
            }
            else
            {
                PrintLine("Deployment finished succesfully.");
            }
            PrintEmptyLine();

            // Service
            Print("Creating service...");
            JObject service = CreateService(name);
            _openshift.Create(service);
            PrintLine("done");
            PrintEmptyLine();

            PrintLine("Application created succesfully.");
        }

        private void PrintPodBuildLog(string podName)
        {
            bool printedBuildLog = false;
            string previousContainer = null;
            while (true)
            {
                Pod pod = _openshift.GetPod(podName);
                bool useNext = previousContainer == null;
                ContainerStatus nextContainer = null;
                foreach (var cont in pod.InitContainers)
                {
                    if (useNext)
                    {
                        nextContainer = cont;
                        break;
                    }
                    useNext = cont.Name == previousContainer;
                }
                if (nextContainer == null)
                {
                    foreach (var cont in pod.Containers)
                    {
                        if (useNext)
                        {
                            nextContainer = cont;
                            break;
                        }
                        useNext = cont.Name == previousContainer;
                    }
                }
                if (nextContainer == null)
                {
                    if (pod.Phase != "Pending")
                    {
                        break;
                    }
                    System.Threading.Thread.Sleep(1000);
                }
                else
                {
                    if (nextContainer.State == "running" || nextContainer.State == "terminated")
                    {
                        if (!printedBuildLog)
                        {
                            PrintLine("Build log:");
                            printedBuildLog = true;
                        }
                        Result result = _openshift.GetLog(podName, nextContainer.Name, ReadToConsole, follow: true, ignoreError: true);
                        if (!result.IsSuccess)
                        {
                            PrintLine($"Container {nextContainer.Name} is {nextContainer.Reason}: {nextContainer.Message}");
                        }
                        previousContainer = nextContainer.Name;
                    }
                    else // nextContainer.State == "waiting"
                    {
                        if (pod.Phase != "Pending" && pod.Phase != "Running")
                        {
                            break;
                        }
                        System.Threading.Thread.Sleep(1000);
                    }
                }
            }
        }

        // TODO: remove
        private string FollowPodState(string podName)
        {
                        string previousDescription = null;
            while (true)
            {
                Pod pod = _openshift.GetPod(podName, mustExist: false);
                string newDescr = Describe(pod);
                if (previousDescription != newDescr)
                {
                    System.Console.WriteLine(newDescr);
                    previousDescription = newDescr;
                }
            }
        }

        // TODO: remove
        private string Describe(Pod pod)
        {
            string s = string.Empty;
            s += $"Phase: {pod.Phase}";
            foreach (var container in pod.InitContainers)
            {
                s+= $" i {container.Name}: {container.State} {container.Reason} {container.Message} {container.RestartCount}";
            }
            foreach (var container in pod.Containers)
            {
                s+= $" c {container.Name}: {container.State} {container.Reason} {container.Message} {container.RestartCount}";
            }
            return s;
        }

        private JObject CreateBuildConfig(string name, string imageStreamName, string imageNamespace, string imageTag, string gitUrl, string gitRef, string startupProject, string sdkVersion)
        {
            string content = File.ReadAllText(PathUtils.ApplicationPath("buildconfig.json"));
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
            string content = File.ReadAllText(PathUtils.ApplicationPath("deploymentconfig.json"));
            content = content.Replace("${NAME}", name);
            content = content.Replace("${IMAGE_STREAM_NAME}", imageStreamName);
            content = content.Replace("${MEMORY_LIMIT}", memoryLimit.ToString());
            return JObject.Parse(content);
        }

        private JObject CreateService(string name)
        {
            string content = File.ReadAllText(PathUtils.ApplicationPath("service.json"));
            content = content.Replace("${NAME}", name);
            return JObject.Parse(content);
        }
    }
}
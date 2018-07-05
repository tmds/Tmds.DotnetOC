using System;
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

        [Option("-n|--name", CommandOptionType.SingleOrNoValue)]
        public string Name { get; }

        [Option("--git-url", CommandOptionType.SingleOrNoValue)]
        public string GitUrl { get; }

        [Option("--git-ref", CommandOptionType.SingleOrNoValue)]
        public string GitRef { get; }

        [Option("--sdk-version", CommandOptionType.SingleOrNoValue)]
        public string SdkVerison { get; }

        [Option("--memory", CommandOptionType.SingleOrNoValue)]
        public int Memory { get; } = 100;

        [Option("--startup-project", CommandOptionType.SingleOrNoValue)]
        public string StartupProject { get; }

        [Option("--runtime-version", CommandOptionType.SingleOrNoValue)]
        public string RuntimeVersion { get; }

        private void DetermineStartupProject(out string startupProjectFullName, out bool multipleProjectFiles)
        {
            multipleProjectFiles = false;
            startupProjectFullName = StartupProject;
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
        }

        protected override async Task ExecuteAsync(CommandLineApplication app)
        {
            // Find the startup project file
            DetermineStartupProject(out string startupProjectFullName, out bool multipleProjectFiles);

            // Find git root
            string gitRoot = GitUtils.FindRepoRoot();
            if (gitRoot == null)
            {
                Fail("The current directory is not a git repository.");
            }

            // Verify the startup project is under the git root
            if (!startupProjectFullName.StartsWith(gitRoot) && startupProjectFullName[gitRoot.Length] == Path.DirectorySeparatorChar)
            {
                Fail("The startup project is not in the current git repository.");
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
                gitUrl = GitUtils.GetRemoteUrl("openshift-origin");
                if (gitUrl == null)
                {
                    gitUrl = GitUtils.GetRemoteUrl("origin");
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
                gitRef = GitUtils.GetCurrentBranch();
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
            JObject buildConfig = CreateBuildConfig(name, imageStreamName, imageNamespace, $"dotnet:{runtimeVersion}", gitUrl, gitRef, startupProject, sdkVersion);
            Console.WriteLine(buildConfig);

            _openshift.CreateImageStream(name);

            _openshift.Create(buildConfig);
        }

        private JObject CreateBuildConfig(string name, string imageStreamName, string imageNamespace, string imageTag, string gitUrl, string gitRef, string startupProject, string sdkVersion)
        {
            string content = File.ReadAllText("buildconfig.json");
            content = content.Replace("${NAME}", name);
            content = content.Replace("${IMAGE_STREAM_NAME}", name);
            content = content.Replace("${SOURCE_REPOSITORY_URL}", gitUrl);
            content = content.Replace("${SOURCE_REPOSITORY_REF}", gitRef);
            content = content.Replace("${DOTNET_IMAGE_NAMESPACE}", imageNamespace);
            content = content.Replace("${DOTNET_IMAGE_STREAM_TAG}", imageTag);
            content = content.Replace("${DOTNET_STARTUP_PROJECT}", startupProject);
            content = content.Replace("${DOTNET_SDK_VERSION}", sdkVersion);
            return JObject.Parse(content);
        }
    }
}
using System.IO;
using McMaster.Extensions.CommandLineUtils;

namespace Tmds.DotnetOC
{
    [Command]
    class AppNewCommand
    {
        private readonly IConsole _console;
        public AppNewCommand(IConsole console)
        {
            _console = console;
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

        private bool DetermineStartupProject(out string startupProjectFullName, out bool multipleProjectFiles)
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
                    _console.WriteErrorLine("No projects found. Specify a '--startup-project'.");
                    return false;
                }
                else if (projFiles.Length > 1)
                {
                    _console.WriteErrorLine("Multiple projects found. Specify a '--startup-project'.");
                    return false;
                }
                else
                {
                    System.Console.WriteLine($"Found project: {projFiles[0]}");
                    startupProjectFullName = Path.GetFullPath(projFiles[0]);
                    multipleProjectFiles = false;
                }
            }
            else
            {
                _console.WriteErrorLine("Startup project does not exist.");
                return false;
            }
            return true;
        }

        int OnExecute(CommandLineApplication app)
        {
            // Find the startup project file
            if (!DetermineStartupProject(out string startupProjectFullName, out bool multipleProjectFiles))
            {
                return 1;
            }

            // Find git root
            string gitRoot = GitUtils.FindRepoRoot();
            System.Console.WriteLine($"gitRoot: {gitRoot}");
            if (gitRoot == null)
            {
                _console.WriteErrorLine("The current directory is not a git repository.");
                return 1;
            }

            // Verify the startup project is under the git root
            if (!startupProjectFullName.StartsWith(gitRoot) && startupProjectFullName[gitRoot.Length] == Path.DirectorySeparatorChar)
            {
                _console.WriteErrorLine("The startup project is not in the current git repository.");
                return 1;
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
                _console.WriteErrorLine("Cannot determine git remote url. Specify a '--git-url'.");
                return 1;
            }

            // Determine git ref
            string gitRef = GitRef;
            if (gitRef == null)
            {
                gitRef = GitUtils.GetCurrentBranch();
            }
            if (gitRef == null)
            {
                _console.WriteErrorLine("Cannot determine the current git branch. Specify '--git-ref'.");
                return 1;
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
                _console.WriteErrorLine("Cannot determine the runtime version. Specify '--runtime-version'.");
                return 1;
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

            System.Console.WriteLine($" - name            : {name}");
            System.Console.WriteLine($" - git-url         : {gitUrl}");
            System.Console.WriteLine($" - git-ref         : {gitRef}");
            System.Console.WriteLine($" - startup-project : {startupProject}");
            System.Console.WriteLine($" - runtime-version : {runtimeVersion}");
            System.Console.WriteLine($" - sdk-version     : {sdkVersion}");
            System.Console.WriteLine($" - memory (MB)     : {memory}");
            return 0;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    [Command(Description = "Install/Update .NET Core support on OpenShift.")]
    class InstallCommand
    {
        private readonly IConsole _console;
        private readonly IOpenShift _openshift;
        private readonly IS2iRepo _s2iRepo;

        [Option("-f|--force", Description = "Force installation.")]
        public bool Force { get; }

        [Option("-g|--global", Description = "Install system-wide.")]
        public bool Global { get; }

        public InstallCommand(IConsole console, IOpenShift openshift, IS2iRepo s2iRepo)
        {
            _console = console;
            _openshift = openshift;
            _s2iRepo = s2iRepo;
        }

        async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            // Check if we can connect to OpenShift
            if (_openshift.CheckDependencies().CheckFailed(_console)
             || _openshift.CheckConnection().CheckFailed(_console))
            {
                return 1;
            }

            // Check if this is a community or RH supported version
            if (_openshift.IsCommunity()
                    .CheckFailed(_console, out bool community))
            {
                return 1;
            }

            // Retrieve installed versions
            _console.Write("Retrieving installed versions: ");
            string ocNamespace = Global ? "openshift" : null;
            if (_openshift.GetImageTagVersions("dotnet", ocNamespace: ocNamespace)
                        .CheckFailed(_console, out ImageStreamTag[] namespaceStreamTags))
            {
                return 1;
            }
            string[] namespaceVersions = namespaceStreamTags.Select(t => t.Version).ToArray();
            VersionStringSorter.Sort(namespaceVersions);
            _console.WriteLine(string.Join(", ", namespaceVersions));

            // Retrieve latest versions
            _console.Write("Retrieving latest versions   : ");
            if (_s2iRepo.GetImageStreams(community)
                        .CheckFailed(_console, out string s2iImageStreams))
            {
                return 1;
            }
            string[] s2iVersions = ImageStreamListParser.GetTags(JObject.Parse(s2iImageStreams), "dotnet");
            VersionStringSorter.Sort(s2iVersions);
            _console.WriteLine(string.Join(", ", s2iVersions));

            _console.EmptyLine();

            // Compare installed and latest versions
            if (!Force)
            {
                IEnumerable<string> newVersions = s2iVersions.Except(namespaceVersions);
                IEnumerable<string> removedVersions = namespaceVersions.Except(s2iVersions);
                if (removedVersions.Any())
                {
                    _console.WriteErrorLine("Namespace has unknown versions. Use '--force' to overwrite.");
                    return 1;
                }
                if (!newVersions.Any())
                {
                    _console.WriteLine("Already up-to-date. Use '--force' to sync all metadata.");
                    return 0;
                }
            }

            // Update installed versions
            if (_openshift.Create(exists: namespaceVersions.Length != 0, content: s2iImageStreams, ocNamespace: ocNamespace)
                          .CheckFailed(_console))
            {
                return 1;
            }
            _console.WriteLine("Succesfully updated.");
            return 0;
        }
    }
}
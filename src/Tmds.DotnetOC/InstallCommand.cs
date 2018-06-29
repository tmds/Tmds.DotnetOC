using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    class InstallCommand
    {
        private readonly IConsole _console;
        private readonly IOpenShift _openshift;
        private readonly IS2iRepo _s2iRepo;

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

            bool community = false; // TODO, handle CentOS (community)
            bool openshift = false; // TODO, handle openshift namespace

            // Retrieve installed versions
            _console.Write("Retrieving installed versions: ");
            if (_openshift.GetImageTagVersions("dotnet", ocNamespace: openshift ? "openshift" : null)
                          .CheckFailed(_console, out string[] namespaceVersions))
            {
                return 1;
            }
            _console.WriteLine(string.Join(", ", namespaceVersions)); // TODO: order versions

            // Retrieve latest versions
            _console.Write("Retrieving latest versions   : ");
            if (_s2iRepo.GetImageStreams(community)
                        .CheckFailed(_console, out string s2iImageStreams))
            {
                return 1;
            }
            string[] s2iVersions = ImageStreamListParser.GetTags(JObject.Parse(s2iImageStreams), "dotnet");
            _console.WriteLine(string.Join(", ", s2iVersions)); // TODO: order versions

            _console.EmptyLine();

            // Compare installed and latest versions
            IEnumerable<string> newVersions = s2iVersions.Except(namespaceVersions);
            IEnumerable<string> removedVersions = namespaceVersions.Except(s2iVersions);
            if (removedVersions.Any()) // TODO, Add force flag
            {
                _console.WriteErrorLine("namespace has unknown versions.");
                return 1;
            }
            if (!newVersions.Any()) // TODO, Add force flag
            {
                _console.WriteLine("System already up-to-date.");
                return 0;
            }

            // Update installed versions
            if (_openshift.Create(exists: namespaceVersions.Length != 0, content: s2iImageStreams)
                          .CheckFailed(_console))
            {
                return 1;
            }
            _console.WriteLine("Succesfully updated.");
            return 0;
        }
    }
}
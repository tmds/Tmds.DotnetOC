using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    [Command(Description = "Install/Update .NET Core support on OpenShift.")]
    class InstallCommand : CommandBase
    {
        private readonly IConsole _console;
        private readonly IOpenShift _openshift;
        private readonly IS2iRepo _s2iRepo;

        [Option("-f|--force", Description = "Force installation.")]
        public bool Force { get; }

        [Option("-g|--global", Description = "Install system-wide.")]
        public bool Global { get; }

        public InstallCommand(IConsole console, IOpenShift openshift, IS2iRepo s2iRepo)
            : base(console)
        {
            _console = console;
            _openshift = openshift;
            _s2iRepo = s2iRepo;
        }

        protected override async Task ExecuteAsync(CommandLineApplication app)
        {
            _openshift.EnsureConnection();

            // Check if this is a community or RH supported version
            bool community = _openshift.IsCommunity();

            // Retrieve installed versions
            Print("Retrieving installed versions: ");
            string ocNamespace = Global ? "openshift" : null;
            ImageStreamTag[] namespaceStreamTags = _openshift.GetImageTagVersions("dotnet", ocNamespace: ocNamespace);
            string[] namespaceVersions = namespaceStreamTags.Select(t => t.Version).ToArray();
            VersionStringSorter.Sort(namespaceVersions);
            PrintLine(string.Join(", ", namespaceVersions));

            // Retrieve latest versions
            Print("Retrieving latest versions   : ");
            JObject s2iImageStreams = _s2iRepo.GetImageStreams(community);
            string[] s2iVersions = ImageStreamListParser.GetTags(s2iImageStreams, "dotnet");
            VersionStringSorter.Sort(s2iVersions);
            PrintLine(string.Join(", ", s2iVersions));

            _console.EmptyLine();

            // Compare installed and latest versions
            if (!Force)
            {
                IEnumerable<string> newVersions = s2iVersions.Except(namespaceVersions);
                IEnumerable<string> removedVersions = namespaceVersions.Except(s2iVersions);
                if (removedVersions.Any())
                {
                    Fail("Namespace has unknown versions. Use '--force' to overwrite.");
                }
                if (!newVersions.Any())
                {
                    PrintLine("Already up-to-date. Use '--force' to sync all metadata.");
                    return;
                }
            }

            if (namespaceVersions.Length != 0)
            {
                _openshift.Replace(s2iImageStreams, ocNamespace);
            }
            else
            {
                _openshift.Create(s2iImageStreams, ocNamespace);
            }

            PrintLine("Succesfully updated.");
        }
    }
}
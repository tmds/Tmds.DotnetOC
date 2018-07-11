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

            var installOperations = new InstallOperations(_openshift, _s2iRepo);

            string ocNamespace = Global ? "openshift" : null;

            // Retrieve installed versions
            Print("Retrieving installed versions: ");
            string[] namespaceVersions = installOperations.GetInstalledVersions(ocNamespace);
            PrintLine(string.Join(", ", namespaceVersions));

            // Retrieve latest versions
            Print("Retrieving latest versions   : ");
            string[] s2iVersions = installOperations.GetLatestVersions(community);
            PrintLine(string.Join(", ", s2iVersions));

            PrintEmptyLine();

            // Compare installed and latest versions
            if (!Force)
            {
                VersionCheckResult result = installOperations.CompareVersions(community, ocNamespace);
                if (result == VersionCheckResult.UnknownVersions)
                {
                    Fail("Namespace has unknown versions. Use '--force' to overwrite.");
                }
                else if (result == VersionCheckResult.AlreadyUpToDate)
                {
                    PrintLine("Already up-to-date. Use '--force' to sync all metadata.");
                }
            }

            installOperations.UpdateToLatest(community, ocNamespace);

            PrintLine("Succesfully updated.");
        }
    }
}
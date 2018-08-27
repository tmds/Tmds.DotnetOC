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

        [Option("-u|--username", CommandOptionType.SingleValue)]
        public string Username { get; }

        [Option("-p|--password", CommandOptionType.SingleValue)]
        public string Password { get; }

        [Option("-c|--community", Description = "Install CentOS images.")]
        public bool Community { get; }

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
            bool community = Community;

            var installOperations = new InstallOperations(_openshift, _s2iRepo);

            string ocNamespace = Global ? "openshift" : _openshift.GetCurrentNamespace();

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

            if (!community)
            {
                PrintLine("Checking registry.redhat.io are present...");
                bool isSecretSetup = _openshift.HasBuilderPullSecret(ocNamespace, "registry.redhat.io");
                if (!isSecretSetup)
                {
                    PrintLine("No credentials for registry.redhat.io are found.");
                    if (Username == null || Password == null)
                    {
                        Fail(@"Specify 'username' and 'password' arguments to configure authentication with registry.redhat.io.
You can verify your credentials using 'docker login redhat.registry.io'.
For more info see: https://access.redhat.com/RegistryAuthentication.
Alternatively, you can install CentOS based images by passing the 'community' argument.");
                    }
                    _openshift.CreateBuilderPullSecret(ocNamespace, "redhat-registry", "registry.redhat.io", Username, Password);
                    PrintLine("A secret for registry.redhat.io has been added.");
                }
                else
                {
                    PrintLine("A secret for registry.redhat.io is already present.");
                }
            }

            installOperations.UpdateToLatest(community, ocNamespace);

            PrintLine("Succesfully updated.");
        }
    }
}
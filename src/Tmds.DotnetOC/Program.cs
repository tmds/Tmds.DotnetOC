using System;
using Microsoft.Extensions.DependencyInjection;
using McMaster.Extensions.CommandLineUtils;

namespace Tmds.DotnetOC
{
    [Command(Name = "dotnet-oc")]
    [Subcommand("install", typeof(InstallCommand))]
    [Subcommand("app", typeof(AppCommand))]
    class Program
    {
        public static int Main(string[] args)
        {
            var services = new ServiceCollection()
                .AddSingleton<IConsole, PhysicalConsole>()
                .AddSingleton<IOpenShift, OCCli>()
                .AddSingleton<IS2iRepo, GithubS2iRepo>()
                .BuildServiceProvider();

            var app = new CommandLineApplication<Program>();
            app.Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(services);
            return app.Execute(args);
        }

        int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 0;
        }
    }
}

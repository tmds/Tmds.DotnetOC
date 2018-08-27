using McMaster.Extensions.CommandLineUtils;

namespace Tmds.DotnetOC
{
    [Command(Description = "Deploy and check .NET Core applications.")]
    [Subcommand("new", typeof(AppNewCommand))]
    [Subcommand("list", typeof(AppListCommand))]
    [Subcommand("status", typeof(AppStatusCommand))]
    class AppCommand
    {
        int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 0;
        }
    }
}
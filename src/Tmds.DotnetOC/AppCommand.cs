using McMaster.Extensions.CommandLineUtils;

namespace Tmds.DotnetOC
{
    [Command(Description = "Deploy/update/remove .NET Core applications.")]
    [Subcommand("new", typeof(AppNewCommand))]
    class AppCommand
    {
        int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 0;
        }
    }
}
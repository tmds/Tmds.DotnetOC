using System;
using McMaster.Extensions.CommandLineUtils;

namespace Tmds.DotnetOC
{
    [Command(Name = "dotnet-oc")]
    class Program
    {
        public static int Main(string[] args)
            => CommandLineApplication.Execute<Program>(args);

        int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 0;
        }
    }
}

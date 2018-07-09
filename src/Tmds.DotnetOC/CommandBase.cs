using System;
using System.IO;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace Tmds.DotnetOC
{
    abstract class CommandBase
    {
        private readonly IConsole _console;

        protected Action<StreamReader> ReadToConsole
        {
            get
            {
                return reader => {
                    while (true)
                    {
                        string line = reader.ReadLine();
                        if (line != null)
                        {
                            PrintLine(line);
                        }
                        else
                        {
                            break;
                        }
                    };
                };
            }
        }

        protected void Print(string msg)
        {
            _console.Write(msg);
        }

        protected void PrintLine(string msg)
        {
            _console.WriteLine(msg);
        }

        protected void PrintEmptyLine() => PrintLine(string.Empty);

        protected bool PromptYesNo(string prompt, bool defaultAnswer)
        {
            return _console.GetYesNo(prompt, defaultAnswer);
        }

        public CommandBase(IConsole console)
        {
            _console = console;
        }

        protected void Fail(string message)
        {
            throw new FailedException(message);
        }

        protected async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            try
            {
                await ExecuteAsync(app);
                return 0;
            }
            catch (FailedException fe)
            {
                _console.WriteErrorLine(fe.Message);
            }
            catch (Exception e)
            {
                _console.WriteErrorLine($"Unhandled exception: {e.ToString()}");
            }
            return 1;
        }

        protected abstract Task ExecuteAsync(CommandLineApplication app);
    }
}
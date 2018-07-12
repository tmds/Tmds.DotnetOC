using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

        protected void PrintTable<T>(string[] columns, List<T> values, Func<string, T, string> rowValue)
        {
            int rowCount = values.Count + 1;
            string[,] cellStrings = new string[columns.Length, rowCount];
            for (int i = 0; i < columns.Length; i++)
            {
                cellStrings[i, 0] = columns[i];
                for (int j = 0; j < values.Count; j++)
                {
                    cellStrings[i, j + 1] = rowValue(columns[i], values[j]);
                }
            }
            var columnWidths = new int[columns.Length];
            for (int i = 0; i < columns.Length; i++)
            {
                int width = 0;
                for (int j = 0; j < rowCount; j++)
                {
                    width = Math.Max(cellStrings[i, j].Length + 2, width);
                }
                columnWidths[i] = width;
            }
            StringBuilder line = new StringBuilder();
            for (int j = 0; j < rowCount; j++)
            {
                for (int i = 0; i < columns.Length; i++)
                {
                    string cellString = cellStrings[i, j];
                    line.AppendFormat(cellString);
                    line.Append(' ', columnWidths[i] - cellString.Length);
                }
                PrintLine(line.ToString());
                line.Clear();
            }
        }

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
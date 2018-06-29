using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Tmds.DotnetOC
{
    static class ProcessUtils
    {
        public static Result Run(string filename, string arguments, string stdin = null)
        {
            return RunAsync(filename, arguments, stdin).GetAwaiter().GetResult();
        }

        public static Task<Result> RunAsync(string filename, string arguments, string stdin)
        {
            var tcs = new TaskCompletionSource<Result>();
            Process process = null;
            try
            {
                process = new Process();
                process.StartInfo.FileName = filename;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardInput = true;
                process.EnableRaisingEvents = true;
                StringBuilder sbOut = new StringBuilder();
                StringBuilder sbError = null;
                process.OutputDataReceived += (_, e) =>
                {
                    sbOut.Append(e.Data);
                };
                process.ErrorDataReceived += (_, e) =>
                {
                    if (sbError == null)
                    {
                        sbError = new StringBuilder();
                    }
                    sbError.Append(e.Data);
                };
                process.Exited += (_, e) =>
                {
                    var processResult = new Result
                    (
                        process.ExitCode == 0,
                        process.ExitCode == 0 ? sbOut.ToString() : sbError?.ToString()
                    );
                    tcs.SetResult(processResult);
                };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                if (stdin != null)
                {
                    process.StandardInput.Write(stdin);
                }
                process.StandardInput.Close();
                return tcs.Task;
            }
            catch (Exception e)
            {
                process?.Dispose();
                tcs.SetException(e);
                return tcs.Task;
            }
        }

        private static string[] s_splitPath = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(':');
        public static Result ExistsOnPath(string program)
        {
            foreach (var pathDir in s_splitPath)
            {
                if (File.Exists(Path.Combine(pathDir, program)))
                {
                    return Result.Success();
                }
            }
            return Result.Error($"The '{program}' binary cannot be found on PATH");
        }
    }
}
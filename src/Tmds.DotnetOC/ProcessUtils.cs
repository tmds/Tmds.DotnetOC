using System;
using System.Diagnostics;
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
                    {
                        IsSuccess = process.ExitCode == 0,
                        Content = process.ExitCode == 0 ? sbOut.ToString() : (sbError?.ToString() ?? "Unknown error")
                    };
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
    }
}
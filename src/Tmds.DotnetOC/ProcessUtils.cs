using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    static class ProcessUtils
    {
        private class VoidType { }

        public static Result Run(string filename, string arguments, JObject input)
        {
            return Run(filename, arguments,
                        streamWriter =>
                        {
                            if (input != null)
                            {
                                using (var writer = new JsonTextWriter(streamWriter))
                                {
                                    input.WriteTo(writer);
                                }
                            }
                        });
        }

        public static Result<JObject> RunAndGetJSon(string filename, string arguments)
        {
            return Run<JObject>(filename, arguments, _ => JObject.Load(new JsonTextReader(new StreamReader(_.BaseStream))), null);
        }

        public static Result<string> RunAndGetString(string filename, string arguments)
        {
            return Run<string>(filename, arguments, _ => _.ReadToEnd(), null);
        }

        public static Result Run(string filename, string arguments, Action<StreamWriter> writeInput = null)
        {
            return RunAsync<VoidType>(filename, arguments, _ => null, writeInput).GetAwaiter().GetResult();
        }

        public static Result<T> Run<T>(string filename, string arguments, Func<StreamReader, T> readOutput, Action<StreamWriter> writeInput = null)
        {
            return RunAsync(filename, arguments, readOutput, writeInput).GetAwaiter().GetResult();
        }

        public static Task<Result<T>> RunAsync<T>(string filename, string arguments, Func<StreamReader, T> readOutput, Action<StreamWriter> writeInput)
        {
            var tcs = new TaskCompletionSource<Result<T>>();
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
                    Result<T> retval;
                    if (process.ExitCode == 0)
                    {
                        retval = Result<T>.Success(readOutput(process.StandardOutput));
                    }
                    else
                    {
                        retval = Result<T>.Error(sbError?.ToString() ?? $"exit code: {process.ExitCode}");
                    }
                    tcs.SetResult(retval);
                };
                process.Start();
                process.BeginErrorReadLine();
                writeInput?.Invoke(process.StandardInput);
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
        public static bool ExistsOnPath(string program)
        {
            foreach (var pathDir in s_splitPath)
            {
                if (File.Exists(Path.Combine(pathDir, program)))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
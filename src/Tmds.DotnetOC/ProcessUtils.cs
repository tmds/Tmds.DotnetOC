using System;
using System.ComponentModel;
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
        sealed class VoidType {
            public static readonly VoidType Instance = new VoidType();
        };

        private static Func<StreamReader, JObject> s_JObjectReader = _ => JObject.Load(new JsonTextReader(new StreamReader(_.BaseStream)));
        private static Func<StreamReader, string> s_StringReader = _ => _.ReadToEnd();

        public static Result<TOut> Run<TOut>(string filename, string arguments)
            => Run<VoidType, TOut>(filename, arguments, VoidType.Instance);

        public static Result Run<TIn>(string filename, string arguments, TIn input)
            => Run<TIn, VoidType>(filename, arguments, input);

        public static Result<TOut> Run<TIn, TOut>(string filename, string arguments, TIn input)
        {
            Func<StreamReader, TOut> readOutput;

            if (typeof(TOut) == typeof(JObject))
            {
                readOutput = (Func<StreamReader, TOut>)(object)s_JObjectReader;
            }
            else if (typeof(TOut) == typeof(string))
            {
                readOutput = (Func<StreamReader, TOut>)(object)s_StringReader;
            }
            else if (typeof(TOut) == typeof(VoidType))
            {
                readOutput = null;
            }
            else
            {
                throw new NotSupportedException($"Cannot read type {typeof(TOut).FullName}");
            }

            Action<StreamWriter>  writeInput;
            if (typeof(TIn) == typeof(JObject))
            {
                writeInput = streamWriter => {
                    using (var writer = new JsonTextWriter(streamWriter))
                    {
                        ((JObject)(object)input)?.WriteTo(writer); // TODO: get rid of Elvis
                    }
                };
            }
            else if (typeof(TIn) == typeof(string))
            {
                writeInput = writer => writer.Write(input.ToString());
            }
            else if (typeof(TIn) == typeof(VoidType))
            {
                writeInput = null;
            }
            else
            {
                throw new NotSupportedException($"Cannot write type {typeof(TIn).FullName}");
            }

            return Run(filename, arguments, readOutput, writeInput);
        }

        private static Result<T> Run<T>(string filename, string arguments, Func<StreamReader, T> readOutput, Action<StreamWriter> writeInput = null)
        {
            return RunAsync(filename, arguments, readOutput, writeInput).GetAwaiter().GetResult();
        }

        private static Task<Result<T>> RunAsync<T>(string filename, string arguments, Func<StreamReader, T> readOutput, Action<StreamWriter> writeInput)
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
                        if (readOutput != null)
                        {
                            retval = Result<T>.Success(readOutput(process.StandardOutput));
                        }
                        else
                        {
                            retval = Result<T>.Success(default(T));
                        }
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
                if (e is Win32Exception)
                {
                    throw new FailedException($"Executable '{filename} not found. Please install the application and add it to PATH.'");
                }
                throw;
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
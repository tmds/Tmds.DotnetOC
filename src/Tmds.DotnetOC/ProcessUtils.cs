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

        public static Result Run(string filename, string arguments, ProcessStartInfo psi = null)
            => Run<VoidType, VoidType>(filename, arguments, VoidType.Instance, psi);

        public static Result<TOut> Run<TOut>(string filename, string arguments, ProcessStartInfo psi = null)
            => Run<VoidType, TOut>(filename, arguments, VoidType.Instance, psi);

        public static Result Run<TIn>(string filename, string arguments, TIn input, ProcessStartInfo psi = null)
            => Run<TIn, VoidType>(filename, arguments, input, psi);

        public static Result<TOut> Run<TIn, TOut>(string filename, string arguments, TIn input, ProcessStartInfo psi = null)
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
                        ((JObject)(object)input).WriteTo(writer);
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

            return Run(filename, arguments, readOutput, writeInput, psi);
        }

        public static Result Run(string filename, string arguments, Action<StreamReader> readOutput)
            => Run<VoidType>(filename, arguments,
                reader => {
                    readOutput(reader);
                    return VoidType.Instance; },
                writeInput: null, psi: null);

        private static Result<T> Run<T>(string filename, string arguments, Func<StreamReader, T> readOutput, Action<StreamWriter> writeInput, ProcessStartInfo psi)
        {
            // System.Console.WriteLine($">> Executing {filename} {arguments}");
            if (psi == null)
            {
                psi = new ProcessStartInfo();
            }
            psi.FileName = filename;
            psi.Arguments = arguments;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardInput = true;
            Process process = null;
            try
            {
                process = Process.Start(psi);
                writeInput?.Invoke(process.StandardInput);
                process.StandardInput.Close();
                Result<T> result = null;
                Exception outputReadException = null;
                if (readOutput != null)
                {
                    try
                    {
                        result = Result<T>.Success(readOutput(process.StandardOutput));
                    }
                    catch (Exception e)
                    {
                        outputReadException = e;
                    }
                }
                else
                {
                    result = Result<T>.Success(default(T));
                }
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    if (outputReadException != null)
                    {
                        throw outputReadException;
                    }
                    return result;
                }
                else
                {
                    return Result<T>.Error(process.StandardError.ReadToEnd());
                }
            }
            catch (Exception e)
            {
                if (e is Win32Exception)
                {
                    throw new FailedException($"Executable '{filename} not found. Please install the application and add it to PATH.'");
                }
                throw;
            }
            finally
            {
                process?.Dispose();
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
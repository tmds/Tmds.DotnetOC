using System;
using System.IO;

namespace Tmds.DotnetOC
{
    static class Prerequisite
    {
        public static bool CheckOCOnPath(IConsole console) =>
            CheckProgramOnPath(console, "oc", "The 'oc' binary can not be found on PATH");

        public static bool CheckOCHasContext(IConsole console)
        {
            Result result = ProcessUtils.Run("oc", "whoami");
            if (result.IsSuccess)
            {
                console.WriteErrorLine($"No active context: {result.Content}");
            }
            return result.IsSuccess;
        }

        private static string[] s_splitPath = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(':');
        private static bool CheckProgramOnPath(IConsole console, string program, string message)
        {
            foreach (var pathDir in s_splitPath)
            {
                if (File.Exists(Path.Combine(pathDir, program)))
                {
                    return true;
                }
            }
            if (message != null)
            {
                console.WriteErrorLine(message);
            }
            return false;
        }
    }
}
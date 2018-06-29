using System;
using System.IO;

namespace Tmds.DotnetOC
{
    static class Prerequisite
    {
        public static bool CheckOCOnPath() =>
            CheckProgramOnPath("oc", "The 'oc' binary can not be found on PATH");

        public static bool CheckOCHasContext()
        {
            ProcessResult result = ProcessUtils.Run("oc", "whoami");
            if (result.ExitCode != 0)
            {
                PrintError($"No active context: {result.StandardError}");
            }
            return result.ExitCode == 0;
        }

        private static string[] s_splitPath = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(':');
        private static bool CheckProgramOnPath(string program, string message)
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
                PrintError(message);
            }
            return false;
        }

        private static void PrintError(string message)
        {
            System.Console.WriteLine($"ERR: {message}");
        }
    }
}
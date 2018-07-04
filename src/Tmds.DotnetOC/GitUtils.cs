using System.IO;

namespace Tmds.DotnetOC
{
    static class GitUtils
    {
        public static string FindRepoRoot()
        {
            string dir = Directory.GetCurrentDirectory();
            do
            {
                if (Directory.Exists($"{dir}/.git"))
                {
                    return dir;
                }
                // null if path denotes a root directory 
                dir = Path.GetDirectoryName(dir);
            } while (dir != null);
            return null;
        }

        public static string GetRemoteUrl(string remoteName)
        {
            Result<string> result = ProcessUtils.Run<string>("git", $"remote get-url {remoteName}");
            if (result.IsSuccess)
            {
                return result.Value.Trim();
            }
            else
            {
                return null;
            }
        }

        public static string GetCurrentBranch()
        {
            Result<string> result = ProcessUtils.Run<string>("git", $"rev-parse --abbrev-ref HEAD");
            if (result.IsSuccess)
            {
                return result.Value.Trim();
            }
            else
            {
                return null;
            }
        }
    }
}
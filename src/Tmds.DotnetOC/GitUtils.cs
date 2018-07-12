using System.Diagnostics;
using System.IO;

namespace Tmds.DotnetOC
{
    static class GitUtils
    {
        public static string FindRepoRoot(string dir)
        {
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

        public static string GetRemoteUrl(string gitRoot, string remoteName)
        {
            Result<string> result = ProcessUtils.Run<string>("git", $"remote get-url {remoteName}", new ProcessStartInfo { WorkingDirectory = gitRoot });
            if (result.IsSuccess)
            {
                return result.Value.Trim();
            }
            else
            {
                return null;
            }
        }

        public static string GetCurrentBranch(string gitRoot)
        {
            Result<string> result = ProcessUtils.Run<string>("git", $"rev-parse --abbrev-ref HEAD", new ProcessStartInfo { WorkingDirectory = gitRoot });
            if (result.IsSuccess)
            {
                return result.Value.Trim();
            }
            else
            {
                return null;
            }
        }

        public static string GetHeadCommitId(string gitRoot)
        {
            Result<string> result = ProcessUtils.Run<string>("git", $"rev-parse HEAD", new ProcessStartInfo { WorkingDirectory = gitRoot });
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
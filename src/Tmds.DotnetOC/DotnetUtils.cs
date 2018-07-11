using Buildalyzer;

namespace Tmds.DotnetOC
{
    static class DotnetUtils
    {
        public static string GetTargetFramework(string projectFile)
        {
            AnalyzerManager manager = new AnalyzerManager();
            ProjectAnalyzer analyzer = manager.GetProject(projectFile);
            return analyzer.Project.GetPropertyValue("TargetFramework");
        }

        public static string GetSdkVersion()
        {
            Result<string> result = ProcessUtils.Run<string>("dotnet", "--version");
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
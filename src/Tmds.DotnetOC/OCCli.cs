using System;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    class OCCli : IOpenShift
    {
        public Result CheckDependencies() => ProcessUtils.ExistsOnPath("oc");

        public Result CheckConnection() => Run("whoami");

        private Result Run(string arguments, string stdin = null) => ProcessUtils.Run("oc", arguments, stdin);

        public Result<string[]> GetImageTagVersions(string name, string ocNamespace)
        {
            string arguments = $"get is -o json {(ocNamespace != null ? "--namespace {ocNamespace}" : "")} dotnet";
            Result result = ProcessUtils.Run("oc", arguments);
            if (result.IsSuccess)
            {
                JObject jobject = JObject.Parse(result.Content);
                return ImageStreamParser.GetTags(jobject);
            }
            else
            {
                // TODO Assume: not found
                return Array.Empty<string>();
            }
        }

        public Result Create(bool exists, string content)
            => Run((exists ? "replace" : "create") + " -f -", content);
    }
}
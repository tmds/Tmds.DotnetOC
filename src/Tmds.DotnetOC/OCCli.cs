using System;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    class OCCli : IOpenShift
    {
        public Result CheckDependencies() => ProcessUtils.ExistsOnPath("oc");

        public Result CheckConnection() => Run("whoami");

        private Result Run(string arguments, string stdin = null) => ProcessUtils.Run("oc", arguments, stdin);

        public Result<bool> IsCommunity()
        {
            Result<ImageStreamTag[]> nodejsStreamTags = GetImageTagVersions("nodejs", ocNamespace: "openshift");
            if (nodejsStreamTags.IsSuccess)
            {
                foreach (var tag in nodejsStreamTags.Value)
                {
                    if (tag.Image.Contains("registry.access.redhat.com"))
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                return Result<bool>.Error("Cannot determine if this is a community installation.");
            }
        }

        public Result<ImageStreamTag[]> GetImageTagVersions(string name, string ocNamespace)
        {
            string arguments = $"get is -o json {NamespaceArg(ocNamespace)} {name}";
            Result result = ProcessUtils.Run("oc", arguments);
            if (result.IsSuccess)
            {
                JObject jobject = JObject.Parse(result.Content);
                return ImageStreamParser.GetTags(jobject);
            }
            else
            {
                if (result.Content.Contains("NotFound"))
                {
                    return Array.Empty<ImageStreamTag>();
                }
                else
                {
                    return result;
                }
            }
        }

        private string NamespaceArg(string ocNamespace)
        {
            if (ocNamespace == null)
            {
                return string.Empty;
            }
            return $"--namespace {ocNamespace}";
        }

        public Result Create(bool exists, string content, string ocNamespace = null)
            => Run((exists ? "replace" : "create") + " " + NamespaceArg(ocNamespace) + " -f -", content);
    }
}
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    class OCCli : IOpenShift
    {
        public Result CheckDependencies()
        {
            if (ProcessUtils.ExistsOnPath("oc"))
            {
                return Result.Success();
            }
            else
            {
                return Result.Error("Origin client tools ('oc') is not found on PATH. The program can be downloaded from https://github.com/openshift/origin/releases.");
            }
        }

        public Result CheckConnection()
        {
            Result result = Run("whoami");
            if (result.IsSuccess)
            {
                return result;
            }
            else
            {
                return Result.Error($"Cannot connect to OpenShift cluster: {result.ErrorMessage}");
            }
        }

        private Result Run(string arguments, JObject input = null)
            => ProcessUtils.Run("oc", arguments, input);

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
            Result<JObject> result = ProcessUtils.RunAndGetJSon("oc", arguments);
            if (result.IsSuccess)
            {
                return ImageStreamParser.GetTags(result.Value);
            }
            else
            {
                if (result.ErrorMessage.Contains("NotFound"))
                {
                    return Array.Empty<ImageStreamTag>();
                }
                else
                {
                    return Result<ImageStreamTag[]>.Error(result.ErrorMessage);
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

        public Result Create(bool exists, JObject content, string ocNamespace = null)
        {
            return Run((exists ? "replace" : "create") + " " + NamespaceArg(ocNamespace) + " -f -", content);
        }
    }
}
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    class OCCli : IOpenShift
    {
        public void EnsureConnection()
        {
            Result result = Run("whoami");
            if (!result.IsSuccess)
            {
                throw new FailedException($"Cannot connect to OpenShift cluster: {result.ErrorMessage}");
            }
        }

        private Result Run(string arguments, JObject input = null)
            => ProcessUtils.Run("oc", arguments, input);

        public bool IsCommunity()
        {
            ImageStreamTag[] nodejsStreamTags = GetImageTagVersions("nodejs", ocNamespace: "openshift");
            foreach (var tag in nodejsStreamTags)
            {
                if (tag.Image.Contains("registry.access.redhat.com"))
                {
                    return false;
                }
            }
            return true;
        }

        public ImageStreamTag[] GetImageTagVersions(string name, string ocNamespace)
        {
            string arguments = $"get is -o json {NamespaceArg(ocNamespace)} {name}";
            Result<JObject> result = ProcessUtils.Run<JObject>("oc", arguments);
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
                    throw new FailedException($"Unable to retrieve image stream tags: {result.ErrorMessage}");
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

        public void Create(JObject value, string ocNamespace = null)
            => RunCommand("create", ocNamespace, value);

        public void Replace(JObject value, string ocNamespace = null)
            => RunCommand("replace", ocNamespace, value);

        private void RunCommand(string command, string ocNamespace, JObject value)
        {
            Result result = Run(command + " " + NamespaceArg(ocNamespace) + " -f -", value);
            if (!result.IsSuccess)
            {
                throw new FailedException($"Unable to '{command}': {result.ErrorMessage}");
            }
        }

        public void CreateImageStream(string name)
        {
            Result result = Run($"create is {name}");
            if (!result.IsSuccess)
            {
                if (result.ErrorMessage.Contains("AlreadyExists"))
                {
                    return;
                }
                throw new FailedException($"Error creating image stream: {result.ErrorMessage}");
            }
        }

        public string GetCurrentNamespace()
        {
            Result<string> result = ProcessUtils.Run<string>("oc", "project -q");
            if (result.IsSuccess)
            {
                return result.Value.Trim();
            }
            else
            {
                throw new FailedException($"Cannot determine current project: {result.ErrorMessage}");
            }
        }
    }
}
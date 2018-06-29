using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    class InstallCommand
    {
        const string ImageStreamsUrl =          "https://raw.githubusercontent.com/redhat-developer/s2i-dotnetcore/master/dotnet_imagestreams.json";
        const string CommunityImageStreamsUrl = "https://raw.githubusercontent.com/redhat-developer/s2i-dotnetcore/master/dotnet_imagestreams_centos.json";

        private IConsole _console;
        public InstallCommand(IConsole console)
        {
            _console = console;
        }

        async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            if (!Prerequisite.CheckOCOnPath(_console) ||
                !Prerequisite.CheckOCHasContext(_console))
            {
                return 1;
            }
            bool community = false; // TODO, handle CentOS (community)
            bool openshift = false; // TODO, handle openshift namespace

            _console.Write("Retrieving installed versions: ");
            string[] namespaceVersions = GetDotnetImageStreamVersions(ocNamespace: openshift ? "openshift" : null);
            _console.WriteLine(string.Join(", ", namespaceVersions)); // TODO: order versions

            _console.Write("Retrieving latest versions   : ");
            string imageStreamsUrl = community ? CommunityImageStreamsUrl : ImageStreamsUrl;
            string s2iImageStreams = await HttpUtils.GetAsString(ImageStreamsUrl);
            string[] s2iVersions = ParseImageStreamListVersions(s2iImageStreams);
            _console.WriteLine(string.Join(", ", s2iVersions)); // TODO: order versions

            _console.EmptyLine();

            IEnumerable<string> newVersions = s2iVersions.Except(namespaceVersions);
            IEnumerable<string> removedVersions = namespaceVersions.Except(s2iVersions);
            if (removedVersions.Any()) // TODO, Add force flag
            {
                _console.WriteErrorLine("namespace has unknown versions.");
                return 1;
            }
            if (!newVersions.Any()) // TODO, Add force flag
            {
                _console.WriteLine("System already up-to-date.");
                return 0;
            }

            Result result = ProcessUtils.Run("oc", $"{(namespaceVersions.Length == 0 ? "create" : "replace")} -f -", s2iImageStreams);
            if (result.IsSuccess)
            {
                _console.WriteLine("Succesfully updated.");
                return 0;
            }
            else
            {
                _console.WriteErrorLine(result.Content);
                return 1;
            }
        }

        private static string[] GetDotnetImageStreamVersions(string ocNamespace = null)
        {
            string arguments = $"get is -o json {(ocNamespace != null ? "--namespace {ocNamespace}" : "")} dotnet";
            Result result = ProcessUtils.Run("oc", arguments);
            if (result.IsSuccess)
            {
                return ParseImageStreamVersions(result.Content);
            }
            else
            {
                // TODO Assume: not found
                return Array.Empty<string>();
            }
        }

        private static string[] ParseImageStreamVersions(string dotnetImageStream)
        {
            JObject jobject = JObject.Parse(dotnetImageStream);
            return ParseImageStreamVersions(jobject);
        }

        private static string[] ParseImageStreamVersions(JToken dotnetImageStream)
        {
            JToken jobject = dotnetImageStream;
            var versionList = new List<string>();
            foreach (var tag in jobject["spec"]["tags"])
            {
                string name = (string)tag["name"];
                if (name != "latest")
                {
                    versionList.Add(name);
                }
            }
            return versionList.ToArray();
        }

        private static string[] ParseImageStreamListVersions(string imageStreamList)
        {
            JObject jobject = JObject.Parse(imageStreamList);
            foreach (var item in jobject["items"])
            {
                string name = (string)item["metadata"]["name"];
                if (name == "dotnet")
                {
                    return ParseImageStreamVersions(item);
                }
            }
            return Array.Empty<string>();
        }
    }
}
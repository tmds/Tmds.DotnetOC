using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    class ImageStreamTag
    {
        public string Version { get; set; }
        public string Image { get; set ;}
    }

    class Build
    {
        public string PodName { get; set; }
        public string StatusMessage { get; set; }
        public string Phase { get; set; }
        public string Reason { get; set; }
    }

    class Pod
    {
        public string Phase { get; set; }
    }

    interface IOpenShift
    {
        bool IsCommunity();

        void EnsureConnection();

        ImageStreamTag[] GetImageTagVersions(string name, string ocNamespace);

        void Create(JObject content, string ocNamespace = null);

        void Replace(JObject value, string ocNamespace = null);

        void CreateImageStream(string name);

        string GetCurrentNamespace();

        Build GetLatestBuild(string buildConfigName);

        void GetLog(string podName, Action<StreamReader> reader);

        Pod GetPod(string podName, bool mustExist = true);
    }
}

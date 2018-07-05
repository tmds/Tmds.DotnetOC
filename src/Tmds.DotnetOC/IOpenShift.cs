using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    class ImageStreamTag
    {
        public string Version { get; set; }
        public string Image { get; set ;}
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
    }
}

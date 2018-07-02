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
        Result<bool> IsCommunity();

        Result CheckDependencies();

        Result CheckConnection();

        Result<ImageStreamTag[]> GetImageTagVersions(string name, string ocNamespace);

        Result Create(bool exists, JObject content, string ocNamespace = null);
    }
}

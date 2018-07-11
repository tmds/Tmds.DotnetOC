using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    class GithubS2iRepo : IS2iRepo
    {
        const string ImageStreamsUrl =          "https://raw.githubusercontent.com/redhat-developer/s2i-dotnetcore/master/dotnet_imagestreams.json";
        const string CommunityImageStreamsUrl = "https://raw.githubusercontent.com/redhat-developer/s2i-dotnetcore/master/dotnet_imagestreams_centos.json";

        public JObject GetImageStreams(bool community)
        {
            string imageStreamsUrl = community ? CommunityImageStreamsUrl : ImageStreamsUrl;
            Result<JObject> result = HttpUtils.GetAsJObject(imageStreamsUrl);;
            if (result.IsSuccess)
            {
                return result.Value;
            }
            else
            {
                throw new System.Exception($"Cannot retrieve 'imageStreamsUrl': {result.ErrorMessage}");
            }
        }
    }
}
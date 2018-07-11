using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    interface IS2iRepo
    {
        JObject GetImageStreams(bool community);
    }
}
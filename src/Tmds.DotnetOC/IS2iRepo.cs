using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    interface IS2iRepo
    {
        Result<JObject> GetImageStreams(bool community);
    }
}
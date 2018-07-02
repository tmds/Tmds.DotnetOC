using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    static class HttpUtils
    {
        public static Result<JObject> GetAsJObject(string url)
            => GetAsJObjectAsync(url).GetAwaiter().GetResult();

        public static async Task<Result<JObject>> GetAsJObjectAsync(string url)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    using (var response = await httpClient.GetAsync(url))
                    {
                        return JObject.Load(new JsonTextReader(new StreamReader(await response.Content.ReadAsStreamAsync())));
                    }
                }
            }
            catch (Exception e)
            {
                return Result<JObject>.Error(e.ToString());
            }
        }
    }
}
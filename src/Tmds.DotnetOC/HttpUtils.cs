using System.Net.Http;
using System.Threading.Tasks;

namespace Tmds.DotnetOC
{
    static class HttpUtils
    {
        public static async Task<string> GetAsString(string url)
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);
                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
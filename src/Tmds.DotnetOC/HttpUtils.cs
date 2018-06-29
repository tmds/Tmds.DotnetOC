using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Tmds.DotnetOC
{
    static class HttpUtils
    {
        public static Result GetAsString(string url)
            => GetAsStringAsync(url).GetAwaiter().GetResult();
        public static async Task<Result> GetAsStringAsync(string url)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(url);
                    return Result.Success(await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception e)
            {
                return Result.Error(e.ToString());
            }
        }
    }
}
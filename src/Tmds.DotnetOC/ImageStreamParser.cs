using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    static class ImageStreamParser
    {
        public static string[] GetTags(JObject imagestream)
        {
            JObject jobject = imagestream;
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
    }

    static class ImageStreamListParser
    {
        public static string[] GetTags(JObject imageStreamList, string image)
        {
            JObject jobject = imageStreamList;
            foreach (var item in jobject["items"])
            {
                string name = (string)item["metadata"]["name"];
                if (name == image)
                {
                    return ImageStreamParser.GetTags(item as JObject);
                }
            }
            return Array.Empty<string>();
        }
    }
}
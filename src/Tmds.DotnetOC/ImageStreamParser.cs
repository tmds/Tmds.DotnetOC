using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    static class ImageStreamParser
    {
        public static ImageStreamTag[] GetTags(JObject imagestream)
        {
            JObject jobject = imagestream;
            var versionList = new List<ImageStreamTag>();
            foreach (var tag in jobject["spec"]["tags"])
            {
                string name = (string)tag["name"];
                string fromName = (string)tag["from"]["name"];
                versionList.Add(new ImageStreamTag
                {
                    Version = name,
                    Image = fromName
                });
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
                    return ImageStreamParser.GetTags(item as JObject)
                        .Select(t => t.Version).ToArray();
                }
            }
            return Array.Empty<string>();
        }
    }
}
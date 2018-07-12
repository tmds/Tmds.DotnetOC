using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    static class BuildParser
    {
        public static Build ParseBuild(JObject build)
        {
            int buildNumber = int.Parse((string)build["metadata"]["annotations"]["openshift.io/build.number"]);
            string podName = (string)build["metadata"]["annotations"]["openshift.io/build.pod-name"];
            JToken status = build["status"];
            string statusMessage = (string)status["message"];
            string phase = (string)status["phase"];
            string imageDigest = (string)status["output"]?["to"]?["imageDigest"];
            string reason = (string)status["reason"];
            JToken revisionTag = build["spec"]["revision"];
            string commit;
            string commitMessage;
            if (revisionTag != null)
            {
                commit = (string)build["spec"]["revision"]["git"]["commit"];
                commitMessage = (string)build["spec"]["revision"]["git"]["message"];
            }
            else
            {
                commit = (string)build["spec"]["source"]["git"]["ref"];
                commitMessage = "??";
            }
            return new Build
            {
                PodName = podName,
                StatusMessage = statusMessage,
                Phase = phase,
                Reason = reason,
                BuildNumber = buildNumber,
                Commit = commit,
                CommitMessage = commitMessage,
                ImageDigest = imageDigest
            };
        }
    }
}
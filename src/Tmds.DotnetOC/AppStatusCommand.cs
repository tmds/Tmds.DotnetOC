using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Humanizer;

namespace Tmds.DotnetOC
{
    class AppStatusCommand : CommandBase
    {
        private readonly IOpenShift _openshift;

        [Argument(0, "name")]
        private string Name { get; } // TODO: try to guess this

        public AppStatusCommand(IConsole console, IOpenShift openshift)
            : base(console)
        {
            _openshift = openshift;
        }

        protected override async Task ExecuteAsync(CommandLineApplication app)
        {
            if (Name == null)
            {
                Fail("name is a required argument");
            }
            const string Build = "BUILD";
            const string Phase = "PHASE";
            const string Pod = "POD";
            const string Ready = "READY";
            const string Status = "STATUS";
            const string Restarts = "RESTARTS";
            const string Age = "AGE";
            const string Commit = "COMMIT";

            string name = Name;
            S2IDeployment deployment = _openshift.GetDotnetDeployment(name);

            PrintLine("Builds:");
            string buildConfigName = deployment.BuildConfig.Name;
            Build[] builds = _openshift.GetBuilds(buildConfigName); // TODO: filter
            List<(Build, Pod)> buildsWithPod = builds.OrderBy(b => b.BuildNumber).Select(b => (b, string.IsNullOrEmpty(b.PodName) ? null :
                // TODO: get pods with a single call?
                _openshift.GetPod(b.PodName, mustExist: false))).ToList();
            {
                string[] Columns = new[] { Build, Phase, Pod, Status, Commit }; // TODO: Add STARTED, DURATION
                PrintTable(Columns, buildsWithPod , (string column, (Build build, Pod pod) _) =>
                {
                    switch (column)
                    {
                        case Build:
                            return _.build.BuildNumber.ToString();
                        case Phase:
                            return _.build.Phase;
                        case Pod:
                            return _.pod?.Name;
                        case Status:
                            return _.pod?.Phase;
                        case Commit:
                            return $"{_.build.Commit.Substring(0, Math.Min(7, _.build.Commit.Length))} ({_.build.CommitMessage.Truncate(20)})";
                        default:
                            return "???";
                    }
                }
                );
            }
            PrintEmptyLine();

            PrintLine("Deployment pods:");
            Pod[] deploymentPods = _openshift.GetPods(name, version: null);
            var buildConfigs = _openshift.GetS2iBuildConfigs("dotnet");
            List<(Pod, Build)> deploymentsWithBuild = deploymentPods.Select(d =>
            {
                Build build = null;
                if (d.Containers.Length > 0)
                {
                    build = builds.FirstOrDefault(b => b.ImageDigest == d.Containers[0].ImageDigest);
                }
                return (d, build);
            }).ToList();
            var deploymentConfigs = _openshift.GetDeploymentConfigs();
            {
                string[] Columns = new[] { Pod, Build, Ready, Status, Restarts, Age };
                PrintTable(Columns, deploymentsWithBuild, (string column, (Pod pod, Build build) _) =>
                {
                    switch (column)
                    {
                        case Pod:
                            return _.pod.Name;
                        case Build:
                            return _.build?.BuildNumber.ToString();
                        case Ready:
                        {
                            bool ready = false;
                            if (_.pod.Containers.Length > 0)
                            {
                                ready = _.pod.Containers[0].Ready;
                            }
                            return ready ? "1/1" : "0/1";
                        }
                        case Status:
                            return _.pod.Phase;
                        case Restarts:
                        {
                            int restarts = 0;
                            if (_.pod.Containers.Length > 0)
                            {
                                restarts = _.pod.Containers[0].RestartCount;
                            }
                            return restarts.ToString();
                        }
                        case Age:
                        {
                            DateTime? startTime = null;
                            if (_.pod.Containers.Length > 0)
                            {
                                startTime = _.pod.Containers[0].StartedAt;
                            }
                            if (startTime == null)
                            {
                                return "";
                            }
                            return (DateTime.UtcNow - startTime.Value).Humanize();
                        }
                        default:
                            return "???";
                    }
                }
                );
            }
        }
    }
}
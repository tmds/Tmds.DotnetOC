using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace Tmds.DotnetOC
{
    [Command(Description = "Lists deployed applications.")]
    class AppListCommand : CommandBase
    {
        private readonly IOpenShift _openshift;
        public AppListCommand(IConsole console, IOpenShift openshift)
            : base(console)
        {
            _openshift = openshift;
        }

        protected override async Task ExecuteAsync(CommandLineApplication app)
        {
            var deployments = _openshift.GetDotnetDeployments();

            const string Deployment = "DEPLOYMENT";
            const string Services = "SERVICES";
            const string Routes = "ROUTES";
            const string Replicas = "REPLICAS";
            string[] Columns = new[] { Deployment, Replicas, Services, Routes };
            PrintTable(Columns, deployments, (column, deployment) =>
            {
                switch (column)
                {
                    case Deployment:
                        return deployment.DeploymentConfig.Name;
                    case Replicas:
                        return $"{deployment.DeploymentConfig.UpdatedReplicas}/{deployment.DeploymentConfig.SpecReplicas}";
                    case Services:
                        return string.Join(',', deployment.Services.Select(svc => svc.Name));
                    case Routes:
                        return string.Join(',', deployment.Routes.Select(rt => $"{(rt.IsTls ? "https://" : "http://")}{rt.Host}"));
                    default:
                        return "???";
                }
            }
            );
        }
    }
}
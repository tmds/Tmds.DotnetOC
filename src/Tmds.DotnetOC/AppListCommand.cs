using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace Tmds.DotnetOC
{
    class AppListCommand : CommandBase
    {
        private readonly IOpenShift _openshift;
        public AppListCommand(IConsole console, IOpenShift openshift)
            : base(console)
        {
            _openshift = openshift;
        }

        class S2IDeployment
        {
            public DeploymentConfig DeploymentConfig { get; set; }
            public S2iBuildConfig BuildConfig { get; set; }
            public Service[] Services { get; set; }
            public Route[] Routes { get; set; }
        }

        protected override async Task ExecuteAsync(CommandLineApplication app)
        {
            var buildConfigs = _openshift.GetS2iBuildConfigs("dotnet");
            var deploymentConfigs = _openshift.GetDeploymentConfigs();
            var services = _openshift.GetServices();
            var routes = _openshift.GetRoutes();

            var deployments = new List<S2IDeployment>();
            foreach (var deploymentConfig in deploymentConfigs)
            {
                foreach (var trigger in deploymentConfig.Triggers)
                {
                    S2iBuildConfig buildConfig = buildConfigs.FirstOrDefault(
                        bc => bc.OutputName == trigger.FromName
                    );
                    if (buildConfig == null)
                    {
                        continue;
                    }
                    Service[] deploymentServices = services.Where(
                        svc => svc.Selectors.IsSubsetOf(deploymentConfig.Labels)
                    ).ToArray();
                    Route[] deploymentRoutes = routes.Where(
                        rt => rt.Backends.Any(be => deploymentServices.Any(ds => ds.Name == be.Name))
                    ).ToArray();
                    deployments.Add(new S2IDeployment
                    {
                        DeploymentConfig = deploymentConfig,
                        BuildConfig = buildConfig,
                        Services = deploymentServices,
                        Routes = deploymentRoutes
                    });
                }
            }

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
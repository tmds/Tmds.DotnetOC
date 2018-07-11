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
            var buildConfigs = _openshift.GetS2iBuildConfigs();
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

            foreach (var deployment in deployments)
            {
                System.Console.WriteLine(deployment.DeploymentConfig.Name);
                System.Console.WriteLine($" git repo: {deployment.BuildConfig.GitUri}");
                System.Console.WriteLine($" services: {string.Join(',', deployment.Services.Select(svc => svc.Name))}");
                System.Console.WriteLine($" routes: {string.Join(',', deployment.Routes.Select(rt => rt.Host))}");
            }
        }
    }
}
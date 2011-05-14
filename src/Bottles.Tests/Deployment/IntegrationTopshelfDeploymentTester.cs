using Bottles.Deployers.Topshelf;
using Bottles.Deployment;
using Bottles.Deployment.Directives;
using Bottles.Deployment.Runtime.Content;
using Bottles.Diagnostics;
using NUnit.Framework;

namespace Bottles.Tests.Deployment
{
    [TestFixture]
    public class IntegrationTopshelfDeploymentTester
    {
        [Test][Explicit]
        public void DeployHelloWorld()
        {
            IBottleRepository bottles = null;
            IProcessRunner process = null;

            var deployer = new TopshelfDeployer(bottles, process);

            var directive = new TopshelfService();

            deployer.Execute(directive, new HostManifest("a"), new PackageLog());
        }
    }
}
using BottleDeployers2;
using Bottles.Deployment;
using Bottles.Deployment.Bootstrapping;
using Bottles.Deployment.Configuration;
using Bottles.Deployment.Runtime;
using Bottles.Deployment.Writing;
using NUnit.Framework;
using StructureMap;
using FubuTestingSupport;
using System.Linq;

namespace Bottles.Tests.Deployment.Bootstrapping
{
    [TestFixture]
    public class DeploymentBootstrapperIntegratedTester
    {
        private IContainer theContainer;

        [SetUp]
        public void SetUp()
        {
            new DeploymentWriter(".\\dbit").Flush(FlushOptions.Wipeout);
            theContainer = DeploymentBootstrapper.Bootstrap(new DeploymentSettings(".\\dbit"));
        }

        [Test]
        public void DirectiveTypeRegistry_has_all_the_types()
        {
            var builder = theContainer.GetInstance<DirectiveTypeRegistry>();

            var x = builder.DirectiveTypes().Where(t => t.Assembly != typeof(ConfigurationDeployer).Assembly);
            x.ShouldHaveTheSameElementsAs(
                typeof(BottleDeployers1.OneDirective),
                typeof(BottleDeployers1.TwoDirective),
                typeof(BottleDeployers1.ThreeDirective),
                typeof(BottleDeployers2.FourDirective),
                typeof(BottleDeployers2.FiveDirective),
                typeof(BottleDeployers2.SixDirective)//,
                //typeof(FubuWebsite),
                //typeof(TopshelfService),
                //typeof(ScheduledTask)
                );
        }

        [Test]
        public void DirectiveBuilder_can_get_a_type_for_a_name()
        {
            var builder = theContainer.GetInstance<DirectiveTypeRegistry>();
            builder.DirectiveTypeFor("OneDirective").ShouldEqual(typeof(BottleDeployers1.OneDirective));
        }

        [Test]
        public void HostManifest_can_create_all_directives_for_itself()
        {
            var host = new HostManifest("h1");
            host.RegisterValue<BottleDeployers1.OneDirective>(x => x.Age, 11);
            host.RegisterValue<BottleDeployers1.OneDirective>(x => x.Name, "Robert");

            host.RegisterValue<BottleDeployers2.SixDirective>(x => x.Direction, "North");
            host.RegisterValue<BottleDeployers2.SixDirective>(x => x.Threshold, 5);

            var registry = theContainer.GetInstance<DirectiveTypeRegistry>();
            var directives = host.BuildDirectives(registry);

            directives.Count().ShouldEqual(2);
            var directiveOne = directives.OfType<BottleDeployers1.OneDirective>().Single();
            directiveOne.Age.ShouldEqual(11);
            directiveOne.Name.ShouldEqual("Robert");

            var directiveSix = directives.OfType<SixDirective>().Single();
            directiveSix.Direction.ShouldEqual("North");
            directiveSix.Threshold.ShouldEqual(5);

        }
    }
}
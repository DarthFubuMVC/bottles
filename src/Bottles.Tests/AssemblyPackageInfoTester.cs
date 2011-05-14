using System.IO;
using System.Reflection;
using AssemblyPackage;
using Bottles.Assemblies;
using FubuTestingSupport;
using NUnit.Framework;
using Rhino.Mocks;
using FubuCore;

namespace Bottles.Tests
{
    [TestFixture]
    public class AssemblyPackageInfoTester
    {
        private Assembly assembly;
        private AssemblyPackageInfo package;

        [SetUp]
        public void SetUp()
        {
            assembly = Assembly.GetExecutingAssembly();
            package = AssemblyPackageInfo.CreateFor(assembly);
        }

        [Test]
        public void name_just_returns_the_assembly_name()
        {
            package.Name.ShouldEqual("Assembly:  " + assembly.GetName().FullName);
        }

        [Test]
        public void load_assemblies_just_tries_to_add_the_inner_assembly_directly()
        {
            var loader = MockRepository.GenerateMock<IAssemblyRegistration>();
            package.LoadAssemblies(loader);

            loader.AssertWasCalled(x => x.Use(assembly));
        }
    }

    [TestFixture]
    public class AssemblyPackageInfoIntegratedTester
    {
        private AssemblyPackageInfo thePackage;

        [SetUp]
        public void SetUp()
        {
            thePackage = AssemblyPackageInfo.CreateFor(typeof (AssemblyPackageMarker).Assembly);
        }


        [Test]
        public void can_retrieve_data_from_package()
        {
            var text = "not the right thing";
            thePackage.ForData("1.txt", (name, data) =>
            {
                name.ShouldEqual("1.txt");
                text = new StreamReader(data).ReadToEnd();
            });

            // The text of this file in the AssemblyPackage data is just "1"
            text.ShouldEqual("1");
        }

        [Test]
        public void can_retrieve_web_content_folder_from_package()
        {
            var expected = "not this";
            thePackage.ForFolder(BottleFiles.WebContentFolder, folder =>
            {
                expected = folder;
            });

            expected.ShouldEqual(FileSystem.Combine("content", "AssemblyPackage", "WebContent").ToFullPath());
        }
    }
}
﻿using System;
using System.IO;
using Bottles.Services.Messaging;
using Bottles.Services.Remote;
using NUnit.Framework;
using System.Linq;
using FubuTestingSupport;
using SampleService;
using FubuCore;
using Bottles.Services.Messaging.Tracking;

namespace Bottles.Services.Tests.Remote
{
    [Explicit("Only for examples")]
    public class Examples
    {
        [Test]
        public void loading_a_custom_application_loader()
        {
            // SAMPLE: bootstrap-custom-loader
            var runner = RemoteServiceRunner.For<IApplicationLoader>(x => {
                // more AppDomain configuration
            });

            // -- or --
            runner = new RemoteServiceRunner(x => {
                // Can be the assembly qualified name of either:
                // 1. An IApplicationLoader class
                // 2. An IApplicationSource<,> class
                // 3. An IBootstrapper class
                x.BootstrapperName = "MyLib.AppLoader, MyLib";
            });
            // ENDSAMPLE
        }

        [Test]
        public void overriding_the_configuration_file()
        {
            // SAMPLE: override-config-file
            var runner = new RemoteServiceRunner(x => {
                x.Setup.ConfigurationFile = "Web.config";
            });
            // ENDSAMPLE
        }

        // SAMPLE: custom-application-loader
        public class CustomApplicationLoader : IApplicationLoader, IDisposable
        {
            public IDisposable Load()
            {
                // Bootstrap the application or service
                return this;
            }

            public void Dispose()
            {
                // shutdown
            }
        }
        // ENDSAMPLE


        [Test]
        public void simplest_usage()
        {
            // SAMPLE: simple-remote-service-runner
            var runner = new RemoteServiceRunner(x => {
                x.ServiceDirectory = @"c:\code\other-service\src\other-service";
                x.Setup.ShadowCopyFiles = true.ToString();
            });

            // stop and restart the remote AppDomain
            runner.Recycle();

            // There is support for event aggregation between
            // AppDomain's for coordination
            runner.SendRemotely(new SomeMessage());

            // Shut down and close the remote AppDomain
            runner.Dispose();

            // ENDSAMPLE

        }

        public class SomeMessage{}

        [Test]
        public void open_to_a_parallel_directory()
        {
            // SAMPLE: parallel-service
            var runner = new RemoteServiceRunner(x => {
                x.UseParallelServiceDirectory("Service1");
            });
            // ENDSAMPLE
        }

        [Test]
        public void control_appdomain_setup()
        {
            // SAMPLE: appdomain-setup
            var runner = new RemoteServiceRunner(x => {

                // ShadowCopyFiles should be either "true" or "false"
                x.Setup.ShadowCopyFiles = true.ToString();

                // RemoteServiceRunner makes some guesses about the
                // PrivateBinPath based on the folders it sees,
                // but once in a while you may want to override
                // the bin path
                x.Setup.PrivateBinPath = "bin";
            });
            // ENDSAMPLE
        }

        [Test]
        public void load_assemblies()
        {
            // SAMPLE: require-assemblies
            var runner = new RemoteServiceRunner(x => {
                // This line of code will copy the Bottles.dll assembly
                // into the remote AppDomain location
                x.RequireAssemblyContainingType<Bottles.IPackageFacility>();
                x.RequireAssembly("MyAssembly");
            });
            // ENDSAMPLE
        }
    }

    [TestFixture, Explicit("These tests do not play nice on the build server")]
    public class BigRemoteServicesIntegrationTester
    {
        private RemoteServiceRunner start()
        {
            return new RemoteServiceRunner(x => {
                x.LoadAssemblyContainingType<SampleService.SampleService>();
                x.RequireAssemblyContainingType<BigRemoteServicesIntegrationTester>(); // This is mostly a smoke test
            });
        }

        [Test]
        public void start_with_only_the_folder_name_with_an_IBottleService()
        {
            var servicePath = ".".ToFullPath().ParentDirectory().ParentDirectory().ParentDirectory().AppendPath("SampleService");
            using (var runner = new RemoteServiceRunner(servicePath))
            {
                runner.WaitForServiceToStart<SampleService.SampleService>();
                runner.Started.Any().ShouldBeTrue(); 
            }
        }

        [Test]
        public void start_with_only_the_folder_name_with_an_IApplicationLoader()
        {
            var servicePath = ".".ToFullPath().ParentDirectory().ParentDirectory().ParentDirectory().AppendPath("ApplicationLoaderService");
            using (var runner = new RemoteServiceRunner(servicePath))
            {
                runner.WaitForMessage<LoaderStarted>().LoaderTypeName.ShouldContain("MyApplicationLoader");
            }
        }

        [Test]
        public void start_with_a_parallel_folder()
        {
            using (var runner = new RemoteServiceRunner(x => {
                x.UseParallelServiceDirectory("ApplicationLoaderService");
            }))
            {
                runner.WaitForMessage<LoaderStarted>().LoaderTypeName.ShouldContain("MyApplicationLoader");
            }
        }

        [Test]
        public void run_a_specific_bootstrapper()
        {
            using (var runner = RemoteServiceRunner.For<SampleBootstrapper>())
            {
                runner.WaitForServiceToStart<SampleService.SampleService>();
                runner.WaitForServiceToStart<SampleService.RemoteService>();

                runner.Started.Any().ShouldBeTrue(); 
            }
        }

        [Test]
        public void coordinate_message_history_via_remote_service()
        {
            

            using (var runner = RemoteServiceRunner.For<SampleBootstrapper>())
            {
                runner.WaitForServiceToStart<SampleService.SampleService>();
                runner.WaitForServiceToStart<SampleService.RemoteService>();

                MessageHistory.StartListening(runner);

                var foo = new Foo();

                EventAggregator.SentMessage(foo);


                EventAggregator.Messaging.WaitForMessage<AllMessagesComplete>(() => runner.SendRemotely(foo), 60000)
                                   .ShouldNotBeNull();

            }
        }

        [Test]
        public void coordinate_message_history_via_remote_service_and_clear_data_does_not_remove_listeners()
        {

            using (var runner = RemoteServiceRunner.For<SampleBootstrapper>())
            {
                runner.WaitForServiceToStart<SampleService.SampleService>();
                runner.WaitForServiceToStart<SampleService.RemoteService>();

                MessageHistory.StartListening(runner);
                MessageHistory.ClearHistory();

                var foo = new Foo();

                EventAggregator.SentMessage(foo);


                EventAggregator.Messaging.WaitForMessage<AllMessagesComplete>(() => runner.SendRemotely(foo))
                                   .ShouldNotBeNull();
            }
        }

        [Test]
        public void spin_up_the_remote_service_for_the_sample_and_send_messages_back_and_forth()
        {
            using (var runner = start())
            {
                runner.WaitForServiceToStart<SampleService.SampleService>();
                runner.WaitForServiceToStart<SampleService.RemoteService>();

                runner.Started.Any().ShouldBeTrue();
            }
        }

        [Test]
        public void spin_up_and_send_and_receive_messages()
        {
            using (var runner = start())
            {
                runner.WaitForServiceToStart<SampleService.RemoteService>();

                runner.WaitForMessage<TestResponse>(() => {
                    runner.SendRemotely(new TestSignal { Number = 1 });
                }).Number.ShouldEqual(1);


                runner.WaitForMessage<TestResponse>(() =>
                {
                    runner.SendRemotely(new TestSignal { Number = 3 });
                }).Number.ShouldEqual(3);

                runner.WaitForMessage<TestResponse>(() =>
                {
                    runner.SendRemotely(new TestSignal { Number = 5 });
                }).Number.ShouldEqual(5);

            }
        }

        [Test]
        public void copy_new_remote_assembly_over_old_assembly_when_copymode_always()
        {
            using (var runner = new RemoteServiceRunner(x =>
            {
                x.AssemblyCopyMode = AssemblyCopyMode.Always;
                x.UseParallelServiceDirectory("ApplicationLoaderService");
                x.RequireAssemblyContainingType<SampleService.SampleService>();
            }))
            {
                var path = ".".ToFullPath();
                var sampleServiceDll = path.AppendPath("bin/Debug/SampleService.dll");

                File.SetLastWriteTime(sampleServiceDll, new DateTime(2014, 01, 01));
                var originalWriteTime = File.GetLastWriteTime(sampleServiceDll);

                runner.Recycle();

                var newWriteTime = File.GetLastWriteTime(sampleServiceDll);

                newWriteTime.ShouldBeGreaterThan(originalWriteTime);
                originalWriteTime.ShouldNotEqual(newWriteTime);
            }
        }

        [Test]
        public void copy_assembly_once_by_default()
        {
            using (var runner = new RemoteServiceRunner(x =>
            {
                x.UseParallelServiceDirectory("ApplicationLoaderService");
                x.RequireAssemblyContainingType<SampleService.SampleService>();
            }))
            {
                var path = ".".ToFullPath();
                var sampleServiceDll = path.AppendPath("bin/Debug/SampleService.dll");

                File.SetLastWriteTime(sampleServiceDll, new DateTime(2014, 01, 01));
                var originalWriteTime = File.GetLastWriteTime(sampleServiceDll);

                runner.Recycle();

                var newWriteTime = File.GetLastWriteTime(sampleServiceDll);

                newWriteTime.ShouldEqual(originalWriteTime);
            }
        }
    }
}
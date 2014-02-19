﻿using System;
using System.Collections.Generic;
using System.IO;
using Bottles.Services.Messaging;
using FubuCore;
using FubuCore.Util;

namespace Bottles.Services.Remote
{
    public interface IAssemblyMover
    {
        void MoveAssemblies(AppDomainSetup setup);
    }

    public class RemoteDomainExpression : IAssemblyMover
    {
        private readonly static FileSystem fileSystem = new FileSystem();
        private readonly MessagingHub _listeners = new MessagingHub();
        private readonly IList<AssemblyRequirement> _requirements = new List<AssemblyRequirement>(); 

        private readonly AppDomainSetup _setup = new AppDomainSetup
        {
            ApplicationName = "Bottle-Services-AppDomain",
            ShadowCopyFiles = "true",
            ConfigurationFile = "BottleServiceRunner.exe.config",
            ApplicationBase = ".".ToFullPath()
        };

        public readonly Cache<string, string> Properties = new Cache<string, string>(key => null);

        public AppDomainSetup Setup
        {
            get { return _setup; }
        }

        public void UseParallelServiceDirectory(string directory)
        {
            var path = AppDomain.CurrentDomain.BaseDirectory.ToFullPath();
            if (Path.GetFileName(path).EqualsIgnoreCase("Debug") || Path.GetFileName(path).EqualsIgnoreCase("Release"))
            {
                path = path.ParentDirectory();
            }

            if (Path.GetFileName(path).EqualsIgnoreCase("bin"))
            {
                path = path.ParentDirectory();
            }

            path = path.ParentDirectory(); // src directory

            ServiceDirectory = path.AppendPath(directory);
        }

        public string ServiceDirectory
        {
            get { return _setup.ApplicationBase; }
            set
            {
                _setup.ApplicationBase = value;

                if (fileSystem.DirectoryExists(value, "bin"))
                {
                    _setup.PrivateBinPath = "bin";
                }

                if (fileSystem.DirectoryExists(value, "bin", "Release"))
                {
                    _setup.PrivateBinPath = "bin".AppendPath("Release");
                }
                else if (fileSystem.DirectoryExists(value, "bin", "Debug"))
                {
                    _setup.PrivateBinPath = "bin".AppendPath("Debug");
                }

                if (fileSystem.FileExists(value.AppendPath("App.config")))
                {
                    _setup.ConfigurationFile = value.ToFullPath().AppendPath("App.config");
                }
                else if (fileSystem.FileExists(value.AppendPath("Web.config")))
                {
                    _setup.ConfigurationFile = value.ToFullPath().AppendPath("Web.config");
                }
            }
        }

        /// <summary>
        /// Use to force the selection of bin/Debug or bin/Release for the private bin path of the remote AppDomain
        /// </summary>
        public string BuildProfile
        {
            set
            {
                if (fileSystem.DirectoryExists(_setup.ApplicationBase, "bin", value))
                {
                    _setup.PrivateBinPath = "bin".AppendPath(value);
                }
            }
        }

        public MessagingHub Listeners
        {
            get { return _listeners; }
        }

        public string BootstrapperName { get; set; }

        /// <summary>
        /// This is primarily used in development and testing scenarios to remotely run
        /// a service in a parallel project.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="compileTarget"></param>
        public void LoadAssemblyContainingType<T>(string compileTarget = "Debug")
        {
            string assemblyName = typeof (T).Assembly.GetName().Name;
            string domainPath = AppDomain.CurrentDomain.BaseDirectory.ParentDirectory().ParentDirectory().ParentDirectory()
                                   .AppendPath(assemblyName, "bin", compileTarget);

            _setup.ApplicationBase = domainPath;
        }

        /// <summary>
        /// Sets the AssemblyCopyMode of each required assembly unless explicitly set in the RequireAssemblyXYZ Methods
        /// </summary>
        public AssemblyCopyMode AssemblyCopyMode { get; set; }

        public void RequireAssembly(string name, AssemblyCopyMode copyMode)
        {
            _requirements.Add(new AssemblyRequirement(name, copyMode));
        }

        public void RequireAssembly(string name)
        {
            RequireAssembly(name, AssemblyCopyMode);
        }

        public void RequireAssemblyContainingType<T>()
        {
            RequireAssemblyContainingType<T>(AssemblyCopyMode);
        }

        public void RequireAssemblyContainingType<T>(AssemblyCopyMode copyMode)
        {
            _requirements.Add(new AssemblyRequirement(typeof(T).Assembly, copyMode));
        }

        void IAssemblyMover.MoveAssemblies(AppDomainSetup setup)
        {
            var binaryPath = setup.ApplicationBase;
            if (setup.PrivateBinPath.IsNotEmpty())
            {
                if (Path.IsPathRooted(setup.PrivateBinPath))
                {
                    binaryPath = setup.PrivateBinPath;
                }
                else
                {
                    binaryPath = setup.ApplicationBase.AppendPath(setup.PrivateBinPath);
                }
            }

            _requirements.Each(x => x.Move(binaryPath));
        }

    }

    public enum AssemblyCopyMode
    {
        Once,
        Always
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Bottles.Assemblies;
using Bottles.Diagnostics;
using FubuCore;

namespace Bottles
{
    //development mode reader
    //already exploded thing
    public class PackageManifestReader : IPackageLoader, IPackageManifestReader
    {
        private readonly string _applicationFolder;
        private readonly IFileSystem _fileSystem;
        private readonly Func<string, string> _getContentFolderFromPackageFolder;

        public PackageManifestReader(string applicationFolder, IFileSystem fileSystem, Func<string, string> getContentFolderFromPackageFolder)
        {
            if (!Path.IsPathRooted(applicationFolder))
            {
                throw new ArgumentOutOfRangeException("applicationFolder", "Only absolute paths can be used here");
            }

            _applicationFolder = applicationFolder;
            _fileSystem = fileSystem;
            _getContentFolderFromPackageFolder = getContentFolderFromPackageFolder;
        }

        public IEnumerable<IPackageInfo> Load(IPackageLog log)
        {
        	var packages = new List<IPackageInfo>();
            //issue??
            var includes = _fileSystem.LoadFromFile<PackageManifest>(_applicationFolder, PackageManifest.FILE);

        	packages.AddRange(includes.LinkedFolders.Select(f => LoadFromFolder(Path.Combine(_applicationFolder, f))));
            packages.AddRange(includes.Assemblies.Select(assemblyName =>
                                                             {
                                                                 var assembly = Assembly.Load(assemblyName);
                                                                 return AssemblyPackageInfo.CreateFor(assembly);
                                                             }));

        	return packages;
        }
        
        public IPackageInfo LoadFromFolder(string folder)
        {
            folder = Path.GetFullPath(folder);

            var manifest = _fileSystem.LoadFromFile<PackageManifest>(folder, PackageManifest.FILE);
            var package = new PackageInfo(manifest.Name){
                Description = "{0} ({1})".ToFormat(manifest.Name, folder),
            };


            // Right here, this needs to be different
            package.RegisterFolder(BottleFiles.WebContentFolder, _getContentFolderFromPackageFolder(folder));
            package.RegisterFolder(BottleFiles.DataFolder, FileSystem.Combine(folder, BottleFiles.DataFolder));
            package.RegisterFolder(BottleFiles.ConfigFolder, FileSystem.Combine(folder, BottleFiles.ConfigFolder));

            var binPath = FileSystem.Combine(_applicationFolder, folder, "bin");
        	var debugPath = FileSystem.Combine(binPath, "debug");
			if(_fileSystem.DirectoryExists(debugPath))
			{
				binPath = debugPath;
			}

            //REVIEW: I feel this whole section is left-hand / right-hand code
            package.Role = manifest.Role;

            var assemblyPaths = findCandidateAssemblyFiles(binPath);
            assemblyPaths.Each(path =>
            {
                var assemblyName = Path.GetFileNameWithoutExtension(path);
                if (manifest.Assemblies.Contains(assemblyName))
                {
                    package.RegisterAssemblyLocation(assemblyName, path);
                }
            });

            return package;
        }

        private static IEnumerable<string> findCandidateAssemblyFiles(string binPath)
        {
            return Directory.GetFiles(binPath).Where(IsPotentiallyAnAssembly);
        }

        public static bool IsPotentiallyAnAssembly(string file)
        {
            var extension = Path.GetExtension(file);
            return extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".dll", StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return "Package Manifest Reader (Development Mode)";
        }
    }
}
﻿using System;
using Bottles.Diagnostics;
using Bottles.Environment;
using FubuCore;
using FubuTestingSupport;
using NUnit.Framework;

namespace Bottles.Tests.Environment
{
    [TestFixture]
    public class FolderExistsTester
    {
        [Test]
        public void positive_test()
        {
            new FileSystem().CreateDirectory("foo");

            var log = new PackageLog();

            var requirement = new FolderExists("foo");

            requirement.Check(log);

            log.Success.ShouldBeTrue();
            log.FullTraceText().ShouldContain("Folder 'foo' exists");
        }

        [Test]
        public void negative_test()
        {
            var log = new PackageLog();

            var folder = Guid.NewGuid().ToString();
            var requirement = new FolderExists(folder);

            requirement.Check(log);

            log.Success.ShouldBeFalse();
            log.FullTraceText().ShouldContain("Folder '{0}' does not exist!".ToFormat(folder));
        }

        [Test]
        public void positive_test_with_generic()
        {
            new FileSystem().CreateDirectory("foo");
            var settings = new FileSettings
            {
                Folder = "foo"
            };

            var log = new PackageLog();

            var requirement = new FolderExists<FileSettings>(x => x.Folder, settings);

            requirement.Check(log);

            log.Success.ShouldBeTrue();
            log.FullTraceText().ShouldContain("Folder 'foo' defined by FileSettings.Folder exists");
        }

        [Test]
        public void negative_test_with_settings()
        {
            var log = new PackageLog();

            var folder = Guid.NewGuid().ToString();
            var settings = new FileSettings
            {
                Folder = folder
            };

            var requirement = new FolderExists<FileSettings>(x => x.Folder, settings);


            requirement.Check(log);

            log.Success.ShouldBeFalse();
            log.FullTraceText().ShouldContain("Folder '{0}' defined by FileSettings.Folder does not exist!".ToFormat(folder));
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using SparkleXrm.Tasks.Config;

namespace SparkleXrm.Tasks.Tests
{
    [TestClass]
    public class DownloadWebresourceFilesTest
    {
        [TestMethod]
        [TestCategory("Integration Tests")]
        public void TestDownloadFiles()
        {
            var crmSvc = TestConnection.Create();
            var task = new DownloadWebresourceFileTask(crmSvc, new TraceLogger());
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Webresources");

            task.Execute(path);
        }

        [TestMethod]
        [TestCategory("Unit Tests")]
        public void NewWebresourceIsSavedBelowRootAndAddedToConfig()
        {
            var config = CreateConfig(@"C:\tests", new WebresourceDeployConfig { root = "Webresources", solution = "testsolution" });
            var saved = new List<string>();
            var service = Arrange(config, saved, "new_/foo/bar.js");

            Run(service, config);

            CollectionAssert.AreEqual(new[] { @"C:\tests\Webresources\new_\foo\bar.js" }, saved);
            var file = config.webresources[0].files[0];
            Assert.AreEqual("new_/foo/bar.js", file.uniquename);
            Assert.AreEqual(@"new_\foo\bar.js", file.file);
            Assert.IsNull(file.displayname);
            A.CallTo(() => config.Save()).MustHaveHappenedOnceExactly();
        }

        [TestMethod]
        [TestCategory("Unit Tests")]
        public void KnownWebresourceIsSavedButNotAddedAgain()
        {
            var section = new WebresourceDeployConfig
            {
                root = "Webresources",
                solution = "testsolution",
                files = new List<WebResourceFile>
                {
                    new WebResourceFile { uniquename = "new_/foo/bar.js", file = @"new_\foo\bar.js" }
                }
            };
            var config = CreateConfig(@"C:\tests", section);
            var saved = new List<string>();
            var service = Arrange(config, saved, "new_/foo/bar.js");

            Run(service, config);

            CollectionAssert.AreEqual(new[] { @"C:\tests\Webresources\new_\foo\bar.js" }, saved);
            Assert.AreEqual(1, section.files.Count);
            A.CallTo(() => config.Save()).MustNotHaveHappened();
        }

        [TestMethod]
        [TestCategory("Unit Tests")]
        public void AutodetectSectionIsNotAddedToConfig()
        {
            var section = new WebresourceDeployConfig { root = "Webresources", solution = "testsolution", autodetect = "yes" };
            var config = CreateConfig(@"C:\tests", section);
            var saved = new List<string>();
            var service = Arrange(config, saved, "new_/foo/bar.js");

            Run(service, config);

            Assert.AreEqual(1, saved.Count);
            Assert.IsNull(section.files);
            A.CallTo(() => config.Save()).MustNotHaveHappened();
        }

        [TestMethod]
        [TestCategory("Unit Tests")]
        public void NameOutsideRootIsSkipped()
        {
            var config = CreateConfig(@"C:\tests", new WebresourceDeployConfig { root = "Webresources", solution = "testsolution" });
            var saved = new List<string>();
            var service = Arrange(config, saved, "../../evil.js");

            Run(service, config);

            Assert.AreEqual(0, saved.Count);
            A.CallTo(() => config.Save()).MustNotHaveHappened();
        }

        [TestMethod]
        [TestCategory("Unit Tests")]
        public void MissingSolutionThrows()
        {
            var config = CreateConfig(@"C:\tests", new WebresourceDeployConfig { root = "Webresources" });
            var service = Arrange(config, new List<string>(), "new_/foo/bar.js");

            var ex = Assert.ThrowsException<SparkleTaskException>(() => Run(service, config));
            Assert.AreEqual(SparkleTaskException.ExceptionTypes.SOLUTION_REQUIRED, ex.ExceptionType);
        }

        [TestMethod]
        [TestCategory("Unit Tests")]
        public void MissingWebresourcesSectionThrows()
        {
            var config = CreateConfig(@"C:\tests");
            var service = Arrange(config, new List<string>(), "new_/foo/bar.js");

            var ex = Assert.ThrowsException<SparkleTaskException>(() => Run(service, config));
            Assert.AreEqual(SparkleTaskException.ExceptionTypes.CONFIG_NOTFOUND, ex.ExceptionType);
        }

        private static ConfigFile CreateConfig(string filePath, params WebresourceDeployConfig[] sections)
        {
            var config = A.Fake<ConfigFile>(a => a.CallsBaseMethods());
            config.filePath = filePath;
            config.webresources = new List<WebresourceDeployConfig>(sections);
            A.CallTo(() => config.Save()).DoesNothing();
            return config;
        }

        private static void Run(IOrganizationService service, ConfigFile config)
        {
            new DownloadWebresourceFileTask(service, new TraceLogger()).Execute(config.filePath);
        }

        private static IOrganizationService Arrange(ConfigFile config, List<string> saved, string webresourceName)
        {
            var configService = A.Fake<IConfigFileService>(a => a.Strict());
            ServiceLocator.ServiceProvider.RemoveService(typeof(IConfigFileService));
            ServiceLocator.ServiceProvider.AddService(typeof(IConfigFileService), configService);
            A.CallTo(() => configService.FindConfig(A<string>.Ignored, A<bool>.Ignored))
                .Returns(new List<ConfigFile> { config });

            var directoryService = A.Fake<IDirectoryService>(a => a.Strict());
            ServiceLocator.ServiceProvider.RemoveService(typeof(IDirectoryService));
            ServiceLocator.ServiceProvider.AddService(typeof(IDirectoryService), directoryService);
            A.CallTo(() => directoryService.SaveFile(A<string>.Ignored, A<byte[]>.Ignored, A<bool>.Ignored))
                .Invokes((string name, byte[] content, bool overwrite) => saved.Add(name));

            var service = A.Fake<IOrganizationService>(a => a.Strict());
            ServiceLocator.ServiceProvider.RemoveService(typeof(IOrganizationService));
            ServiceLocator.ServiceProvider.AddService(typeof(IOrganizationService), service);
            A.CallTo(() => service.Execute(A<OrganizationRequest>.Ignored))
                .ReturnsLazily((OrganizationRequest request) =>
                {
                    var webresource = new WebResource
                    {
                        Name = webresourceName,
                        DisplayName = webresourceName,
                        Content = Convert.ToBase64String(Encoding.ASCII.GetBytes("test"))
                    };
                    var response = new RetrieveMultipleResponse { ResponseName = "RetrieveMultipleResponse" };
                    response.Results["EntityCollection"] = new EntityCollection(new List<Entity> { webresource });
                    return response;
                });

            return service;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SparkleXrm.Tasks.Config;

namespace SparkleXrm.Tasks.Tests
{
    [TestClass]
    public class ConfigDiscoveryTests
    {
        private string _root;

        [TestInitialize]
        public void CreateSolutionFolder()
        {
            ServiceLocator.Init();
            _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            CreateFile(@"Tooling\spkl.json", "{ \"plugins\": [ { \"solution\": \"Test\", \"assemblypath\": \"..\\\\Plugins\\\\bin\\\\Debug\\\\Contoso.Plugins.dll\" } ] }");
            CreateFile(@"Tooling\Nested\spkl.json", "{}");
            CreateFile(@"Plugins\bin\Debug\Contoso.Plugins.dll", "");
            CreateFile(@"Tooling\bin\Debug\Contoso.Plugins.dll", "");
            CreateFile(@"Webresources\new_\account.js", "");
            CreateFile(@"Webresources\new_\dialog.htm", "");
        }

        [TestCleanup]
        public void DeleteSolutionFolder()
        {
            Directory.Delete(_root, true);
        }

        [TestMethod]
        [TestCategory("Unit Tests")]
        public void FindConfigLoadsOnlyTheGivenFolder()
        {
            var configs = ServiceLocator.ConfigFileFactory.FindConfig(Path.Combine(_root, "Tooling"));

            Assert.AreEqual(1, configs.Count);
            Assert.AreEqual(Path.Combine(_root, "Tooling"), configs[0].filePath);
        }

        [TestMethod]
        [TestCategory("Unit Tests")]
        public void FindConfigDoesNotSearchSubFolders()
        {
            SparkleTaskException.ExceptionTypes? exceptionType = null;

            try
            {
                ServiceLocator.ConfigFileFactory.FindConfig(_root);
            }
            catch (SparkleTaskException ex)
            {
                exceptionType = ex.ExceptionType;
            }

            Assert.AreEqual(SparkleTaskException.ExceptionTypes.CONFIG_NOTFOUND, exceptionType);
        }

        [TestMethod]
        [TestCategory("Unit Tests")]
        public void AssemblyPathIsRelativeToConfigFile()
        {
            var config = ServiceLocator.ConfigFileFactory.FindConfig(Path.Combine(_root, "Tooling")).Single();

            var assemblies = config.GetAssemblies(config.plugins[0]);

            CollectionAssert.AreEqual(
                new List<string> { Path.Combine(_root, @"Plugins\bin\Debug\Contoso.Plugins.dll") },
                assemblies);
        }

        [TestMethod]
        [TestCategory("Unit Tests")]
        public void AssemblyPathWithoutParentIsNotSearchedInSubFolders()
        {
            var config = new ConfigFile { filePath = _root };

            var assemblies = config.GetAssemblies(new PluginDeployConfig { assemblypath = @"bin\Debug\Contoso.Plugins.dll" });

            Assert.AreEqual(0, assemblies.Count);
        }

        [TestMethod]
        [TestCategory("Unit Tests")]
        public void SearchSupportsMultiplePatterns()
        {
            var files = ServiceLocator.DirectoryService.Search(Path.Combine(_root, "Webresources"), "*.js|*.htm");

            Assert.AreEqual(2, files.Count);
        }

        private void CreateFile(string relativePath, string content)
        {
            var path = Path.Combine(_root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content);
        }
    }
}

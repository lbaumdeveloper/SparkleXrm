using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SparkleXrm.Tasks.Tests
{
    [TestClass]
    public class CoreToolsLocatorTests
    {
        private string _root;
        private string _solutionPackages;
        private string _globalPackages;
        private string _spklTools;

        [TestInitialize]
        public void CreateFolders()
        {
            _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _solutionPackages = Path.Combine(_root, "packages");
            _globalPackages = Path.Combine(_root, "nuget");
            _spklTools = Path.Combine(_solutionPackages, "spkl.1.0.0", "tools");
            Directory.CreateDirectory(_spklTools);
            Directory.CreateDirectory(_globalPackages);
        }

        [TestCleanup]
        public void DeleteFolders()
        {
            Directory.Delete(_root, true);
        }

        [TestMethod]
        [TestCategory("Unit Tests")]
        public void FindsHighestVersionInPackagesConfigFolder()
        {
            CreateFile(_solutionPackages, @"Microsoft.CrmSdk.CoreTools.9.1.0.79\content\bin\coretools\SolutionPackager.exe");
            var expected = CreateFile(_solutionPackages, @"Microsoft.CrmSdk.CoreTools.9.1.0.217\content\bin\coretools\SolutionPackager.exe");

            Assert.AreEqual(expected, Locator().Find("SolutionPackager.exe"));
        }

        [TestMethod]
        [TestCategory("Unit Tests")]
        public void FindsToolInGlobalPackagesFolder()
        {
            var expected = CreateFile(_globalPackages, @"microsoft.crmsdk.coretools\9.1.0.79\content\bin\coretools\CrmSvcUtil.exe");

            Assert.AreEqual(expected, Locator().Find("CrmSvcUtil.exe"));
        }

        [TestMethod]
        [TestCategory("Unit Tests")]
        public void PrefersToolNextToSpkl()
        {
            CreateFile(_solutionPackages, @"Microsoft.CrmSdk.CoreTools.9.1.0.79\content\bin\coretools\CrmSvcUtil.exe");
            var expected = CreateFile(_spklTools, "CrmSvcUtil.exe");

            Assert.AreEqual(expected, Locator().Find("CrmSvcUtil.exe"));
        }

        [TestMethod]
        [TestCategory("Unit Tests")]
        public void IgnoresToolsOutsideTheKnownFolders()
        {
            CreateFile(_solutionPackages, @"Other.Package\content\bin\coretools\CrmSvcUtil.exe");
            CreateFile(_globalPackages, @"other.package\1.0.0\CrmSvcUtil.exe");

            var ex = Assert.ThrowsException<SparkleTaskException>(() => Locator().Find("CrmSvcUtil.exe"));
            Assert.AreEqual(SparkleTaskException.ExceptionTypes.UTILSNOTFOUND, ex.ExceptionType);
        }

        private CoreToolsLocator Locator()
        {
            return new CoreToolsLocator(_spklTools, _globalPackages);
        }

        private static string CreateFile(string root, string relativePath)
        {
            var path = Path.Combine(root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "");
            return path;
        }
    }
}

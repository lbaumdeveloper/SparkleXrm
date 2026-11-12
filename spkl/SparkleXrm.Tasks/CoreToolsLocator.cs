using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SparkleXrm.Tasks
{
    /// <summary>
    /// Finds CrmSvcUtil.exe and SolutionPackager.exe in the Microsoft.CrmSdk.CoreTools package
    /// by checking the known package folders only, without scanning the NuGet cache.
    /// </summary>
    public class CoreToolsLocator
    {
        private const string PackageId = "Microsoft.CrmSdk.CoreTools";
        private static readonly string ToolsSubFolder = Path.Combine("content", "bin", "coretools");

        private readonly string _applicationDirectory;
        private readonly string _globalPackagesFolder;

        public CoreToolsLocator(string applicationDirectory, string globalPackagesFolder)
        {
            _applicationDirectory = applicationDirectory;
            _globalPackagesFolder = globalPackagesFolder;
        }

        /// <summary>
        /// Creates a locator for the running spkl.exe and the NuGet global packages folder
        /// (%NUGET_PACKAGES% or %USERPROFILE%\.nuget\packages).
        /// </summary>
        public static CoreToolsLocator CreateDefault()
        {
            var globalPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
            if (string.IsNullOrEmpty(globalPackages))
                globalPackages = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

            return new CoreToolsLocator(ServiceLocator.DirectoryService.GetApplicationDirectory(), globalPackages);
        }

        /// <summary>
        /// Returns the full path of the tool, checked in this order:
        /// next to spkl.exe; packages.config folder (packages\Microsoft.CrmSdk.CoreTools.&lt;version&gt;);
        /// global packages folder (microsoft.crmsdk.coretools\&lt;version&gt;). The highest version wins.
        /// </summary>
        public string Find(string toolFileName)
        {
            var nextToSpkl = Path.Combine(_applicationDirectory, toolFileName);
            if (File.Exists(nextToSpkl))
                return nextToSpkl;

            var candidates = PackagesConfigFolders().Concat(GlobalPackageFolders())
                .Select(c => new { c.Version, Path = Path.Combine(c.Folder, ToolsSubFolder, toolFileName) })
                .Where(c => File.Exists(c.Path))
                .OrderByDescending(c => c.Version)
                .ToList();

            if (candidates.Count == 0)
                throw new SparkleTaskException(SparkleTaskException.ExceptionTypes.UTILSNOTFOUND,
                    $"{toolFileName} not found. Restore the {PackageId} package (a dependency of spkl). Searched: {string.Join("; ", SearchedLocations())}");

            return candidates[0].Path;
        }

        private IEnumerable<string> SearchedLocations()
        {
            yield return _applicationDirectory;
            yield return Path.Combine(PackagesConfigRoot(), PackageId + ".<version>", ToolsSubFolder);
            if (!string.IsNullOrEmpty(_globalPackagesFolder))
                yield return Path.Combine(_globalPackagesFolder, PackageId.ToLowerInvariant(), "<version>", ToolsSubFolder);
        }

        // packages.config: <solution>\packages\spkl.<version>\tools\spkl.exe
        private string PackagesConfigRoot()
        {
            return Path.GetFullPath(Path.Combine(_applicationDirectory, "..", ".."));
        }

        private IEnumerable<(Version Version, string Folder)> PackagesConfigFolders()
        {
            var root = PackagesConfigRoot();
            if (!Directory.Exists(root))
                yield break;

            foreach (var folder in Directory.GetDirectories(root, PackageId + ".*"))
            {
                if (TryParseVersion(Path.GetFileName(folder).Substring(PackageId.Length + 1), out var version))
                    yield return (version, folder);
            }
        }

        private IEnumerable<(Version Version, string Folder)> GlobalPackageFolders()
        {
            if (string.IsNullOrEmpty(_globalPackagesFolder))
                yield break;

            var root = Path.Combine(_globalPackagesFolder, PackageId.ToLowerInvariant());
            if (!Directory.Exists(root))
                yield break;

            foreach (var folder in Directory.GetDirectories(root))
            {
                if (TryParseVersion(Path.GetFileName(folder), out var version))
                    yield return (version, folder);
            }
        }

        private static bool TryParseVersion(string value, out Version version)
        {
            var dash = value.IndexOf('-');
            return Version.TryParse(dash < 0 ? value : value.Substring(0, dash), out version);
        }
    }
}

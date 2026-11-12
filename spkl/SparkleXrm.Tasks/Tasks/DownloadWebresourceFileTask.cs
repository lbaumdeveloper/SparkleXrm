using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using SparkleXrm.Tasks.Config;

namespace SparkleXrm.Tasks
{
    /// <summary>
    /// Downloads the web resources of each <c>webresources</c> entry's <c>solution</c> into its <c>root</c> folder.
    /// The local path is the web resource name below <c>root</c>, e.g. <c>new_/js/form.js</c> is saved as <c>root\new_\js\form.js</c>.
    /// Web resources not yet listed in <c>files</c> are added to spkl.json unless the entry uses <c>autodetect</c>.
    /// </summary>
    public class DownloadWebresourceFileTask : BaseTask
    {
        public bool Overwrite { get; set; }

        public DownloadWebresourceFileTask(IOrganizationService service, ITrace trace) : base(service, trace)
        {
        }

        public DownloadWebresourceFileTask(OrganizationServiceContext context, ITrace trace) : base(context, trace)
        {
        }

        protected override void ExecuteInternal(string filePath, OrganizationServiceContext ctx)
        {
            var config = ServiceLocator.ConfigFileFactory.FindConfig(filePath).First();

            var sections = config.GetWebresourceConfig(Profile);
            if (sections.Length == 0)
                throw new SparkleTaskException(SparkleTaskException.ExceptionTypes.CONFIG_NOTFOUND,
                    $"No 'webresources' entry for profile '{Profile ?? "default"}' in '{Path.Combine(config.filePath, "spkl.json")}'. Add one with 'root' and 'solution'.");

            var knownNames = new HashSet<string>(
                config.webresources.Where(w => w.files != null).SelectMany(w => w.files)
                    .Where(f => f?.uniquename != null).Select(f => f.uniquename),
                StringComparer.OrdinalIgnoreCase);

            var added = 0;
            foreach (var section in sections)
            {
                config.RequireSolution(section.solution, "webresources");
                added += Download(ctx, config, section, knownNames);
            }

            if (added > 0)
            {
                config.Save();
                _trace.WriteLine("Added {0} web resource(s) to spkl.json", added);
            }
        }

        private int Download(OrganizationServiceContext ctx, ConfigFile config, WebresourceDeployConfig section, HashSet<string> knownNames)
        {
            var root = Path.GetFullPath(Path.Combine(config.filePath, section.root ?? ""))
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var addToConfig = !IsAutodetect(section);

            _trace.WriteLine("Downloading web resources of solution '{0}' to '{1}'", section.solution, root);
            var webresources = ServiceLocator.Queries.GetWebresourcesInSolution(ctx, section.solution);
            if (webresources.Count == 0)
            {
                _trace.WriteLine("No web resources found in solution '{0}'", section.solution);
                return 0;
            }

            var added = 0;
            foreach (var webresource in webresources)
            {
                var relativePath = webresource.Name.Replace('/', '\\');
                if (relativePath.IndexOfAny(Path.GetInvalidPathChars()) != -1)
                {
                    _trace.WriteLine("Skipped '{0}': the name is not a valid path", webresource.Name);
                    continue;
                }

                var path = Path.GetFullPath(Path.Combine(root, relativePath));
                if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    _trace.WriteLine("Skipped '{0}': the name points outside of '{1}'", webresource.Name, root);
                    continue;
                }

                ServiceLocator.DirectoryService.SaveFile(path, Convert.FromBase64String(webresource.Content), Overwrite);

                if (!addToConfig || !knownNames.Add(webresource.Name))
                    continue;

                if (section.files == null)
                    section.files = new List<WebResourceFile>();
                section.files.Add(new WebResourceFile
                {
                    uniquename = webresource.Name,
                    file = relativePath,
                    displayname = webresource.DisplayName == webresource.Name ? null : webresource.DisplayName,
                    description = webresource.Description
                });
                added++;
            }
            return added;
        }

        private static bool IsAutodetect(WebresourceDeployConfig section)
        {
            var value = section.autodetect?.ToLowerInvariant();
            return value == "yes" || value == "true";
        }
    }
}
